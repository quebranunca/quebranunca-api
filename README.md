# Base de Conhecimento Backend QNF

Conhecimento especializado do backend da Plataforma QuebraNunca Futevolei.

## Escopo

- Arquitetura .NET, ASP.NET Core, EF Core e PostgreSQL.
- Controllers, services, DTOs, repositorios, mapeamentos e migrations.
- Autenticacao, autorizacao, integracoes externas e seguranca operacional.

## Fora do escopo

- Decisao de produto pura: usar `../../Contextos`.
- UX, componentes e layout: usar `../Web`.

## Uso

Leia `Contextos/ArquiteturaBackend.md` antes de alterar fluxo backend e use os agentes de revisao conforme o tipo de mudanca.


# Plataforma QuebraNunca Futevôlei

Plataforma web para registro de partidas, grupos, rankings, campeonatos, convites e arenas de futevôlei.

---

## Estrutura do Workspace

Este workspace contém dois repositórios Git separados:

- `quebranunca-api`: backend .NET 10, ASP.NET Core Web API, EF Core e PostgreSQL.
- `quebranunca-web`: frontend React + Vite em JavaScript.

A raiz do workspace concentra documentação compartilhada, `infra/`, scripts e a base de conhecimento `.ai`.

Projetos principais do backend:

- `quebranunca-api/PlataformaFutevolei.Api`
- `quebranunca-api/PlataformaFutevolei.Aplicacao`
- `quebranunca-api/PlataformaFutevolei.Dominio`
- `quebranunca-api/PlataformaFutevolei.Infraestrutura`
- `quebranunca-api/PlataformaFutevolei.Aplicacao.Tests`

---

## Conhecimento do Projeto

- `AGENTS.md`: guardrails operacionais do workspace.
- `.ai/INDEX.md`: ponto central de navegação da base de conhecimento.
- `.ai/Contextos`: regras e decisões de domínio.
- `.ai/Projetos/Api`: arquitetura, persistência, segurança e integrações backend.
- `.ai/Projetos/Web`: arquitetura frontend, navegação, componentes, design system e integração com API.
- `CHECKLIST-MASTER.md`: checklist de prontidão por repositório.

---

## Fluxo Obrigatório para IA

Antes de qualquer implementação:

1. Ler `AGENTS.md`.
2. Ler `.ai/INDEX.md`.
3. Identificar domínio impactado.
4. Ler os contextos relevantes.
5. Executar mentalmente:
   - MapearContexto
   - PlanejarFeature
6. Implementar.
7. Executar:
   - AuditarFluxo
   - Checklist aplicável.
8. Atualizar contextos quando uma regra recorrente mudar.

---

## Reutilização Obrigatória

Antes de criar:

- entidade
- endpoint
- DTO
- service
- repository
- componente
- hook
- helper

verificar implementações existentes.

Priorizar extensão antes de criação.

Evitar:

- fluxo paralelo
- endpoint paralelo
- service paralelo
- componente paralelo

sem justificativa clara.

---

## Fonte Oficial de Verdade

### Frontend

Responsável por:

- apresentar dados
- coletar entradas
- controlar navegação
- apresentar estados

### Backend

Responsável por:

- validar regras
- validar permissões
- validar ownership
- validar domínio

A API é a fonte oficial de verdade.

Não duplicar regras de negócio entre frontend e backend.

---

## Decisões Atuais

- Cadastro público está desativado.
- Convites de cadastro criam usuários do tipo `Atleta` no fluxo padrão atual.
- Primeiro usuário `Administrador` deve ser criado por bootstrap operacional fora do fluxo normal.
- Arena é o domínio oficial de local esportivo.
- `/api/locais` existe apenas como compatibilidade legada para clientes antigos e delega para Arena.
- Em partida de grupo, o usuário autenticado que registra precisa pertencer ao grupo, exceto permissões administrativas previstas.
- Atletas informados na partida não precisam estar previamente no grupo; a API vincula automaticamente os ausentes ao salvar.
- Grupo Geral mantém fluxo livre/manual.

---

## Domínios Críticos

Qualquer alteração deve avaliar impacto em:

- Atletas
- Partidas
- Grupos
- Ranking
- Aprovações
- Pendências
- Competições
- Ligas
- Arenas
- Convites
- Dashboards

Se houver impacto indireto:

- documentar
- explicar
- validar antes da implementação

---

## Docker Local

O `docker-compose` em `infra/` sobe PostgreSQL e API para desenvolvimento local.

```bash
cd infra
docker compose up --build -d
```

Serviços padrão:

- API: `http://localhost:5000`
- Swagger: `http://localhost:5000/swagger`
- PostgreSQL: `localhost:55432`

Observações:

- A API do container roda em `Development` e escuta internamente em `8080`, exposta como `5000` pelo compose.
- Se quiser apenas o banco para desenvolvimento manual, rode `docker compose up -d postgres`.
- Antes de subir backend/frontend manualmente, confira se containers ou processos locais já estão usando as mesmas portas.

---

## Backend Local sem Container da API

### 1. Subir PostgreSQL

```bash
cd infra
docker compose up -d postgres
```

### 2. Executar API

```bash
cd ../quebranunca-api
PORT=5000 ASPNETCORE_ENVIRONMENT=Development dotnet run --project PlataformaFutevolei.Api --no-launch-profile
```

### 3. Validar

```bash
curl -s http://localhost:5000/health
```

Observação:

`Program.cs` usa a variável `PORT` e, sem override, assume `8080`.

---

## Frontend Local

```bash
cd quebranunca-web
npm install
npm run dev
```

