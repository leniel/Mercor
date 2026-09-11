# Decisions Log

## 1. Upgrading to .NET 6.0
- **Decision**: Upgraded `TodoApi.csproj` `TargetFramework` from `netcoreapp3.1` to `net6.0`.
- **Reasoning**: Only .NET 6.0 runtime is installed on the target environment.
- **Package Updates**: Bumped Entity Framework Core, JwtBearer, and Swashbuckle packages to `6.0.0` (and `6.5.0` for Swashbuckle) to be compatible with .NET 6.0.

## 2. TodoItem.User CSV Import
- **Decision**: The `User` column in the imported CSV is strictly required. Any rows with a missing or blank `User` will be explicitly rejected, logged, and skipped while the rest of the rows continue processing.
- **Reasoning**: A `TodoItem` requires an exact match on `User` for it to be returned by `GetTodoItems` for the authenticated user. Defaulting to a placeholder value (e.g., "System") means those items will effectively be invisible/orphaned. Thus, the CSV file must contain the exact identity to match the importing user.

## 3. TodoItem.Priority CSV Import
- **Decision**: The CSV `Priority` column will accept both string enum names (e.g., "Low", "Normal", "High" - case-insensitive) and numeric values (0, 1, 2).
- **Reasoning**: While the model serializes numerically by default, accepting string representations makes it much more user-friendly for people creating or editing CSV files manually.

## 4. Test Framework
- **Decision**: Creating a new backend test project using `xUnit` and `Moq`.
- **Reasoning**: xUnit is the standard testing framework for modern .NET Core / .NET applications.

## 5. Duplicate Row Detection
- **Decision**: A row is considered a duplicate if it has the exact same `Name` and `User` as an existing record in the database, or as a previously processed valid row in the same CSV file. Duplicates will be rejected and logged as errors.
- **Reasoning**: Prevents accidental double-imports when a user imports a CSV multiple times or has copy-pasted rows.

## 6. Structural Integrity and Date Formatting
- **Decision**: Rows with missing columns or wrong column counts will be caught via `CsvHelper` exceptions and rejected. Dates must parse via `DateTime.TryParseExact` using `InvariantCulture` and an explicit array of accepted formats (`yyyy-MM-dd` and `MM/dd/yyyy`).
- **Reasoning**: Ensures that the data is well-formed and avoids localization parsing issues. Rejecting malformed dates strictly prevents garbage data or 1/1/0001 defaults.
