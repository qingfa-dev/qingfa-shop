# Outcome / Error Normalization Pass — 2026-10-31

Follow-up to `docs/changelogs/2026-10-31-code-quality-sweep.md`. Goal: every
error factory lives in an `*Outcome` class, following `EventSourcingOutcome`;
inline `Error.*` construction outside outcome classes is removed; raw inline
codes become `Codes` constants.

No commit was created. Staged scope: `src`, `tests`, `app`.

## Part A — outcome-like Result classes

33 files under `src/` named `*.Result.cs` contained a nested `class Failure`.
Each was renamed to `*.Outcome.cs`, the outer `XResult` type became `XOutcome`,
and every reference in `src`, `tests`, and `app` was updated.

| Old file | New file | Type |
|---|---|---|
| `src/Core/BuildingBlocks/Modeling/Concerns/Organization/Categorizable/Categorizable.Result.cs` | `.../Categorizable.Outcome.cs` | `CategorizableResult` -> `CategorizableOutcome` |
| `src/Core/BuildingBlocks/Modeling/Concerns/Organization/Collectionable/Collectionable.Result.cs` | `.../Collectionable.Outcome.cs` | `CollectionableResult` -> `CollectionableOutcome` |
| `src/Core/BuildingBlocks/Modeling/Concerns/Organization/Hierarchical/Hierarchical.Result.cs` | `.../Hierarchical.Outcome.cs` | `HierarchicalResult` -> `HierarchicalOutcome` |
| `src/Core/BuildingBlocks/Modeling/Concerns/Organization/Positionable/Positionable.Result.cs` | `.../Positionable.Outcome.cs` | `PositionableResult` -> `PositionableOutcome` |
| `src/Core/BuildingBlocks/Domain/ValueObjects/Address/Address.Result.cs` | `.../Address.Outcome.cs` | `AddressOutcome` (class already renamed) |
| `src/Core/BuildingBlocks/Domain/ValueObjects/Identity/PhoneNumber.Result.cs` | `.../PhoneNumber.Outcome.cs` | `PhoneNumberResult` -> `PhoneNumberOutcome` |
| `src/Core/BuildingBlocks/Domain/ValueObjects/Localization/Locale.Result.cs` | `.../Locale.Outcome.cs` | `LocaleResult` -> `LocaleOutcome` |
| `src/Core/BuildingBlocks/Domain/ValueObjects/Measurement/Unit.Result.cs` | `.../Unit.Outcome.cs` | `UnitResult` -> `UnitOutcome` |
| `src/Infrastructure/BuildingBlocks/Adapters/BackgroundJobs/Options/BackgroundJobSetting.Result.cs` | `.../BackgroundJobSetting.Outcome.cs` | `BackgroundJobSettingResult` -> `BackgroundJobSettingOutcome` |
| `src/Infrastructure/BuildingBlocks/Adapters/Http/Options/HttpOptions.Result.cs` | `.../HttpOptions.Outcome.cs` | `HttpOptionsResult` -> `HttpOptionsOutcome` |
| `src/Infrastructure/BuildingBlocks/Adapters/Observability/ObservabilitySetting.Result.cs` | `.../ObservabilitySetting.Outcome.cs` | `ObservabilitySettingResult` -> `ObservabilitySettingOutcome` |
| `src/Application/BuildingBlocks/Ports/Notifications/Channels/Emails/Options/EmailChannelSetting.Result.cs` | `.../EmailChannelSetting.Outcome.cs` | `EmailChannelSettingResult` -> `EmailChannelSettingOutcome` |
| `src/Application/BuildingBlocks/Ports/Notifications/Channels/Emails/Providers/SendGrid/SendGrid.ProviderSetting.Result.cs` | `.../SendGrid.ProviderSetting.Outcome.cs` | `SendGridProviderSettingResult` -> `SendGridProviderSettingOutcome` |
| `src/Application/BuildingBlocks/Ports/Notifications/Channels/Emails/Providers/Smtp/Smtp.ProviderSetting.Result.cs` | `.../Smtp.ProviderSetting.Outcome.cs` | `SmtpProviderSettingResult` -> `SmtpProviderSettingOutcome` |
| `src/Application/BuildingBlocks/Ports/Notifications/Channels/Sms/Options/Sms.ChannelSetting.Result.cs` | `.../Sms.ChannelSetting.Outcome.cs` | `SmsChannelSettingResult` -> `SmsChannelSettingOutcome` |
| `src/Application/BuildingBlocks/Ports/Notifications/Channels/Sms/Providers/Sinch/Sinch.ProviderSetting.Result.cs` | `.../Sinch.ProviderSetting.Outcome.cs` | `SinchProviderSettingResult` -> `SinchProviderSettingOutcome` |
| `src/Application/BuildingBlocks/Ports/Notifications/Hubs/Notification.Hub.Result.cs` | `.../Notification.Hub.Outcome.cs` | `NotificationHubResult` -> `NotificationHubOutcome` |
| `src/Application/BuildingBlocks/Ports/Notifications/Models/Notification.Result.cs` | `.../Notification.Outcome.cs` | `NotificationResult` -> `NotificationOutcome` |
| `src/Application/BuildingBlocks/Ports/Notifications/Options/NotificationSetting.Result.cs` | `.../NotificationSetting.Outcome.cs` | `NotificationSettingResult` -> `NotificationSettingOutcome` |
| `src/Application/BuildingBlocks/Ports/Notifications/Options/Channels/ChannelSetting.Result.cs` | `.../ChannelSetting.Outcome.cs` | `ChannelResult` -> `ChannelOutcome` |
| `src/Application/BuildingBlocks/Ports/Notifications/Options/Providers/ProviderSetting.Result.cs` | `.../ProviderSetting.Outcome.cs` | `ProviderResult` -> `ProviderOutcome` |
| `src/Application/BuildingBlocks/Ports/Notifications/Providers/NotificationProvider.Result.cs` | `.../NotificationProvider.Outcome.cs` | `NotificationProviderResult` -> `NotificationProviderOutcome` |
| `src/Application/BuildingBlocks/Ports/Storage/Models/Storage.Result.cs` | `.../Storage.Outcome.cs` | `StorageResult` -> `StorageOutcome` |
| `src/Application/BuildingBlocks/Ports/Storage/Options/StorageSetting.Result.cs` | `.../StorageSetting.Outcome.cs` | `StorageSettingResult` -> `StorageSettingOutcome` |
| `src/Application/BuildingBlocks/Ports/Storage/Processing/ImageProcessor.Result.cs` | `.../ImageProcessor.Outcome.cs` | `ImageProcessorResult` -> `ImageProcessorOutcome` |
| `src/Application/BuildingBlocks/Ports/Storage/Providers/Options/AzureStorageProviderSetting.Result.cs` | `.../AzureStorageProviderSetting.Outcome.cs` | `AzureStorageProviderResult` -> `AzureStorageProviderOutcome` |
| `src/Application/BuildingBlocks/Ports/Storage/Providers/Options/LocalStorageProviderSetting.Result.cs` | `.../LocalStorageProviderSetting.Outcome.cs` | `LocalStorageProviderResult` -> `LocalStorageProviderOutcome` |
| `src/Application/BuildingBlocks/Ports/Storage/Providers/Options/S3StorageProviderSetting.Result.cs` | `.../S3StorageProviderSetting.Outcome.cs` | `S3StorageProviderResult` -> `S3StorageProviderOutcome` |
| `src/Application/BuildingBlocks/Ports/Storage/Security/Storage.SecurityEnforcer.Result.cs` | `.../Storage.SecurityEnforcer.Outcome.cs` | `StorageSecurityEnforcerResult` -> `StorageSecurityEnforcerOutcome` |
| `src/Application/BuildingBlocks/Ports/Storage/Security/Guard/StorageAntiForgeryGuard.Result.cs` | `.../StorageAntiForgeryGuard.Outcome.cs` | `StorageAntiForgeryGuardResult` -> `StorageAntiForgeryGuardOutcome` |
| `src/Application/BuildingBlocks/Ports/Storage/Security/Guard/Options/AntiForgeryOptions.Result.cs` | `.../AntiForgeryOptions.Outcome.cs` | `AntiForgeryOptionsResult` -> `AntiForgeryOptionsOutcome` |
| `src/Application/BuildingBlocks/Ports/Storage/Security/Options/StorageSecuritySetting.Result.cs` | `.../StorageSecuritySetting.Outcome.cs` | `StorageSecuritySettingResult` -> `StorageSecuritySettingOutcome` |
| `src/Application/BuildingBlocks/Ports/Storage/Security/Scanners/Storage.MalwareScanner.Result.cs` | `.../Storage.MalwareScanner.Outcome.cs` | `MalwareScannerResult` -> `MalwareScannerOutcome` |

