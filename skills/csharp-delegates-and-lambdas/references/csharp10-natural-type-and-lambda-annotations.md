# Natural Type, Explicit Return Types, and Attributes on Lambdas (C# 10 / .NET 6)

C# 10 (November 2021) closed a gap every earlier tier lived with: a lambda or method group with no
delegate-typed target context (a bare `var`, for instance) previously had nowhere to infer a type
from. C# 10 gives lambdas and method groups a **natural type**, lets a lambda declare its return
type explicitly when the compiler can't infer one, and allows attributes on lambda parameters and
on the lambda itself.

## Syntax

```csharp
var add = (int x, int y) => x + y;              // natural type: Func<int, int, int>
var describe = int (int x) => x.ToString();      // explicit return type
var log = ([NotNull] string message) => Console.WriteLine(message); // attribute on a parameter
```

## Basic use case: natural type from `var`

```csharp
var isEven = (int n) => n % 2 == 0; // inferred as Func<int, bool> — usable anywhere a delegate is

Console.WriteLine(isEven(4)); // true
```

The compiler picks `Func<>`/`Action<>` when the lambda's shape fits one of those families; it
synthesizes an anonymous compiler-generated delegate type instead when it doesn't — for example, a
lambda with a `ref`/`out`/`in` parameter, or more parameters than `Func<>`/`Action<>` cover (this
skill's baseline covers up to 16, per
[csharp4-variance-and-extended-func-action.md](csharp4-variance-and-extended-func-action.md), so the
synthesized-type case is rare in practice). A method group gets the same treatment:

```csharp
static bool IsPositive(int n) => n > 0;

var isPositive = IsPositive; // natural type: Func<int, bool>, inferred from IsPositive's signature
```

## Advanced use case: explicit return type resolving ambiguity in a generic context

```csharp
public static TResult Invoke<TResult>(Func<TResult> factory) => factory();

// without an explicit return type, `() => default` can't tell the compiler what TResult is
var result = Invoke(object () => default!); // explicit return type pins TResult to object
```

An explicit return type is written *before* the parameter list, and once present, the parameter
list must be parenthesized even for a single parameter (`int (int x) => ...`, not `int x => ...`).
This mirrors ordinary method syntax and mainly matters when the compiler otherwise can't infer a
return type from the lambda body alone (a `default` literal, a ternary between incompatible-looking
branches, or a generic factory method like the one above).

## Requirements and restrictions

- Natural typing only fills in a delegate type when none is already available from a target-typed
  context (a declared parameter type, a field type, an existing `Func<>` variable) — it doesn't
  override an explicit target type.
- Attributes on a lambda are emitted onto the compiler-generated method backing the lambda, so
  they're visible via reflection on the resulting `Delegate`'s `MethodInfo`, same as an attribute on
  an ordinary method.
- `params` on a lambda parameter still requires the parameter's type to be written explicitly (this
  particular restriction persists even after later tiers relax other modifier-related typing rules
  — see [csharp14-lambda-parameter-modifiers.md](csharp14-lambda-parameter-modifiers.md)).

## Fallback

On C# 9.0 and earlier, a lambda assigned to `var` doesn't compile — give it an explicit
`Func<>`/`Action<>`/delegate-typed target instead of `var`, and drop the explicit return-type syntax
and parameter attributes (neither exists yet). See
[csharp9-static-lambdas-and-discard-parameters.md](csharp9-static-lambdas-and-discard-parameters.md).
