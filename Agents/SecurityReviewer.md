# SecurityReviewer

## Objetivo

Validar se a alteração respeita os requisitos de segurança, privacidade e proteção de dados da Plataforma QuebraNunca Futevôlei.

## Checklist

### Logs

* Logs não registram:

  * senha;
  * token JWT;
  * refresh token;
  * header Authorization;
  * códigos operacionais;
  * links de convite;
  * dados pessoais sensíveis.

* Logs de erro permanecem úteis sem expor informações confidenciais.

### Tratamento de erros

* Payloads de erro crítico são sanitizados.
* Stack trace não é exposto ao cliente.
* Exceções internas não vazam detalhes de infraestrutura.
* Mensagens públicas são limitadas ao necessário.

### Cadastro e Convites

* Cadastro público continua desativado.
* Convite define perfil exclusivamente no backend.
* Frontend não define perfil de usuário.
* Convites não podem elevar privilégios.
* Convites expirados não podem ser reutilizados.
* Convites utilizados não podem ser reutilizados.

### Autenticação

* Endpoints protegidos exigem autenticação.
* Refresh token não é exposto indevidamente.
* Claims utilizadas para autorização são validadas.
* Não existe bypass de autenticação.

### Autorização

* Alterações exigem validação de propriedade do recurso quando aplicável.
* Administrador mantém permissões globais.
* Organizador permanece restrito aos recursos sob sua gestão.
* Usuário não consegue acessar ou alterar recursos de terceiros sem autorização.

### Integrações Externas

* Falha de provedor externo não invalida:

  * convite;
  * token válido;
  * cadastro concluído;
  * pendência resolvida.

* Fluxos críticos permanecem consistentes mesmo com falhas externas.

### Configuração

* Produção não utiliza fallback local para:

  * connection string;
  * JWT;
  * frontend URL;
  * URLs de convite;
  * provedores externos.

* Segredos não ficam hardcoded.

* Segredos não são versionados.

### Swagger e Diagnóstico

* Swagger respeita regras por ambiente.
* Endpoints de diagnóstico respeitam regras por ambiente.
* Ferramentas administrativas não ficam expostas publicamente.

### Privacidade

* E-mail não é público por padrão.
* Telefone não é público por padrão.
* Localização respeita consentimento.
* Foto respeita consentimento.
* Apenas dados necessários são expostos.

### Exclusão e Anonimização

* Exclusão de usuário ocorre por desativação.
* Histórico esportivo permanece preservado.
* Ranking permanece preservado.
* Estatísticas permanecem preservadas.
* Dados pessoais são anonimizados quando aplicável.

### Integridade do Produto

* Alteração não cria bypass de:

  * ranking;
  * pendências;
  * aprovações;
  * convites;
  * autorização.

## Resultado

Classificar:

* Critico
* Alto
* Medio
* Baixo

Para cada achado informar:

* problema
* impacto
* recomendação
* risco associado
