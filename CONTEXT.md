# CONTEXT.md — Sistema Financeiro Pessoal

> Fonte da verdade do projeto. Se a IA sugerir algo que contradiz este arquivo,
> o arquivo vence. Sempre que abrir nova conversa, cole este arquivo no início.

## Identidade do Projeto

Sistema web financeiro pessoal, local, para controle de receitas, despesas,
transferências e aportes. Evolui de PF individual para família, múltiplas
fontes de renda (YouTube, blog, site) e investimentos — sem reescrita.

## Stack Oficial

| Camada | Tecnologia |
|---|---|
| Backend | .NET 10 WebAPI + FastEndpoints |
| Padrão | Vertical Slice + REPR |
| Persistência | EF Core + PostgreSQL |
| Validação | FluentValidation |
| Frontend | Blazor Server + MudBlazor |
| Gráficos | ApexCharts.Blazor (ou equivalente) |
| Importação | Parser CSV em C# (OFX planejado) |
| Testes | xUnit + FluentAssertions + Testcontainers + WebApplicationFactory + Bunit |
| Hospedagem | Local → TV Box com Linux (evolução) |
| Estrutura | 2 projetos: `src/Financeiro.Api` + `tests/Financeiro.Tests` |

## Modelo de Domínio — Eixos Independentes

- **Natureza:** Receita | Despesa | Transferencia | Investimento
- **Categoria:** hierárquica (Categoria > Subcategoria)
- **Contexto:** Pessoal | Familiar | YouTube | Blog | Site | Investimentos | Outro
- **Pessoa:** Eu | Conjuge | Filho | Outro
- **Tags:** livres, várias por transação, transversais

Conta bancária é **meio**, não contexto. Contexto é atributo da transação.

## Regras Invioláveis

- **D-001:** Transferência é neutra. Não conta como receita nem despesa.
- **D-002:** Conta ≠ Contexto. Conta é onde o dinheiro está; contexto é para quê.
- **D-003:** Aporte/Resgate de investimento = transferência neutra no MVP.
- **D-004:** Dinheiro usa `decimal`, nunca `double`/`float`. Value Object `Dinheiro`.
- **D-005:** Nenhuma funcionalidade sem teste. Bug corrigido = teste que reproduz o bug.
- **D-006:** Modelo de dados evolui para família/investimentos sem reescrita.
- **D-007:** Contexto e Pessoa são enum agora. Migração para tabela é evolução controlada.
- **D-008:** Moeda BRL no MVP, mas `Dinheiro` já carrega moeda.
- **D-011:** Atualize CLAUDE.md e CONTEXT.md conforme trabalha. Documentação reflete código.

## Escopo do MVP

1. Importação manual de CSV/OFX
2. Normalização de transações
3. Classificação multi-eixo
4. Regras automáticas + sugestão por histórico + fila de revisão
5. ✅ Dashboard: composição + fluxo de caixa realizado (D-019)
6. ✅ Relatório por conta (com total no final)
7. ✅ Filtros por período, categoria, tags, contexto, pessoa
8. ✅ Visão sintética, analítica e drill-down (backend ✅; UI ⏳)
9. ✅ Comparação por período e por categoria
10. Testes automatizados em todos os níveis

## Fora do MVP (Evolução)

- Orçamento/metas
- Investimentos com posição e rendimento
- Multiusuário com autenticação
- Open Finance via API
- Dashboard por contexto/fonte
- Rateio de transação (uma linha → várias transações)
- Acesso remoto seguro (VPN/Tailscale)
- TV Box com Linux como servidor
- Fluxo de caixa projetado (recorrências/lançamentos futuros) — MVP entrega apenas realizado

## Fluxo de Entrada de Dados

Importação (CSV/OFX) → Normalização → Regras automáticas →
Sugestão por histórico → Fila de revisão → Classificação final

## Decisões de Produto

- Comparação = períodos (A) + categorias (B). Real vs. orçado fica fora.
- Dashboard MVP = composição (B) + fluxo de caixa realizado (C) — ver D-019.
- Analítico/Sintético = agregado vs. detalhe, com drill-down.
- Relatório por conta é separado, com total no final.
- Frontend começa simples: tabelas e números antes de gráficos sofisticados.

## Mecanismo Anti-Alucinação

- Este arquivo é a fonte da verdade.
- Decisões são numeradas (D-XXX) e datadas.
- Se a IA contradizer este arquivo, o arquivo vence.
- Usuário é o guardião. IA não é confiável para memória de longo prazo.
- Testes são a memória executável do sistema.

## Histórico de Decisões

