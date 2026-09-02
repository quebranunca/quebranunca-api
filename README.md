# QuebraNunca API

[![CI](https://github.com/quebranunca/quebranunca-api/actions/workflows/ci.yml/badge.svg)](https://github.com/quebranunca/quebranunca-api/actions/workflows/ci.yml)
[![CodeQL](https://github.com/quebranunca/quebranunca-api/actions/workflows/codeql.yml/badge.svg)](https://github.com/quebranunca/quebranunca-api/actions/workflows/codeql.yml)

Backend da Plataforma QuebraNunca Futevôlei. A API concentra autenticação, regras de partidas e grupos, rankings, scouts, competições, convites, arenas, pendências e Pontos QN.

> Repositório proprietário. O uso, a cópia e a distribuição dependem de autorização dos responsáveis pelo projeto.

## Tecnologias

- .NET 10 e ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL
- autenticação JWT
- xUnit para testes unitários e de integração
- Railway para produção

## Arquitetura

O código segue a divisão em camadas existente:

- `PlataformaFutevolei.Api`: endpoints, autenticação e pipeline HTTP;
- `PlataformaFutevolei.Aplicacao`: casos de uso, DTOs e orquestração;
- `PlataformaFutevolei.Dominio`: entidades e regras de domínio;
- `PlataformaFutevolei.Infraestrutura`: EF Core, repositórios, migrations e integrações;
- `PlataformaFutevolei.Admin`: comandos administrativos fora do fluxo público;
- projetos `*.Tests`: testes unitários, de API e de integração.

As regras recorrentes para manutenção estão em [AGENTS.md](AGENTS.md) e os contextos técnicos em [Contextos](Contextos/).

## Ambiente local

Pré-requisitos:

- .NET SDK definido em `global.json`;
- PostgreSQL 16 ou superior;
- Docker, opcional, para iniciar apenas o banco local.

Inicie o PostgreSQL local:

```bash
docker compose -f infra/docker-compose.yml up -d postgres
```

Configure os valores locais com `dotnet user-secrets` no projeto da API. Nunca grave credenciais em `appsettings.*`:

```bash
dotnet user-secrets set --project PlataformaFutevolei.Api "ConnectionStrings:DefaultConnection" "Host=localhost;Port=55432;Database=plataforma_futevolei_dev;Username=postgres;Password=postgres;Ssl Mode=Disable"
dotnet user-secrets set --project PlataformaFutevolei.Api "Jwt:Chave" "substitua-por-uma-chave-local-longa"
```

Restaure, compile e execute em `http://localhost:5080`:

```bash
dotnet restore PlataformaFutevolei.sln
ASPNETCORE_ENVIRONMENT=Development PORT=5080 dotnet run --project PlataformaFutevolei.Api
```

Verificações locais:

- saúde: `http://localhost:5080/health`;
- banco: `http://localhost:5080/db-test`;
- Swagger: `http://localhost:5080/swagger/index.html`.

Se a API local usar um banco compartilhado da Railway, defina `Database__MigrateOnStartup=false` e use a conexão pública/TCP Proxy. O endereço interno da Railway não funciona fora da plataforma.

## Testes

Os testes de integração exigem PostgreSQL. Com o banco local ativo:

```bash
QNF_TEST_DATABASE_URL="Host=localhost;Port=55432;Database=plataforma_futevolei_test;Username=postgres;Password=postgres;Ssl Mode=Disable;Include Error Detail=true" dotnet test PlataformaFutevolei.sln --configuration Release
```

O workflow de CI executa restore, build e todos os testes em cada pull request e em cada atualização de `main`.

## Banco de dados

Migrations do EF Core são a fonte oficial do schema. Para listar ou aplicar migrations localmente:

```bash
dotnet ef migrations list --project PlataformaFutevolei.Infraestrutura --startup-project PlataformaFutevolei.Api
dotnet ef database update --project PlataformaFutevolei.Infraestrutura --startup-project PlataformaFutevolei.Api
```

Em produção, quando `Database:MigrateOnStartup=false`, use [scripts/aplicar-migrations-producao.sh](scripts/aplicar-migrations-producao.sh). Não adicione SQL estrutural ao startup.

## Configuração e deploy

O deploy de produção usa o `Dockerfile` da raiz e [railway.json](railway.json). Connection string, JWT, URLs de ambiente e credenciais de integrações devem ser configurados como variáveis protegidas na Railway.

Antes de promover uma versão:

1. confirme que o CI de `main` está verde;
2. valide migrations no banco de destino;
3. verifique `/health` depois do deploy;
4. crie uma tag SemVer e uma release com as mudanças relevantes.

## Colaboração e segurança

Consulte [CONTRIBUTING.md](CONTRIBUTING.md) antes de abrir uma mudança. Vulnerabilidades devem seguir o canal privado descrito em [SECURITY.md](SECURITY.md), nunca uma issue pública.
