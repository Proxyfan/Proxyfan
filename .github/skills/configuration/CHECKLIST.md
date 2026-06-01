# Configuration checklist

Detailed reference for the `configuration` skill.

## Surfaces

- `Domain.Configuration/ConfigurationSnapshot.cs` — the immutable
  in-memory view of the active configuration.
- `Domain.Configuration/UserPreferences.cs`,
  `UserPreferencesDefaults.cs`, `IUserPreferencesStore.cs` — user-scoped
  settings (theme, locale, recent files, font size, …).
- `Domain.Configuration/FileConfigurationLoader.cs`,
  `IMigratingConfigurationLoader.cs`,
  `MigratingConfigurationLoadResult.cs`,
  `KeyValueConfigurationParser.cs`,
  `KeyValueConfigurationWriter.cs`,
  `ConfigurationMerger.cs` — load / parse / merge / write pipeline.
- `Domain.Configuration/Migration/` and
  `StartupConfigurationMigration.cs` — version-to-version migrations.
- `Framework.Serialization` (YamlDotNet adapter) — wire format.
- `src/Clients/Client/AppStartupConfigurationMigrationRunner.cs` — invokes
  migrations during host startup.

## Precedence

The active value of any setting is resolved in this order (highest first):

1. CLI arguments (when running `Cli`).
2. Environment variables (`PROXYFAN_*`).
3. `%LOCALAPPDATA%\Proxyfan\config.yaml`.
4. `defaults.yaml` shipped in the install.

Confirm each loader honours its position in the chain — a regression that
silently moves a setting "up" the precedence list is a P1 finding.

## Analysis

1. **Schema validation.** Every setting has a known type, range, and
   default. Validation runs at load time (`IValidateOptions<T>` patterns
   in `Domain.Proxy/ProxyOptionsValidator.cs` are the reference). Flag a
   load path that accepts invalid values silently.

2. **Hot-reload boundaries.** The following are hot-reloadable:
   - Mutable rules (`MutableAllowListRule`, `MutableBlockListRule`,
     `MutableMapLocalRule`, `MutableMapRemoteRule`, `MutableNoCachingRule`,
     `MutableBreakpointConfiguration`).
   - Mutable scripting (`MutableScriptingConfiguration`).
   - Mutable certificate authority (`MutableCertificateAuthorityProvider`).
   - Mutable throttle profile (`MutableThrottleProfile`).
   - SNI proxying list (`ServerNameIndicationProxyingList`).
   - UI theme, font size, locale.

   The following require a restart:
   - `proxy.port`.
   - `proxy.upstream.*`.
   - Reverse-proxy route bindings.
   - Listener TLS settings.

   Flag any path that hot-reloads a restart-only setting.

3. **Migration.** `StartupConfigurationMigration` runs through
   `AppStartupConfigurationMigrationRunner` at host startup. Each
   migration:
   - Detects the on-disk version.
   - Produces a new in-memory representation.
   - Writes the migrated form back with a backup of the previous file.
   - Bumps the stored version.

   Flag a migration that mutates the file in place without a backup, or
   that runs lazily (on first read of an affected setting) rather than at
   startup.

4. **Defaults.** `UserPreferencesDefaults` carries the canonical
   defaults. Changes to a default value should be reflected in
   `docs/ARCHITECTURE.md` (the "Key Configuration Settings" section).

5. **Atomic write.** `KeyValueConfigurationWriter` writes to a temp file
   and renames. A crash mid-write must not corrupt the existing config.

6. **File-watcher hot reload.** The configuration loader subscribes to a
   file watcher. Validate:
   - Editor write patterns (write → rename, atomic save) are handled
     without double-loads.
   - A malformed save does not replace the in-memory snapshot — it logs
     a warning and keeps the previous good snapshot.
   - The watcher unsubscribes cleanly on host shutdown.

7. **Locale resolution.** See `localization.instructions.md`. Resolution
   order is user preference → Windows system locale → `en-US`.

8. **Environment variables.** The convention is `PROXYFAN_<dotted_key>`
   with `_` replacing `.` (`PROXYFAN_PROXY_PORT` overrides
   `proxy.port`). Validate the binding maps every documented setting and
   rejects unknown variables with a startup warning, not a failure.

9. **CLI overrides.** `Cli` handlers consume the same configuration
   chain, but their command-line flags take precedence. Validate flag
   names and stored key names map consistently.

10. **Privacy on persistence.** The config file is per-user. It must not
    contain secrets in plain text — the upstream proxy credential is
    held via DPAPI, not in `config.yaml`. Flag any new setting that
    would land a secret in the YAML file.
