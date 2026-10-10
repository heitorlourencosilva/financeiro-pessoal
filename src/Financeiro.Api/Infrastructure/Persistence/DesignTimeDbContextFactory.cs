using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Financeiro.Api.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<FinanceiroDbContext>
{
    public FinanceiroDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<FinanceiroDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=financeiro;Username=postgres;Password=postgres");

        return new FinanceiroDbContext(optionsBuilder.Options);
    }
}
