using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;

namespace Testing;

/// <summary>
/// Detects the container engine available to the test host and tells the test
/// code whether one exists.
/// </summary>
/// <remarks>
/// Testcontainers and Aspire DCP both need a container endpoint, but which one
/// they find is environment-specific (Docker Desktop, rootless Podman, a socket
/// override). Centralising the probe here lets every container-backed test:
/// <list type="bullet">
///   <item>point the libraries at the same socket this process detected, via
///   <see cref="ConfigureProcessEnvironment"/>, and</item>
///   <item>skip with a precise reason instead of failing when the machine has
///   no engine at all (see <see cref="SkipReason"/>).</item>
/// </list>
/// </remarks>
public static class ContainerRuntime
{
    private const string DockerHostVariable = "DOCKER_HOST";
    private const string AspireRuntimeVariable = "ASPIRE_CONTAINER_RUNTIME";
    private const string RyukDisabledVariable = "TESTCONTAINERS_RYUK_DISABLED";

    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromMilliseconds(500);

    private static readonly Lazy<Result> Detected = new(Probe);

    /// <summary>The engine family the probe resolved to.</summary>
    public enum Kind
    {
        /// <summary>No container engine was found on this machine.</summary>
        None,

        /// <summary>A Docker-compatible engine (Docker Desktop, Colima, podman-docker).</summary>
        Docker,

        /// <summary>Podman, typically in rootless mode.</summary>
        Podman,
    }

    /// <summary>True when a usable container endpoint was detected.</summary>
    public static bool IsAvailable => Detected.Value.Kind != Kind.None;

    /// <summary>The detected engine family.</summary>
    public static Kind Engine => Detected.Value.Kind;

    /// <summary>The socket or host the engine listens on, when one was found.</summary>
    public static string? Endpoint => Detected.Value.Endpoint;

    /// <summary>
    /// Reason handed to <c>Assert.Skip</c> by container tests when
    /// <see cref="IsAvailable"/> is false.
    /// </summary>
    public static string SkipReason =>
        "No container runtime detected (Podman or Docker). Start the engine and, " +
        "for Podman, export DOCKER_HOST=unix://${XDG_RUNTIME_DIR}/podman/podman.sock.";

    /// <summary>
    /// Aligns the current process with the detected engine so Testcontainers and
    /// Aspire DCP target the same socket that <see cref="Probe"/> found.
    /// </summary>
    /// <remarks>
    /// Rootless Podman cannot mount its own socket into the Ryuk helper container,
    /// so Ryuk is disabled for that engine; Testcontainers then leaves cleanup to
    /// container disposal.
    /// </remarks>
    public static void ConfigureProcessEnvironment()
    {
        var result = Detected.Value;
        if (result.Kind == Kind.None)
        {
            return;
        }

        Environment.SetEnvironmentVariable(
            AspireRuntimeVariable,
            result.Kind == Kind.Podman ? "podman" : "docker");
        Environment.SetEnvironmentVariable(DockerHostVariable, ToDockerHost(result.Endpoint!));

        if (result.Kind == Kind.Podman)
        {
            Environment.SetEnvironmentVariable(RyukDisabledVariable, "true");
        }
    }

    // Validate: Testcontainers rejects a bare filesystem path as DOCKER_HOST (a
    // relative URI), so a guessed socket gets its unix:// scheme back here.
    private static string ToDockerHost(string endpoint) =>
        endpoint.Contains("://", StringComparison.Ordinal) ? endpoint : $"unix://{endpoint}";

    private static Result Probe()
    {
        var hint = Environment.GetEnvironmentVariable(AspireRuntimeVariable);
        var dockerHost = Environment.GetEnvironmentVariable(DockerHostVariable);

        // Assign: an explicit DOCKER_HOST is authoritative; trial sockets are only a fallback.
        if (!string.IsNullOrWhiteSpace(dockerHost))
        {
            if (TryGetUnixSocketPath(dockerHost, out var configured) && CanConnect(configured))
            {
                return Classify(hint, configured);
            }

            // Skip: a tcp:// or npipe:// endpoint cannot be probed from this process,
            // so it is trusted as configured.
            if (!dockerHost.StartsWith("unix:", StringComparison.OrdinalIgnoreCase))
            {
                return new Result(HintKind(hint), dockerHost);
            }
        }

        // Validate: a socket file may exist while nothing listens on it (a stale
        // rootless-podman leftover at /var/run/docker.sock, for example), so
        // availability is proven by connecting, not by File.Exists.
        foreach (var candidate in KnownSocketPaths())
        {
            if (CanConnect(candidate))
            {
                return Classify(hint, candidate);
            }
        }

        return new Result(Kind.None, null);
    }

    private static bool CanConnect(string unixSocketPath)
    {
        try
        {
            using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            using var timeout = new CancellationTokenSource(ConnectTimeout);
            socket.ConnectAsync(new UnixDomainSocketEndPoint(unixSocketPath), timeout.Token)
                .AsTask()
                .GetAwaiter()
                .GetResult();
            return socket.Connected;
        }
        catch (SocketException)
        {
            return false;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            // Catch: a malformed socket path means "no engine", not a detection crash.
            return false;
        }
    }

    private static Result Classify(string? hint, string socketPath)
    {
        var hinted = HintKind(hint);
        if (hinted != Kind.None)
        {
            return new Result(hinted, socketPath);
        }

        // Fallback: with no runtime hint, the socket path is the only signal for the engine family.
        var looksLikePodman = socketPath.Contains("podman", StringComparison.OrdinalIgnoreCase);
        return new Result(looksLikePodman ? Kind.Podman : Kind.Docker, socketPath);
    }

    private static Kind HintKind(string? hint) => hint?.ToLowerInvariant() switch
    {
        "podman" => Kind.Podman,
        "docker" => Kind.Docker,
        _ => Kind.None,
    };

    private static bool TryGetUnixSocketPath(string dockerHost, [NotNullWhen(true)] out string? path)
    {
        const string prefix = "unix://";
        if (dockerHost.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            path = dockerHost[prefix.Length..];
            return true;
        }

        path = null;
        return false;
    }

    private static IEnumerable<string> KnownSocketPaths()
    {
        var xdgRuntime = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR");
        if (!string.IsNullOrWhiteSpace(xdgRuntime))
        {
            yield return Path.Combine(xdgRuntime, "podman", "podman.sock");
        }

        yield return "/run/podman/podman.sock";
        yield return "/var/run/docker.sock";
        yield return "/run/docker.sock";

        var home = Environment.GetEnvironmentVariable("HOME");
        if (!string.IsNullOrWhiteSpace(home))
        {
            yield return Path.Combine(home, ".docker", "run", "docker.sock");
        }
    }

    private readonly record struct Result(Kind Kind, string? Endpoint);
}