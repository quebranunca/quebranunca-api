# BackendReviewer

## Objetivo

Validar se a alteracao backend respeita a arquitetura, o dominio e os padroes existentes da Plataforma QuebraNunca Futevolei.

## Checklist

### Arquitetura

* Alteracao respeita as camadas existentes.
* Dominio nao depende de Infraestrutura.
* Controller nao contem regra de negocio.
* Controller nao acessa persistencia diretamente.
* Program.cs permanece enxuto.
* Nao foram criadas abstrações desnecessarias.

### Aplicacao e Dominio

* Services mantem regras centralizadas.
* Mensagens de negocio permanecem em portugues.
* Regras configuraveis por competicao continuam sendo respeitadas.
* Nao existe hardcode de regras esportivas.
* Nao existem regras duplicadas em multiplos services.
* Mudanca respeita os contextos oficiais da plataforma.

### Contratos

* DTOs nao expoem entidades diretamente.
* Requests e Responses permanecem consistentes.
* Alteracoes de contrato foram avaliadas quanto a compatibilidade.
* Endpoints existentes foram reutilizados quando possivel.

### Persistencia

* Entidades permanecem consistentes com o dominio.
* Migrations sao realmente necessarias.
* Nao existe duplicacao de dados.
* Nao existe criacao de entidade paralela para conceito existente.

### Fluxos

* Fluxos em lote reutilizam servicos existentes.
* Fluxos novos reutilizam regras existentes quando possivel.
* Mudanca nao cria fluxo paralelo desnecessario.

### Seguranca e Autorizacao

* Autorizacao continua respeitada.
* Permissoes por perfil continuam validas.
* Permissoes por propriedade do recurso continuam validas.
* Dados pessoais nao foram expostos indevidamente.

### Integridade do Produto

* Mudanca nao cria bypass de ranking.
* Mudanca nao cria bypass de pendencias.
* Mudanca nao cria bypass de convites.
* Mudanca nao cria bypass de aprovacao de partidas.
* Mudanca nao cria bypass de autorizacao.

### Testes

* Testes cobrem a regra alterada quando existir risco.
* Casos de sucesso foram validados.
* Casos de erro foram validados.
* Casos de autorizacao foram validados quando aplicavel.

## Resultado

Classificar:

* Critico
* Alto
* Medio
* Baixo

Para cada achado informar:

* problema
* impacto
* recomendacao
* arquivos envolvidos
