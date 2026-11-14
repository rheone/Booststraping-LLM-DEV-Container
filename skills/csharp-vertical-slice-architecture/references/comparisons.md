# VSA compared with other architectures

These styles answer different questions, and several combine. N-tier and Clean/Onion/Hexagonal organize
by **dependency direction between technical roles**. VSA organizes by **use case**. Monolith, modular
monolith, and microservices describe **deployment and module boundaries**, and a slice can live inside
any of them.

## Summary table

| Style | Primary axis | Dependency rule | Adding a feature touches | Typical abstractions | Test style |
| --- | --- | --- | --- | --- | --- |
| **N-tier (UI / BLL / DAL)** | Technical layer | UI → BLL → DAL; BLL depends on data access | A controller, a service, a repository, DTOs, often several projects | Service and repository per entity | Unit tests with mocks per layer |
| **Clean / Onion / Hexagonal** | Dependency direction toward the core | Everything points inward; infrastructure implements core interfaces | Core use case or service, interfaces, infrastructure implementation, API | Ports (interfaces) for every outside concern | Unit tests of the core; integration tests of adapters |
| **Vertical Slice** | Use case | Slices independent; shared domain and infrastructure | One slice folder (plus domain when rules change) | Only those that earn a place | Integration-first per slice |
| **Traditional monolith** | None in particular | Whatever grew | Anywhere | Varies | Varies |
| **Modular monolith** | Bounded context / module | Modules talk through contracts only | One module | Module contracts | Per module |
| **Microservices** | Business capability per deployable | Network contracts | One service (if well cut) | Service contracts, messaging | Per service plus contract tests |

Sources: <https://learn.microsoft.com/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures>
(N-tier, Clean family, monolith), <https://simonbrown.je/modular-monolith> (package by component),
<https://learn.microsoft.com/dotnet/architecture/microservices/architect-microservice-container-applications/microservices-architecture>.

## N-tier

Microsoft's description: the UI interacts only with the business layer, and the business layer interacts
with persistence only through the data-access layer; the business layer therefore depends on data-access
details. VSA keeps one deployable unit and drops the mandatory hops: a slice reads and writes data
directly when that is all it needs.

## Clean, Onion, Hexagonal

Microsoft treats these as one family: dependencies point inward to an application core, and
infrastructure implements interfaces the core defines. VSA keeps the part that pays: the domain depends on
nothing, and you add a port for an external system you must replace or fake. VSA drops what is mandatory
in strict Clean templates: a project per layer, an interface for every dependency, repositories as a rule.

They combine. The ardalis `min-clean` template applies Clean's dependency rules inside one project
organized by slices, and enforces them with an analyzer. Jason Taylor's template keeps Clean's projects and
organizes its Application project by feature folders. Microsoft's guidance still recommends the Clean
family for non-trivial monoliths; a sliced application with a clean domain satisfies both.

Martin's "screaming architecture" also asks for use cases that are independent of frameworks and testable
without a web server or database. Bogard-style slices sit on ASP.NET Core and the ORM and are tested
through them. That is a deliberate difference.

## Traditional monolith

"Entirely self-contained ... deployed as a single unit." VSA is an internal organization choice, not a
deployment choice. A sliced app is usually a monolith; the slices make it navigable.

## Modular monolith

Modules split the app by bounded context, each owning its data and exposing a small contract. Slices split
each module by use case. Use both when the app has several distinct business areas with separate teams or
data (Layout E in [project-layouts.md](project-layouts.md)). Module boundaries are the place where Evans'
shared kernel applies.

## Microservices

Each service implements one business capability behind a network boundary. VSA works inside each service.
A modular monolith with sliced modules is a common step before extracting a service: a module with its own
data and contract can move out with little change.

## Which to recommend

| Situation | Recommendation |
| --- | --- |
| New line-of-business API, one team | VSA, single project (Layout A or B) |
| Rich domain with invariants many use cases share | VSA with a domain folder or project (Layout F) |
| Several business areas, several teams, one deployable | Modular monolith with slices inside modules (Layout E) |
| Requirement to swap persistence or UI technology | Clean-style ports for that one concern, slices for the rest |
| Existing Clean/N-tier app that works and rarely changes | Leave it; migrate only areas under active change |
