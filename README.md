# Korp_Teste_ArthurMendes

Dois microsserviços ASP.NET Core (**Estoque** e **Faturamento**) com PostgreSQL, Entity Framework Core e Swagger. O **Faturamento** chama o **Estoque** na impressão de notas fiscais (baixa de saldo) e gera PDF com QuestPDF.

## Requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download) (projetos com `TargetFramework` `net10.0`)
- [Docker](https://docs.docker.com/get-docker/) (opcional, para `docker-compose`)
- PostgreSQL 15 (se não usar o Compose para os bancos)

## Estrutura

| Caminho | Descrição |
|--------|------------|
| `Estoque/` | API de produtos e movimentação de estoque |
| `Faturamento/` | API de notas fiscais e geração de PDF |
| `Korp_Teste_ArthurMendes.slnx` | Solução com os dois projetos |
| `docker-compose.yml` | Postgres (dois bancos), Estoque e Faturamento em contêineres |

## Executar com Docker Compose

Na raiz do repositório:

```powershell
docker compose up --build
```

Serviços e portas expostas no host:

| Serviço | URL / porta |
|---------|-------------|
| Estoque | `http://localhost:5259` |
| Faturamento | `http://localhost:5260` |
| Postgres (estoque) | `localhost:5432` — banco `estoque_db`, usuário/senha `postgres` |
| Postgres (faturamento) | `localhost:5433` — banco `faturamento_db`, usuário/senha `postgres` |

Variáveis usadas pelos contêineres das APIs estão definidas em `docker-compose.yml` (`ConnectionStrings__DefaultConnection`, `EstoqueServiceBaseUrl` no Faturamento, etc.).

## Executar localmente (sem Compose nas APIs)

1. Suba dois PostgreSQL compatíveis com as connection strings de `Estoque/appsettings.json` e `Faturamento/appsettings.json` (portas **5432** para `estoque_db` e **5433** para `faturamento_db`, ou ajuste os arquivos).
2. Inicie o Estoque antes do Faturamento (o Faturamento usa `EstoqueServiceBaseUrl`, por padrão `http://localhost:5259`).

```powershell
dotnet run --project Estoque/Estoque.csproj
dotnet run --project Faturamento/Faturamento.csproj
```

URLs padrão de desenvolvimento (`Properties/launchSettings.json`):

- Estoque: `http://localhost:5259`
- Faturamento: `http://localhost:5260`

Compilar a solução:

```powershell
dotnet build Korp_Teste_ArthurMendes.slnx
```

## Documentação e saúde

Em ambas as APIs:

- **Swagger UI:** `/swagger`
- **Health check:** `/health`

## Configuração (`appsettings.json`)

- **Estoque:** `ConnectionStrings:DefaultConnection`, `Cors:AllowedOrigins` (padrão inclui `http://localhost:4200`).
- **Faturamento:** os itens acima mais `EstoqueServiceBaseUrl` (URL base do microsserviço de Estoque).

CORS também permite origens em loopback e hosts terminados em `.vercel.app`, conforme `Program.cs` de cada projeto.

## API Estoque (`api/Produtos`)

| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `api/Produtos` | Lista produtos |
| GET | `api/Produtos/{codigo}` | Obtém produto pelo código |
| POST | `api/Produtos` | Cria produto (corpo: `codigo`, `descricao`, `saldoInicial` opcional) |
| POST | `api/Produtos/{codigo}/saida` | Saída de estoque |
| POST | `api/Produtos/{codigo}/entrada` | Entrada de estoque |
| PUT | `api/Produtos/{codigo}/saldo` | Define saldo absoluto (`novoSaldo`) |

Várias operações aceitam o cabeçalho opcional `Idempotency-Key` (comportamento implementado em `ProdutosController` e `IIdempotencyService`).

## API Faturamento (`api/Invoices`)

| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `api/Invoices` | Lista notas fiscais |
| GET | `api/Invoices/{id}` | Obtém nota por id |
| POST | `api/Invoices` | Cria nota (corpo: modelo `Invoice` com itens) |
| POST | `api/Invoices/{id}/print` | Fecha a nota, integra com Estoque e retorna PDF (`application/pdf`) |

Status da nota: enum `InvoiceStatus` (`Aberta`, `Fechada`) em `Faturamento/Models/Invoice.cs`. A impressão exige nota em estado adequado (comportamento descrito nos comentários XML do controller).

## Arquivos HTTP de exemplo

- `Estoque/Korp_Teste_ArthurMendes.http`
- `Faturamento/Faturamento.http`
