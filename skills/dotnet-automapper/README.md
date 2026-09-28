# AutoMapper

AutoMapper is a convention-based library for mapping one object shape to another, typically an
entity to a DTO. This skill covers setting up profiles and DI registration, configuring individual
member mappings, projecting queries with `ProjectTo`, and validating that a mapping configuration is
actually satisfiable.

> [!NOTE]
> AutoMapper carries a non-standard license as of v15.0. Research current licensing terms
> independently before adopting it for a project.

## When to reach for it

- Setting up a `Profile` and `CreateMap` for a new entity-to-DTO mapping, or registering AutoMapper
  with `AddAutoMapper`.
- A property doesn't map the way you expect and you need `ForMember`, `ForPath`, or a custom value
  resolver.
- Projecting an `IQueryable<T>` (for example, from EF Core) straight to a DTO shape with
  `ProjectTo` instead of loading full entities first.
- A mapping exception shows up at runtime and you want to catch it earlier with
  `AssertConfigurationIsValid`.
- Deciding whether AutoMapper is even the right tool for a given mapping, versus writing it by hand.

## Using it

This skill is model-invoked: it fires automatically when the situation matches, such as reviewing an
AutoMapper profile or debugging a mapping exception. You can also invoke it directly as
`/dotnet-automapper`.

## What it covers

| Topic | Reference |
| --- | --- |
| CreateMap, Profile, IMapper, DI registration | [references/core-concepts.md](references/core-concepts.md) |
| Flattening/unflattening, ForMember, ForPath, ForAllMembers | [references/member-mapping.md](references/member-mapping.md) |
| Custom value resolvers, value converters, type converters | [references/custom-resolvers-converters.md](references/custom-resolvers-converters.md) |
| Conditional mapping, null substitution, before/after map hooks | [references/conditional-and-null-handling.md](references/conditional-and-null-handling.md) |
| Mapping collections and nested object graphs | [references/collections-and-nested-objects.md](references/collections-and-nested-objects.md) |
| Projecting IQueryable with ProjectTo | [references/queryable-projection.md](references/queryable-projection.md) |
| Validating configuration is fully satisfiable | [references/configuration-validation.md](references/configuration-validation.md) |
| Testing mapping profiles and resolvers | [references/testing.md](references/testing.md) |
| Common failure modes and when to skip AutoMapper entirely | [references/pitfalls-and-alternatives.md](references/pitfalls-and-alternatives.md) |

## Example prompts

- "Set up an AutoMapper profile that maps Order to OrderDto, flattening the Customer's name."
- "This ProjectTo call is pulling back more columns than I expect: what's going on?"
- "Add a test that asserts this AutoMapper configuration is valid at startup."
