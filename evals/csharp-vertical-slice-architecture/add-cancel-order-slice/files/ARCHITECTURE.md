# Architecture

Vertical slices. One folder per use case under `src/Shop/Features/<Area>/<Slice>/`, one file per type.

| Topic | Convention |
| --- | --- |
| Dispatch | Endpoints inject their handler class directly. No mediator. |
| Data access | Handlers use `AppDbContext` directly and call `SaveChangesAsync` once. |
| Outcomes | C# 15 `union` of domain-named records; the endpoint maps it with an exhaustive switch. |
| Validation | Built-in Minimal API validation (`AddValidation`), attributes on record parameters. |
| Isolation | Area-level sharing. Slices never reference other slices. |
| Tests | Integration tests through `WebApplicationFactory`, mirrored under `tests/Shop.IntegrationTests/Features/`. |
