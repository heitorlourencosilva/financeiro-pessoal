# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Financeiro Pessoal** — a personal finance system built as a .NET 10 WebAPI with FastEndpoints (backend) and Blazor Server + MudBlazor (frontend). It uses a vertical-slice + REPR architecture with EF Core + PostgreSQL for persistence. The project is a single-solution monorepo with two projects: `src/Financeiro.Api` (API + domain + components) and `tests/Financeiro.Tests` (test suite).

The system controls income, expenses, transfers, and investments across multiple independent classification axes: **Natureza** (Receita/Despesa/Transferencia/Investimento), **Categoria** (hierarchical), **Contexto** (Pessoal/Familiar/YouTube/Blog/Site/Investimentos/Outro), **Pessoa** (Eu/Conjuge/Filho/Outro), and **Tags** (free-form, multiple per transaction).

## Primary Source of Truth

**`CONTEXT.md`** — read this file entirely before any work. It defines the domain model, rules (D-001 through D-010), invariants, and the execution workflow. The system uses an "anti-hallucination mechanism": `CONTEXT.md` is the authority; if any AI output contradicts it, the file wins. The current status and next steps are tracked at the bottom of `CONTEXT.md` — check it before starting work.

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

Despite the stack table in `CONTEXT.md` showing separate layers, the actual implementation is a **single project** (`Financeiro.Api`). Domain entities, value objects, enums, features, infrastructure, and Blazor components all live under `src/Financeiro.Api/`. Namespaces provide organization:

- `Financeiro.Domain.Entities` — Conta, Categoria, Transacao, Tag
- `Financeiro.Domain.Enums` — NaturezaTransacao, TipoConta, Pessoa, Contexto
- `Financeiro.Domain.ValueObjects` — Dinheiro, Periodo
- `Financeiro.Features.<NomeDaFeature>` — vertical slices (one folder per use case)
- `Financeiro.Infrastructure.Persistence` — EF Core DbContext (currently a placeholder)
- `Financeiro.Infrastructure.Importers` — CSV/OFX import (not yet started)
- `Financeiro.Components` — Blazor Server + MudBlazor UI (not yet started)

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

### Transaction Flow (not yet implemented)

The data ingestion pipeline is defined but not built:

1. Import CSV/OFX → 2. Normalize → 3. Apply rules → 4. Suggest by history → 5. Review queue → 6. Final classification

### Testing Strategy

- **Domain tests** live in `tests/Financeiro.Tests/Domain/` and use xUnit + FluentAssertions (no external dependencies).
- **Integration tests** with Testcontainers + WebApplicationFactory will be added when API endpoints exist.
- **Component tests** with Bunit will be added when Blazor components exist.
- Tests follow the naming convention `MetodoCenarioResultadoEsperado` (e.g., `Transferencia_EhNeutra_NaoAfetaFluxoDeCaixa`) and the explicit Arrange/Act/Assert pattern.

## Current State

The project is at the start of implementation. The domain model is defined and fully tested:

- ✅ Entities: Conta, Categoria, Transacao, Tag (all with domain tests)
- ✅ Value objects: Dinheiro, Periodo (with tests)
- ✅ Enums: NaturezaTransacao, Contexto, Pessoa, TipoConta (with tests)
- ✅ Project structure confirmed (2 projects, namespaces within Financeiro.Api)
- ❌ EF Core DbContext configuration (placeholder exists, entities not configured)
- ❌ Features/endpoints (not started)
- ❌ Importers (not started)
- ❌ Blazor components (not started)

**Next step per `CONTEXT.md`:** Write domain tests → EF Core + DbContext → first FastEndpoints → CSV/OFX import → review queue → dashboard + reports.

## Conventions

- `DateOnly` for transaction dates; `DateTime` (UTC) for audit timestamps.
- `Guid` IDs generated in entity constructors.
- Entities are `sealed`.
- Private setters on all properties; mutations via behavior methods only.
- No business logic in endpoints — it goes in domain or feature handlers.
- Test naming: `MetodoCenarioResultadoEsperado`

## Frontend Design

When creating or modifying UI code (HTML, CSS, component layout):

1. **Always use the `frontend-design` skill first** to generate the design review/checklist.
2. **Do not proceed** with implementation if the skill fails to load.
3. The skill will provide guidance on MudBlazor component selection, accessibility (WCAG), and responsive design patterns.
4. Apply the design feedback before writing any code.
