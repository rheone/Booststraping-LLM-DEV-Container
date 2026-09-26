# Autofac

Guidance on Autofac, a third-party IoC/dependency-injection container for C#/.NET — the routing
table (by situation, not by Autofac/C# version) is in [SKILL.md](SKILL.md).

**`references/`** — one file per category, not per Autofac/C# version

| File | Covers |
| --- | --- |
| `core-concepts.md` | ContainerBuilder, RegisterType, RegisterInstance, Build(), IContainer, Resolve\<T>/TryResolve\<T> |
| `registration-syntax.md` | As\<T>, AsSelf, AsImplementedInterfaces, named/keyed registrations, open generics |
| `lifetime-scopes.md` | InstancePerDependency, SingleInstance, InstancePerLifetimeScope, InstancePerMatchingLifetimeScope, InstancePerRequest, nested scopes, disposal |
| `modules.md` | Autofac.Module, Load(ContainerBuilder), modules vs. plain registration calls |
| `property-method-injection.md` | PropertiesAutowired, InjectProperties, method injection |
| `decorators-interceptors.md` | RegisterDecorator, EnableInterfaceInterceptors, EnableClassInterceptors |
| `aspnetcore-integration.md` | UseServiceProviderFactory(AutofacServiceProviderFactory), ConfigureContainer |
| `assembly-scanning.md` | RegisterAssemblyTypes, Where/As conventions |
| `relationship-types.md` | Lazy\<T>, Func\<T>, Owned\<T>, circular dependencies |
| `common-pitfalls.md` | captive dependencies, over-scoping, disposal surprises |
| `testing-with-autofac.md` | minimal test containers, verifying registrations resolve, testing modules in isolation, when to skip the container |

## Scope

Autofac's core container API (`Autofac` package) and its official ASP.NET Core hosting bridge
(`Autofac.Extensions.DependencyInjection`). Out of scope: `Microsoft.Extensions.DependencyInjection`
internals, legacy framework-specific Autofac packages (WCF, OWIN, MVC5), and general DI theory not
specific to Autofac's API.

Each reference file notes a version-introduced fact inline; version is not the file-splitting axis
for this skill (see [SKILL.md](SKILL.md) for why). Current stable release as of this writing:
9.3.4.
