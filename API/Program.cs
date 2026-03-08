using FleetTracker.API.Hubs;
using FleetTracker.API.Services;
using FleetTracker.Infrastructure.EventStore;
using FleetTracker.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Connection string
var connectionString = builder.Configuration.GetConnectionString("FleetDb")!;

// Infrastructure
builder.Services.AddSingleton(new SqlEventStore(connectionString));
builder.Services.AddSingleton(new VehicleRepository(connectionString));
builder.Services.AddSingleton(new TripRepository(connectionString));
builder.Services.AddSingleton(new LocationRepository(connectionString));

// Services
builder.Services.AddScoped<VehicleService>();
builder.Services.AddScoped<TripService>();
builder.Services.AddScoped<LocationService>();

// SignalR
builder.Services.AddSignalR();

// Controllers
builder.Services.AddControllers();

// CORS - needed for MAUI to connect
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors();
app.MapControllers();
app.MapHub<FleetHub>("/fleetHub");

app.Run();
