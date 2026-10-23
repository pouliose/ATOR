using System.Net;
using System.Net.Sockets;
using ATOR.Api;
using ATOR.Receiver;
using ATOR.Reports;
using ATOR.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var udpPort = builder.Configuration.GetValue<int?>("Telemetry:UdpPort") ?? 5000;
var listenAddressText = builder.Configuration["Telemetry:ListenAddress"] ?? "127.0.0.1";
var connectionString = builder.Configuration.GetConnectionString("Telemetry")
    ?? throw new InvalidOperationException(
        "Configure the PostgreSQL connection string in ConnectionStrings:Telemetry.");
if (udpPort is < 1 or > 65535)
{
    throw new InvalidOperationException("Telemetry:UdpPort must be between 1 and 65535.");
}

if (!IPAddress.TryParse(listenAddressText, out var listenAddress) ||
    listenAddress.AddressFamily != AddressFamily.InterNetwork)
{
    throw new InvalidOperationException("Telemetry:ListenAddress must be a valid IPv4 address.");
}

builder.Services.AddDbContextFactory<TelemetryDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddSingleton<PostgresTrackReportRepository>();
builder.Services.AddSingleton<ICurrentTracksReader>(serviceProvider =>
    serviceProvider.GetRequiredService<PostgresTrackReportRepository>());
builder.Services.AddScoped<ICurrentTracksService, CurrentTracksService>();
builder.Services.AddHostedService<PostgresDatabaseInitializer>();
builder.Services.AddHostedService(serviceProvider =>
    new UdpTelemetryReceiverService(
        listenAddress,
        udpPort,
        serviceProvider.GetRequiredService<PostgresTrackReportRepository>(),
        serviceProvider.GetRequiredService<ILogger<UdpTelemetryReceiverService>>()));

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.MapOpenApi();
app.MapCurrentTracksEndpoints();
await app.RunAsync();
