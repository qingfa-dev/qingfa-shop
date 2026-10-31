# Code Quality Sweep — 2026-10-31

## Scope

- Inline `Error.*` construction outside outcome classes.
- Inline error codes and magic values that belong in constants.
- FluentValidation messages/codes that bypass outcome/constant helpers.
- Options/appsettings binding and startup validation.
- Scrutor registration sites.
- Unit tests for the corrected behaviour.

## Scan summary

| Area | Finding |
|---|---|
| `Error.*` occurrences | 133 across `src` and `app` |
| Outcome-like classes | 74 `*Outcome.cs` files; 33 `*.Result.cs` files also contain nested `Failure` factories |
| Outcome files with `Codes` constants | 16 of 74 |
| Inline errors outside outcome files | 15 source files, mostly Identity/Storage handlers and endpoints |
| FluentValidation literal messages/codes | `MessagingSetting.Validator` is the only validator still using literal codes/messages; the rest already reference outcome/constant members |
| Options registrations | 21 `AddOptions<T>` sites; `Storage.Extensions` lacks `ValidateOnStart`; JWT, refresh-token, blacklist, external-auth, and identity options are bound manually |
| Scrutor scans | 3 sites: persistence query maps, persistence initialization steps, permission metadata registry |

## Scrutor registry

| Site | Scanned types | Lifetime |
|---|---|---|
| `Infrastructure/BuildingBlocks/Adapters/Persistence/Persistence.Composition.cs` | `IQueryMapDefinition` implementations | Singleton |
| `Infrastructure/BuildingBlocks/Adapters/Persistence/PersistenceInitialization.Composition.cs` | `IInitializationStep` implementations | Scoped |
| `Infrastructure/BuildingBlocks/Adapters/Security/Authorization/Authorization.Composition.cs` | `IPermissionMetadataRegistry` implementations | Singleton |

Application MediatR handlers and validators use assembly scanning rather than
Scrutor. Any new registry added by this sweep should follow the same
`FromAssemblyOf` + role-interface + explicit lifetime pattern.

## Violations to fix

1. Move inline `Error.*` construction out of endpoints, guards, mappers,
   services, and result helpers into the owning outcome class.
2. Normalise outcome-like `*.Result.cs` classes to the `*Outcome` pattern with a
   nested `Codes` class, and replace raw code strings with constants.
3. Replace literal value bounds in validators with the owning `*Constant`
   members.
4. Give `MessagingSetting` an outcome and use its codes/messages in the
   validator.
5. Add `ValidateOnStart()` to Storage options and convert JWT, refresh-token,
   blacklist, external-auth, and identity settings to the options pattern with
   explicit validators.
6. Add unit tests for the changed outcomes, validators, and option registration.

## Status

Complete.

- Outcome-like `*.Result.cs` classes were renamed to `*.Outcome.cs`; 553
  `Codes` constants were added across 81 outcome files.
- 20 inline error sites were moved into outcome factories.
- A reflection/source guard (`InlineErrorUsage.Tests`) now rejects `Error.*`
  construction outside outcome files.
- Messaging validation uses `MessagingSettingOutcome` and shared bounds.
- Storage options now validate on start.
- JWT, refresh-token, blacklist, identity, and external-auth options use the
  options pipeline with startup validation, and their dependency singletons now
  resolve from `IOptions<T>` rather than raw configuration reads.
- Validator and startup tests were added.

Verification: `make ci.full` passed, including lint, build, unit coverage,
Api tests, the RabbitMQ broker suite, the Infrastructure integration suite,
Compose smoke, and teardown. `make coverage.infra` (215/215) and `make host.test`
(7/7) also passed.

Remaining documented exceptions:

- `PermissionMetadata.cs` maps dynamic FluentValidation failure codes/messages;
  it is an intentional allowlist entry.
- `Result.Generic.cs` is allowlisted for a non-construction `ToString()`
  reference.
- Runtime-interpolated codes (`Notification.*`, `Provider.*`, `Permission.*`)
  remain interpolated because they contain runtime identifiers.
