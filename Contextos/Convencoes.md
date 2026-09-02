# Convencoes

## Codigo

* Preservar nomes, mensagens e padrao em portugues.
* Reutilizar servicos existentes antes de criar novos.
* Reutilizar DTOs existentes antes de criar novos.
* Reutilizar mapeadores existentes antes de criar novos.
* Reutilizar repositorios existentes antes de criar novos.
* Evitar abstracoes prematuras.
* Evitar duplicacao de regras de negocio.

## Aplicacao

* Importacao CSV deve passar pelos servicos de aplicacao.
* Nao criar importador paralelo para fluxo existente.
* Regras de negocio devem permanecer centralizadas.
* Controllers nao devem conter regra de negocio.
* Controllers nao devem acessar persistencia diretamente.

## Contratos

* Controllers nao expoem entidades diretamente.
* Utilizar DTOs para requests e responses.
* Alteracoes de contrato devem considerar impacto no frontend.
* Evitar DTOs duplicados para o mesmo objetivo.

## Configuracao

* Fora de Development, configuracoes obrigatorias ausentes devem falhar explicitamente.
* Nao utilizar fallback local em producao.
* Segredos nao devem ficar hardcoded.
* Variaveis obrigatorias devem ser validadas durante inicializacao.

## Infraestrutura

* Antes de validar localmente, conferir:

  * Docker;
  * containers;
  * portas;
  * banco de dados;
  * variaveis de ambiente.

* Compatibilizacao estrutural nao deve ficar escondida em Program.cs.

* Preparacao do banco deve ficar em classe propria.

## E-mail

* Templates devem reutilizar branding QNF.
* Templates devem ser responsivos.
* Templates devem ser simples.
* Templates devem ser compativeis com clientes de e-mail.
* Templates devem funcionar sem CSS avancado.

## Dominio

* Regras oficiais de Arena e compatibilidade legada de `/api/locais` ficam em `../AGENTS.md` e nos fluxos existentes do domínio.
* Regras de autenticacao e convites ficam em `Autenticacao.md`.
* Regras de negócio oficiais ficam em `../AGENTS.md` e nos arquivos deste diretório.

## Evolucao

Antes de criar:

* novo endpoint;
* novo service;
* novo DTO;
* novo repositorio;
* nova entidade;

verificar se existe implementacao equivalente reutilizavel.

Preferir extensao de comportamento antes de criar estruturas paralelas.
