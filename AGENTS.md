# Diretrizes do backend

- Este é um projeto existente; antes de mudar comportamento, entender e estender os fluxos atuais em vez de tratar como implementação nova
- O backend segue a arquitetura em camadas já existente: `Api`, `Aplicacao`, `Dominio` e `Infraestrutura`
- Antes de implementar, analisar o fluxo atual, localizar as camadas impactadas e só então editar
- Regra de domínio deve ficar fora de controllers; controllers permanecem finos e delegam para a aplicação
- `Program.cs` deve ficar enxuto: configuração, registro de serviços, autenticação/autorização, CORS, Swagger, pipeline HTTP e chamada da inicialização do banco
- Preparação do banco deve ficar centralizada em classe própria; evitar lógica de banco espalhada no startup
- Migrations do EF Core são a fonte oficial de evolução do schema
- Migrations manuais precisam estar no catálogo do EF: incluir `[DbContext(typeof(PlataformaFutevoleiDbContext))]` e `using Microsoft.EntityFrameworkCore.Infrastructure;`; se faltar, a classe pode compilar, mas `dotnet ef migrations list` não mostra a migration e ela não será aplicada
- `Atleta.email` possui unicidade por e-mail normalizado via índice funcional PostgreSQL `ix_atletas_email_normalizado_unico`, criado em migration SQL manual. Não recriar como índice literal em `Email` e não remover em baselines/recriações de migrations.
- Não adicionar SQL estrutural em startup, middleware, controller ou serviço de aplicação para criar/alterar tabela, coluna, índice ou foreign key
- Ao mudar entidade ou relacionamento, revisar `DbContext`, mapeamentos Fluent API, migrations, repositórios, serviços, DTOs e endpoints afetados
- Seed operacional e validação de conexão não substituem migration
- Em `Development`, tolerância maior na subida pode existir se já adotada no projeto; fora disso, falha de preparação do banco deve interromper a aplicação
- Reutilizar padrões existentes do repositório antes de criar abstração nova
- Templates de e-mail devem reutilizar o branding QNF e os assets públicos de `public/branding`, mantendo consistência visual com app e artes de compartilhamento
- Conteúdo HTML de e-mail deve priorizar layout simples, responsivo e compatível com clientes de e-mail, com estilos inline e versão em texto equivalente
- Templates de e-mail devem usar estrutura compatível com clientes mobile/desktop, com `body` e container externo com fundo explícito e estilos principais inline
- Dados pessoais devem ser coletados e expostos apenas quando necessários ao fluxo; e-mail público, localização e imagem/foto precisam respeitar consentimentos e preferências de privacidade
- Perfil Esportivo e Medidas/Uniformes do atleta pertencem ao cadastro esportivo; medidas devem usar estrutura própria, com campos opcionais e incompatíveis mantidos como nulos conforme sexo/gênero.
- Arena principal do atleta deve ser vínculo opcional com Arena existente, sem texto livre.
- Política de Privacidade e Termos de Uso são consentimentos obrigatórios; localização e imagem/foto são consentimentos separados e não devem bloquear uso básico quando recusados
- Fotos de perfil devem ser armazenadas em serviço externo; o banco deve guardar apenas URL e PublicId, sem arquivos locais no Railway/Vercel e sem binário no PostgreSQL. Cloudinary é o padrão atual para imagens de usuário.
- Fotos/avatar de grupo são opcionais e devem seguir o padrão existente de mídia externa, persistindo apenas URL e identificador público necessários para troca/remoção.
- Solicitação de exclusão deve preferir desativação/anonimização, preservando partidas, rankings, métricas e histórico compartilhado quando necessário
- Logs devem sanitizar payloads e nunca registrar senha, token, refresh token, authorization, códigos operacionais ou dados pessoais sensíveis
- Para `Staging` e `Production`, não confiar em fallback local para connection string, JWT, `Frontend:Url` ou URLs de convite
- Não deixar segredos reais em `appsettings.*`, exemplos locais, chats ou arquivos de publish
- O primeiro usuário `Administrador` é bootstrap operacional fora do fluxo normal: não criar endpoint público, seed automático em startup ou bypass no `Program.cs`
- Para bootstrap do primeiro `Administrador`, gerar `senha_hash` com `scripts/gerar-hash-senha-admin.sh` e inserir/promover no banco com `perfil = 1`, `ativo = true` e `atleta_id = null`
- Quando `Database:MigrateOnStartup` estiver desabilitado em produção, aplicar migrations via `scripts/aplicar-migrations-producao.sh`; não substituir migration por SQL estrutural manual no startup
- Antes de assumir que produção está migrada, validar `dotnet ef migrations list` contra o banco alvo e conferir se migrations críticas aparecem como aplicadas; no caso do endpoint `POST /api/partidas`, conferir especialmente `20260401103000_AdicionarCriadoPorUsuarioNaPartida`, `20260401233000_AdicionarFluxoAprovacaoResultados` e `20260402213000_CompatibilizarStatusAprovacaoPartidas`
- Antes de subir `master`, revisar também `.gitignore`, artefatos de publish, documentação de deploy e checklist operacional
- Toda feature criada ou alterada deve avaliar se `AGENTS.md`, `AGENTS.override.md` ou `.ai` precisam registrar uma decisão recorrente

