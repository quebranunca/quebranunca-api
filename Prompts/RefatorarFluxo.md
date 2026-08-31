# RefatorarFluxo

Refatore um fluxo backend existente sem criar solucao paralela.

## Objetivo

Melhorar ou corrigir um fluxo existente preservando o comportamento esperado, os contratos publicos e a consistencia do dominio.

## Antes de alterar

Mapeie:

* entrada do fluxo;
* controller;
* endpoint;
* request;
* response;
* service principal;
* services auxiliares;
* repositorios;
* entidades;
* DTOs;
* mapeadores;
* testes existentes;
* consumidores frontend.

## Analise

Verifique impacto em:

* dominio;
* ranking;
* historico;
* pendencias;
* aprovacoes;
* convites;
* autorizacao;
* persistencia;
* frontend.

## Regras

* Nao criar service paralelo para o mesmo fluxo.
* Nao criar endpoint paralelo quando o existente puder ser ajustado.
* Nao duplicar regra de negocio.
* Preservar contratos quando possivel.
* Regras de negocio devem permanecer em Aplicacao/Dominio.
* Controller deve continuar fino.
* Persistencia deve continuar via repositorios/EF Core conforme padrao existente.

## Implementacao

Implemente em passos pequenos e verificaveis:

1. Isolar o comportamento atual.
2. Ajustar regra central.
3. Atualizar contratos somente se necessario.
4. Ajustar consumidores impactados.
5. Atualizar testes.
6. Validar build.
7. Revisar regressao.

## Testes

Validar:

* fluxo principal;
* casos de erro;
* autorizacao;
* impacto em ranking;
* impacto em pendencias;
* compatibilidade com frontend.

## Entrega

Informar:

* fluxo refatorado;
* arquivos alterados;
* contratos preservados;
* contratos alterados;
* regras centralizadas;
* riscos encontrados;
* testes executados;
* atualizacoes necessarias em AGENTS ou Contextos.
