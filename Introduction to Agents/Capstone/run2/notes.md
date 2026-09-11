## Run 2 — Instruction changes made, and why
- Added explicit malformed-row categories (structural, duplicate definition,
  date formats) directly to instruction.md, rather than leaving "decide how to
  handle each" to be discovered only during plan review. [Instructions lever]
- Made deterministic date parsing (TryParseExact + InvariantCulture) an explicit
  requirement rather than something caught after implementation. [Instructions
  lever]
- Added an explicit scope-discipline line prohibiting reformatting of
  unrelated pre-existing code. [Instructions lever]
- Added a required self-verification step before reporting completion:
  DECISIONS.md must be checked against actual code, not just prior chat
  agreement. [Instructions lever — targets the trajectory finding from Run 1
  where an agreed fix didn't land in the final implementation]
- Deliberately left the User and Priority decisions open-ended, unchanged from
  Run 1 — these are genuine judgment calls, not gaps, and re-testing them is
  the point.
- Same model (Gemini 3.1 Pro, High) used for both runs, to isolate the effect
  of the instruction changes from any model difference.

## Run 2 — Model change (forced, not planned)
Gemini 3.1 Pro hit its quota limit mid-setup before Run 2 could start. Switched
to Claude Opus 4.6 (Thinking) to complete the run. This means Run 2's instruction
changes and its model change are confounded — any difference from Run 1 could be
due to either factor, and I can't cleanly isolate which. Treating this as a
qualitative comparison rather than a controlled one, and calling that out
explicitly rather than presenting Run 2 as an apples-to-apples instruction test.

## Run 2 - initial prompt
Read instruction.md in \run2 directory first, in full, before doing anything else.
Then begin the task it describes. Work only within this directory (run2) — do not
touch anything outside it, and do not commit or push to git. Confirm you've read
instruction.md and summarize back to me: the two open design decisions, the
malformed-row categories you'll handle, and the date-parsing requirement — before you start writing code.

Unlike Run 1, this confirmation didn't need a follow-up correction to be complete.

## Run 2 — Model comparison observation
Opus 4.6 (Thinking) independently proposed rejecting missing/blank User rather
than defaulting to a placeholder, correctly reasoning from GetTodoItems' exact-
match filtering — the same trap Gemini 3.1 Pro fell into in Run 1 and needed a
direct correction to avoid. Since instruction.md was also tightened between
runs (though not on this specific decision, which was deliberately left open
both times), this is a meaningful signal on reasoning depth for this decision
specifically, even though the runs aren't fully controlled overall.

## Run 2 — Antigravity appeared to hang after `dotnet build` succeeded; this
turned out to be Opus quota being exhausted mid-task, not an extension bug.
Baseline quota refreshes 9/18/2026. Switched to Claude Sonnet 4.6 (Thinking)
to continue Run 2 without waiting a week. Run 2 is now a three-model story
(started Gemini, switched to Opus, quota-forced switch to Sonnet partway
through) — noting this plainly rather than presenting the run as cleaner
than it was.

## Run 2 — Cross-model verification catch
Codex (GPT, via ChatGPT Go) was asked to verify prior work rather than trust it,
and found DECISIONS.md claims a TodoApi.Tests project was created when it does
not exist in the working directory. This likely happened because Opus's session
was cut off mid-task by the quota exhaustion — DECISIONS.md may have been
written aspirationally/in-progress before the test project step actually ran.
Regardless of cause, this validates the earlier concern about verifying
documentation against actual code rather than trusting it at face value — a
second model, independently checking, is what caught it.

Also found: Enum.TryParse accepts out-of-range numeric values (e.g. Priority=3),
contradicting both DECISIONS.md and the error text, which claim only 0/1/2 are
valid. A real validation gap, not just documentation drift.

## Run 2 — Backend verification (independently run, not agent-reported)
dotnet test in TodoApi.Tests: 8/8 passed. Confirmed directly in terminal —
matches the agent's claim, first time in this run a claim held up without requiring a correction round.

## Run 2 — Frontend verification (independently run, not agent-reported)
CI=true npm test -- --watchAll=false: 2/2 passed (TodoImport.test.js, App.test.js).
Confirmed directly in terminal — matches agent's claim.

## Run 2 — Repeated pattern: claimed-vs-actual gap under Codex/GPT (High)
Across the test-writing phase, Codex reported changes as complete on three
separate occasions (Enum.TryParse fix, missing User/DueDate test coverage,
splitting into separate [Fact] methods) that turned out not to be reflected
in the actual file when the raw content was requested directly. Each time,
asking for the raw file rather than a summary was what caught the gap. On the
fourth request, the raw file finally matched the claim. This mirrors the
DECISIONS.md-vs-code gap seen under Opus earlier in the same run — suggesting
this "summary outpaces actual state" failure mode isn't isolated to one model,
and that verifying raw artifacts rather than trusting natural-language status
reports was the single most load-bearing review habit across this entire run.

## Run 2 — Scope/standard tension: App.test.js
Codex rewrote the pre-existing (broken) App.test.js rather than leaving it
untouched and adding a separate test file, technically conflicting with
instruction.md's "no existing tests removed" line. Unlike Run 1's PutTodoItem
reformatting (pure scope creep, no functional justification), this had a real
justification — the old test could not run against the current app — but the
agent made that call unilaterally rather than surfacing the conflict for a
decision. A case of two of my own instructions being in tension with each
other, which the agent resolved without flagging it.

## Run 2 — Quota cost observation
Codex/GPT at "5.6 Terra High" consumed ~57% of a monthly ChatGPT Go quota
allotment completing the remaining implementation, test-writing, and multiple
correction rounds. Worth noting as a real cost axis alongside capability —
not measured for Gemini/Opus/Sonnet in comparable terms since those runs hit
a hard weekly quota wall rather than a percentage metric.

## Run 1 vs Run 2 — Summary
- Tightening instructions.md up front (explicit malformed-row categories,
  deterministic date parsing, scope discipline, self-verification requirement)
  measurably reduced the number of gaps discovered only during plan review —
  Run 2's initial confirmation didn't need a correction round, unlike Run 1.
- However, Run 2 was not a clean instruction-only comparison: a three-model
  pivot (Gemini → Opus → Sonnet → Codex) was forced by quota limits, so
  capability differences and instruction changes are confounded.
- The most consistent finding across both runs, independent of model: agents
  reliably report work as complete or documentation as accurate before
  verifying it against the actual code. This happened under Gemini (Run 1,
  TryParse claim), Opus (Run 2, DECISIONS.md false test-project claim), and
  Codex (Run 2, three separate under-delivered fix claims). The single most
  effective review technique across the entire capstone was asking for raw
  file content instead of accepting natural-language summaries.