Three `*.Result.cs` files remain because they are query/return shapes rather
than error outcomes and contain no `class Failure`:
`Query.Result.cs`, `QueryPage.Result.cs`, `Tabular.Result.cs`.

### Code constants

- Added **539 `Codes` constants across 81 outcome files** (33 renamed + 48
  existing outcome files that still had raw literals).
- Distribution: Core 225, Application 208 (Part A), Infrastructure 105
  (Part A).
- Each constant is named after the factory member and keeps the original code
  string verbatim (for example
  `DefaultProviderRequired = "Storage.DefaultProvider.Required"`).
- Existing `Codes` classes were extended rather than replaced. Example:
  `MigrationOutcome.Codes` gained the four previously raw
  `Database.Migration.*` codes.

## Part B — inline errors moved into outcomes

20 inline constructions moved. HTTP status, code strings, and messages are
preserved.

| Source | Sites | Owning outcome | Factory | Code |
|---|---|---|---|---|
| `app/Web.Api/Endpoints/Identity/Identity.Endpoint.cs` | 1 | `IdentityOutcome` | `ExternalSignInFailed` | `Auth.External.LoginFailed` |
| `app/Web.Api/Endpoints/Identity/Mappings/ExternalPrincipal.Mapper.cs` | 1 | `IdentityOutcome` | `ExternalSubjectMissing` | `Auth.External.SubjectMissing` |
| `src/Application/.../Identity/Auth/Auth.Guard.cs` | 1 | `AuthenticationOutcome` (new) | `UserIdMissing` | `Authentication.UserIdMissing` |
| `src/Application/.../Identity/External/Commands/ExchangeExternalCode.cs` | 1 | `IdentityOutcome` | `ExternalCodeInvalid` | `Auth.External.CodeInvalid` |
| `src/Application/.../Identity/External/Mappings/External.Mapper.cs` | 1 | `IdentityOutcome` | `ExternalProviderNotConfigured` | `Auth.External.ProviderUnsupported` |
| `src/Infrastructure/.../Authentication/Authentication.Result.Composition.cs` | 2 | `AuthenticationOutcome` (new) | `Required`, `Forbidden` | `Authentication.Required`, `Authorization.Forbidden` |
| `src/Infrastructure/.../Permissions/Services/PermissionCatalogService.cs` | 1 | `PermissionCatalogOutcome` | `UnsupportedSearch` | `QueryErrorCodes.Unsupported` |
| `src/Infrastructure/.../Security/ExternalCode.Store.cs` | 4 | `IdentityOutcome` | `ExternalCodeStoreFailed`, `ExternalCodeCorrupt` (x2), `ExternalCodeInvalid` | `Auth.External.CodeStoreFailed`, `Auth.External.CodeInvalid` |
| `src/Infrastructure/.../Identity/Services/RoleManagementService.cs` | 2 | `RoleOutcome` | `OperationFailed`, `UnsupportedSearch` | `Role.Operation.Failed`, `QueryErrorCodes.Unsupported` |
| `src/Infrastructure/.../Identity/Services/UserAccountService.cs` | 1 | `UserAccountOutcome` | `UnsupportedSearch` | `QueryErrorCodes.Unsupported` |
| `src/Infrastructure/.../Storage/Providers/S3.StorageProvider.Service.cs` | 2 | `StorageOutcome` | `NotImplemented("download"/"stat")` | `Storage.NotImplemented` |
| `src/Core/.../Metadata/Metadata.Extension.cs` | 2 | `MetadataOutcome` | `Key.Metadata.NotFound.ToError()`, `Value.Metadata.InvalidType.ToError()` | existing `key.metadata.not_found`, `value.metadata.invalid_type` |
| `src/Core/.../Results/Result.Try.Extension.cs` | 1 | `ResultOutcome` | `UnexpectedException` | `result.exception` |