| ID | Data | Decisão |
|---|---|---|
| D-001 | 2026-10-10 | Transferência é neutra |
| D-002 | 2026-10-10 | Conta ≠ Contexto |
| D-003 | 2026-10-10 | Aporte/Resgate = transferência neutra |
| D-004 | 2026-10-10 | Dinheiro com decimal + Value Object |
| D-005 | 2026-10-10 | Testes obrigatórios |
| D-006 | 2026-10-10 | Modelo evolutivo sem reescrita |
| D-007 | 2026-10-10 | Contexto e Pessoa como enum no MVP |
| D-008 | 2026-10-10 | BRL no MVP, Dinheiro com moeda |
| D-009 | 2026-10-10 | Estrutura enxuta: 2 projetos |
| D-010 | 2026-10-10 | Frontend: Blazor Server + MudBlazor |
| D-011 | 2026-10-11 | Atualizar CLAUDE.md e CONTEXT.md conforme trabalha |
| D-012 | 2026-10-12 | Classificar retorna 404 se qualquer TagId não existir (não ignora silenciosamente) |
| D-013 | 2026-10-12 | Endpoints PUT com FeatureRequest vazio exigem Content-Type application/json; cliente envia corpo `{}` |
| D-014 | 2026-10-12 | Normalização de descrição é regra de domínio; vive em `Financeiro.Domain.Services.DescricaoNormalizador` e é aplicada pelo construtor de `Transacao` e por `Atualizar`, nunca nos endpoints |
| D-015 | 2026-10-12 | Endpoints de listagem/obtenção não incluem `Tags` via `.Include()` — o `TransacaoResponse` não expõe tags; carregar é desperdício. **Aplica-se a endpoints, não a testes** — testes que verificam persistência de tags devem usar `.Include(t => t.Tags)` |
| D-016 | 2026-10-12 | `Desclassificar` limpa as Tags associadas além de remover a categoria |
| D-017 | 2026-10-12 | Reclassificação é permitida — chamar `Classificar` em transação já classificada sobrescreve a classificação anterior sem erro |
| D-018 | 2026-10-12 | `FeatureResponse` de transações é compartilhado em `TransacaoResponse` (record único), eliminando duplicação entre endpoints |
| D-019 | 2026-10-20 | Dashboard MVP entrega fluxo de caixa **realizado** apenas; projeção futura (recorrências/agendamentos) fica como evolução, dependente de decisão de modelo própria |
| D-020 | 2026-10-22 | Toda resposta da IA deve informar o percentual estimado de contexto em uso no final |

## Como Trabalhar Neste Projeto

### Princípios de Execução

1. **Leia este arquivo inteiro antes de qualquer ação.**
2. **Não contradiga o CONTEXT.md.** Se algo parecer faltar, pergunte antes de assumir.
3. **Uma tarefa por vez.** Não tente implementar múltiplas features de uma vez.
4. **Testes junto com código.** Nenhuma feature entra sem teste.
5. **Não invente requisitos.** Se não está no CONTEXT.md nem no status atual, não existe.
6. **Reportar uso de contexto:** ao final de cada resposta, informar o percentual estimado da janela de contexto em uso.

### Estrutura Física do Projeto

```
Financeiro.sln
src/
  Financeiro.Api/
    Financeiro.Api.csproj
    Program.cs
    Domain/
      Entities/
        Conta.cs
        Categoria.cs
        Transacao.cs
        Tag.cs
      Enums/
        NaturezaTransacao.cs
        TipoConta.cs
        Pessoa.cs
        Contexto.cs
      ValueObjects/
        Dinheiro.cs
        Periodo.cs
      Services/
        DescricaoNormalizador.cs
    Features/
      (Vertical Slices — uma pasta por caso de uso)
    Infrastructure/
      Persistence/
        FinanceiroDbContext.cs
        Migrations/
      Importers/
        ItauCsvParser.cs
    Components/
      (Blazor Server + MudBlazor)
      App.razor
      Routes.razor
      _Imports.razor
      Layout/
        MainLayout.razor
tests/
  Financeiro.Tests/
    Financeiro.Tests.csproj
    Domain/
    Features/
    Infrastructure/
```

### Namespaces

- `Financeiro.Domain.Entities`
- `Financeiro.Domain.Enums`
- `Financeiro.Domain.ValueObjects`
- `Financeiro.Domain.Services`
- `Financeiro.Features.<NomeDaFeature>`
- `Financeiro.Infrastructure.Persistence`
- `Financeiro.Infrastructure.Importers`
- `Financeiro.Api.Components`

Projeto único. Namespaces organizam. Migração futura para múltiplos projetos é possível sem reescrita.

### Convenções de Código

- **Dinheiro:** sempre `Dinheiro` (Value Object). Nunca `decimal` solto em entidade.
- **Datas de transação:** `DateOnly`. Datas de auditoria: `DateTime` em UTC.
- **IDs:** `Guid` gerado no construtor da entidade.
- **Construtores:** `private` para EF Core, `public` para criação real.
- **Setters:** todos `private`. Mudança só via método de comportamento.
- **Validação:** invariantes no construtor + FluentValidation nos requests da API.
- **Nada de saldo em entidade.** Saldo é cálculo.
- **Nada de lógica de negócio em endpoint.** Lógica vai no domínio ou em handler de feature.
- **FastEndpoints:** nunca usar `Request`/`Response` como nome de record — sempre `FeatureRequest`/`FeatureResponse` (colisão com membros da base class).
- **Resposta compartilhada:** endpoints de transações usam `TransacaoResponse` (record único com `FromTransacao`).

### Convenções de Teste

