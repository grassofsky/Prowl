---
description: "Review current milestone progress for Prowl. Use when: checking sprint status, reviewing what's done vs pending, planning next steps. Keywords: sprint, review, progress, milestone, status."
mode: "ask"
---

Generate a release progress report.

## Steps

1. Read `agent/plan.md` to get the release versions and all items per release.

2. For each item in each release table, read its `agent/backlog/{id}.md` file to check:
   - Current `status` and `release` in YAML frontmatter
   - How many Acceptance Criteria are checked vs total
   - Whether an `owner` is assigned

3. Output a per-release summary:

```
## Release Progress: {version} — {theme}

| ID | Title | Status | AC Progress | Owner |
|----|-------|--------|-------------|-------|
| ... | ... | ... | 2/5 | ... |

**Status breakdown:** X spec, Y ready, Z in-progress, W in-review, V done, U blocked
```

4. Output the cross-release status summary table (matching the one in plan.md) with updated counts.

5. List any blocked items and their blocking dependencies.

6. List items with `status: spec` that still need Acceptance Criteria filled in.

7. Suggest which items to move to `ready` next based on priority and dependencies.

8. Flag any items whose `release` field in the backlog file doesn't match their position in plan.md tables.
