# Regras específicas do domínio

- Manter entidades sem dependência de infraestrutura, HTTP ou detalhes da API
- Preservar invariantes do domínio e a consistência das relações do projeto
- Evitar setters e mutações públicas desnecessárias, sem quebrar as necessidades atuais do EF Core
- Ao alterar regra de entidade ou relacionamento, alinhar aplicação, mapeamentos e constraints do banco
- Não criar abstrações de domínio genéricas sem ganho real
- Preservar as invariantes já adotadas: partida sem empate, dupla com exatamente dois atletas e categoria ligada a competição
- Convite de cadastro precisa manter token único e impedir uso quando estiver vencido, inativo ou já utilizado
- Se o convite registrar status operacional de envio por e-mail ou WhatsApp, isso não pode relaxar as invariantes de uso do token
- Usuário organizador pode existir sem atleta vinculado; não tratar `AtletaId` como obrigatório no domínio
- Quando a regra estiver hoje centralizada na aplicação, não forçar migração prematura para entidades sem ganho claro
- Mudança estrutural decorrente do domínio deve ser refletida fora dele via mapeamentos e migrations do EF Core, nunca por SQL estrutural no startup
