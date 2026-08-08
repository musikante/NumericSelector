# Security policy

## Supported versions

While the project has no published versions, the main development branch is the only version considered for security fixes.

| Version | Receives security fixes |
| --- | --- |
| Current development | Yes |
| Future published versions | Will be stated when each version is released |

## Reporting a vulnerability

Do not publish potential vulnerabilities in public issues. Send a private description to the repository maintainer through the contact channel that gets set up when the project is published on GitHub.

The report should include:

- an explanation of the impact;
- minimal steps to reproduce it;
- the affected version, operating system and runtime;
- a possible mitigation, if you know of one.

Receipt will be acknowledged, the problem assessed, and disclosure coordinated once a reasonable fix or mitigation exists.

## Current scope

`BoundedNumericSelector`, the control that lives in the `NumericSelector` assembly, is a WPF interface control with no network services, persistence or credential handling of its own. Even so, reports about denial of service in layout, unsafe use from XAML, and build or distribution dependencies will be reviewed.
