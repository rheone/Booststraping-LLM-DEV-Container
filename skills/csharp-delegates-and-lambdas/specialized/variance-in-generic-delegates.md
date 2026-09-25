# Variance in Generic Delegates

[references/csharp4-variance-and-extended-func-action.md](../references/csharp4-variance-and-extended-func-action.md)
covers where `in`/`out` variance annotations on generic delegates came from (C# 4.0). This file
works through the two BCL delegate families' variance in practice — `Func<>`'s covariant `TResult`,
`Action<>`'s contravariant parameters — and what happens designing a custom variant generic
delegate.

## Basic: `Func<>`'s covariant `TResult`

```csharp
public class Animal { }
public class Dog : Animal { }

Func<Dog> createDog = () => new Dog();
Func<Animal> createAnimal = createDog; // legal: TResult is covariant — a more-derived producer
                                        // can stand in for a less-derived one
Animal result = createAnimal();        // actually a Dog, referenced through the less-derived type
```

`Func<out TResult>`'s (and every arity's) `TResult` is covariant because a caller of
`Func<Animal>` only ever *reads* the result — anything that can produce a `Dog` can satisfy "give
me an `Animal`."

## Basic: `Action<>`'s contravariant parameters

```csharp
Action<Animal> handleAnimal = animal => Console.WriteLine($"handling {animal.GetType().Name}");
Action<Dog> handleDog = handleAnimal; // legal: T is contravariant — a consumer of the less-derived
                                       // type can stand in for a consumer of the more-derived type

handleDog(new Dog()); // handleAnimal's body only ever reads members Animal already guarantees,
                       // so it can safely accept the more-specific Dog argument
```

`Action<in T>`'s `T` is contravariant because a caller of `Action<Dog>` only ever *supplies* the
argument — anything that knows how to consume any `Animal` already knows how to consume a `Dog`.

## Advanced: designing a custom variant generic delegate

```csharp
// covariant in the result, contravariant in the input — same shape as Func<in T, out TResult>
public delegate TOutput Converter<in TInput, out TOutput>(TInput input);

Converter<Dog, string> describeDog = dog => $"Dog: {dog.GetType().Name}";
Converter<Animal, object> describeAnimalAsObject = describeDog;
// legal: TInput narrows Animal->Dog (contravariant, accepts anything that can consume a Dog-or-less-
// derived input), TOutput widens string->object (covariant, produces anything assignable to object)
```

The compiler enforces the *position* check at the delegate's own declaration, not at each use site:
`TInput` may only appear in parameter positions, `TOutput` only in the return position. A type
parameter that needs to appear in both (for example, a `TryConvert(TInput input, out TOutput
result)`-shaped delegate where `TInput` also somehow needed covariance) can't be made variant —
that's the same "mixed position" restriction interfaces face, and the reason `IComparer<in T>`
exists as `in`-only rather than attempting both directions.

## Advanced: variance and multicast composition

```csharp
Action<Animal> pipeline = animal => Log(animal);
pipeline += (Dog dog) => AlertOnDog(dog); // does NOT compile — combining requires the SAME
                                           // delegate type, Action<Dog> is not Action<Animal>
```

Variance applies to **conversion** (assigning one delegate-typed value to a differently-typed
variable/parameter), not to `+=`/`Delegate.Combine` — combining two delegates into one multicast
delegate still requires both operands to already be the identical delegate type. Convert first,
then combine, if that's genuinely needed:

```csharp
Action<Animal> pipeline = animal => Log(animal);
Action<Dog> alertOnDog = dog => AlertOnDog(dog);
pipeline += (Action<Animal>)(animal => alertOnDog((Dog)animal)); // explicit re-wrap, not a variance conversion
```

## Fallback

Drop the `in`/`out` annotations from a custom delegate and it still compiles and works from C# 2.0
onward — callers just need an explicit cast or a manual re-wrap wherever the variance conversion
used to apply implicitly. `Func<>`/`Action<>` themselves don't exist before C# 3.0/C# 4.0 depending
on arity (see
[references/csharp3-lambdas-and-func-action.md](../references/csharp3-lambdas-and-func-action.md)
and
[references/csharp4-variance-and-extended-func-action.md](../references/csharp4-variance-and-extended-func-action.md)),
so on an older target, a hand-declared generic delegate stands in for both the callback shape and
(from C# 4.0 on) its variance.
