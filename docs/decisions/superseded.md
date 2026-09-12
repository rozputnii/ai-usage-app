# Superseded alternatives

Use this table to avoid reviving outdated recommendations from the long discussion.

| Earlier proposal | Final decision |
|---|---|
| Android first | Windows 11 and .NET 10 first; Android follows. |
| One project or no ORM | Three projects with SQLite and EF Core 10. |
| DI without a Host | Generic Host. |
| Small custom navigation service | MVVM-first routing; Uno.Extensions.Navigation remains the selected candidate subject to a build/integration gate. |
| Credential Manager with DPAPI fallback | Only app-owned DPAPI CurrentUser records for application credentials. |
| Full configuration validation at every launch | Cheap schema/journal compatibility checks; full checks after migrations/restores or evidence of corruption. |
| Global HTTP concurrency of four | No arbitrary global network cap; respect actual provider throttling and retry constraints. |
| Manual refresh joins the background request | Manual refresh cancels/replaces quota fetching; token rotation remains a separate serialized lifecycle. |
| Used percentage as the main number | Remaining percentage is primary. |
| Automatic risk-based card ordering | User-controlled order with status highlighting. |
| Tray click simply opens the main window | Single click opens the mini-dashboard; double-click opens the main window; right-click opens the menu. |
| Separate dashboard/tray provider reads | One immutable current-state store, with separate history queries. |
| Portable import merges data | Replace-only import; preserve existing credentials only for matching identities. |
| Encrypted portable export | Open unencrypted export without secrets. Local migration backups remain DPAPI-encrypted. |
| Either factory reset or settings reset | Provide both. Factory reset clears mutable local state, not a literal reinstall. |
| Stable only or separate Preview installation | One direct package identity with Stable/Preview feeds and no downgrade. |
| Microsoft Store immediately | Direct distribution first; Store later with explicit identity/data migration. |
| Mandatory 24-72-hour soak | Risk/evidence-based promotion; hotfixes may have no waiting period. Stable promotion remains manual. |
| Self-contained deployment or AOT immediately | Framework-dependent deployment; Native AOT is not required. Measure first. |
| Owner approves every feature spec/design | Escalation-only within an authorized goal, with Interactive selection or explicitly scoped Autopilot. |
| Only one code-writing agent | Independent isolated write workers are allowed; the primary is the sole integrator. |
| Full repeated reviews after each fix | One full independent review, then targeted verification. Zero findings is valid. |
| Automatic skill learning | Auto-learn is disabled. Authored repository skills are authoritative; local memory is supplementary. |
| Every fresh session automatically starts another feature | Interactive sessions ask for selection/resume; only internal Autopilot handoff carries existing authorization. |
| Absolutely no remote policy | No dynamic remote configuration; a narrow signed static disable-only compatibility manifest is approved. |
| Missing quota fields become zero/default quota | Unknown remains unknown. Critical semantic mismatch cannot publish fabricated quota data. |
| Every planned feature must exist in the first build | Ship an early runnable MSIX and add the v1 scope after the Codex slice. |
| Ukrainian or mixed-language repository documents | English-only repository, documentation, decisions and development artifacts. Ukrainian is conversation only. |

For a new conflict, compare accepted intent with actual evidence. Record an amendment or escalate rather than silently picking whichever option is easier to implement.
