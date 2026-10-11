# Harness smoke (S0, S1)

Run with `-Model haiku -Effort low` in arm D (expects the scout) and arm A (no scout).

## Smoke {#smoke}

This is a harness smoke test, not real work. Do exactly this and nothing else:
1. Run `git log -1 --format=%h` and note the hash.
2. If an agent named `scout` is available, ask it once: "Where is the class PeakHours defined? Answer with the path only." Otherwise skip this step.
3. Make no changes and no commits. Finish with the run report; put the hash and the scout's answer (or "no scout") in NOTES.
