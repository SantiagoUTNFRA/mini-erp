using MiniErp.Api.Customers;
using MiniErp.Application.Customers;
using MiniErp.Persistence.EfCore;

var builder = WebApplication.CreateBuilder(args);

// In development the connection string comes from dotnet user-secrets (ADR-0004); never from appsettings.
string connectionString = builder.Configuration.GetConnectionString("MiniErp")
    ?? throw new InvalidOperationException(
        "Missing connection string 'ConnectionStrings:MiniErp'. In development, set it with dotnet user-secrets "
        + "(see specs/001-gestion-clientes/quickstart.md).");

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddEfCorePersistence(connectionString);

builder.Services.AddScoped<CreateCustomer>();
builder.Services.AddScoped<GetCustomer>();
builder.Services.AddScoped<ListCustomers>();
builder.Services.AddScoped<UpdateCustomer>();
builder.Services.AddScoped<DeleteCustomer>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    await app.Services.MigrateDatabaseAsync(app.Lifetime.ApplicationStopping);
}

app.UseHttpsRedirection();

app.MapCustomers();

app.Run();
