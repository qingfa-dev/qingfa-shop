using Microsoft.Extensions.Options;

using Application.BuildingBlocks.Ports.Storage.Models;
using Application.BuildingBlocks.Ports.Storage.Security.Options;

namespace Infrastructure.BuildingBlocks.Adapters.Storage.Security;

/// <summary>Validates uploads against extension allowlist/blocklist, file size cap, and optional magic-byte signatures — mitigates TMT-FILE-001.</summary>
// Invariant: Extension check passes only if extension is in AllowedExtensions AND not in BlockedExtensions; magic-byte validation is optional.
// Context: File upload validation prevents malicious file execution (Threat TMT-FILE-001). Extension-only checks are insufficient — magic bytes provide content-type verification.
public sealed partial class StorageSecurityEnforcer(
    IOptions<StorageSecuritySetting> options,
    ILogger<StorageSecurityEnforcer> logger)
    : IStorageSecurityEnforcer
{
    /// <summary>Validates a file upload against extension allowlist/blocklist, size cap, and magic-byte signatures.</summary>
    // Contract: pre=request!=null, post=return.IsSuccess implies all security checks passed, throws=never
    public async Task<Result> EnforceAsync(UploadRequest request, CancellationToken ct = default)
    {
        string extension = Path.GetExtension(request.Key);

        // Validate: reject blocked extensions before checking allowlist — blocklist takes priority
        Result blockedResult = CheckBlockedExtension(extension);
        if (!blockedResult.IsSuccess)
        {
            Loggers.LogBlockedExtension(logger, request.Key, extension);
            return blockedResult;
        }

        // Validate: reject extensions not in the configured allowlist
        Result allowedResult = CheckAllowedExtension(extension);
        if (!allowedResult.IsSuccess)
        {
            Loggers.LogForbiddenExtension(logger, request.Key, extension);
            return allowedResult;
        }

        // Validate: reject files exceeding configured maximum size
        Result sizeResult = CheckFileSize(request.Content);
        if (!sizeResult.IsSuccess)
        {
            Loggers.LogFileSizeExceeded(logger, request.Key, options.Value.MaxFileSizeBytes);
            return sizeResult;
        }

        // Validate: optionally verify file header bytes match known signatures for the extension
        if (options.Value.ValidateMagicBytes)
        {
            Result magicResult = await CheckMagicBytesAsync(request.Content, extension, ct);
            if (!magicResult.IsSuccess)
            {
                Loggers.LogMagicBytesMismatch(logger, request.Key, extension);
                return magicResult;
            }
        }

        Loggers.LogSecurityCheckPassed(logger, request.Key);
        return Result.Ok();
    }

    // Validate: check extension against configured blocklist — rejects dangerous extensions like .exe, .scr
    private Result CheckBlockedExtension(string extension)
    {
        if (string.IsNullOrEmpty(extension))
            return StorageSecurityEnforcerOutcome.Failure.BlockedExtension("(none)");

        if (options.Value.BlockedExtensions.Contains(extension))
            return StorageSecurityEnforcerOutcome.Failure.BlockedExtension(extension);

        return Result.Ok();
    }

    // Validate: check extension against configured allowlist — only known-safe extensions permitted
    private Result CheckAllowedExtension(string extension)
    {
        if (string.IsNullOrEmpty(extension))
            return StorageOutcome.Failure.ForbiddenExtension("(none)");

        if (options.Value.AllowedExtensions.Count == 0)
            return StorageOutcome.Failure.ForbiddenExtension(extension);

        if (!options.Value.AllowedExtensions.Contains(extension))
            return StorageOutcome.Failure.ForbiddenExtension(extension);

        return Result.Ok();
    }

    // Validate: check file size against configured maximum to prevent resource exhaustion
    private Result CheckFileSize(Stream content)
    {
        long length;
        try
        {
            length = content.Length;
        }
        catch (NotSupportedException)
        {
            return StorageSecurityEnforcerOutcome.Failure.FileSizeUnknown();
        }

        if (length > options.Value.MaxFileSizeBytes)
            return StorageOutcome.Failure.FileTooLarge(options.Value.MaxFileSizeBytes);

        return Result.Ok();
    }

    // Validate: read file header bytes and compare against known magic signatures for the extension
    private static async Task<Result> CheckMagicBytesAsync(Stream content, string extension, CancellationToken ct)
    {
        if (!KnownMagicSignatures.TryGetValue(extension, out byte[][]? signatures))
            return Result.Ok();

        int maxSignatureLength = signatures.Max(s => s.Length);
        byte[] header = await ReadExactAsync(content, maxSignatureLength, ct);

        bool matched = false;
        foreach (byte[] signature in signatures)
        {
            if (header.AsSpan(0, signature.Length).SequenceEqual(signature))
            {
                matched = true;
                break;
            }
        }

        content.Seek(0, SeekOrigin.Begin);

        if (!matched)
            return StorageSecurityEnforcerOutcome.Failure.MagicBytesMismatch(extension);

        return Result.Ok();
    }

    // Compute: read exact byte count from stream — handles partial reads from network streams
    private static async Task<byte[]> ReadExactAsync(Stream stream, int count, CancellationToken ct)
    {
        byte[] buffer = new byte[count];
        int offset = 0;
        while (offset < count)
        {
            int read = await stream.ReadAsync(buffer.AsMemory(offset, count - offset), ct);
            if (read == 0)
                break;
            offset += read;
        }
        return buffer;
    }
}
