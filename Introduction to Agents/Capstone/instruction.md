Pinned commit: be1efe8060b49f42afcb61bb78643236064330f4

Goal: Add CSV import for todos — a user selects a .csv file in the React UI, and its
rows are parsed, validated, and appended to the existing todo list (both in the UI
state and persisted via the API). A sample CSV is provided at
todos-import-sample.csv — it includes intentionally malformed rows; decide how to
handle each and document your reasoning.

Open design decisions — resolve and document both:
- TodoItem.User is [Required], but it was previously populated from an Auth0 claim
  that no longer exists on an anonymous endpoint. Decide how each imported row's
  User gets populated (CSV column, request parameter, or default) and document the
  choice.
- TodoItem.Priority has no JsonStringEnumConverter registered, so it serializes
  numerically (Low=0, Normal=1, High=2) by default. Decide whether the CSV's
  Priority column accepts the enum name, its numeric value, or both, and document
  the choice.

Sources: Use the existing Todo model/entity, the existing TodoApi controller(s), and
the existing React todo-list component(s) as the pattern to follow.

Out of scope — do not modify: TodoApi/HasScopeHandler.cs, TodoApi/HasScopeRequirement.cs,
azure-pipelines.yml, and the [Authorize(...)] attributes / Auth0 config in Startup.cs.
The new import endpoint should use [AllowAnonymous]; existing authorized endpoints
must be left untouched.

Environment note: The pinned commit targets netcoreapp3.1 (TodoApi.csproj), but
only the .NET 6.0 runtime is installed on this machine (confirmed via
`dotnet --list-runtimes`) — no 3.1 runtime is available. As a first step, retarget
TodoApi.csproj to net6.0 and bump any package references that require it for
compatibility (EF Core, JwtBearer, Swashbuckle, etc.). Note exactly which packages
you changed and why in DECISIONS.md. Do this before anything else, since nothing
else can be verified as actually running until the project builds and runs on
this machine.

Output:
- A new backend endpoint (e.g. POST /api/todos/import) plus a frontend file-upload
  control that calls it, following the existing project structure — not a new
  architecture.
- At least one frontend test, following the existing pattern in src/App.test.js
  (@testing-library/react).
- At least one backend test. No C# test project currently exists — create one
  following standard .NET conventions (e.g. xUnit), and document that decision in
  the log.
- A decision log written to DECISIONS.md at the repo root: what you inspected, what
  you decided (including the two open design decisions above), and anything you
  were unsure about, as you go.

Standard (accept/reject):
- C# single-line if/foreach — no braces, body on the next indented line (never inline)
- Multi-arg method signatures — one parameter per line, closing paren on the last
  param's line
- var used throughout; no manual alignment of assignments
- /// triple-slash XML docs on new public members; never // for doc tags
- No existing comments, docs, or tests removed
- Malformed rows (bad dates, missing columns, duplicates) must be handled
  explicitly, not silently ignored or crashed on — state how in DECISIONS.md