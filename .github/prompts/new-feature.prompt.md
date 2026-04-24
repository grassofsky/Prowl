---
description: "Create a new backlog item for Prowl. Use when: adding a feature request, filing a bug, or planning a new task. Keywords: backlog, feature, requirement, task, plan."
mode: "ask"
---

Create a new backlog item under `agent/backlog/`.

## Steps

1. Ask the user for:
   - **Title**: short name for the feature or fix
   - **Area**: rendering, editor, runtime, or player
   - **Priority**: P0 (blocker), P1 (must), P2 (should), P3 (nice-to-have)
   - **Problem**: what is broken or missing
   - **Acceptance Criteria**: specific, verifiable conditions (at least 3)
   - **Scope IN/OUT**: what to touch and what to exclude

2. Read `agent/backlog/_template.md` for the canonical format.

3. Generate the backlog ID: `{area}-{next-number}`. Check existing files in `agent/backlog/` to determine the next number for that area.

4. Create the file at `agent/backlog/{id}-{short-slug}.md` using the template format.

5. Add a row to the appropriate milestone table in `agent/plan.md`.

6. Report the created file path and ID back to the user.
