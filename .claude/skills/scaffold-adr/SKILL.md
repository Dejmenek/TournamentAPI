---
name: scaffold-adr
description: Scaffold a new Architecture Decision Record in docs/adr/ for the TournamentAPI repo, picking the next sequence number, following the established template, and cross-referencing related prior ADRs the way this repo already does. Use this whenever the conversation has weighed two or more real implementation approaches (concurrency strategy, storage or auth trade-offs, scheduling, rate limiting, a status-model change, and similar) and has settled on one, or whenever the user directly asks to write, document, or create an ADR. Do not use it for README updates, PR descriptions, or general documentation, only for an actual "which approach do we go with" decision.
---

# Scaffold an ADR

## When this applies

Reach for this only once a real decision has happened: at least two options were named with genuine trade-offs, and the conversation converged on one. If you're just describing how something already works, that's not an ADR, it's documentation, and this skill doesn't apply. If the user asks for an ADR directly, that overrides the check above, just do it.

## Step 1: find the next number and file name

ADRs live in `docs/adr/` as `NNNN_snake_case_title.md`, a 4-digit zero-padded sequence. List the directory, take the highest existing number, and use the next one. Build the slug from the title: lowercase, spaces to underscores, drop filler words (`for`, `a`, `the`, `of`). For example, "Concurrency Control for Tournament MaxParticipants" became `0004_max_participants_concurrency_control.md`.

## Step 2: find related ADRs and cite them if they exist

Read the existing files in `docs/adr/` (there are only a handful) and look for real overlap: same entity (`Match`, `Tournament`, `TournamentParticipant`), same technique (optimistic concurrency via `[Timestamp]`, unique-index arbitration, `DbUpdateException` handling, Hangfire), or an option that was rejected once before and just came up again.

If you find one, cite it by number and state the relationship, mirroring phrasing already in the repo ("reuses the pattern already established for X, ADR #0002", "rejected previously in ADR #0004 for the same reasons", "accepted under the same trade-off ADR #0002 already made"), and add a `## Related ADRs` section (see the template below) listing it.

If nothing overlaps, don't add the section at all.

## Step 3: draft from the conversation, not a fresh interview

The Context, Considered Options, and Decision almost always already exist in what was just discussed. Pull them from there instead of re-asking the user to restate things they already said. Only ask about whatever's genuinely missing, most often that's which specific trade-off tipped the decision, or a GitHub issue number to cite (existing ADRs reference issues like `#95`, `#102` when one exists, but don't invent one).

Follow the mature template style used from ADR #0004 onward, not the sparser style of #0001-#0003:

- mark the winning option inline with `(chosen)`
- give each option real pros and cons, specific to this decision, not generic ones
- if the decision actually bundles more than one sub-question (ADR #0006 does this for "how to correct a result" and "how to fix the resulting race" together), split `Considered Options` into `### For X` / `### For Y` subsections

## Step 4: fill the template

```
# Title
<Title Case Summary>

# Date
<DD/MM/YYYY, today's date>

## Status
Accepted

## Context
<what forced this decision; what's the current gap or risk>

## Considered Options
1. <option> (chosen)
   - Pros: ...
   - Cons: ...
2. <option>
   - Pros: ...
   - Cons: ...

## Decision
<what was chosen and why, referencing the pros/cons above rather than repeating them>

## Related ADRs
- ADR #NNNN <title>: <relationship, e.g. reuses/rejects the same option/inherits a trade-off>
```

Only include `## Related ADRs` when step 2 actually found one; leave it out otherwise.

Status is `Accepted` unless the user says the decision isn't final, in which case use `Proposed` instead and say so out loud when you show the draft, so it's clear it needs to be flipped to `Accepted` later. Every existing ADR in this repo says `Accepted`, so `Proposed` has no precedent here yet, that's fine, the rest of the template stays identical either way.

## Step 5: confirm, then write

Show the full drafted file before creating it. Once confirmed, write it to `docs/adr/NNNN_slug.md`. If `CLAUDE.md` or another doc in the repo happens to keep a list or index of ADRs at the time, add the new entry there too, matching whatever format that list already uses.
