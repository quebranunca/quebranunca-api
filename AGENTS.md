# Diretrizes do backend

- O backend segue a arquitetura em camadas já existente: `Api`, `Aplicacao`, `Dominio` e `Infraestrutura`
- Antes de implementar, analisar o fluxo atual, localizar as camadas impactadas e só então editar
- `Program.cs` deve ficar enxuto: configuração, registro de serviços, autenticação/autorização, CORS, Swagger, pipeline HTTP e chamada da inicialização do banco
- Preparação do banco deve ficar centralizada em classe própria; evitar lógica de banco espalhada no startup
- Migrations do EF Core são a fonte oficial de evolução do schema
- Migrations manuais precisam estar no catálogo do EF: incluir `[DbContext(typeof(PlataformaFutevoleiDbContext))]` e `using Microsoft.EntityFrameworkCore.Infrastructure;`; se faltar, a classe pode compilar, mas `dotnet ef migrations list` não mostra a migration e ela não será aplicada
- Não adicionar SQL estrutural em startup, middleware, controller ou serviço de aplicação para criar/alterar tabela, coluna, índice ou foreign key
- Ao mudar entidade ou relacionamento, revisar `DbContext`, mapeamentos Fluent API, migrations, repositórios, serviços, DTOs e endpoints afetados
- Seed operacional e validação de conexão não substituem migration
- Em `Development`, tolerância maior na subida pode existir se já adotada no projeto; fora disso, falha de preparação do banco deve interromper a aplicação
- Reutilizar padrões existentes do repositório antes de criar abstração nova
- Templates de e-mail devem reutilizar o branding QNF e os assets públicos de `public/branding`, mantendo consistência visual com app e artes de compartilhamento
- Conteúdo HTML de e-mail deve priorizar layout simples, responsivo e compatível com clientes de e-mail, com estilos inline e versão em texto equivalente
- Para `Staging` e `Production`, não confiar em fallback local para connection string, JWT, `Frontend:Url` ou URLs de convite
- Não deixar segredos reais em `appsettings.*`, exemplos locais, chats ou arquivos de publish
- O primeiro usuário `Administrador` é bootstrap operacional fora do fluxo normal: não criar endpoint público, seed automático em startup ou bypass no `Program.cs`
- Para bootstrap do primeiro `Administrador`, gerar `senha_hash` com `scripts/gerar-hash-senha-admin.sh` e inserir/promover no banco com `perfil = 1`, `ativo = true` e `atleta_id = null`
- Quando `Database:MigrateOnStartup` estiver desabilitado em produção, aplicar migrations via `scripts/aplicar-migrations-producao.sh`; não substituir migration por SQL estrutural manual no startup
- Antes de assumir que produção está migrada, validar `dotnet ef migrations list` contra o banco alvo e conferir se migrations críticas aparecem como aplicadas; no caso do endpoint `POST /api/partidas`, conferir especialmente `20260401103000_AdicionarCriadoPorUsuarioNaPartida`, `20260401233000_AdicionarFluxoAprovacaoResultados` e `20260402213000_CompatibilizarStatusAprovacaoPartidas`
- Antes de subir `master`, revisar também `.gitignore`, artefatos de publish, documentação de deploy e checklist operacional

## Contexto local recorrente

- Banco local de desenvolvimento costuma usar Postgres em `localhost:55432`, database `quebranunca`, conforme `PlataformaFutevolei.Api/appsettings.Development.json`
- Para teste manual da API, usar `ASPNETCORE_URLS=http://localhost:5080 ASPNETCORE_ENVIRONMENT=Development dotnet run --project PlataformaFutevolei.Api --no-build --no-launch-profile`
- Validar disponibilidade com `GET http://localhost:5080/health`
- Em `Development`, login local pode usar o fluxo de código: `POST /api/autenticacao/login/codigo/solicitar` e depois `POST /api/autenticacao/login/codigo`; não registrar tokens ou códigos gerados em arquivos
- Convites de cadastro usam código curto no formato `000-000`; manter um único código vigente por convite e reutilizá-lo em link, e-mail e WhatsApp
- Não regenerar código de convite como efeito colateral de consultar link, enviar e-mail ou enviar WhatsApp; regeneração só deve existir como ação explícita e rastreável
- Ao persistir código de convite para reenvio, manter também o hash usado na validação e limpar o código em claro quando o convite for utilizado
- Para testar `POST /api/partidas` sem competição prévia, o caminho mais simples é partida de `Grupo`: informar `grupoId` ou `nomeGrupo`; se ambos ficarem vazios, a API usa/cria automaticamente o grupo global `Geral`. Informar quatro atletas por `Id` ou `Nome`, `status` `1` para agendada ou `2` para encerrada, e placar válido quando encerrada
- Regra importante da partida de grupo: se o usuário autenticado for atleta, o atleta vinculado ao usuário precisa estar na primeira dupla
- Se a API retornar erro de coluna/tabela inexistente apesar de dizer que migrations estão atualizadas, conferir `__EFMigrationsHistory` contra os arquivos em `PlataformaFutevolei.Infraestrutura/Persistencia/Migracoes`; corrigir o banco local alinhando com migrations existentes, nunca adicionando SQL estrutural ao startup ou aos serviços

## Contexto Railway/produção

- `postgres.railway.internal` só resolve dentro da rede privada da Railway; para comandos locais como `dotnet ef database update`, usar a URL pública/TCP Proxy (`*.proxy.rlwy.net` com porta numérica)
- Em `Production`, a API exige `Jwt__Chave` e `Frontend__Url` válidos; para debug local temporário, pode-se passar por variável de ambiente, sem salvar secrets no repositório
- Não colocar connection string real, senha de banco ou chave JWT em `appsettings.Production.json`; se isso acontecer, remover antes de commit e rotacionar o segredo exposto
- Se `POST /api/partidas` falhar em produção com coluna/tabela faltando, validar no banco alvo a existência de `partidas.status_aprovacao`, `partidas_aprovacoes` e `pendencias_usuarios`, e validar pelo EF se as migrations acima aparecem no catálogo
