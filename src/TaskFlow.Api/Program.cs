using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Features.Tasks;
using TaskFlow.Api.Features.Users;
using TaskFlow.Api.Persistence;
using TaskStatus = TaskFlow.Api.Features.Tasks.TaskStatus;

var builder = WebApplication.CreateBuilder(args);
var databasePath = Path.GetFullPath(
    builder.Configuration["TaskFlow:DatabasePath"] ?? "data/taskflow.db",
    builder.Environment.ContentRootPath);
Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={databasePath}"));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<TaskService>();
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter<TaskStatus>()));

var app = builder.Build();
var initializeDatabaseOnly = args.Contains("--initialize-database", StringComparer.Ordinal);
app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    context.RequestServices
        .GetRequiredService<ILogger<Program>>()
        .LogError(exception, "An unhandled exception occurred while processing the request.");

    await Results.Problem(
        statusCode: StatusCodes.Status500InternalServerError,
        title: "An unexpected error occurred.",
        instance: context.Request.Path)
        .ExecuteAsync(context);
}));

if (app.Environment.IsDevelopment())
{
    await using (var scope = app.Services.CreateAsyncScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();
        await DbSeeder.SeedAsync(dbContext);
    }
}

if (initializeDatabaseOnly)
{
    if (!app.Environment.IsDevelopment())
    {
        throw new InvalidOperationException("Database initialization is only available in Development.");
    }

    await app.DisposeAsync();
    return;
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
        options.SwaggerEndpoint("/openapi/v1.json", "TaskFlow API v1.0"));
}

app.MapTaskEndpoints();
app.MapUserEndpoints();
app.MapGet("/health", () => Results.Ok(new { status = "Healthy" })).WithTags("Health");

await app.RunAsync();

public partial class Program
{
}
