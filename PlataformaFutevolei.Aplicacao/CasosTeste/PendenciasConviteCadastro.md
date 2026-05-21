# Casos de teste - convite ao resolver pendência de atleta

- Ao concluir uma pendência `CompletarContatoAtletaDaPartida` com e-mail válido para atleta sem usuário, salvar o e-mail, concluir a pendência e criar um convite `Atleta` com `CriadoPorUsuarioId`, `AtletaId` e `PartidaId` da pendência quando houver.
- Ao concluir pendência com e-mail que já possui convite ativo, pendente, não utilizado e não expirado, manter a pendência concluída e não criar novo convite.
- Ao concluir pendência com e-mail que já pertence a usuário não anonimizado, manter o fluxo atual de vínculo/retorno de usuário encontrado e não criar convite.
- Ao ocorrer falha na criação ou envio do convite automático, manter a pendência concluída e registrar log estruturado no fluxo de convite.
