---
name: convergence-review
description: Perform the single independent acceptance review
---
# convergence-review

Freeze a code reference and evidence. Use a fresh different-family model, memory off, explicit read/grep/glob tools, no implementation conversation. PASS, BLOCKED and INSUFFICIENT_EVIDENCE are valid; zero findings is valid. BLOCKER/MAJOR block completion. Deduplicate MINORs into backlog. After fixes, verify only affected behavior; do not repeat the full review.

For the verified OMP 18.1.18 path, use the installed native SDK rather than adding a project SDK dependency. Resolve its location and profile from the actual environment; do not copy a personal installation path. Create separate read-only Settings with memory.backend off, autolearn.enabled false, advisor.enabled false and plan.defaultOnStartup false.

Use native createAgentSession with a custom review-only system prompt, the profile-local different-family model role, toolNames read/grep/glob, restrictToolNames true, enableMCP/enableIrc/enableLsp false, disableExtensionDiscovery true, empty skills/rules/contextFiles/promptTemplates/slashCommands, SessionManager.inMemory and a finite deadline. Before prompting, inspect the resolved model family, exact tools, memory setting and zero initial messages. Fail closed if those prerequisites are unknown or wrong.

Call session.prompt once with only the frozen reference, relevant specification/code paths and verification evidence, not the primary chat or explanation of its implementation choices. Record the actual model/tools/settings, result and usage, then dispose the session. A setup-only capability check is not the full review. A parseable PASS with an empty findings array is accepted without another review request.

All repository artifacts and inter-agent output must be English.
