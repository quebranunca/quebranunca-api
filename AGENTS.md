# Diretrizes do backend

- O backend segue a arquitetura em camadas já existente: `Api`, `Aplicacao`, `Dominio` e `Infraestrutura`
- Antes de implementar, analisar o fluxo atual, localizar as camadas impactadas e só então editar
- `Program.cs` deve ficar enxuto: configuração, registro de serviços, autenticação/autorização, CORS, Swagger, pipeline HTTP e chamada da inicialização do banco
- Preparação do banco deve ficar centralizada em classe própria; evitar lógica de banco espalhada no startup
- Migrations do EF Core são a fonte oficial de evolução do schema
- Não adicionar SQL estrutural em startup, middleware, controller ou serviço de aplicação para criar/alterar tabela, coluna, índice ou foreign key
- Ao mudar entidade ou relacionamento, revisar `DbContext`, mapeamentos Fluent API, migrations, repositórios, serviços, DTOs e endpoints afetados
- Seed operacional e validação de conexão não substituem migration
- Em `Development`, tolerância maior na subida pode existir se já adotada no projeto; fora disso, falha de preparação do banco deve interromper a aplicação
- Reutilizar padrões existentes do repositório antes de criar abstração nova
- Para `Staging` e `Production`, não confiar em fallback local para connection string, JWT, `Frontend:Url` ou URLs de convite
- Não deixar segredos reais em `appsettings.*`, exemplos locais, chats ou arquivos de publish
- O primeiro usuário `Administrador` é bootstrap operacional fora do fluxo normal: não criar endpoint público, seed automático em startup ou bypass no `Program.cs`
- Para bootstrap do primeiro `Administrador`, gerar `senha_hash` com `scripts/gerar-hash-senha-admin.sh` e inserir/promover no banco com `perfil = 1`, `ativo = true` e `atleta_id = null`
- Quando `Database:MigrateOnStartup` estiver desabilitado em produção, aplicar migrations via `scripts/aplicar-migrations-producao.sh`; não substituir migration por SQL estrutural manual no startup
- Antes de subir `master`, revisar também `.gitignore`, artefatos de publish, documentação de deploy e checklist operacional
