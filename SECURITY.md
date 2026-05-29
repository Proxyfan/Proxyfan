# Security policy

We take the security of Proxyfan seriously. Thank you for helping us keep it safe.

## Supported versions

| Version | Supported |
| --- | :---: |
| Latest release | ✅ |
| Older releases | ❌ |

Only the latest GitHub release receives security updates.

## Reporting a vulnerability

**Do not open a public GitHub issue for security vulnerabilities.**

Instead, please report security issues privately via GitHub's
[Security Advisories](https://github.com/Proxyfan/Proxyfan/security/advisories/new) page.
Use the *"Report a vulnerability"* button to start a private advisory.

When reporting, please include:

- A clear description of the issue and its impact
- Steps to reproduce (proof-of-concept if possible)
- Affected version(s)
- Suggested fix (optional)

You should receive a first response within **5 business days**.

## Scope

The following are in scope:

- The Proxyfan desktop application (`src/Clients/Client.Desktop`)
- The Proxyfan CLI (`src/Cli`)
- The proxy engine and TLS interception (`src/Framework.Networking`, `src/Domain.Proxy`)
- The plugin system (`src/Framework.Extensibility`)
- The scripting engine sandbox (`src/Domain.Scripting`)
- The certificate management workflow (`src/Domain.Certificates`, `src/Framework.Platform`)

The following are **out of scope**:

- Vulnerabilities in third-party dependencies (report those to the dependency authors)
- Issues caused by user-installed third-party plugins
- Issues in user-authored scripts running inside Proxyfan
- Social engineering of project maintainers

## Disclosure

After a fix has been merged and released, we will publish a security advisory crediting
the reporter (unless they prefer to remain anonymous) and assign a CVE if appropriate.

## Hardening guidance

Proxyfan ships with privacy- and security-conscious defaults:

- **No telemetry.** No data ever leaves your machine except user-captured traffic and the
  GitHub Releases update check (which can be disabled in preferences).
- **HTTPS interception is opt-in per-domain.** The SSL Proxying List defaults to disabled
  and individual hosts must be explicitly added.
- **Bodies are never logged.** Headers are logged only at the Trace level. `Authorization`,
  `Cookie`, and `Set-Cookie` headers are redacted by default.
- **Root CA private key is encrypted via Windows DPAPI** in `%LOCALAPPDATA%\Proxyfan\certificates\`.
- **Plugins run in isolated `AssemblyLoadContext`** with no file-system, network, reflection
  emit, or threading access by default.
- **Scripts have memory and time limits** (50 MB / 5 sec by default).
