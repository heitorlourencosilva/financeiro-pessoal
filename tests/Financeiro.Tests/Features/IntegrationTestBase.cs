using Financeiro.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Financeiro.Tests.Features;

[Collection(PostgresCollection.Name)]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;

    protected WebApplicationFactory<Program> Factory { get; private set; }
    protected HttpClient Client { get; private set; }

    protected IntegrationTestBase(PostgresFixture fixture)
    {
        _fixture = fixture;
        Factory = null!;
        Client = null!;
    }

    public async Task InitializeAsync()
    {
        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<DbContextOptions<FinanceiroDbContext>>();
                    services.RemoveAll<FinanceiroDbContext>();

                    services.AddDbContext<FinanceiroDbContext>(options =>
                        options.UseNpgsql(_fixture.ConnectionString));
                });
            });

        Client = Factory.CreateClient();

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        await Factory.DisposeAsync();
    }

    protected async Task<FinanceiroDbContext> GetDbContextAsync()
    {
        var scope = Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
    }

    protected async Task ClearDatabaseAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FinanceiroDbContext>();
        await context.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE transacao_tags, transacoes, tags, categorias, contas RESTART IDENTITY CASCADE");
    }
}