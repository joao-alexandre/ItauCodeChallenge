using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using FluentValidation.AspNetCore;
using Prometheus;
using Serilog;
using StackExchange.Redis;
using URLShortener.Data;
using URLShortener.Services;
using URLShortener.Validators;

var builder = WebApplication.CreateBuilder(args);

#region Logging (Serilog)
builder.Host.UseSerilog((ctx, lc) =>
{
    lc.ReadFrom.Configuration(ctx.Configuration)
  .Enrich.FromLogContext();
});
#endregion

#region Configuration & Connections
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

var redisConnection = builder.Configuration.GetConnectionString("Redis")
                      ?? Environment.GetEnvironmentVariable("ConnectionStrings__Redis")
                      ?? "redis:6379";
#endregion

#region Services - Infrastructure
// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(redisConnection));
builder.Services.AddSingleton<ICacheService, RedisCacheService>();

// EF / Postgres com Pooling (boa prática)
builder.Services.AddDbContextPool<AppDbContext>(options =>
    options.UseNpgsql(connectionString,
        npgsql => npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));
#endregion

#region Services - Application
builder.Services.AddScoped<IUrlService, UrlService>();
#endregion

#region API, Swagger, FluentValidation
builder.Services.AddControllers();

builder.Services.AddValidatorsFromAssemblyContaining<CreateUrlRequestValidator>();
builder.Services.AddFluentValidationAutoValidation()
                .AddFluentValidationClientsideAdapters();

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(kvp => kvp.Value.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
            );

        return new BadRequestObjectResult(new { errors });
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
#endregion

#region Observability
Metrics.SuppressDefaultMetrics();

builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgres")
    .AddRedis(redisConnection, name: "redis");
#endregion

var app = builder.Build();

#region Apply Migrations (with simple retry)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}
#endregion

#region Middleware
app.UseRouting();

app.UseHttpMetrics();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();
app.MapMetrics("/metrics");
app.MapHealthChecks("/health");
#endregion

#region Host URLs
app.Urls.Add("http://0.0.0.0:5000");
app.Run();
#endregion
