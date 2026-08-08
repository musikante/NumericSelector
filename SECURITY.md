# Security policy

## Supported versions

Fixes are made on the `main` branch and reach the published package as a new version. While the numbering stays in `0.x`, only the latest version receives them: there are no maintenance branches for earlier ones.

| Version | Receives security fixes |
| --- | --- |
| `0.1.0` (latest published) | Yes |
| `main` (development) | Yes |
| Earlier `0.x` versions | No — upgrade to the latest one |

## Reporting a vulnerability

Do not publish potential vulnerabilities in public issues. Report it privately through the repository's **Security** tab, with *Report a vulnerability*: that opens a private conversation with the maintainer, visible to nobody else until there is a fix.

The report should include:

- an explanation of the impact;
- minimal steps to reproduce it;
- the affected version, operating system and runtime;
- a possible mitigation, if you know of one.

Receipt will be acknowledged, the problem assessed, and disclosure coordinated once a reasonable fix or mitigation exists.

## Current scope

`BoundedNumericSelector`, the control that lives in the `NumericSelector` assembly, is a WPF interface control with no network services, persistence or credential handling of its own. Even so, reports about denial of service in layout, unsafe use from XAML, and build or distribution dependencies will be reviewed.
