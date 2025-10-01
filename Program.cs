using Microsoft.EntityFrameworkCore;
using URLShortener.Data;
using URLShortener.Services;
using Prometheus;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) =>
{
    lc.ReadFrom.Configuration(ctx.Configuration, sectionName: "Serilog")
      .Enrich.FromLogContext();
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<IUrlService, UrlService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//FUNCIONA DEMONIO PELO AMOR DE DEUS
Metrics.SuppressDefaultMetrics();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

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

app.Urls.Add("http://0.0.0.0:5000");

app.Run();
