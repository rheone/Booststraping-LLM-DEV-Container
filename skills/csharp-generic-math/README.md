# C# Generic Math

Helps you write numeric algorithms that work across `int`, `long`, `double`, `decimal`, and custom
number types without duplicating the logic per type, using `INumber<T>` and the wider .NET numeric
interface hierarchy built on static abstract/virtual interface members.

## When to reach for it

- Writing an algorithm (a sum, an average, a clamp) that should work for any number-like type
- Deciding which numeric interface (`INumber<T>`, `IFloatingPoint<T>`, `IBinaryInteger<T>`, ...) to constrain a generic parameter to
- Implementing a custom numeric type, such as a `Fraction` or fixed-point number, against these interfaces
- Writing or reviewing a static abstract or static virtual interface member
- Testing a generic-math algorithm across several concrete numeric types at once

## Using it

This skill is model-invoked: it fires automatically when you're writing a numeric algorithm generic
over `INumber<T>`, choosing a numeric interface constraint, or implementing a custom numeric type.

## What it covers

| Topic | Reference |
| --- | --- |
| Writing methods/types generic over `INumber<T>` | [references/writing-generic-numeric-algorithms.md](references/writing-generic-numeric-algorithms.md) |
| The numeric interface hierarchy and how the pieces relate | [references/numeric-interface-hierarchy.md](references/numeric-interface-hierarchy.md) |
| Static abstract/virtual interface members | [references/static-abstract-members.md](references/static-abstract-members.md) |
| Implementing the interfaces on a custom numeric type | [references/implementing-a-custom-numeric-type.md](references/implementing-a-custom-numeric-type.md) |
| Testing a generic-math algorithm or custom numeric type | [references/testing.md](references/testing.md) |

## Example prompts

- "Write a `Sum<T>` method that works for `int`, `double`, and `decimal` without overloads."
- "Which numeric interface should I constrain this generic method to if it only needs comparison and addition?"
- "Implement `INumber<T>` on my custom `Fraction` type."
