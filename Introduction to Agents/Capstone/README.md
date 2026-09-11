# CSV Import Capstone — Directing an Agent on Real Work

A capstone exercise in directing coding agents on a real task: adding a CSV
import feature to [ReactToDo](https://github.com/leniel/ReactToDo), a
personal React + ASP.NET Core todo app, at pinned commit
`be1efe8060b49f42afcb61bb78643236064330f4`.

This repo captures the setup, instructions, two full runs (across four
different models, due to quota constraints), and the review/diagnosis/
iteration process — not just the final code.

## Contents

- **`instruction.md`** — the final (Run 2) prompt given to the agent: goal,
  sources, out-of-scope files, output requirements, and the accept/reject
  standard.
- **`notes.md`** — the full write-up: problems found in each run, the lever
  (context/workspace/instructions) diagnosed for each, trajectory
  observations, the Run 1 → Run 2 comparison, and a row-by-row manual
  verification of the shipped feature (including a direct database query
  confirming an ambiguous-date parsing fix actually worked).
- **`todos-import-sample.csv`** — the test data used across both runs,
  including deliberately malformed rows (missing fields, invalid dates,
  duplicates, an ambiguous date format) designed to exercise every rule in
  the standard.
- **`run1-changes.diff`** / **`run2-changes.diff`** — full unified diffs of
  each run's code changes against the pinned baseline commit.

## Summary

Two runs were completed. Run 2 tightened the initial instructions based on
gaps found in Run 1, but a shared model-quota limit forced an unplanned
four-model handoff (Gemini → Claude Opus → Claude Sonnet → OpenAI Codex)
partway through, so the comparison isn't a clean single-variable test. The
most consistent finding across both runs and all four models: agents
reliably report work as complete before it's actually been verified against
the real code — asking for raw file content instead of trusting
natural-language summaries caught every real gap found in this exercise.

See `notes.md` for the full account.
