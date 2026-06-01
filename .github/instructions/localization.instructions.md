---
applyTo: "**/*.resx,src/**/Resources/**/*.cs,src/Clients/**/Resources/**/*"
---

# Localisation rules

Every user-visible string lives in a `.resx` resource. Hard-coded English in
ViewModels, AXAML, or dialog text fails review.

## Key naming

Keys follow `{Feature}_{Context}_{Element}`, PascalCase parts separated by
underscores. Examples:

- `TrafficList_Header_StatusCode`
- `Tools_MapLocal_Add_DialogTitle`
- `Shell_StatusBar_FlowsCaptured`
- `Scripting_Diagnostic_Severity_Error`

The first part names the feature/slice. The second names the context (panel,
dialog, message kind). The third names the element (label, button, tooltip,
header, body). For format templates, suffix the key with the placeholder
shape, e.g. `Shell_StatusBar_CountFormat` for `"{0} flows captured"`.

## File layout

- One `.resx` file per locale per feature surface (`Strings.resx`,
  `Strings.fr.resx`, `Strings.de.resx`, …).
- Generated `Strings.Designer.cs` is excluded from cleanup; do not hand-edit.
- New keys are added to **all** locale files in the same commit. A key
  present in `Strings.resx` but missing from a sibling locale file fails
  `.tools/Test-ResourceKeys.ps1` and breaks the build.
- The neutral `Strings.resx` always contains the canonical English string.

## What is and is not localised

Localised:
- Window titles, dialog text, button labels, tooltips, status messages.
- Validation error messages shown to the user.
- Toast / notification text.

Not localised:
- Log messages (`%LOCALAPPDATA%\Proxyfan\logs\`) — logs are an
  engineering artefact, not a user surface.
- CLI output from `Cli` — automation pipes consume it.
- `DomainError.Code` values — they are machine-readable identifiers.
- Configuration key names in `config.yaml`.
- File extensions, MIME types, header names.

## Format placeholders

- Composite strings use `{0}`, `{1}` indexed placeholders.
- Comments on the resource entry name each placeholder
  (`{0} = file name, {1} = byte count`).
- Avoid concatenating localised fragments at runtime; assemble the full
  template in the resource string and pass arguments to
  `string.Format(CultureInfo.CurrentUICulture, …)`.

## Pluralisation

For counts, prefer two keys (`…_Singular`, `…_Plural`) with the call site
choosing based on the integer, or use ICU-style messages through the
appropriate format library when added. Do not embed `"{0} flow(s)"`-style
parenthetical workarounds.

## ViewModel access

ViewModels access localised strings through a `Strings` accessor generated
by the ResourceManager. Bind directly to the property in XAML
(`Text="{x:Static res:Strings.TrafficList_Header_Url}"`) when the string is
static. For computed strings, format inside the ViewModel and expose the
final value through an `[ObservableProperty]`.

## Locale resolution

The active locale is resolved in this order:

1. The user's stored preference (`UserPreferences.Locale`).
2. The Windows system UI locale (`CultureInfo.InstalledUICulture`).
3. `en-US` fallback.

Switching the locale at runtime takes effect immediately — Views re-resolve
their bindings against the new `CultureInfo.CurrentUICulture`. Do not cache
the localised string in a long-lived `static readonly` field.

## Validation gate

`.tools/Test-ResourceKeys.ps1` checks:

- Every key in `Strings.resx` appears in every sibling locale file.
- No locale file contains a key that is not in the neutral file.
- Placeholder counts match across locales (`{0}` in `en` must mean the same
  in `fr`).

`Invoke-Build.ps1` calls this script after the build step. Run it directly
when iterating on resources:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .tools/Test-ResourceKeys.ps1 -Path .
```
