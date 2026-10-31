# Validation and Options Pass — 2026-10-31

## Status

Complete. No commit was made. `src` and `tests` are staged.

## Scope

- Messaging validation moved behind `MessagingSettingOutcome` codes/messages and shared
  min/max constants.
- Storage option chains now validate on host start.
- JWT, refresh-token, blacklist, identity, and external-auth options now use the
  options pipeline with startup validation while keeping their singleton registrations
  for dependent services.
- Added validators, outcomes, and unit tests for identity and external-auth options.

## Changes

### Messaging

- Added `MessagingSetting.Outcome.cs` with `Failure` factories and `Codes` for
  `TransportUnknown`, `ExchangeNameRequired`, `QueuePrefixRequired`,
  `PrefetchCountOutOfRange`, `MaxConcurrencyOutOfRange`, `MaxAttemptsOutOfRange`,
  and `RetryDelaySecondsOutOfRange`.
- Added `PrefetchCountMin`, `MaxConcurrencyMin`, `MaxAttemptsMin`, and
  `RetryDelaySecondsMin` to `MessagingDefaults.Constraints`.
- Rewrote `MessagingSetting.Validator` to use outcome codes/messages and the
  constraints, removing literal codes, messages, and `1` bounds.
- Extended `MessagingSetting.Validator.Test` to assert each code and message plus
  inclusive min/max boundaries.

### Storage

- Added `.ValidateOnStart()` to every options chain in `Storage.Extensions`:
  `StorageSetting`, `StorageSecuritySetting`, local/Azure/S3 provider settings,
  `AntiForgeryOptions`, and `StorageMalwareScannerOptions`.
- Added `Storage.Options.Startup.Test`, which uses a `WebApplicationBuilder` with an
  invalid storage or local-provider value and proves `IStartupValidator` reports the
  matching `OptionsValidationException`.

### Token options

- Registered `JwtTokenOption`, `RefreshTokenOption`, and `TokenBlacklistOption` through
  `AddOptions<T>().Bind(...).Validate(TokenOptionValidator...).ValidateOnStart()`.
- Kept the existing singleton registrations and JWT bearer scheme configuration intact.

### Identity and external authentication

- Added `Identity.Option.Validator` and `Identity.Option.Outcome`:
  default role required, public base URL must be an absolute HTTP(S) URL, and
  confirmation/reset paths required.
- Added `External.Option.Validator`, `External.Option.Outcome`, and
  `External.Option.Constant`:
  success redirect required, code lifetime bounded to 1–3600 seconds, enabled
  providers require `ClientId`/`ClientSecret`, and OIDC requires an absolute
  HTTP(S) authority, at least one scope, and a callback path.
- Registered both through `AddOptions<T>().Bind(...).ValidateFluentValidation().ValidateOnStart()`
  and kept the existing singleton registrations.
- Added validators tests covering the valid defaults and each invalid branch.

## Verification

| Command | Result |
|---|---|
| `make lib.build` | Passed (0 warnings, 0 errors) |
| `dotnet test --project tests/Unit/Application.Unit.Tests/Application.Unit.Tests.csproj -c Release --no-build` | Passed: 2579 / 2579 |
| `dotnet test --project tests/Unit/Infrastructure.Unit.Tests/Infrastructure.Unit.Tests.csproj -c Release --no-build` | Passed: 2261 / 2261 |
| `dotnet test --project tests/Architecture.Tests/Architecture.Tests.csproj -c Release --no-build` | Passed: 32 / 32 |
| `make structure.check` | Passed |
| `make lib.format.check` | Passed |

## Concerns

- `IStartupValidator.Validate()` can throw either an `OptionsValidationException` or an
  `AggregateException` containing several of them, depending on how many option types
  fail. The startup test flattens both shapes so it remains stable.
- Token and identity/external option validation runs at host start but the existing
  manually-bound singletons are still what dependent services receive. Both paths bind
  the same configuration sections, so they stay aligned.
- External OIDC authority and identity public base URL intentionally require HTTP(S);
  `Uri.TryCreate(..., UriKind.Absolute)` alone treats `/relative` as a file URI on Linux.
- Pre-existing staged test renames elsewhere in the worktree were left untouched.