Frontend:

```text
http://localhost:5173
```

Utilize:

```text
quebranunca-web/.env.example
```

como base para configuração local.

Sem override, o frontend utiliza `/api` e o proxy configurado em `vite.config.js`.

---

## Staging e Produção

O backend possui:

- `appsettings.Staging.json`
- `appsettings.Production.json`

Defaults restritivos:

```text
Database:MigrateOnStartup=false
Database:ValidateOnStartup=true
Diagnostics:EnableSwagger=false
Diagnostics:EnableDbTestEndpoint=false
HttpsRedirection:Enabled=true
```

---

## Variáveis Mínimas Fora de Development

```bash
ASPNETCORE_ENVIRONMENT=Production

ConnectionStrings__DefaultConnection=Host=...;Port=5432;Database=...;Username=...;Password=...;Ssl Mode=Require;Trust Server Certificate=true

Jwt__Chave=uma-chave-forte-e-unica
Jwt__Emissor=PlataformaFutevolei.Api
Jwt__Audiencia=PlataformaFutevolei.Web

Frontend__Url=https://app.seudominio.com

EmailConvitesCadastro__UrlApp=https://app.seudominio.com
WhatsappConvitesCadastro__UrlApp=https://app.seudominio.com
```

---

## E-mail

Convites:

- `EmailConvitesCadastro`

Login por código:

- `EmailCodigoLogin`

Pode reaproveitar:

- `EmailConvitesCadastro`
- `RESEND_API_KEY`

quando a configuração específica não estiver preenchida.

Sem provedor configurado:

- convite continua válido
- envio fica pendente/manual

---

## Variáveis Comuns de E-mail

```bash
EmailConvitesCadastro__ApiKey=...
EmailConvitesCadastro__RemetenteEmail=plataforma@seudominio.com
EmailConvitesCadastro__RemetenteNome=Plataforma QuebraNunca Futevolei
EmailConvitesCadastro__ReplyTo=contato@seudominio.com

EmailCodigoLogin__ApiKey=...
EmailCodigoLogin__RemetenteEmail=plataforma@seudominio.com
EmailCodigoLogin__RemetenteNome=Plataforma QuebraNunca Futevolei
EmailCodigoLogin__UrlApp=https://app.seudominio.com
```

---

## Observações de Produção

- A API falha ao iniciar se `Jwt:Chave` estiver vazia ou usando placeholder.
- `Frontend:Url` deve apontar para URL pública real.
- Não utilizar localhost em Staging ou Production.
- Application Insights é opcional.

---

## Azure App Service + Key Vault

Arquitetura recomendada:

- Backend: Azure App Service
- Banco: Azure PostgreSQL Flexible Server
- Segredos: Azure Key Vault
- Frontend: Azure Static Web Apps ou App Service

### Configurações não sensíveis

```bash
ASPNETCORE_ENVIRONMENT=Production
Jwt__Emissor=PlataformaFutevolei.Api
Jwt__Audiencia=PlataformaFutevolei.Web
Jwt__ExpiracaoMinutos=120

Frontend__Url=https://app.seudominio.com

EmailConvitesCadastro__UrlApp=https://app.seudominio.com
WhatsappConvitesCadastro__UrlApp=https://app.seudominio.com

Database__MigrateOnStartup=false

Diagnostics__EnableSwagger=false
Diagnostics__EnableDbTestEndpoint=false

WhatsappConvitesCadastro__Enabled=false
```

### Segredos

Utilizar Key Vault References.

---

## Build e Publicação

### Backend

```bash
cd quebranunca-api

dotnet build PlataformaFutevolei.sln

dotnet publish PlataformaFutevolei.Api \
  -c Release \
  -o ./publish
```

### Frontend

```bash
cd quebranunca-web

npm run build
```

Variáveis possíveis:

```bash
VITE_API_URL=https://api.seudominio.com
```

ou

```bash
VITE_API_BASE_URL=https://app.seudominio.com/api
```

---

## Migrations

Quando:

```text
Database:MigrateOnStartup=false
```

Aplicar manualmente:

```bash
cd quebranunca-api

dotnet ef database update \
  --project PlataformaFutevolei.Infraestrutura \
  --startup-project PlataformaFutevolei.Api \
  --configuration Release
```

Script operacional:

```bash
scripts/aplicar-migrations-producao.sh
```

---

## Bootstrap Inicial

O primeiro Administrador deve ser criado fora do fluxo padrão.

Motivos:

- Cadastro público desativado.
- Convites comuns criam Atletas.
- Convites exigem autenticação administrativa.

Gerar hash:

```bash
scripts/gerar-hash-senha-admin.sh
```

Promover usuário:

```text
perfil = 1
ativo = true
atleta_id = null
```

---

## Checklist de Master

Antes de publicar ou mergear:

```text
CHECKLIST-MASTER.md
```

Aplicar separadamente para:

- quebranunca-api
- quebranunca-web

---

## Domínios Customizados

Para App Service:

- CNAME apontando para `<app>.azurewebsites.net`
- TXT `asuid` conforme validação do Azure

Erro:

```text
DNS_PROBE_FINISHED_NXDOMAIN
```

normalmente indica DNS inexistente, não falha da aplicação.

---

## Autenticação

Endpoints principais:

```text
POST /api/autenticacao/registrar
POST /api/autenticacao/login
POST /api/autenticacao/login/codigo/solicitar
POST /api/autenticacao/login/codigo
GET  /api/autenticacao/me
```

JWT:

```text
Authorization: Bearer <token>
```
