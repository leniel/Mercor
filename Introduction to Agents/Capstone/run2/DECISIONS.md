# Decisions Log

## 1. Retarget to .NET 6.0

- **What I inspected**: The pinned `TodoApi.csproj` targeted `netcoreapp3.1` and used 3.1.x package versions. The current project is targeted at `net6.0` and builds with the installed .NET 6 SDK.
- **Decision**: Retargeted `TargetFramework` to `net6.0`. Updated these existing package references for .NET 6 compatibility:
  - `Microsoft.AspNetCore.Authentication.JwtBearer` 3.1.1 → 6.0.0
  - `Microsoft.EntityFrameworkCore.Design` 3.1.1 → 6.0.0
  - `Microsoft.EntityFrameworkCore.InMemory` 3.1.1 → 6.0.0
  - `Microsoft.EntityFrameworkCore.SqlServer` 3.1.1 → 6.0.0
  - `Microsoft.Extensions.Options.ConfigurationExtensions` 3.1.1 → 6.0.0
  - `Microsoft.VisualStudio.Web.CodeGeneration.Design` 3.1.0 → 6.0.0
  - `Swashbuckle.AspNetCore` 5.0.0 → 6.5.0
- **Additional package changes**: Added `CsvHelper` 33.1.0 to parse CSV quoting and records safely. Removed `Microsoft.AspNetCore.Authorization` 3.1.1 because .NET 6 supplies it through the shared framework.
- **Reasoning**: The project must target an installed runtime before its import behavior can be built or tested.

## 2. TodoItem.User — Required from CSV

- **What I inspected**: `TodoItem.User` has `[Required]`, and the existing list endpoint filters todos by `User`.
- **Decision**: The CSV `User` column is required. Blank values are rejected with a row number and an explicit required-field error; there is no fallback user.
- **Reasoning**: A placeholder identity would create items no real user can retrieve.

## 3. TodoItem.Priority — Accept defined names and numeric values

- **What I inspected**: `TodoItem.PriorityEnum` defines `Low = 0`, `Normal = 1`, and `High = 2`; the API uses default numeric enum serialization.
- **Decision**: CSV priority accepts case-insensitive enum names and only the defined numeric values `0`, `1`, and `2`. Blank priority is a missing-required-field error; values such as `3` and `Urgent` are rejected.
- **Reasoning**: Names are readable for people editing a CSV, while defined numbers match the API representation. `Enum.IsDefined` is required because `Enum.TryParse` otherwise accepts undefined integers.

## 4. Duplicate row detection

- **Decision**: A duplicate has the same `User` and `Name` as an existing database todo or an earlier accepted CSV row. It is rejected with its row number and reason.
- **Reasoning**: This prevents repeat imports and duplicate rows within one upload.

## 5. DueDate parsing

- **Decision**: Dates are parsed only with `DateTime.TryParseExact`, `CultureInfo.InvariantCulture`, and the formats `yyyy-MM-dd` and `MM/dd/yyyy`. Blank, impossible, and unsupported dates are rejected.
- **Reasoning**: Explicit invariant formats prevent culture-dependent interpretation of ambiguous day/month values.

## 6. Required fields and structural CSV validation

- **Decision**: `User`, `Name`, `Completed`, `DueDate`, and `Priority` each have explicit blank-value validation and a row-numbered error. Every parsed row is also compared with the header's column count, so both missing and extra columns are rejected. CsvHelper parser exceptions for malformed records are also reported per row.
- **Reasoning**: CsvHelper can parse extra fields unless the application explicitly enforces the expected record width; checking the count prevents such rows from being silently imported.

## 7. Backend test framework

- **What I inspected**: No C# test project existed in the pinned repository.
- **Decision**: Created `TodoApi.Tests`, a net6.0 xUnit project referencing `TodoApi` and using EF Core InMemory for isolated controller tests.
- **Reasoning**: xUnit is a standard .NET test framework, and InMemory allows the import endpoint to be tested without a SQL Server dependency.

## 8. Frontend import flow

- **What I inspected**: The existing todo screen is the class component `src/components/todo/Todo.js`, and `src/service/TodoService.js` centralizes API calls.
- **Decision**: Added a `.csv` file selector in `TodoImport`, used by `Todo`. `TodoService.importTodos` posts the file as multipart form data to `/api/TodoItems/import`. After a successful import, `Todo` re-fetches todos and replaces its `todos` state with the persisted list, then shows imported/rejected counts.
- **Reasoning**: The import response reports aggregate results rather than todo entities. Refreshing through the established list API keeps UI state aligned with the persisted data and existing UI pattern.

## 9. Test coverage

- **Decision**: `TodoImport.test.js` uses React Testing Library to verify that selecting a CSV file calls the import handler with that file. `TodoItemsControllerTests` uses separate test methods for valid rows, duplicates, blank User, invalid DueDate, blank Completed, blank Priority, undefined numeric priorities, and extra columns. Added `@testing-library/dom` 10.0.0 because the existing `@testing-library/react` package requires it as a peer dependency. Updated `src/setupTests.js` to use the current Jest DOM v6 import path, and updated the obsolete App smoke test to assert the current authentication-loading UI.
- **Reasoning**: These tests cover the frontend upload handoff and the backend feature's critical validation and persistence behavior.

## 10. Verification

- **Result**: `dotnet build TodoApi/TodoApi.csproj --no-restore` completed with zero warnings and errors. `dotnet test TodoApi.Tests/TodoApi.Tests.csproj --no-restore` passed 8/8 tests. `CI=true npm test -- --watchAll=false` passed 2/2 tests.
- **Decision-log reconciliation**: Rechecked this log against `TodoItemsController`, `ImportResult`, `TodoImport`, `Todo`, `TodoService`, and `TodoApi.Tests`. The package, validation, UI-state, and test-project descriptions above match the implemented code.
