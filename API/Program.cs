using FleetTracker.API.Hubs;
using FleetTracker.API.Services;
using FleetTracker.API.Handlers;
using FleetTracker.Application.Handlers;
using FleetTracker.Infrastructure.EventStore;
using FleetTracker.Infrastructure.Repositories;
using MediatR;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("FleetDb")!;

// Infrastructure — repositories stay Singleton (stateless Dapper wrappers)
builder.Services.AddSingleton(new VehicleRepository(connectionString));
builder.Services.AddSingleton(new TripRepository(connectionString));
builder.Services.AddSingleton(new LocationRepository(connectionString));

// MediatR — scans both API and Application assemblies for handlers
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<ReadModelHandler>(); // Application project
    cfg.RegisterServicesFromAssemblyContaining<Program>();          // API project (SignalRHandler)
});

// SqlEventStore — Scoped so it can receive the Scoped IPublisher
builder.Services.AddScoped<SqlEventStore>(sp =>
    new SqlEventStore(connectionString, sp.GetRequiredService<IPublisher>()));

// Services
builder.Services.AddScoped<VehicleService>();
builder.Services.AddScoped<TripService>();
builder.Services.AddScoped<LocationService>();

// SignalR
builder.Services.AddSignalR();

// Controllers
builder.Services.AddControllers();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

app.UseCors();
app.MapControllers();
app.MapHub<FleetHub>("/fleetHub");

app.Run();
