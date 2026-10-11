# Micro-trial: runner (S2)

Tests H3: whether a haiku runner is cheaper than an opus medium primary running quiet checks
itself. Base `1a8526e`, primary opus medium, `-AgentsFile ..\arms\D-agents.json`. One run per
variant, plus one repeat of each to see the variance. Compare the whole run's cost (primary and
subagent) and the correctness of the reported verdicts against the logs.

Advisor (H7) has no separate micro-trial: an artificial "stuck" step would not be realistic.
It is measured where arm D's primary chooses to use it in S3 and S4.

## Self {#self}

This is a measurement probe, not real work. Run these checks yourself, quietly (output to
`.ai-usage-local/logs/<check>.log`, print only the exit code and at most 20 first error lines
on failure): restore (the offline one-liner in docs/workflow/verification.md), C2, C6, C4, C5.
Make no changes. Report each check as PASS or FAIL in CHECKS.

## Delegated {#delegated}

This is a measurement probe, not real work. Delegate to the agent `runner` this brief and
nothing else: "Run restore (the offline one-liner in docs/workflow/verification.md, section
Checks), C2, C6, C4 and C5 from docs/workflow/verification.md, in this clone, with logs in
`.ai-usage-local/logs/`." Do not run checks yourself. Report the runner's verdicts in CHECKS.
