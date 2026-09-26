# MVC, Razor Pages, and Minimal API Conventions

Every mechanism elsewhere in this skill (roles, claims, policies, resources) still needs applying to
an actual endpoint or page. Each hosting model has its own way to do that at a scale bigger than one
attribute per action.

## Minimal API endpoints and route groups

```csharp
app.MapGet("/hello", () => "Hello world!")
    .RequireAuthorization();

app.MapGet("/admin/report", () => Results.Ok())
    .RequireAuthorization("RequireAdministrator");

app.MapGet("/public", () => "Anyone can see this")
    .AllowAnonymous();
```

`MapGroup` applies `RequireAuthorization`/`AllowAnonymous` to every endpoint in the group at once,
without repeating it per route:

```csharp
var privateApi = app.MapGroup("/private").RequireAuthorization();
privateApi.MapGet("/todos", GetPrivateTodos);
privateApi.MapPost("/todos", CreatePrivateTodo);

var publicApi = app.MapGroup("/public").AllowAnonymous();
publicApi.MapGet("/todos", GetPublicTodos);
```

Adding filters or metadata to a group behaves the same as adding it to every endpoint inside that
group individually — order matters between metadata on the *same* group or endpoint, but not
between sibling groups added independently of each other.

## MVC controllers and actions

```csharp
[Authorize]
public class VacationController : Controller
{
    public IActionResult Index() => View();

    [AllowAnonymous]
    public IActionResult VacationPolicy() => View();
}
```

`[Authorize]`/`[AllowAnonymous]` at the controller level apply to every action; an action-level
attribute overrides the controller-level one for that one action only — see
[simple-authorization.md](simple-authorization.md).

## Razor Pages folder/page conventions

Applying `[Authorize]` to every `PageModel` individually doesn't scale once a folder holds many
pages that all need the same rule — `PageConventionCollection` conventions apply an
`AuthorizeFilter`/`AllowAnonymousFilter` by path instead:

```csharp
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizePage("/Contact");
    options.Conventions.AuthorizeFolder("/Private");
    options.Conventions.AuthorizeFolder("/Admin", "RequireAdminRole");
    options.Conventions.AllowAnonymousToPage("/Private/PublicPage");
    options.Conventions.AllowAnonymousToFolder("/Private/PublicPages");
});
```

- `AuthorizePage(path)` / `AuthorizeFolder(path)` — require authorization for one page, or every page
  under a folder path. An overload taking a policy name (`AuthorizeFolder("/Admin", "RequireAdminRole")`)
  applies a specific named policy instead of the default policy.
- `AllowAnonymousToPage(path)` / `AllowAnonymousToFolder(path)` — allow anonymous access to one page,
  or every page under a folder.
- `AuthorizeAreaPage`/`AuthorizeAreaFolder`/`AllowAnonymousToAreaFolder` — the same conventions,
  scoped to a Razor Pages *area* instead of the default pages root.

Path arguments are the View Engine path: the Razor Pages root-relative path with no file extension,
forward slashes only.

### Combining folder- and page-level conventions

```csharp
// Works: a folder requires authorization, one page inside it is carved out as public.
options.Conventions.AuthorizeFolder("/Private").AllowAnonymousToPage("/Private/Public");

// Does not work: a folder allows anonymous access, one page inside it can't re-require authorization.
options.Conventions.AllowAnonymousToFolder("/Public").AuthorizePage("/Public/Private");
```

When both an `AllowAnonymousFilter` and an `AuthorizeFilter` end up applied to the same page,
`AllowAnonymousFilter` always wins — a folder-wide anonymous-access convention can't be narrowed back
down to "except this one page" by adding an authorize convention on top of it. Structure folders so
the more restrictive convention is the outer one when a mix is needed.

## API endpoints and the 401-vs-redirect distinction

As of .NET 10, ASP.NET Core distinguishes endpoints it recognizes as APIs (controllers marked
`[ApiController]`, minimal API handlers that read/write JSON bodies, endpoints returning
`TypedResults`, SignalR hubs) from page-rendering endpoints when a cookie-authenticated request fails
authorization: API endpoints return the appropriate `401`/`403` status code directly, while
non-API endpoints still redirect to the configured login/access-denied path. This detection is based
on inferred endpoint metadata, not the request's `Accept` header — a minimal API handler with a bare
`string`/`void`/`IResult` declared return type doesn't contribute this metadata through its return
type alone, so it still redirects like a page-rendering endpoint unless something else about it
(reading a JSON body, for instance) marks it as an API endpoint.
