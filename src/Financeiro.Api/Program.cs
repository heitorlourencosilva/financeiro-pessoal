using FastEndpoints;
using FastEndpoints.Swagger;
using Financeiro.Api.Components;
using Financeiro.Api.Infrastructure.Persistence;
using Financeiro.Infrastructure.Importers;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------- Configuração ----------
var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Connection string 'Postgres' não configurada.");

// ---------- EF Core + PostgreSQL ----------
builder.Services.AddDbContext<FinanceiroDbContext>(options =>
    options.UseNpgsql(connectionString));

// ---------- FastEndpoints + FluentValidation ----------
builder.Services
    .AddFastEndpoints()
    .SwaggerDocument(o =>
    {
        o.DocumentSettings = s =>
        {
            s.Title = "Financeiro.Api";
            s.Version = "v1";
        };
    });

builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddSingleton<ItauCsvParser>();

// ---------- Blazor Server ----------
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

// ---------- App ----------
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// FastEndpoints
app.UseFastEndpoints(c =>
{
    c.Errors.UseProblemDetails();
});
app.UseSwaggerGen();

// Blazor
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

// Necessário para WebApplicationFactory nos testes de integração
public partial class Program { }