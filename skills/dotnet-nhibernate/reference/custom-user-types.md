# Custom User Types (`IUserType` / `ICompositeUserType`)

Use a custom user type whenever a domain concept doesn't map 1:1 to a simple column — enums stored as strings, value objects (`Money`, date ranges), JSON columns, encrypted columns. This is one of the more obscure corners of NHibernate's API and easy to get subtly wrong from general knowledge alone — the interface members have specific mutability/equality semantics that don't match intuition.

## Three overlapping ways to map a "not just a scalar column" value — pick deliberately

This file, `Component(...)` in `mapping-conventions.md`, and composite keys in `composite-keys.md` all touch multi-property/value-object mapping, and it's easy to reach for the wrong one:

| Mechanism | Use when | Downside |
|---|---|---|
| `Component(...)` | A value object spanning multiple columns, with no custom conversion logic needed — the components map straightforwardly (a `string`, an `int`, etc.) | No control over conversion; can't do custom serialization or validation at the mapping layer |
| `IUserType` | A **single column**, but the conversion between column value and domain type needs custom logic (enum-to-string, encrypted string, JSON blob) | Only handles one column; if the domain concept genuinely needs several columns, this is the wrong tool |
| `ICompositeUserType` | A value object spanning **multiple columns** AND needing custom conversion logic on top (e.g. a `Money` type where the currency code needs validation/normalization on the way in) | Most code to write and most surface area to get equality/mutability wrong (see below) — don't reach for this if plain `Component(...)` would do |

Default to `Component(...)` first for a plain multi-column value object; only escalate to `ICompositeUserType` when `Component` genuinely can't express the conversion you need. This ordering matters because `ICompositeUserType` carries the same equality/`IsMutable`/deep-copy correctness burden as `IUserType` (below), which is unnecessary complexity for something `Component` would map cleanly.

## When to use which (single-column custom types)

| Situation | Use |
|---|---|
| Single column, custom conversion logic (enum ↔ string, encrypted string, JSON-serialized single value) | `IUserType` |
| Value object spanning **multiple columns** on the owning table (e.g. `Money { decimal Amount; string CurrencyCode; }` as two columns) | `ICompositeUserType` |
| Simple enum-to-string/int with no other logic | Consider `Map(x => x.Status).CustomType<MyEnum>()` shorthand first — only write a full `IUserType` if you need control beyond what the built-in enum handling gives you (e.g. a custom string representation that doesn't match the enum member names) |

## `IUserType` — things worth getting right

The interface requires implementing equality and mutability semantics explicitly — don't assume default `Equals`/reference equality is fine:

- **`Equals(object x, object y)`** must handle `null` on either side and should compare by value, not reference, for the wrapped domain type — this affects NHibernate's dirty-checking. Get this wrong and entities appear "dirty" (triggering unnecessary UPDATEs) or, worse, appear unchanged when they actually changed.
- **`IsMutable`** — set this correctly. If the underlying domain type is immutable (most value objects should be), set `IsMutable => false`; this lets NHibernate skip some defensive copying. Getting this wrong when the type actually IS mutable can cause dirty-checking to miss real changes.
- **`DeepCopy(object value)`** — used for the first-level cache / dirty-checking snapshot. For an immutable type, this can often just return the value itself; for a mutable one, it must produce a genuine independent copy or dirty-checking breaks.
- **`SqlTypes`** — declare the actual underlying DB column type(s) precisely; a mismatch here (e.g. declaring `String` for what's actually stored as `NVARCHAR(50)` vs an unbounded type) can cause silent truncation or provider-specific query translation issues.
- **`NullSafeGet` / `NullSafeSet`** — must handle DB `NULL` explicitly (via `rs.IsDBNull(...)` or the NHibernate equivalent) — a naive implementation that doesn't check for `DBNull` will throw on legitimately nullable columns rather than returning your domain type's "empty"/null representation.

See `templates/user-type.cs` for a skeleton with all of these correctly stubbed — start from that rather than free-handing the interface, since a plausible-looking but subtly wrong implementation (e.g. reference-equality `Equals`) will not throw immediately; it causes intermittent, hard-to-reproduce dirty-checking bugs that surface much later.

## `ICompositeUserType`

Same equality/mutability concerns as above, plus:
- `GetPropertyNames()` / `GetPropertyValue(...)` must stay in sync with the actual column mapping order declared where the type is used (`.Columns("Amount", "CurrencyCode")`) — a mismatch here silently swaps values between columns rather than throwing.
- Composite types can be queried against by sub-property in HQL/QueryOver (e.g. filtering by `Money.CurrencyCode`) but this requires the composite type to be registered correctly — verify with an actual query against it in a test rather than assuming it works from the mapping alone.

## Enum mapping specifics

Default convention for this team: map enums as their string name, not their integer value, via `CustomType<TEnum>()` or an explicit `IUserType` if custom string representations are needed. Reasoning: integer-valued enums are fragile against reordering or inserting new members in the middle of the enum definition — a string-mapped enum survives that refactor safely, an int-mapped one silently corrupts existing data's meaning. Flag any new enum mapping using the integer form in review unless there's a specific, stated performance/storage reason.

## JSON columns

For a property that should be stored as a JSON blob in a single column (common for flexible/extensible metadata fields), a small `IUserType` wrapping `System.Text.Json` serialization is the standard approach — `NullSafeGet`/`NullSafeSet` handle serialize/deserialize, `Equals` should do a value comparison of the deserialized shape (or of the raw JSON string if ordering/formatting is guaranteed stable) rather than reference equality. Don't map a JSON column as a plain `string` and serialize/deserialize manually at every call site — that scatters the concern across the codebase instead of centralizing it in the type.
