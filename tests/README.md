# Test infrastructure notes

This file documents test infrastructure that mutates the developer's local machine.
Read it before running the test suite on a new machine so you understand what is
created, where, and how to undo it.

## Persistent test certificate authority

The Proxyfan test suite uses a single, persisted root certificate authority — the
**Proxyfan test CA** — so the certificate-store tests do not have to install and
remove a fresh root certificate on every run. The persistent CA is the foundation
for unattended test execution: once installed it is reused indefinitely, with no
further security-warning dialogs.

The orchestration lives in `tests/Tests.Common/TestPki.cs` and is called from
the `[Before(Class)]` hook of every test class that needs a real, trusted CA in
the `CurrentUser\Root` store (currently
`tests/Framework.Platform.Tests/WindowsCertificateStoreTests.cs`).

### What gets created on the machine

| Resource | Path / location | Purpose |
|---|---|---|
| Persisted CA PFX (with private key) | `%LOCALAPPDATA%\Proxyfan\test-pki\test-ca.pfx` | The stable root authority reused across every test run |
| Trusted root certificate | `CurrentUser\Root` store, subject `CN=Proxyfan Certificate Authority` | Trust anchor so the proxy's leaf certs validate without callbacks |
| Registry suppression flag (best effort) | `HKCU\Software\Policies\Microsoft\SystemCertificates\Root\ProtectedRoots\Flags = 0x1` | Disables the Windows root-cert install dialog (see "Group Policy caveat" below) |

The first time the test suite runs, Windows shows a single
"Security Warning — Install this certificate?" dialog for the new
`CN=Proxyfan Certificate Authority` thumbprint. **You must accept it once.** Every
subsequent test run silently reuses the already-trusted thumbprint and runs
unattended.

### Group Policy caveat

On enterprise-managed Windows machines, a domain-pushed Group Policy commonly
locks the `HKCU\Software\Policies\Microsoft\SystemCertificates` hive and
overrides the per-user suppression flag. On those machines the registry write
performed by `WindowsRootCertificatePromptSuppressor.Suppress()` is silently
rejected (the helper returns `false` and proceeds). This does not block the
persistent-CA flow — the install dialog still only appears **once per
thumbprint**, and the persistent CA's thumbprint never changes, so the unattended
contract still holds.

## Cleaning up

To fully reset the test PKI state on a developer machine:

1. Remove the persistent CA from the `CurrentUser\Root` store. The easiest path is
   to open `certmgr.msc`, navigate to *Trusted Root Certification Authorities →
   Certificates*, find the `Proxyfan Certificate Authority` entry, right-click,
   *Delete*. Confirm the security-warning dialog. (Note: the `CN=Proxyfan`
   certificate — without the *Certificate Authority* suffix — is the
   production app's CA, not a test artifact.)
2. Delete the persisted PFX file:
   ```powershell
   Remove-Item "$env:LOCALAPPDATA\Proxyfan\test-pki" -Recurse -Force
   ```
3. (Optional) clear the registry suppression flag if it was previously written:
   ```powershell
   Remove-ItemProperty "HKCU:\Software\Policies\Microsoft\SystemCertificates\Root\ProtectedRoots" -Name "Flags" -ErrorAction SilentlyContinue
   ```

The next test run regenerates everything from scratch and re-prompts for the new
thumbprint.

## Why the previous fresh-install round-trip tests were removed

Earlier versions of `WindowsCertificateStoreTests` included two tests that
generated a brand new root CA on every run and installed it into
`CurrentUser\Root` to exercise the install/uninstall round-trip
(`InstallAsync_FreshAuthority_UninstallsDuringTeardown`,
`InstallAsync_TeardownThrows_StillRemovesCertificate`). Those tests were
deleted: on enterprise-GPO machines the suppression flag is overridden, so they
prompted the user every single run, leaving stale certs behind when the dialog
was dismissed. The XML doc comment on `WindowsCertificateStoreTests` records
the full rationale and lists the replacement coverage. **Do not re-enable
those tests** — see the comment for the constraints any future fresh-install
coverage must satisfy.
