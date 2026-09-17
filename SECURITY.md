# Security Policy

## Supported versions

Beep.Skia is pre-1.0 (`1.0.0-alpha`). Security fixes are applied to `master` and included in the
next tagged release; there are no maintained older branches yet.

| Version | Supported |
|---------|-----------|
| `master` (pre-release) | Yes |
| Tagged releases | Best effort until 1.0 |

## Reporting a vulnerability

Please **do not open a public issue for security problems**. Report them privately:

- Use GitHub's [private vulnerability reporting](https://github.com/The-Tech-Idea/Beep.Skia/security/advisories/new)
  on the repository, or
- Contact the maintainers through the organization profile if you cannot use GitHub.

Include:

- A description of the issue and its impact.
- Reproduction steps or a minimal proof of concept.
- Affected package(s) and version(s).
- Any suggested fix or mitigation.

You can expect an acknowledgement within a few business days. We will confirm the issue, prepare a
fix, and credit you in the release notes unless you prefer to stay anonymous.

## Scope

The framework's security-relevant surfaces are:

- **Extension packages** — `.beepkg` installation validates archives (path traversal and
  decompression-bomb protection), dependency and version gates, and install provenance.
- **Credential vault** — secrets are exported with AES encryption using a PBKDF2-SHA256 derived
  key; tampered ciphertext and wrong passphrases fail loudly; `List()` returns masked values.
- **Collaboration** — role checks guard comment and share operations; the audit trail records
  privileged actions.
- **Deserialization** — diagram loading tolerates malformed input without throwing, and recursive
  parsers (mind map, JSON/XML flattening, expressions, BPMN/XMI) are depth-limited.
- **Data source access** — automation nodes reference data sources by name; credentials are
  resolved from the vault, never stored in diagram files.

## Guidance for users

- Keep the private `TheTechIdea.Beep.*` feed trusted; the core package depends on it.
- Do not commit vault exports, connection strings or API keys. Use `CredentialVault` and reference
  entries by name.
- Treat exported migration scripts from the ERD tools as reviewed artifacts: verify destructive
  changes before running them against a database.
- Only install extension packages from sources you trust; the safety gates reduce but do not
  eliminate risk from arbitrary code, which extensions inherently are.
