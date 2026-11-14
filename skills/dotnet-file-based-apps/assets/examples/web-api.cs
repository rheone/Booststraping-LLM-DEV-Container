#:sdk Microsoft.NET.Sdk.Web

// One-file minimal API. Run: dotnet web-api.cs --urls http://localhost:5080
// PublishAot is on by default, which turns off reflection-based JSON at run time.
// Every type an endpoint returns as JSON is listed in AppJson below.
using System.Text.Json.Serialization;

var builder = WebApplication.CreateSlimBuilder(args);
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJson.Default));

var app = builder.Build();

app.MapGet("/", () => "ok");
app.MapGet("/hello/{name}", (string name) => new Greeting($"Hello, {name}!"));

app.Run();

record Greeting(string Message);

[JsonSerializable(typeof(Greeting))]
partial class AppJson : JsonSerializerContext;
