# Focused independent review

2026-09-22, existing Claude Code with reported model claude-sonnet-5, fresh
nonpersistent safe-mode session, Read/Grep/Glob only. Requested candidate dc0548e,
base f708954. No transcript, provider state or signing material supplied. Codex's
required Luna override was unavailable; no alternate Codex model was selected.

Initial verdict: FAIL for the shallow validate checkout. The release ancestry test
requires the parent of HEAD, while default checkout only fetched one commit. Actual
CI independently reproduced the same failure. Commit 9064d76 fetches two commits and
checks that history exists. The subsequent run also exposed an expected negative
Git exit code leaking into the successful test script's result; ff30d80 corrects
the handled native status. Final local assertions and hosted validate job PASS.

No proven signing, authorization or material leakage defect was found. The reviewer
checked new-key-only provisioning, stdin secret transport, cleanup, job permissions,
main/PR boundaries, version reservations, source ancestry, dependency identities and
forward-only feed settings. Review was static: it does not prove provisioning,
signing on the hosted runner, remote publication or automatic Windows update.

The review ran against shared read-only working files while narrow CI fixes advanced
main. Its requested reference is not a claim of an isolated filesystem snapshot.
The primary checked the post-candidate changes: MSBuild restore array (55286df),
checkout depth (9064d76) and handled Git status (ff30d80). No signing/privilege boundary
changed in those fixes. The original full response remains ignored locally.

Reviewer uncertainty about queue:max is resolved by current official GitHub docs and
the accepted actual workflow run. Its statement that windows-package depends on
validate is incorrect: these jobs run independently; preview requires both. Neither
detail affects the identified shallow-checkout defect. Draft visibility, real
signing/feed delivery and package activation still require operational evidence.

## Runner trust fix review

2026-09-23, fresh read-only Claude Code subagent (Plan type: no edit/write tools),
candidate d28e6be against base 265c489, with the failed run's SignTool evidence and
the spec. It had no implementation transcript and no signing material. Verdict:
PASS, no material findings. It checked that verification is unchanged and fails
closed, that the guard limits trust to the hosted main-push `preview` job, that only
the signer's own public self-signed CER is accepted, that pre-existing trust is never
removed, that root trust and the key are removed on every path, that no secret is
newly exposed, and that the regression never opens a certificate store.

Minor findings. The test comment overstated TrustedPeople: the repository's guest
smoke uses LocalMachine\TrustedPeople for Get-AuthenticodeSignature, so the comment
now states only the observed CurrentUser\TrustedPeople failure. The guard assertions
now require the guard's own message instead of any exception. Two findings were
accepted without change: a cleanup exception can mask the original failure (the job
still fails closed), and the certificate-rejection branches are untested because
faking the preview job next to a store write is unsafe. Static review cannot
establish runner elevation or hosted SignTool success; only the hosted run can.
