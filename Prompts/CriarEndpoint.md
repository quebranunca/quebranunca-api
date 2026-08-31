# CriarEndpoint

Planeje e implemente um endpoint backend da Plataforma QuebraNunca Futevolei.

## Antes de implementar

1. Ler os contextos e agentes aplicaveis.
2. Identificar se existe endpoint semelhante.
3. Identificar se existe service existente reutilizavel.
4. Identificar se existe DTO existente reutilizavel.
5. Identificar se existe regra de negocio ja implementada.
6. Confirmar que nao existe fluxo paralelo para o mesmo objetivo.

## Planejamento

Descrever:

* objetivo do endpoint;
* perfil autorizado;
* validacao de propriedade do recurso;
* impacto em dominio;
* impacto em ranking;
* impacto em pendencias;
* impacto em aprovacoes;
* impacto em privacidade.

## Implementacao

### Controller

* Controller permanece fino.

* Controller cuida apenas de:

  * HTTP;
  * binding;
  * autorizacao;
  * status code.

* Controller nao contem regra de negocio.

* Controller nao acessa persistencia diretamente.

### Aplicacao

* Regras ficam em Aplicacao e Dominio.
* Reutilizar services existentes quando possivel.
* Reutilizar validacoes existentes quando possivel.

### Contratos

* Reutilizar DTOs quando fizer sentido.
* Criar novos DTOs apenas quando necessario.
* Nao expor entidades diretamente.

### Seguranca

* Resposta publica nao expoe:

  * e-mail;
  * telefone;
  * token;
  * permissao;
  * dados administrativos.

* Validar autenticacao.

* Validar autorizacao.

* Validar propriedade do recurso quando aplicavel.

### Performance

* Propagar `CancellationToken`.
* Evitar consultas desnecessarias.
* Evitar carregamento excessivo de relacionamentos.

## Revisao obrigatoria

Revisar impacto em:

* DTOs;
* mapeadores;
* services;
* repositories;
* testes;
* frontend;
* documentacao;
* contextos .ai.

## Testes

Validar:

* sucesso;
* autorizacao;
* recurso inexistente;
* regras de negocio;
* casos de erro.

## Entrega

Informar:

* arquivos alterados;
* services reutilizados;
* DTOs reutilizados;
* regras de negocio afetadas;
* testes adicionados;
* atualizacoes necessarias nos contextos ou agentes.
