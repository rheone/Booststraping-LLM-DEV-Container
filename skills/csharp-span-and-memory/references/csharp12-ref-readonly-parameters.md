# `ref readonly` parameters (C# 12 / .NET 8, November 2023)

C# 12 shipped November 2023 alongside .NET 8. It adds `ref readonly` as a parameter modifier —
distinct from both `in` (C# 7.2) and `ref readonly` *returns* (also C# 7.2, on the return side).
Before this release, `in` was the only by-reference, non-mutating parameter modifier, and it has a
gap this tier closes: `in` silently accepts an ordinary by-value argument at the call site (the
compiler makes a temporary copy and passes that by reference instead), which can hide a performance
mistake the caller never intended.

## Syntax

```csharp
public static double Dot(ref readonly Vector3D a, ref readonly Vector3D b) =>
    a.X * b.X + a.Y * b.Y + a.Z * b.Z;
```

```csharp
Vector3D v1 = new(1, 2, 3);
Vector3D v2 = new(4, 5, 6);
double result = Dot(in v1, in v2); // caller must pass by reference explicitly (or ref readonly / ref)
```

## Basic use case: forcing callers to pass an existing variable by reference, not a copy

```csharp
public readonly struct Matrix4x4 { /* 16 floats */ }

public static Matrix4x4 Multiply(ref readonly Matrix4x4 a, ref readonly Matrix4x4 b)
{
    // reads a and b; never assigns through either parameter
    // ...
}

Matrix4x4 m1 = LoadMatrix();
Matrix4x4 m2 = LoadMatrix();
Matrix4x4 result = Multiply(in m1, in m2); // explicit `in` required at call site
```

Unlike `in Matrix4x4 m` parameters, `ref readonly Matrix4x4 m` cannot be satisfied by a bare
expression like `Multiply(LoadMatrix(), m2)` — there's no addressable variable for the compiler to
create an implicit temporary from, so a call site accidentally passing a freshly computed value (and
therefore getting a silent copy under `in`) becomes a compile error instead under `ref readonly`,
surfacing the mistake immediately rather than leaving a hidden copy in a hot path.

## Advanced use case: an API that intentionally supports both `in` and `ref readonly` callers

```csharp
public static double SumOfSquares(scoped in Vector3D v) => v.X * v.X + v.Y * v.Y + v.Z * v.Z;

// callable either way — `ref readonly` parameters are call-site compatible with `in` callers
// and vice versa, since both compile to the same "pass by reference, don't mutate" IL shape
double a = SumOfSquares(in someVector);
double b = SumOfSquares(someVector); // still legal with `in` on the parameter — not with `ref readonly`
```

A library choosing between `in` and `ref readonly` on a parameter is choosing how strict to be
about accidental copies at *its own* call sites, not changing what kind of argument the callee can
receive — both still receive by reference.

## Requirements and restrictions

- The caller must pass an existing, addressable variable (a local, field, array element, or another
  `ref`/`in`/`ref readonly` value) — a literal, a method call result, or any other non-addressable
  expression is rejected at the call site, unlike `in`.
- `ref readonly` is allowed on indexer parameters (like `in`, unlike plain `ref`) but disallowed on
  operator parameters (like `ref`, unlike `in`) — the same asymmetric rule set `in` already had,
  extended to this modifier.
- Overloading a method with both an `in` and a `ref readonly` version of the same parameter type is
  not meaningfully different overload resolution-wise — don't do it; pick one modifier per API.

## Fallback

Below C# 12, `in` is the closest available modifier — it accepts by-reference arguments and
prevents mutation inside the callee, but cannot reject a call site that passes a non-addressable
expression (the compiler silently materializes a temporary copy instead of erroring):

```csharp
public static double Dot(in Vector3D a, in Vector3D b) =>
    a.X * b.X + a.Y * b.Y + a.Z * b.Z;

// pre-C#12: this compiles and silently copies the freshly constructed Vector3D into a temporary
double result = Dot(new Vector3D(1, 2, 3), v2);
```

If avoiding that silent-copy risk matters on an older target, the only enforcement available is
code review or an analyzer rule flagging non-variable arguments to `in` parameters — the language
itself has no way to require it before C# 12.
