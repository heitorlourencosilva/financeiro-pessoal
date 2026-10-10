using Microsoft.EntityFrameworkCore;

namespace Financeiro.Api.Infrastructure.Persistence;

public class FinanceiroDbContext : DbContext
{
    public FinanceiroDbContext(DbContextOptions<FinanceiroDbContext> options)
        : base(options) { }

    // DbSets entram quando o mapeamento for feito.
    // Agora, só o esqueleto para o Program.cs compilar.
}