## Contexto local recorrente

- Para execução local sem Docker, usar `ASPNETCORE_ENVIRONMENT=Development` e preferencialmente `PORT=5080`, pois `5000` pode estar ocupada no macOS
- Backend local conectado ao Railway deve usar conexão pública/TCP Proxy; nunca usar `postgres.railway.internal` fora do Railway
- Secrets locais devem ficar em `dotnet user-secrets` ou variáveis de ambiente; não versionar connection strings, `DATABASE_URL`, `Jwt:Chave`, senhas, `.env.local` ou `appsettings.Development.json`
- Em execução local contra banco compartilhado do Railway, manter `Database:MigrateOnStartup=false`; não executar migrations ou seeds automaticamente
- Frontend local deve apontar para a API local via `VITE_API_BASE_URL=http://localhost:5080`
- Validar disponibilidade com `GET http://localhost:5080/health`, `/db-test` e Swagger em `http://localhost:5080/swagger/index.html`
- Em `Development`, login local pode usar o fluxo de código: `POST /api/autenticacao/login/codigo/solicitar` e depois `POST /api/autenticacao/login/codigo`; não registrar tokens ou códigos gerados em arquivos
- Massa técnica `[AI TESTE]` deve ser idempotente, habilitada por configuração e usar senha apenas via user-secrets ou variáveis de ambiente; o usuário principal deve ser comum
- Não criar partidas automaticamente na massa `[AI TESTE]` base nem tornar competição/categoria obrigatórias para partida comum de grupo
- Convites de cadastro usam código curto no formato `000-000`; manter um único código vigente por convite e reutilizá-lo em link, e-mail e WhatsApp
- Não regenerar código de convite como efeito colateral de consultar link, enviar e-mail ou enviar WhatsApp; regeneração só deve existir como ação explícita e rastreável
- Ao persistir código de convite para reenvio, manter também o hash usado na validação e limpar o código em claro quando o convite for utilizado
- Pendências de atleta sem e-mail, ao serem resolvidas com um e-mail de atleta ainda não cadastrado, devem disparar automaticamente convite para o atleta, evitando duplicidade de convite.
- Pendências de atleta sem e-mail, ao serem resolvidas com e-mail de atleta já cadastrado, devem vincular a partida ao atleta existente e encerrar a pendência, sem duplicar atleta, dupla, vínculo ou estatística.
- Pendência de vínculo identifica participante da partida e nunca cancela ou apaga a partida; preservar o nome registrado. Pode ser resolvida por atleta ativo do grupo da partida ou por e-mail. E-mail sem cadastro ativo deixa a pendência como `AguardandoCadastro`, rastreável e fora das ações obrigatórias; buscas devem retornar somente atletas ativos do grupo.
- Rankings de grupo devem considerar todos os atletas membros do grupo e/ou participantes de partidas do grupo, mesmo sem pontuação. Não filtrar atletas apenas por pontos, vitórias ou partidas vencidas.
- Ranking de grupo deve ser restrito ao grupo consultado, sem puxar partidas de outros grupos nem duplicar atleta que é membro e participante.
- Para testar `POST /api/partidas` sem competição prévia, o caminho mais simples é partida de `Grupo`: informar `grupoId` ou `nomeGrupo`; se ambos ficarem vazios, a API usa/cria automaticamente o grupo global `Geral`. Informar quatro atletas por `Id` ou `Nome`, `status` `1` para agendada ou `2` para encerrada, e placar válido quando encerrada
- Regra importante da partida de grupo: o usuário autenticado pode ser apenas responsável pelo cadastro e não precisa estar entre os atletas da partida. Em grupo privado, a restrição é sobre quem registra: o usuário autenticado precisa pertencer ao grupo. Os atletas informados podem estar fora do grupo, e o backend adiciona automaticamente os ausentes ao salvar. No Grupo Geral, manter o fluxo livre/manual.
- Em aprovação/contestação de partida, apenas atletas da DuplaB validante respondem; uma resposta resolve a partida e cancela pendências correlatas de aprovação.
- Duplicidade de partida deve ser avaliada dentro do mesmo contexto de registro. Em partidas de grupo, considerar o `GrupoId`; a mesma combinação de atletas, resultado e data em outro grupo não deve bloquear o registro. As mesmas duplas devem ser reconhecidas mesmo quando os lados A/B forem invertidos; com placar detalhado, comparar o placar também de forma invertida; em modo apenas resultado, comparar a dupla vencedora equivalente considerando a inversão dos lados. Possível duplicidade é fluxo esperado de confirmação; usar retorno estruturado por `CriarComResultadoAsync` (`Criada` ou `RequerConfirmacaoDuplicidade`), sem exception como controle de fluxo. O código `PARTIDA_DUPLICADA_CONFIRMAR` permanece apenas como identificador de resposta para o frontend.
- Edição básica de partida deve validar criador ou administrador no backend, aceitar atletas, placares e alteração de grupo, preservar status/data/categoria e manter pendências, rankings, métricas e dashboards consistentes pelo fluxo existente de atualização.
- Alteração de grupo em edição de partida deve validar a permissão do usuário autenticado no grupo de destino; em grupo privado, quem precisa pertencer ao grupo é o usuário autenticado que edita.
- Ao mexer em pontuação de competição/ranking, regras configuráveis da competição devem prevalecer sobre defaults, inclusive vitória, derrota e participação; usar fallback padrão só quando não houver regra customizada.
- Se a API retornar erro de coluna/tabela inexistente apesar de dizer que migrations estão atualizadas, conferir `__EFMigrationsHistory` contra os arquivos em `PlataformaFutevolei.Infraestrutura/Persistencia/Migracoes`; corrigir o banco local alinhando com migrations existentes, nunca adicionando SQL estrutural ao startup ou aos serviços

