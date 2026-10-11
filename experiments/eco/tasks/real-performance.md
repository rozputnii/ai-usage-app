# Real task S4d: performance, memory and UI tree

Owner answer 2026-10-11: Q3 yes (whole app, no visible change). Two runs:

1. **S4d-0, measurement tool** (single run, arm A, merged first as its own T1 change), so that
   both arms of the pair measure with the same neutral instrument.
2. **S4d, the pair** (arms A and E) from `main` after S4d-0, with the installed-app log digest
   appended: `-Appendix ../results/S4-local-log-digest.md`.

## Measurement tool {#tool}

> Owner request, 2026-10-11: add a repeatable resource measurement for the app, to be used
> before and after performance work. Create `tools/windows/Measure-AppResources.ps1` (Windows
> PowerShell 5.1) that builds nothing itself and:
> - starts the unpackaged Release app (the C7 output) with `--demo` and an isolated
>   `AIU_DEVELOPMENT_STATE_DIRECTORY` under a temporary folder, so no real account is touched;
> - records the time to the first visible main window;
> - samples every few seconds for a configurable duration (default 10 minutes) the working set,
>   private bytes, handle count and GDI and USER object counts of the app process;
> - drives the refresh path during the run if the app offers a supported way (a refresh
>   command or interval setting reachable by UI Automation or a documented switch); otherwise
>   says in its output that only idle behavior was measured;
> - opens and closes the settings sheet and the history panel a configurable number of times
>   through UI Automation, following the desktop lock in docs/workflow/verification.md;
> - writes a CSV of samples and a short summary (start, steady state, growth per hour, peaks)
>   to a git-ignored folder under `.ai-usage-local/`, and exits the app cleanly.
> Add a dry-run or self-test the way other scripts in `tools/` do, and document the script in
> docs/workflow/verification.md as an opt-in check. Tier T1.

## Pair brief {#brief}

> Owner request, 2026-10-11: find and fix performance problems, memory and handle leaks, and
> incorrect or suboptimal UI constructions across the whole app (UI tree and bindings, event
> subscriptions, timers, the refresh loop, HTTP clients and handlers, DI lifetimes, the tray),
> without any visible change and without changing behavior.
>
> - Measure first with `tools/windows/Measure-AppResources.ps1` (baseline numbers in the
>   verification record), then again after each fix that claims an effect. A fix without a
>   measured or test-proven effect is not kept unless it removes a clear defect (for example an
>   unsubscribed event handler), which then needs a test.
> - Anything that would change what the owner sees (layout, timing of visible updates, copy) is
>   recorded as a finding or a `T-NEW` backlog item, not changed.
> - Use the installed-app log digest appended below (counts and record types only) as leads.
> - Logging changes follow the logging policy (T3 area).
>
> Acceptance:
> - AC-01 A before/after table: startup time, steady-state working set and private bytes,
>   growth per hour, handles, GDI and USER objects, each fix's measured effect.
> - AC-02 Every leak fixed has a regression test that fails at the base commit.
> - AC-03 C4, C5, C7, C8, C9, C2 and C6 pass; the launch, window-size and activation smokes
>   pass under the desktop lock; the UI suite's screenshots show no layout change.
> - AC-04 Findings not fixed are listed with evidence and a reason.
