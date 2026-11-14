var app = WebApplication.Create(args);
app.MapGet("/", () => "app");
app.Run();
