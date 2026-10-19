# Security Policy

## Supported versions

Security fixes are provided for the latest released version of Dataverse Audit Manager.

## Reporting a vulnerability

Do not report security vulnerabilities in a public issue.

Use GitHub's private vulnerability reporting feature from the repository's **Security** tab. If private reporting is unavailable, contact Pobuca through [pobuca.com](https://www.pobuca.com/) and include a link to this project.

Please include:

- The affected version
- Reproduction steps
- The potential impact
- Any suggested mitigation

Do not include Dataverse credentials, access tokens, customer records, audit exports, or other sensitive environment data.

## Operational security

The plugin uses the active XrmToolBox connection and does not store credentials. Audit-history deletion is permanent and should be tested in a nonproduction environment before operational use.
