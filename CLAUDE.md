# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Financeiro Pessoal** — a personal finance system built as a .NET 10 WebAPI with FastEndpoints (backend) and Blazor Server + MudBlazor (frontend). It uses a vertical-slice + REPR architecture with EF Core + PostgreSQL for persistence. The project is a single-solution monorepo with two projects: `src/Financeiro.Api` (API + domain + components) and `tests/Financeiro.Tests` (test suite).

The system controls income, expenses, transfers, and investments across multiple independent classification axes: **Natureza** (Receita/Despesa/Transferencia/Investimento), **Categoria** (hierarchical), **Contexto** (Pessoal/Familiar/YouTube/Blog/Site/Investimentos/Outro), **Pessoa** (Eu/Conjuge/Filho/Outro), and **Tags** (free-form, multiple per transaction).

## Primary Source of Truth

**`CONTEXT.md`** — read this file entirely before any work. It defines the domain model, rules (D-001 onwards), invariants, and the execution workflow. The system uses an "anti-hallucination mechanism": `CONTEXT.md` is the authority; if any AI output contradicts it, the file wins. The current status and next steps are tracked at the bottom of `CONTEXT.md` — check it before starting work.

## Commands

```bash
# Build
dotnet build

# Run all tests
dotnet test

# Run a single test class
dotnet test --filter "FullyQualifiedName~TransacaoTests"

# Run a single test method
dotnet test --filter "FullyQualifiedName~TransacaoTests.Transferencia_EhNeutra_NaoAfetaFluxoDeCaixa"

# Watch mode for development (API)
dotnet watch run --project src/Financeiro.Api
```

No lint command is configured; `TreatWarningsAsErrors` is `false` in the API project.

## Architecture

### Single-Project Domain

Despite the stack table in `CONTEXT.md` showing separate layers, the actual implementation is a **single project** (`Financeiro.Api`). Domain entities, value objects, enums, services, features, infrastructure, and Blazor components all live under `src/Financeiro.Api/`. Namespaces provide organization:

- `Financeiro.Domain.Entities` — Conta, Categoria, Transacao, Tag
- `Financeiro.Domain.Enums` — NaturezaTransacao, TipoConta, Pessoa, Contexto
- `Financeiro.Domain.ValueObjects` — Dinheiro, Periodo
- `Financeiro.Domain.Services` — DescricaoNormalizador (text normalization, used by Transacao)
- `Financeiro.Features.<NomeDaFeature>` — vertical slices (one folder per use case)
- `Financeiro.Infrastructure.Persistence` — EF Core DbContext + migrations
- `Financeiro.Infrastructure.Importers` — CSV/OFX import (CSV Itaú done; OFX pending)
- `Financeiro.Api.Components` — Blazor Server + MudBlazor UI (scaffolding only)

### Key Design Decisions

- **Entities are `sealed`.** All entity classes use the `sealed` modifier.
- **No balance on entities.** `Conta.Saldo` is a calculation, not a stored property.
- **`Dinheiro`** is always stored as `decimal` (never `double`/`float`) and is always the absolute value. The sign comes from `NaturezaTransacao` and the `SinalFluxo()` method.
- **Transferencia and Investimento are neutral** — they don't affect cash flow (`EhNeutra()` returns true, `SinalFluxo()` returns 0).
- **Contexto and Pessoa are enums in the MVP**, not database tables. Migration to lookup tables is planned for later.
- **Conta ≠ Contexto.** Account is "where the money is"; Context is "for what purpose."
- **Aporte/Resgate of investments = neutral transfer** in the MVP.
- **Private setters** on all entities — changes happen only via behavior methods (e.g., `Classificar`, `Desclassificar`, `AdicionarTag`, `NormalizarDescricao`).
- **EF Core constructors are private**; EF Core uses them for materialization while domain creation uses public constructors.
- **Normalization is a domain concern.** `DescricaoNormalizador` lives in `Financeiro.Domain.Services` and is invoked by `Transacao`'s constructor and `Atualizar` method — never by endpoints.
- **`TransacaoResponse` is shared.** All transaction endpoints use the same response record (defined in `Features/Transacoes/TransacaoResponse.cs`), eliminating duplication.
- **`Desclassificar` clears tags.** When a transaction is unclassified, its tags are removed along with its category.
- **Reclassification is allowed.** Calling `Classificar` on an already-classified transaction overwrites the previous classification silently.
- **Classificar returns 404 for missing tags.** If any `TagId` in the request does not exist, the endpoint returns 404 instead of ignoring it.

