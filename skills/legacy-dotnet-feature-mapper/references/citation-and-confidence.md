# Citation and Confidence Tagging

Every factual claim in a feature doc or shared-component doc - every business rule, permission check, DB interaction, failure state - must carry both:

1. **A citation**: file path + line number(s) for code (`Billing/OrderApproval.aspx.cs:112-118`), or SQL object name + line(s) within its definition script for DB objects (`usp_ApproveOrder.sql:34`).
2. **A confidence tag**: one of the three below.

## The three tags

- **`verified in code`** - Directly read and confirmed in the source. This includes confirming an *absence* (e.g., "no authorization check found" after reading the full handler is itself `verified in code`, not a guess).
- **`inferred from naming`** - A reasonable conclusion drawn from naming conventions, patterns, or partial evidence, but not fully confirmed by reading the actual logic (e.g., a method called `ValidateCreditLimit` that is called but whose body wasn't fully traced because it's out of scope, or whose logic branches in a way that wasn't fully resolved).
- **`unverified assumption`** - Static analysis genuinely could not resolve this. Always include a short reason: what specifically blocked verification (dynamic SQL with an unresolvable string, reflection-based method dispatch, a config/feature-flag value only known at runtime, a third-party/compiled dependency with no available source).

## Formatting

Inline in prose or table cells:

```
Orders over $10,000 require a second approval - `OrderApproval.aspx.cs:88` `(verified in code)`
```

```
Assumed to check inventory availability before approval, based on method name `CheckStock` - `(inferred from naming, OrderService.cs:201 - body not fully traced)`
```

```
Approval threshold may be overridden per-customer via a config value not resolvable statically - `(unverified assumption - value read from AppSettings key built by string concatenation, OrderService.cs:340)`
```

## Rules

- Never state a business rule, permission, or behavior without a citation - if you can't cite it, it goes in the Open Questions section instead, tagged `unverified assumption`, rather than being stated as fact.
- Don't upgrade a tag to look more confident than the evidence supports - `inferred from naming` should stay that way unless you actually trace the logic and confirm it, at which point it becomes `verified in code`.
- One claim can have multiple citations if it spans several files/objects - list all of them.