## Contexto Railway/produção

- `postgres.railway.internal` só resolve dentro da rede privada da Railway; para comandos locais como `dotnet ef database update`, usar a URL pública/TCP Proxy (`*.proxy.rlwy.net` com porta numérica)
- Em `Production`, a API exige `Jwt__Chave` e `Frontend__Url` válidos; para debug local temporário, pode-se passar por variável de ambiente, sem salvar secrets no repositório
- Não colocar connection string real, senha de banco ou chave JWT em `appsettings.Production.json`; se isso acontecer, remover antes de commit e rotacionar o segredo exposto
- Se `POST /api/partidas` falhar em produção com coluna/tabela faltando, validar no banco alvo a existência de `partidas.status_aprovacao`, `partidas_aprovacoes` e `pendencias_usuarios`, e validar pelo EF se as migrations acima aparecem no catálogo

## Fase atual do produto

- A fase atual é atleta/grupo/ranking/scout-first, não campeonato-first
- Priorizar registro rápido de partidas, grupos, scout individual, scout de duplas, ranking individual por grupo, ranking de duplas por grupo, histórico e qualidade dos dados
- Grupos são o contexto principal da fase atual; campeonato, evento, categoria e liga são visão futura ou fluxo específico quando solicitado
- Partida comum de grupo não deve exigir competição, categoria ou liga
- Partida é dupla contra dupla; cada dupla possui exatamente 2 atletas
- Partida pode ter placar completo ou apenas vencedor
- Com placar, validar regras aplicáveis e calcular pontos pró, pontos contra e saldo quando o fluxo exigir essas métricas
- Em modo apenas vencedor, exigir dupla vencedora, não permitir empate, não aplicar mínimo/diferença de pontos e não calcular pontos pró, pontos contra ou saldo
- Scout individual e scout de duplas são núcleo atual do produto
- Dupla é derivada das partidas nesta fase; não exigir cadastro fixo de dupla para registrar partida comum
- Normalizar dupla para estatística e histórico: Atleta A + Atleta B é equivalente a Atleta B + Atleta A
- Ranking individual e ranking de duplas por grupo devem usar cálculo centralizado, recalculável e testável
- Não confundir pontuação de competição com ranking da plataforma
- Regras configuráveis de competição só se aplicam aos fluxos de competição; ranking da plataforma deve manter regra central própria
- Pendências e vínculos impactam partida, grupo, ranking, scout individual e scout de dupla
- Pendência pode ser resolvida por `atletaId` ou e-mail quando o fluxo suportar; preservar o status `AguardandoCadastro` quando o atleta ainda não existir
- Resolver vínculo não deve corromper partida, histórico, nome informado, ranking ou scout; evitar duplicidade de atleta
- Alterações em domínio, aplicação, partida, ranking, scout, grupo, vínculo ou autenticação devem criar ou ajustar testes compatíveis com o risco da mudança