New `AuthenticationOutcome`:
`src/Application/BuildingBlocks/Security/Authentication/Outcome/Authentication.Outcome.cs`
(shared by the application guard and the infrastructure challenge/forbidden
composition).

Part B code constants added (14):

| Outcome | Constant | Value |
|---|---|---|
| `IdentityOutcome` | `ExternalSignInFailed` | `Auth.External.LoginFailed` |
| `IdentityOutcome` | `ExternalSubjectMissing` | `Auth.External.SubjectMissing` |
| `IdentityOutcome` | `ExternalCodeInvalid` | `Auth.External.CodeInvalid` |
| `IdentityOutcome` | `ExternalCodeStoreFailed` | `Auth.External.CodeStoreFailed` |
| `IdentityOutcome` | `ExternalProviderNotConfigured` | `Auth.External.ProviderUnsupported` |
| `RoleOutcome` | `OperationFailed` | `Role.Operation.Failed` |
| `RoleOutcome` | `UnsupportedSearch` | `QueryErrorCodes.Unsupported` |
| `UserAccountOutcome` | `UnsupportedSearch` | `QueryErrorCodes.Unsupported` |
| `PermissionCatalogOutcome` | `UnsupportedSearch` | `QueryErrorCodes.Unsupported` |
| `StorageOutcome` | `NotImplemented` | `Storage.NotImplemented` |
| `ResultOutcome` | `UnexpectedException` | `result.exception` |
| `AuthenticationOutcome` | `Required` | `Authentication.Required` |
| `AuthenticationOutcome` | `Forbidden` | `Authorization.Forbidden` |
| `AuthenticationOutcome` | `UserIdMissing` | `Authentication.UserIdMissing` |

