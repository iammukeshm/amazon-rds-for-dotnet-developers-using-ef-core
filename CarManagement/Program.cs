using Amazon.SecretsManager;
using CarManagement.Domain;
using CarManagement.Persistence;
using CarManagement.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Register AWS Secrets Manager client
builder.Services.AddSingleton<IAmazonSecretsManager, AmazonSecretsManagerClient>();

// Register the DatabaseCredentialsService
builder.Services.AddSingleton<IDatabaseCredentialsService>(sp =>
    new DatabaseCredentialsService(sp.GetRequiredService<IAmazonSecretsManager>()));

// Add PostgreSQL DbContext
builder.Services.AddDbContext<CarDbContext>((serviceProvider, options) =>
{
    var credentialsService = serviceProvider.GetRequiredService<IDatabaseCredentialsService>();
    var databaseSecretName = builder.Configuration["RDSOptions:DatabaseSecretName"]
        ?? throw new ArgumentNullException("RDSOptions:DatabaseSecretName", "Database secret name is missing from configuration.");
    var databaseName = builder.Configuration["RDSOptions:DatabaseName"]
        ?? throw new ArgumentNullException("RDSOptions:DatabaseName", "Database name is missing from configuration.");
    var hostName = builder.Configuration["RDSOptions:HostName"]
        ?? throw new ArgumentNullException("RDSOptions:HostName", "Database host name is missing from configuration.");

    // Return the connection string from the credentials service
    var connectionString = credentialsService.GetConnectionStringAsync(databaseSecretName, databaseName, hostName).Result;
    options.UseNpgsql(connectionString)
    .UseSeeding((context, _) =>
    {
        var demoCar = context.Set<Car>().FirstOrDefault(b => b.Id == 101);
        if (demoCar == null)
        {
            context.Set<Car>().Add(new Car { Id = 101, Make = "Tesla", Model = "Model S", Year = 2022 });
            context.SaveChanges();
        }
    }).UseAsyncSeeding(async (context, _, cancellationToken) =>
    {
        var demoCar = await context.Set<Car>().FirstOrDefaultAsync(b => b.Id == 101);
        if (demoCar == null)
        {
            context.Set<Car>().Add(new Car { Id = 101, Make = "Tesla", Model = "Model S", Year = 2022 });
            await context.SaveChangesAsync();
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// CRUD Endpoints

// Create a car
app.MapPost("/cars", async (Car car, CarDbContext db) =>
{
    db.Cars.Add(car);
    await db.SaveChangesAsync();
    return Results.Created($"/cars/{car.Id}", car);
});

// Read all cars
app.MapGet("/cars", async (CarDbContext db) => await db.Cars.ToListAsync());

// Read a specific car
app.MapGet("/cars/{id:int}", async (int id, CarDbContext db) =>
{
    var car = await db.Cars.FindAsync(id);
    return car != null ? Results.Ok(car) : Results.NotFound();
});

// Update a car
app.MapPut("/cars/{id:int}", async (int id, Car updatedCar, CarDbContext db) =>
{
    var car = await db.Cars.FindAsync(id);
    if (car is null) return Results.NotFound();

    car.Make = updatedCar.Make;
    car.Model = updatedCar.Model;
    car.Year = updatedCar.Year;

    await db.SaveChangesAsync();
    return Results.NoContent();
});

// Delete a car
app.MapDelete("/cars/{id:int}", async (int id, CarDbContext db) =>
{
    var car = await db.Cars.FindAsync(id);
    if (car is null) return Results.NotFound();

    db.Cars.Remove(car);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.Run();
