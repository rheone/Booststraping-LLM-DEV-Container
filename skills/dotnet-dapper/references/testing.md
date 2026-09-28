# Testing

## How to test it

Code built on Dapper is, in essence, "SQL text plus a mapping" wrapped in a method — the meaningful
correctness question is whether that SQL, run against a real schema, returns and maps what the
calling code expects. That's not something a mock of `IDbConnection` can verify: mocking the
connection only proves your code called `Query`/`Execute` with the arguments you told the mock to
expect, never that the SQL text is valid, that it joins the right tables, or that the parameter names
in the SQL match the parameter object's property names (a mismatch here doesn't throw — an
unmatched named parameter is simply left unbound, which most providers then reject or misinterpret
at execution time, not at the mock layer).

**Run Dapper-backed queries against a real (or realistically disposable) database in tests.** A
throwaway SQLite database, a containerized instance of the real target engine, or a shared test
database reset between runs all give you a genuine SQL parser and query planner to validate against.
Seed known rows, call the method under test, and assert on the mapped result — this is the only way
to catch a typo'd column name, a wrong `JOIN` condition, or a `splitOn` mismatch (see
[multi-mapping-and-multiple-results.md](multi-mapping-and-multiple-results.md)), all of which compile
fine and fail only at execution or, worse, only in the shape of subtly wrong data.

**Reserve mocking `IDbConnection` for testing the calling code's control flow, not the SQL.** If the
unit under test is "does this service call the repository method and handle its result/exception
correctly," a fake repository (not a mocked `IDbConnection`) is the right seam — mock at the
repository interface boundary your own code defines, one layer above Dapper, rather than trying to
intercept Dapper's extension methods directly (they're static extension methods over `IDbConnection`
and don't offer a natural interception seam for a mocking library in the first place).

**Test parameter binding explicitly for anything built with `DynamicParameters` conditionally.** A
method that adds parameters based on runtime conditions (an optional filter, a nullable value) is
easy to get wrong in a way a single happy-path test won't catch — write one test per branch of the
conditional parameter logic, asserting the query returns the right filtered rows.

## Most likely scenarios

**Testing a repository method with a straightforward parameterized `SELECT`.** Seed a disposable
database with rows that should and shouldn't match the filter, call the method, and assert the
returned set contains exactly the expected rows — this single pattern covers the majority of
Dapper-backed query code.

**Testing a multi-mapped join.** Seed both sides of the join with related and unrelated rows (an
order with no matching customer, if that's a real case your schema allows) and assert the mapped
object graph is attached correctly — this is where a `splitOn` or column-order mistake actually
surfaces, since it produces plausible-looking but wrong data rather than an exception.

**Testing a stored procedure call with output parameters.** Call the procedure against a real
database, then assert on the value read back from the `DynamicParameters` instance via `Get<T>` —
this exercises the full round trip a mock could never simulate meaningfully, since the output value
is computed by the procedure itself.
