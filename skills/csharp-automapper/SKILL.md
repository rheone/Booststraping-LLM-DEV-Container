---
name: csharp-automapper
description: Guidance on the AutoMapper NuGet package (verified current release 16.2.0) for object-to-object mapping in C#/.NET — CreateMap and Profile setup, AddAutoMapper dependency injection registration, property mapping and flattening conventions, ForMember/ForPath/ForAllMembers configuration, custom value resolvers/converters/type converters, conditional mapping and null substitution, pre/post-processing, collection and nested-object mapping, ProjectTo for IQueryable/EF Core query projection, AssertConfigurationIsValid configuration validation, and testing mapping profiles. Also documents AutoMapper's current dual-license model (RPL 1.5 open-source / paid commercial subscription as of v15.0+) since this affects whether a project can adopt it. Use when writing or reviewing AutoMapper profiles/mappings, deciding whether AutoMapper is appropriate for a project given its licensing, debugging a mapping exception, or setting up ProjectTo against EF Core.
license: Apache-2.0
user-invocable: true
metadata:
  author: Robert Engelhardt <rheone@gmail.com>
  version: 1.0.0
---

# C# AutoMapper

Guidance on AutoMapper, the convention-based object-to-object mapping library for .NET. Organized
by task/category, not by C# or AutoMapper version — AutoMapper's core API surface (`CreateMap`,
`Profile`, `IMapper`) has been stable across major versions; each reference file notes a
version-introduced fact inline where it matters (e.g. licensing changes at v15.0).

## Read this first: licensing

AutoMapper is **no longer unconditionally free** for commercial use. As of **v15.0 (2025)**,
new versions ship under a dual license: free (Reciprocal Public License 1.5) for individuals,
small companies (under $5M annual gross revenue), non-profits, education, and non-production use;
a **paid commercial subscription** (tiered by developer count, via Lucky Penny Software) for
everyone else. Versions prior to v15.0 remain under their original MIT license and are unaffected
retroactively. **Read [references/licensing.md](references/licensing.md) before recommending
AutoMapper for a project** — this determines whether adoption is even viable, independent of any
technical merits below.

## Pick your reference file by task

| You're doing this... | Reference file |
| --- | --- |
| Deciding if AutoMapper fits, or checking license obligations before adding the package | [references/licensing.md](references/licensing.md) |
| Setting up `CreateMap`, `Profile` classes, `IMapper`, or DI registration (`AddAutoMapper`) | [references/core-concepts.md](references/core-concepts.md) |
| Mapping flat DTOs to/from nested objects (flattening/unflattening conventions) | [references/member-mapping.md](references/member-mapping.md) |
| Customizing individual member mappings: `ForMember`, `ForPath`, `ForAllMembers` | [references/member-mapping.md](references/member-mapping.md) |
| Writing a custom value resolver, value converter, or type converter | [references/custom-resolvers-converters.md](references/custom-resolvers-converters.md) |
| Skipping a mapping conditionally, substituting nulls, or running code before/after a map | [references/conditional-and-null-handling.md](references/conditional-and-null-handling.md) |
| Mapping collections (`List<T>`, arrays, custom collection types) or nested object graphs | [references/collections-and-nested-objects.md](references/collections-and-nested-objects.md) |
| Projecting an `IQueryable<T>` (e.g. EF Core) to a DTO shape without loading full entities | [references/queryable-projection.md](references/queryable-projection.md) |
| Validating that all configured mappings are actually satisfiable | [references/configuration-validation.md](references/configuration-validation.md) |
| Unit testing mapping profiles, resolvers, or configuration | [references/testing.md](references/testing.md) |
| Deciding whether AutoMapper is the right tool at all, or debugging a mapping gone wrong | [references/pitfalls-and-alternatives.md](references/pitfalls-and-alternatives.md) |

## Quick start

A minimal profile and registration, current API (v12+ through the verified current 16.2.0):

```csharp
public sealed class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>();
    }
}

// Program.cs (ASP.NET Core, AddAutoMapper is part of the core AutoMapper package since v13.0)
builder.Services.AddAutoMapper(cfg => { }, typeof(OrderProfile));

// Usage
public sealed class OrdersController(IMapper mapper)
{
    public OrderDto Get(Order order) => mapper.Map<OrderDto>(order);
}
```

The single most common miss: adding a `CreateMap<TSource, TDest>()` and never calling
`AssertConfigurationIsValid()` anywhere (test or startup), so unmapped/misconfigured members are
only discovered at runtime in production. See
[references/configuration-validation.md](references/configuration-validation.md).

## Out of scope

- Other .NET mapping libraries (Mapster, Mapperly) — mentioned only by name in
  [references/pitfalls-and-alternatives.md](references/pitfalls-and-alternatives.md) as
  alternatives people evaluate when AutoMapper's licensing or performance doesn't fit; not
  documented here.
- MediatR (a separate Lucky Penny Software product with a similar 2024 licensing change) — not
  covered; this skill is AutoMapper-only.
- ORM-specific query behavior beyond what `ProjectTo` itself does (e.g. EF Core migrations,
  change tracking) — out of scope beyond the projection mechanics in
  [references/queryable-projection.md](references/queryable-projection.md).
