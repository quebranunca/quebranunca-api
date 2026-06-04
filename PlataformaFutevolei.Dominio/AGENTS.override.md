# Regras específicas do domínio

- Manter entidades sem dependência de infraestrutura, HTTP ou detalhes da API
- Preservar invariantes do domínio e a consistência das relações do projeto
- Evitar setters e mutações públicas desnecessárias, sem quebrar as necessidades atuais do EF Core
- Ao alterar regra de entidade ou relacionamento, alinhar aplicação, mapeamentos e constraints do banco
- Não criar abstrações de domínio genéricas sem ganho real
- Preservar as invariantes já adotadas: partida sem empate, dupla com exatamente dois atletas e categoria ligada a competição
- Convite de cadastro precisa manter um único código vigente e impedir uso quando estiver vencido, inativo ou já utilizado
- Se o convite registrar status operacional de envio por e-mail ou WhatsApp, isso não pode relaxar as invariantes de uso do código
- Ações de entrega por canais diferentes não devem alterar a identidade do convite nem invalidar o código vigente
- Usuário organizador pode existir sem atleta vinculado; não tratar `AtletaId` como obrigatório no domínio
- Arena é a entidade central de local esportivo e substitui gradualmente `Local`; não criar entidades principais paralelas `Local`, `Quadra` ou `Rede`
- Espaços internos podem representar quadras, redes ou áreas de treino da Arena; `ArenaAdmin` e professor vinculados à Arena devem se relacionar a `Usuario`
- Arena é vínculo opcional para partidas, treinos, competições e grupos; partidas avulsas sem Arena permanecem válidas
- Arena principal do atleta é vínculo opcional com Arena cadastrada e não substitui cidade, bairro ou local textual de outros fluxos.
- Medidas e Uniformes do atleta devem ficar em entidade própria, sem poluir `Atleta`, e campos incompatíveis com sexo/gênero devem permanecer nulos.
- Imagens públicas de Arena devem usar o padrão existente de mídia externa, sem persistência binária local no domínio
- Quando a regra estiver hoje centralizada na aplicação, não forçar migração prematura para entidades sem ganho claro
- Mudança estrutural decorrente do domínio deve ser refletida fora dele via mapeamentos e migrations do EF Core, nunca por SQL estrutural no startup
