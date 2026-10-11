# Real task S4f: architecture analysis

Paired run (arms A and E) from `main` after the other real tasks merged, so the analysis sees
the final code. The winner's analysis merges; the loser's unique, verified points are listed in
`results/S4f.md` for the owner.

## Brief {#brief}

> Owner request, 2026-10-11: analyze the application's architecture and write down ideas that
> would make it more stable and make new features easier to add. The output is a proposal that
> needs the owner's agreement before any implementation; change no code.
>
> Look at the Core, Infrastructure and Windows boundaries (AGENTS.md, docs/constitution.md),
> the provider abstraction and how a fifth provider would be added, persistence and migrations,
> the presentation layer (view models, controls, the tray), desktop lifetime, refresh and
> failure handling, and diagnostics. Read docs/decisions/accepted.md for the decisions behind
> the current shape before proposing to change it.
>
> Deliver one backlog item `T-NEW` with status research-needed and its specification folder,
> following docs/workflow/formats.md. Its specification holds:
> - the current structure in a few paragraphs, with the main pain points and file:line evidence;
> - proposals ranked by value against risk; each says what becomes simpler or more stable, what
>   it costs, how it would be migrated in small verified steps, and which decisions it would
>   amend;
> - what should explicitly stay as it is, and why.
>
> Acceptance: AC-01 every pain point and proposal cites code evidence; AC-02 each proposal
> names its migration steps and risk; AC-03 no code changes; C2 and C6 pass.
