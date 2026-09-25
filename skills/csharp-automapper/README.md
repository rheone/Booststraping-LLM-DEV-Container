# C# AutoMapper

Task-organized guidance on the AutoMapper NuGet package — the routing table (by task, not
AutoMapper or C# version) is in [SKILL.md](SKILL.md).

```text
references/                          one file per topic, not per version
  licensing.md                         current dual-license model (RPL 1.5 / paid commercial),
                                        who owes what, how to check, versions affected
  core-concepts.md                     CreateMap, Profile, IMapper, MapperConfiguration,
                                        AddAutoMapper dependency-injection registration
  member-mapping.md                    flattening/unflattening conventions, ForMember, ForPath,
                                        ForAllMembers
  custom-resolvers-converters.md       IValueResolver, IMemberValueResolver, IValueConverter,
                                        ITypeConverter
  conditional-and-null-handling.md     Condition, PreCondition, null substitution (NullSubstitute),
                                        BeforeMap/AfterMap, BeforeMapAction/AfterMapAction
  collections-and-nested-objects.md    collection mapping, nested object mapping, polymorphic
                                        maps (Include/IncludeBase)
  queryable-projection.md              ProjectTo for IQueryable<T> / EF Core, deferred execution,
                                        differences from Map
  configuration-validation.md          AssertConfigurationIsValid, CompileMappings
  testing.md                           unit testing profiles, AssertConfigurationIsValid in
                                        tests, testing custom resolvers, testing DI registration
  pitfalls-and-alternatives.md         common failure modes, the "should you use AutoMapper at
                                        all" community debate, when to prefer manual mapping
```

## Scope

AutoMapper (the `AutoMapper` NuGet package and its DI extension, now merged into the core package
since v13.0) only. Out of scope: other mapping libraries (Mapperly, Mapster — named only as
alternatives in `pitfalls-and-alternatives.md`), MediatR, and ORM internals beyond what
`ProjectTo` itself touches.

Each reference file notes an AutoMapper version fact inline where relevant (e.g. "since v13.0",
"as of v15.0"); version is not the file-splitting axis for this skill (see SKILL.md for why).

## Verified facts (as of 2026-09-25)

- **Current latest release: AutoMapper 16.2.0** (published July 2, 2026), targeting .NET 8.0+,
  .NET Standard 2.0, and .NET Framework 4.7.1+. Source: the NuGet Gallery package page
  (nuget.org/packages/automapper).
- **Licensing changed starting with v15.0**: dual-licensed under the Reciprocal Public License
  1.5 (RPL 1.5, an open-source copyleft license) and a paid Lucky Penny Software commercial
  subscription. Versions before v15.0 remain MIT-licensed and unaffected. Full detail in
  [references/licensing.md](references/licensing.md).

These facts were verified via live web search against nuget.org and jimmybogard.com at the time
this skill was written; re-verify before relying on exact version numbers or pricing, both of
which change over time.
