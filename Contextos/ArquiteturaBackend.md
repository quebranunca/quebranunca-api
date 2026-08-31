# ArquiteturaBackend

## Stack

* Backend em .NET 10 (`net10.0`).
* ASP.NET Core Web API.
* Entity Framework Core.
* Banco PostgreSQL.

## Projetos

A solucao backend possui os projetos:

* `PlataformaFutevolei.Api`
* `PlataformaFutevolei.Aplicacao`
* `PlataformaFutevolei.Dominio`
* `PlataformaFutevolei.Infraestrutura`
* `PlataformaFutevolei.Aplicacao.Tests`

## Camadas

### Api

Responsavel por:

* controllers;
* binding HTTP;
* autenticacao;
* autorizacao;
* status code;
* configuracao da aplicacao;
* pipeline HTTP.

Nao deve conter regra de negocio.

### Aplicacao

Responsavel por:

* casos de uso;
* services de aplicacao;
* validacoes de fluxo;
* orquestracao entre dominio e infraestrutura;
* DTOs, requests, responses e mapeadores.

### Dominio

Responsavel por:

* entidades;
* invariantes;
* regras centrais do dominio;
* contratos conceituais.

Nao deve depender de Infraestrutura.

### Infraestrutura

Responsavel por:

* EF Core;
* PostgreSQL;
* repositorios;
* mapeamentos;
* migrations;
* integracoes externas;
* implementacoes tecnicas.

## Program.cs

`Program.cs` deve permanecer enxuto.

Pode conter apenas:

* configuracao;
* DI;
* autenticacao/autorizacao;
* CORS;
* Swagger;
* pipeline HTTP;
* chamada para preparacao do banco.

Nao deve conter:

* regra de negocio;
* SQL estrutural;
* compatibilizacao escondida;
* migrations manuais;
* middlewares duplicados;
* logica de dominio.

## Banco de dados

* Preparacao do banco fica em classe propria de inicializacao.
* Mudancas estruturais devem passar por entidade, mapeamento, DbContext e migration.
* Seed operacional nao substitui migration.
* Compatibilizacao estrutural nao deve ficar escondida no startup.

## Contratos

* Controllers nao expoem entidades diretamente.
* Utilizar DTOs, requests, responses e mapeadores existentes.
* Alteracao de contrato deve considerar impacto no frontend.
* Evitar duplicar DTOs para o mesmo objetivo.

## Repositorios Git

O workspace possui dois repositorios Git separados:

* `quebranunca-api`
* `quebranunca-web`

Validacoes, status, builds e checklists devem ser aplicados por repositorio.

## Guardrails

* Regras de negocio ficam em Aplicacao e Dominio.
* Controllers delegam para Aplicacao.
* Infraestrutura nao define regra de negocio.
* Dominio nao depende de Aplicacao, Api ou Infraestrutura.
* Aplicacao nao depende da Api.
* Reaproveitar services, repositorios e mapeadores existentes antes de criar novos.
* Evitar abstracoes prematuras.
* Evitar fluxo tecnico paralelo quando existir implementacao reutilizavel.