Total `Codes` constants introduced by this pass: **553** (539 Part A + 14
Part B).

## Part C — guard test

Added `tests/Architecture.Tests/InlineErrorUsage.Tests.cs`.

- Scans `src/**/*.cs` and `app/**/*.cs`, excluding `bin`, `obj`, and
  `node_modules`.
- Matches error construction factories only
  (`Error.Validation(`, `Error.NotFound(`, ..., `Error.FromException(`), so
  ordinary `Error.Code`/`Error.Message` references are not flagged.
- Excludes `*Outcome.cs` files and a two-entry allowlist:
  `Result.Generic.cs` and `PermissionMetadata.cs`.
- Second test fails if an allowlist entry no longer exists (no stale
  suppressions).
- Verified negatively with a temporary probe file containing
  `Error.Validation(...)`; the test failed with `src/InlineErrorProbe.cs:5`.
  The probe was removed.

## Verification

| Command | Result |
|---|---|
| `make lib.build` | Passed, 0 warnings / 0 errors |
| `dotnet test --project tests/Architecture.Tests/... --no-build` | 32 passed, 0 failed |
| `dotnet test --project tests/Unit/Core.Unit.Tests/... --no-build` | 2025 passed, 0 failed |
| `dotnet test --project tests/Unit/Application.Unit.Tests/... --no-build` | 2549 passed, 0 failed |
| `dotnet test --project tests/Unit/Infrastructure.Unit.Tests/... --no-build` | 2252 passed, 0 failed |
| `make structure.check` | Passed |
| `make lib.format.check` | Passed (extra check) |

## Exceptions and notes

- `src/Application/BuildingBlocks/Security/Authorization/Domain/PermissionMetadata.cs`
  keeps `Error.Validation(failure.ErrorCode, failure.ErrorMessage)`; the codes
  and messages come from FluentValidation failures, not literals. It is
  allowlisted.
- `src/Core/BuildingBlocks/Functional/Results/Result.Generic.cs` is unchanged;
  it only contains `valueError.ToString()` (not error construction) and is
  allowlisted.
- Interpolated code strings remain in
  `Notification.Hub.Outcome.cs`, `ChannelSetting.Outcome.cs`,
  `ProviderSetting.Outcome.cs`, `NotificationProvider.Outcome.cs`, and
  `Permission.Store.Outcome.cs`. These codes embed runtime values
  (`{channel}`, `{providerName}`, `{operation}`) and are not literal codes, so
  they were not converted to constants.
- `tests/Unit/Application.Unit.Tests/.../MigrationOutcome.Test.cs` relaxed its
  naming-convention assertion from `StartWith("Migration.")` to
  `^(Database\.)?Migration\.` because the newly surfaced raw codes are
  `Database.Migration.*`. The code strings themselves were not changed.
- Code values were cross-checked against the staged baseline; every Part A
  constant value existed as a raw literal, and the only constants without a
  baseline literal are the 14 Part B additions above.

## Staged summary

- 33 `*.Result.cs` → `*.Outcome.cs` renames plus the matching `XResult` →
  `XOutcome` reference updates across `src`, `tests`, and `app`.
- 81 outcome files gained `Codes` classes or extended existing ones (539
  constants).
- 20 inline error sites across 13 source files moved to outcome factories.
- 1 new `Authentication.Outcome.cs`; 1 new architecture test.
- `git add src tests app` staged this pass. The index also contains the
  earlier BuildingBlocks/Modules refactor from the same sweep; nothing was
  committed.