### Transaction Flow

The data ingestion pipeline is partially implemented:

1. ✅ Import CSV/OFX (CSV Itaú done; OFX pending)
2. ✅ Normalize (applied automatically by `Transacao` constructor and `Atualizar`)
3. ⏳ Apply rules
4. ⏳ Suggest by history
5. ✅ Review queue (`/api/transacoes/pendentes`, `/classificar`, `/desclassificar`)
6. ⏳ Final classification (endpoint exists; automatic rules pending)

### Testing Strategy

- **Domain tests** live in `tests/Financeiro.Tests/Domain/` and use xUnit + FluentAssertions (no external dependencies).
- **Integration tests** use Testcontainers + WebApplicationFactory against a real PostgreSQL instance, via a shared `PostgresFixture` and `IntegrationTestBase`. Each test class inherits from `IntegrationTestBase` and calls `ClearDatabaseAsync()` in the Arrange phase.
- **Component tests** with Bunit will be added when Blazor components exist.
- Tests follow the naming convention `MetodoCenarioResultadoEsperado` (e.g., `Transferencia_EhNeutra_NaoAfetaFluxoDeCaixa`) and the explicit Arrange/Act/Assert pattern.

## Current State

- ✅ Entities: Conta, Categoria, Transacao, Tag (with domain tests)
- ✅ Value objects: Dinheiro, Periodo (with tests)
- ✅ Enums: NaturezaTransacao, Contexto, Pessoa, TipoConta (with tests)
- ✅ Domain services: DescricaoNormalizador (moved from Infrastructure, with tests)
- ✅ Project structure confirmed (2 projects, namespaces within Financeiro.Api)
- ✅ EF Core DbContext configuration (complete with complex property for Dinheiro)
- ✅ Features/endpoints: Contas CRUD (Criar, Atualizar, Desativar, Listar, Obter)
- ✅ Features/endpoints: Transações CRUD (Criar, Atualizar, Listar, Obter)
- ✅ Integration tests infrastructure (Testcontainers + WebApplicationFactory + PostgreSQL)
- ✅ Integration tests: Contas (5 test classes, covering all CRUD operations)
- ✅ Integration tests: Transações (5 test classes, covering all CRUD operations)
- ✅ Importação CSV Itaú
- ✅ Review queue: `GET /api/transacoes/pendentes`, `PUT /api/transacoes/{Id}/classificar`, `PUT /api/transacoes/{Id}/desclassificar` + `FilaRevisaoTests`
- ✅ Shared `TransacaoResponse` (eliminated 7 duplicated response records)
- ✅ Blazor Server scaffolding (App.razor, Routes.razor, MainLayout.razor) — components not yet built
- ✅ Test suite: 130 tests passing
- ❌ Blazor components (business screens not started)
- ❌ Dashboard + reports (next step)

**Next step per `CONTEXT.md`:** dashboard + reports.

## Conventions

- `DateOnly` for transaction dates; `DateTime` (UTC) for audit timestamps.
- `Guid` IDs generated in entity constructors.
- Entities are `sealed`.
- Private setters on all properties; mutations via behavior methods only.
- No business logic in endpoints — it goes in domain or feature handlers.
- Test naming: `MetodoCenarioResultadoEsperado`
- **FastEndpoints naming:** Never use `Request` or `Response` as record/class names inside endpoints — `Response` is a reserved member of the `Endpoint` base class. Always use `FeatureRequest` and `FeatureResponse`. For shared responses, use a plain record like `TransacaoResponse`.
- **Migrations location:** EF Core migrations must be placed inside the `Infrastructure/Persistence/Migrations` folder, not at the project root.
- **No unnecessary `Include()`:** if the response record doesn't expose a navigation property, don't `.Include()` it.

## Frontend Design

When creating or modifying UI code (HTML, CSS, component layout):

1. **Always use the `frontend-design` skill first** to generate the design review/checklist.
2. **Do not proceed** with implementation if the skill fails to load.
3. The skill will provide guidance on MudBlazor component selection, accessibility (WCAG), and responsive design patterns.
4. Apply the design feedback before writing any code.

## Progress Tracking

**Update CLAUDE.md and CONTEXT.md as you work.** After completing a task, update both files to reflect the current state of implementation. This ensures the documentation stays synchronized with the code and serves as the source of truth for the project status.