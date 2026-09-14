# Security

## Reporting

Do not publish tokens, credentials, private provider responses or exploit details in public issues or pull requests.

GitHub private vulnerability reporting was **disabled** when this repository was checked during AIU-001. No alternative private security inbox has been configured or verified. Until the maintainer enables a private reporting channel, a public issue may request a private contact method without including sensitive details. Do not assume that a private-report button or confidential response process exists.

The current bootstrap has no released Windows application or supported product-version range. Production signing, updates and live provider integrations remain outside AIU-001.

## Development boundaries

- OMP project extensions execute trusted local code. Native isolation and ownership checks are not an OS security sandbox.
- The work bridge requires explicit owner confirmation, bounds its guarded native calls/transitions, rejects unauthenticated headless-primary writes and process launches, and checks actual returned patches before primary integration.
- Arbitrary commands inside an already permitted shell/eval/process call are not individually mediated. Do not run untrusted repository code or grant it credentials on the strength of these gates.
- Authentication, concrete model mappings, sessions and memory are user-local. Use sanitized fixtures for provider tests; this bootstrap did not import provider credentials.
- Main branch protection was absent at preflight. No remote push, PR, merge, protection change or release authority was exercised. Required independent remote checks remain a publication prerequisite.

See [observed environment evidence](docs/workflow/environment.md) and the [workflow limits](docs/workflow/omp-native.md). Report security-relevant limitations accurately rather than weakening requirements to obtain a green check.