- **xUnit** para todos os testes.
- **FluentAssertions** para asserções.
- **Testcontainers** para testes de integração com PostgreSQL real.
- **WebApplicationFactory** para testes de endpoint.
- **Bunit** para testes de componente Blazor (quando existirem).
- **Nomenclatura:** `MetodoCenarioResultadoEsperado` (ex.: `Classificar_SemCategoria_LancaExcecao`).
- **Padrão:** Arrange / Act / Assert explícito.
- **Um comportamento por teste.**

### Regras Invioláveis (repetidas aqui para reforço)

- Transferência é neutra. Não conta como receita nem despesa.
- Conta ≠ Contexto.
- Aporte/Resgate = transferência neutra no MVP.
- Dinheiro com `decimal`, nunca `double`/`float`.
- Nenhuma funcionalidade sem teste.
- Bug corrigido = teste que reproduz o bug antes da correção.

### O Que NÃO Fazer

- Não criar projetos adicionais sem pedido explícito.
- Não adicionar orçamento, metas, posição de investimento ou multiusuário.
- Não trocar a stack.
- Não usar `double`/`float` para dinheiro.
- Não colocar saldo como propriedade de entidade.
- Não classificar transferência como receita ou despesa.
- Não usar Angular, React ou qualquer frontend JS.
- Não criar tabela para Contexto/Pessoa no MVP (são enums).
- Não ignorar testes para "ir mais rápido".
- Não incluir `Tags` em `.Include()` quando o response não expõe tags.

### Fluxo de Trabalho

1. Ler `CONTEXT.md` inteiro.
2. Ler o status atual no final do arquivo.
3. Confirmar o próximo passo com o usuário antes de agir.
4. Implementar domínio + testes juntos.
5. Rodar `dotnet test` antes de considerar concluído.
6. Atualizar o status no final do `CONTEXT.md`.
7. Nunca avançar sem confirmação do usuário.

### Status Atual

**Concluído:**
- Definição do projeto
- Stack oficial
- Modelo de domínio (Conta, Categoria, Transacao, Tag, Dinheiro, Periodo, enums)
- Estrutura de projeto confirmada (2 projetos, namespaces `Financeiro.Domain.*` dentro de `Financeiro.Api`)
- **Testes de domínio:** 39 testes (xUnit + FluentAssertions)
- **EF Core + DbContext:** configuração completa com complex property para Dinheiro
- **Endpoints FastEndpoints - Contas:** CRUD completo (Criar, Atualizar, Desativar, Listar, Obter)
- **Endpoints FastEndpoints - Transações:** CRUD completo (Criar, Atualizar, Listar, Obter)
- **Infraestrutura de testes de integração:** Testcontainers + WebApplicationFactory + PostgreSQL
- **Testes de integração - Contas:** 5 classes de teste (CriarContaTests, ListarContasTests, ObterContaTests, AtualizarContaTests, DesativarContaTests)
- **Testes de integração - Transações:** 5 classes de teste (CriarTransacaoTests, ListarTransacoesTests, ObterTransacaoTests, AtualizarTransacaoTests, FilaRevisaoTests)
- Importação CSV Itaú
- **Fila de revisão:** `GET /api/transacoes/pendentes`, `PUT /api/transacoes/{Id}/classificar`, `PUT /api/transacoes/{Id}/desclassificar`
- **Refatorações de qualidade:**
  - `TransacaoResponse` compartilhado (eliminou 7 duplicações)
  - `DescricaoNormalizador` movido para `Financeiro.Domain.Services` e aplicado no domínio
  - `Include(Tags)` removido de endpoints que não expõem tags
  - `Desclassificar` limpa Tags
  - Namespace `Financeiro.Infrastructure.Persistence` (sem `.Api.` no meio)
- Filtros na listagem de transações (dataInicio, dataFim, categoriaId, contexto, pessoa, tagId) — 9 testes
- Relatório por conta com total no final (GET /api/relatorios/por-conta) — 5 testes
- Composição por categoria (GET /api/relatorios/composicao) — 6 testes
- Fluxo de caixa realizado (GET /api/relatorios/fluxo-caixa) — 6 testes (D-019: sem projeção)
- Comparação por período e categoria (GET /api/relatorios/comparacao) — 6 testes
- **Suíte total:** 164 testes passando (51 domínio + 113 integração)

**Próximo passo:**
- **Fase 2 — Frontend Blazor + MudBlazor:** telas consumindo os endpoints de relatório
  - Página de relatório por conta (tabela + total no final)
  - Página de composição (tabelas de receitas/despesas por categoria)
  - Página de fluxo de caixa realizado (tabela diária + totais)
  - Página de comparação A vs B (por período + categoria)
  - Filtros de período/categoria/contexto/pessoa/tag nas listagens
  - Drill-down: sintético → analítico

**Pendências de decisão:**
- Como tratar `TipoConta.CartaoCredito` na importação? (fatura vem em XLSX, relação 1:N com transações)
- Futuro: mover `DescricaoNormalizador` para um projeto `Financeiro.Domain` físico? (hoje é pasta dentro de `Financeiro.Api`)
- Futuro: parser OFX (quando houver fonte que exporte nesse formato)