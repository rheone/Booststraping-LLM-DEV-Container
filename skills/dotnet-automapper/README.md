# AutoMapper

Task-organized guidance on the AutoMapper NuGet package — the routing table (by task, not
AutoMapper or C# version) is in [SKILL.md](SKILL.md).

**`references/`** — one file per topic, not per version

| File | Covers |
| --- | --- |
| `core-concepts.md` | CreateMap, Profile, IMapper, MapperConfiguration, AddAutoMapper dependency-injection registration |
| `member-mapping.md` | flattening/unflattening conventions, ForMember, ForPath, ForAllMembers |
| `custom-resolvers-converters.md` | IValueResolver, IMemberValueResolver, IValueConverter, ITypeConverter |
| `conditional-and-null-handling.md` | Condition, PreCondition, null substitution (NullSubstitute), BeforeMap/AfterMap, BeforeMapAction/AfterMapAction |
| `collections-and-nested-objects.md` | collection mapping, nested object mapping, polymorphic maps (Include/IncludeBase) |
| `queryable-projection.md` | ProjectTo for IQueryable\<T> / EF Core, deferred execution, differences from Map |
| `configuration-validation.md` | AssertConfigurationIsValid, CompileMappings |
| `testing.md` | unit testing profiles, AssertConfigurationIsValid in tests, testing custom resolvers, testing DI registration |
| `pitfalls-and-alternatives.md` | common failure modes, the "should you use AutoMapper at all" community debate, when to prefer manual mapping |

## Scope

AutoMapper (the `AutoMapper` NuGet package and its DI extension, now merged into the core package
since v13.0) only. Out of scope: other mapping libraries (Mapperly, Mapster — named only as
alternatives in `pitfalls-and-alternatives.md`), MediatR, and ORM internals beyond what
`ProjectTo` itself touches.

Each reference file notes an AutoMapper version fact inline where relevant (e.g. "since v13.0");
version is not the file-splitting axis for this skill (see SKILL.md for why).

AutoMapper carries a non-standard license as of v15.0 — research current terms independently
before adopting it for a project.

## Verified facts (as of 2026-09-25)

- **Current latest release: AutoMapper 16.2.0** (published July 2, 2026), targeting .NET 8.0+,
  .NET Standard 2.0, and .NET Framework 4.7.1+. Source: the NuGet Gallery package page
  (nuget.org/packages/automapper).

These facts were verified via live web search against nuget.org at the time this skill was
written; re-verify before relying on exact version numbers.
