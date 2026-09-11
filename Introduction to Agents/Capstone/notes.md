# Capstone Notes — CSV Import Feature (ReactToDo)

**Repo:** [leniel/ReactToDo](https://github.com/leniel/ReactToDo)
**Pinned commit:** `be1efe8060b49f42afcb61bb78643236064330f4`
**Task:** Add CSV import for todos — new `[AllowAnonymous]` backend endpoint,
frontend upload control, explicit malformed-row handling, backend + frontend
tests, and a running `DECISIONS.md` log.

---

## 1. Task Setup

- **Context package:** Full repo cloned at the pinned commit into an isolated
  `baseline/` (never touched), copied fresh into `run1/` and `run2/` for each
  attempt. `instruction.md` + a hand-built `todos-import-sample.csv` (with
  deliberately malformed rows) provided alongside.
- **Excluded on purpose:** `TodoApi/HasScopeHandler.cs`,
  `TodoApi/HasScopeRequirement.cs`, `azure-pipelines.yml`, and the
  `[Authorize(...)]`/Auth0 config in `Startup.cs` — named explicitly as
  out-of-scope in `instruction.md`, since the new import endpoint needed
  `[AllowAnonymous]` and none of the existing authorized endpoints should
  be touched.
- **Initial prompt** named the goal, sources, output format (backend
  endpoint + frontend control + tests + `DECISIONS.md`), and the accept/
  reject standard (malformed-row handling, C# formatting rules) explicitly.
- **Nothing confidential:** public, MIT-licensed personal repo; all sample
  data (names, emails) is synthetic.

---

## 2. Run 1

**Setup:** Google Antigravity (VS Code extension) · Gemini 3.1 Pro (High) ·
Google AI Plus (2TB) base/free Antigravity quota tier.

### Problems found before any code was written (plan review)
| Lever | Problem |
|---|---|
| Instructions | Plan defaulted a missing/blank `User` to a placeholder (`"AnonymousImporter"`). Since `GetTodoItems` filters by exact `User` match, this would make imported todos permanently invisible — a technically-working but functionally broken solution. Caught by reading the existing controller logic, not by running anything. |
| Instructions | First plan omitted duplicate-row detection, structurally malformed rows (wrong column count), and `DueDate` format handling entirely — all three are named requirements in `instruction.md`. Needed an explicit follow-up before the plan was complete. |
| Instructions | Plan proposed plain `DateTime.TryParse` for `DueDate`, which is culture-dependent and can silently misparse ambiguous dates (e.g. `01/02/2026`) instead of rejecting them. Caught by domain knowledge — the original sample CSV had no actually-ambiguous date, so this gap wouldn't have surfaced by testing alone. Fixed in the context package for Run 2. |

### Workspace problem (caught before launch)
- Pinned commit targets `netcoreapp3.1`; only .NET 6.0 was installed locally
  (`dotnet --list-runtimes` confirmed no 3.1 runtime present). Resolved by
  adding an explicit environment note to `instruction.md` directing the
  agent to retarget to `net6.0` first, rather than discovering the mismatch
  mid-task.

### Trajectory observations
- ✅ Agent stopped after reading `instruction.md` and summarized both open
  design decisions before writing any code, rather than guessing silently.
- ❌ Agent verbally agreed to switch `DueDate` parsing to `TryParseExact`
  during design discussion, but the "implementation complete" summary
  didn't mention it — the code still used plain `TryParse`. Only fixed
  after a second, direct prompt. **A trajectory failure, not an output
  failure:** a decision agreed to mid-session didn't survive to
  implementation, and completion was reported without flagging the gap.
- ❌ `DECISIONS.md`'s date-parsing entry stayed stale (described the old
  `TryParse` approach) even after the code fix landed — not reconciled
  until directly asked to check it against the actual code.
- ❌ When asked to fix formatting-standard violations (one arg per line on
  multi-arg calls), the agent also reformatted `PutTodoItem` and
  `CreatedAtAction` — pre-existing methods unrelated to this feature and
  outside scope. Over-applied the standard to code it wasn't asked to
  touch, bundling an unnecessary change into the diff.
- ✅ When told about the scope creep, reverted both methods without
  pushback and correctly re-scoped the diff.

**Pattern:** this agent implemented correctly once a gap was pointed out
directly, but didn't reliably self-catch scope creep or drift from earlier
agreements without a second, explicit prompt.

### Design decisions made well
- **User:** reject missing/blank rows explicitly rather than default —
  correct once corrected, reasoned from the actual `GetTodoItems` filter.
- **Priority:** accept both enum name and numeric value, case-insensitive.
- **Duplicates:** same `Name` + `User`, checked against DB and prior rows
  in the same file — clear, defensible definition.

---

## 3. Run 2

### Instruction changes made, and why
- Baked in explicit malformed-row categories (structural, duplicate
  definition, date formats) directly, instead of leaving "decide how to
  handle each" to be discovered only during plan review. *[Instructions]*
- Made deterministic date parsing (`TryParseExact` + `InvariantCulture`)
  an explicit requirement rather than something caught after the fact.
  *[Instructions]*
- Added an explicit scope-discipline line prohibiting reformatting of
  unrelated pre-existing code. *[Instructions]*
- Added a required self-verification step before reporting completion:
  `DECISIONS.md` must be checked against actual code, not just prior chat
  agreement. *[Instructions — targets Run 1's trajectory finding]*
- Deliberately left the `User` and `Priority` decisions open-ended,
  unchanged from Run 1 — genuine judgment calls worth re-testing, not gaps.
- Added one deliberately ambiguous date row (`01/02/2026`, slash-separated,
  isolated from any other malformed condition) to the sample CSV, since
  Run 1's sample never exercised the exact ambiguity the `TryParseExact`
  fix was meant to resolve.
- **Planned but not achieved:** same model (Gemini 3.1 Pro) for both runs,
  to isolate the effect of instruction changes from any model difference.
  Quota exhaustion made this impossible in practice (see below).

### Initial prompt (same opener as Run 1)
> "Read instruction.md in this directory first, in full, before doing
> anything else. Then begin the task it describes. Work only within this
> directory — do not touch anything outside it, and do not commit or push
> to git. Confirm you've read instruction.md and summarize back to me: the
> two open design decisions, the malformed-row categories you'll handle,
> and the date-parsing requirement — before you start writing code."

Unlike Run 1, this confirmation didn't need a follow-up correction — every
element of the tightened `instruction.md` was captured correctly on the
first pass.

### Forced model changes (quota-driven, not planned)
Run 2 ended up as a **four-model story**, driven entirely by quota limits
rather than choice — noted plainly rather than presented as cleaner than
it was:

1. **Gemini 3.1 Pro** hit its quota limit before Run 2 could even start.
2. Switched to **Claude Opus 4.6 (Thinking)** to run the design/plan phase
   and begin implementation.
3. Antigravity appeared to hang after a successful `dotnet build` — turned
   out to be **Opus's quota exhausted mid-task**, not an extension bug.
   Discovered the quota is *shared across all models* on the Google AI
   Plus plan (Gemini, Opus, Sonnet, GPT-OSS all showed the same warning
   simultaneously) — a workspace/tooling constraint, not per-model, and
   not obvious until hit.
4. Switched to **Claude Sonnet 4.6 (Thinking)**, which also hit the same
   shared wall shortly after.
5. Switched to **OpenAI Codex** (VS Code extension, "5.6 Terra" model,
   High reasoning) via an existing ChatGPT Go subscription — a completely
   separate tool/account. Confirmed Go does include Codex extension
   access, despite the extension's marketplace listing naming only
   Plus/Pro/Business/Edu/Enterprise and a "Get Plus" upsell button
   visible in the panel.

**Consequence:** Run 2's instruction changes and its model changes are
confounded — differences from Run 1 could stem from either factor and
can't be cleanly isolated. Treated as a qualitative comparison, not a
controlled one.

### Model comparison observation (Opus vs. Gemini, User decision)
Opus independently proposed rejecting a missing/blank `User` rather than
defaulting to a placeholder, correctly reasoning from `GetTodoItems`'
exact-match filtering — the same trap Gemini fell into in Run 1 and needed
a direct correction to avoid. Since this specific decision was left open
in both runs (unlike the instruction tightening elsewhere), this is a
fairly clean signal on reasoning depth for this particular judgment call.

### Cross-model verification catch (Codex checking Opus's work)
Codex was explicitly instructed to verify the prior agent's work rather
than trust it, and found:
- `DECISIONS.md` claimed a `TodoApi.Tests` project had been created —
  **it did not exist** in the working directory. Likely cause: Opus's
  session was cut off by quota exhaustion mid-task, and the log was
  written aspirationally before the step actually ran.
- `Enum.TryParse` accepted **out-of-range numeric `Priority` values**
  (e.g. `3`), contradicting both `DECISIONS.md` and the error text, which
  claimed only `0`/`1`/`2` were valid. A real validation gap, not just
  documentation drift.

This validates a concern raised before Run 1 even started: verify
documentation against actual code, don't trust it at face value. A second
model, independently checking, is what caught it here.

### Repeated pattern: claimed-vs-actual gap under Codex/GPT (High)
During test-writing, Codex reported changes as complete **three separate
times** (the `Enum.TryParse` fix, missing `User`/`DueDate` test coverage,
splitting the test into separate `[Fact]` methods) that weren't actually
reflected in the file when the raw content was requested directly. Each
time, asking for the raw file — not a summary — is what caught the gap.
On the fourth request, the raw file finally matched the claim exactly
(8 separate `[Fact]` methods, correct coverage, correct formatting).

This **mirrors the `DECISIONS.md` gap seen under Opus** earlier in the
same run — the "summary outpaces actual state" failure mode isn't
isolated to one model or vendor. Verifying raw artifacts instead of
trusting natural-language status reports was the single most
load-bearing review habit across this entire run.

### Scope/standard tension: `App.test.js`
Codex rewrote the pre-existing (broken/outdated) `App.test.js` rather than
leaving it untouched and adding a separate test file — technically
conflicting with `instruction.md`'s "no existing tests removed" line.
Unlike Run 1's `PutTodoItem` reformatting (pure scope creep, no
justification), this had a real reason: the old test couldn't run against
the current, Auth0-driven app. But the agent made that call unilaterally
rather than surfacing the conflict between two of my own instructions
("keep tests working" vs. "don't remove existing tests") for a decision.

### Quota/cost observation
Codex/GPT at "5.6 Terra High" consumed roughly **57% of a monthly ChatGPT
Go quota allotment** (93% → 36%) completing the remaining implementation,
test-writing, and multiple correction rounds. Worth noting as a real cost
axis alongside capability — not directly comparable to Gemini/Opus/Sonnet,
which hit a hard weekly wall rather than reporting a percentage.

### Automated verification (independently run, not agent-reported)
- **Backend:** `dotnet test` in `TodoApi.Tests` → **8/8 passed**, confirmed
  by running it personally. First claim this run that held up without
  needing a correction round.
- **Frontend:** `CI=true npm test -- --watchAll=false` → **2/2 passed**
  (`TodoImport.test.js`, `App.test.js`), confirmed directly.

### Pre-existing repo defects discovered during manual verification
- **ESLint blocks `npm start` entirely:** nearly every pre-existing
  component (`Auth.js`, `EnhancedTable.js`, `TodoFormWithFormik.js`,
  `ResponsiveDrawer.js`, etc.) has long-standing `react/prop-types` and
  `no-debugger` violations that fail webpack's build. Only one new
  violation (`TodoImport.js` missing an `onImport` prop-type) was actually
  introduced by this feature. Invisible to every prior check, since
  `npm test` (Jest) never routes through webpack's ESLint plugin the way
  the dev server does. Bypassed with `DISABLE_ESLINT_PLUGIN=true npm start`
  for verification purposes only — not fixed, as it's out of scope.
- **Stale hardcoded API port:** `TodoService.js` hardcodes
  `http://localhost:8888/api/TodoItems`, which matches no launch profile
  (actual: `5000`/`5001`). A pre-existing bug, unrelated to this feature,
  found only because manual end-to-end verification was actually
  attempted. Temporarily patched locally to `5000` for testing, then
  reverted.
- **No real SQL Server running:** all automated backend tests use EF
  Core's InMemory provider — none exercise the actual SQL Server
  connection path configured in `appsettings.json`. The app's ability to
  genuinely *run* was never verified until the manual step, when it
  crashed on startup with a SQL connection failure. Resolved by starting
  a matching SQL Server container via Docker (`mssql/server:2019-latest`,
  matching the `sa`/`StrongPassword#777` credentials already in
  `appsettings.json`).
- **Auth0 login impossible from a fresh clone:** the Auth0 domain is an
  intentional placeholder (`yourdevdomainhere.auth0.com`) — the real
  domain lives only in local, gitignored config never committed to the
  public repo. Full UI login therefore isn't possible in this
  environment; pivoted to testing the endpoint directly via Swagger,
  which the import endpoint supports since it's `[AllowAnonymous]`.

### Manual verification — row-by-row result

Uploaded `todos-import-sample.csv` via Swagger
(`POST /api/TodoItems/import`, `http://localhost:5000/`) — its
`[AllowAnonymous]` status was visually confirmed by its open padlock icon
versus every other endpoint's closed padlock.

**Result:** `{ totalProcessed: 12, imported: 6, failed: 6 }`

| Row | Content (summary) | Expected | Actual |
|---|---|---|---|
| 2 | Buy groceries — valid | ✅ import | ✅ imported |
| 3 | Finish quarterly report — valid | ✅ import | ✅ imported |
| 4 | Call the dentist — `09/20/2026` (`MM/dd/yyyy`) | ✅ import | ✅ imported |
| 5 | Blank `Name` | ❌ reject | ✅ "Name is required and cannot be blank." |
| 6 | `Completed: notabool` | ❌ reject | ✅ "not a valid boolean." |
| 7 | Blank `DueDate` | ❌ reject | ✅ "not a valid date." |
| 8 | Water the plants — valid (not a duplicate, since row 7's attempt was rejected first) | ✅ import | ✅ imported |
| 9 | `DueDate: 2026-13-40` (impossible date) | ❌ reject | ✅ "not a valid date." |
| 10 | Blank `User` | ❌ reject | ✅ "User is required and cannot be blank." |
| 11 | `Priority: Urgent` | ❌ reject | ✅ "not valid." |
| 12 | `Priority: 2` (numeric) — valid | ✅ import | ✅ imported |
| 13 | `DueDate: 01/02/2026` (ambiguous, slash-separated) — valid | ✅ import | ✅ imported |

Every row landed exactly where expected. 12 processed, 6 imported, 6
failed — the math and the row-level reasoning both check out.

**Critical follow-up — did the ambiguous date actually resolve correctly?**
The `ImportResult` only reports success/failure, not stored values, so
"imported" alone doesn't prove `01/02/2026` was interpreted as January 2nd
rather than February 1st. Since `GET /api/TodoItems` requires Auth0 auth
(unavailable in this environment), queried the database directly:

```sql
SELECT Name, DueDate FROM TodoDatabase.dbo.TodoItems
WHERE Name = 'Update dependencies'
-- Result: 2026-01-02 00:00:00.0000000
```

**Confirmed: January 2nd.** The `TryParseExact` + `InvariantCulture` fix
genuinely works — not just claimed to work. This was the single most
important verification in the whole capstone, since it's the one row
whose correctness couldn't be established by reading code or trusting a
summary, only by checking the actual stored data.

Full end-to-end UI verification (including `User`-based visibility in the
authenticated app) wasn't possible given the Auth0 constraint above.
Direct API + database inspection fully exercised the feature's actual
logic instead.

---

## 4. Run 1 vs. Run 2 — Summary

- Tightening `instruction.md` up front (explicit malformed-row categories,
  deterministic date parsing, scope discipline, self-verification
  requirement) measurably reduced the number of gaps discovered only
  during plan review — Run 2's initial confirmation needed no correction
  round, unlike Run 1's.
- Run 2 was **not** a clean instruction-only comparison: a four-model
  pivot (Gemini → Opus → Sonnet → Codex) was forced entirely by quota
  limits, so capability differences and instruction changes are
  confounded and can't be cleanly separated.
- **The most consistent finding across both runs, independent of model:**
  agents reliably report work as complete, or documentation as accurate,
  before it has actually been verified against the real code. This
  happened under Gemini (Run 1, `TryParse` claim), Opus (Run 2, false
  `TodoApi.Tests` claim), and Codex (Run 2, three separate
  under-delivered fix claims in a row). **Asking for raw file content
  instead of accepting natural-language summaries was the single most
  effective review technique across the entire capstone** — it caught
  every real gap found in either run.
- **Secondary finding:** reasoning depth seems to matter specifically for
  judgment-heavy decisions (Opus avoided the `User`-placeholder trap
  unprompted, where Gemini did not) — but didn't prevent the
  claimed-vs-actual gap pattern, which showed up under both Opus and the
  higher-reasoning Codex tier. Thoroughness/honesty-of-reporting appears
  to be a distinct axis from raw reasoning capability.

### Note on verification depth: Run 1 vs. Run 2
Run 1 was verified at the code/plan/test-suite level only; Run 2 received
an additional live manual pass (Swagger + direct DB query) because Run
2's sample CSV introduced the one test case (an ambiguous date) that code
review alone couldn't settle. Not re-running Run 1 end-to-end was a
deliberate scoping choice, not an oversight — Run 1's actual defects
(`User`-placeholder trap, stale decision log, scope creep) were all found
and fixed at the code level, which was sufficient for those particular
issues.