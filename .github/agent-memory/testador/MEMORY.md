# Memória do testador

- Comando completo da suíte: `dotnet test` (solution `Financeiro.slnx`).
- Build completo: `dotnet build`.
- Fixtures/helpers de integração: `tests/Financeiro.Tests/Features/PostgresFixture.cs` (container PostgreSQL/Testcontainers) e `IntegrationTestBase.cs` (WebApplicationFactory, migrações e limpeza do banco).
- Testes instáveis conhecidos: nenhum identificado.
- Armadilha conhecida: o restore emite NU1902 para AngleSharp 1.2.0 no projeto `tests/Financeiro.Tests`; aviso informativo, sem impedir build/teste.
