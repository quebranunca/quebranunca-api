using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Interfaces.Seguranca;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Aplicacao.Mapeadores;
using PlataformaFutevolei.Aplicacao.Utilitarios;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Aplicacao.Servicos;

public class PendenciaServico(
    IPartidaRepositorio partidaRepositorio,
    IPartidaAprovacaoRepositorio partidaAprovacaoRepositorio,
    IPendenciaUsuarioRepositorio pendenciaUsuarioRepositorio,
    IUsuarioRepositorio usuarioRepositorio,
    IGrupoAtletaRepositorio grupoAtletaRepositorio,
    IUnidadeTrabalho unidadeTrabalho,
    IAutorizacaoUsuarioServico autorizacaoUsuarioServico,
    IResolvedorAtletaDuplaServico resolvedorAtletaDuplaServico
) : IPendenciaServico
{
    public async Task<IReadOnlyList<PendenciaUsuarioDto>> ListarMinhasAsync(CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        var pendencias = await pendenciaUsuarioRepositorio.ListarPendentesPorUsuarioAsync(usuario.Id, cancellationToken);

        return pendencias
            .Where(PendenciaAindaAcionavel)
            .Select(x => x.ParaDto())
            .OrderBy(x => x.Tipo)
            .ThenByDescending(x => x.DataCriacao)
            .ToList();
    }

    public async Task<bool> ExistePendenciaAsync(CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        var existePendencia = await pendenciaUsuarioRepositorio.ExistePendentePorUsuarioAsync(usuario.Id, cancellationToken);

        return existePendencia;
    }

    public async Task<PendenciaUsuarioDto> AprovarPartidaAsync(
        Guid pendenciaId,
        ResponderPendenciaPartidaDto dto,
        CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        var pendencia = await ObterPendenciaPendenteAsync(
            pendenciaId,
            TipoPendenciaUsuario.AprovarPartida,
            usuario.Id,
            cancellationToken);
        var aprovacao = await ObterAprovacaoDaPendenciaAsync(pendencia, usuario.Id, cancellationToken);
        await GarantirPartidaAindaAguardandoRespostaAsync(pendencia.Partida!, pendencia.AtletaId!.Value, cancellationToken);

        aprovacao.Status = StatusPartidaAprovacao.Aprovada;
        aprovacao.DataResposta = DateTime.UtcNow;
        aprovacao.Observacao = dto.Observacao?.Trim();
        aprovacao.AtualizarDataModificacao();
        partidaAprovacaoRepositorio.Atualizar(aprovacao);

        ConcluirPendencia(pendencia, dto.Observacao);
        await CancelarPendenciasAprovacaoResolvidasAsync(
            pendencia.Partida!,
            pendencia.AtletaId!.Value,
            pendencia.Id,
            cancellationToken);

        await RecalcularStatusPartidaAsync(pendencia.Partida!, cancellationToken);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
        return pendencia.ParaDto();
    }

    public async Task<PendenciaUsuarioDto> ContestarPartidaAsync(
        Guid pendenciaId,
        ResponderPendenciaPartidaDto dto,
        CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        var pendencia = await ObterPendenciaPendenteAsync(
            pendenciaId,
            TipoPendenciaUsuario.AprovarPartida,
            usuario.Id,
            cancellationToken);
        var aprovacao = await ObterAprovacaoDaPendenciaAsync(pendencia, usuario.Id, cancellationToken);
        await GarantirPartidaAindaAguardandoRespostaAsync(pendencia.Partida!, pendencia.AtletaId!.Value, cancellationToken);

        aprovacao.Status = StatusPartidaAprovacao.Contestada;
        aprovacao.DataResposta = DateTime.UtcNow;
        aprovacao.Observacao = dto.Observacao?.Trim();
        aprovacao.AtualizarDataModificacao();
        partidaAprovacaoRepositorio.Atualizar(aprovacao);

        ConcluirPendencia(pendencia, dto.Observacao);
        await CancelarPendenciasAprovacaoResolvidasAsync(
            pendencia.Partida!,
            pendencia.AtletaId!.Value,
            pendencia.Id,
            cancellationToken);
        await RecalcularStatusPartidaAsync(pendencia.Partida!, cancellationToken);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
        return pendencia.ParaDto();
    }

    public async Task<AtualizarContatoPendenciaResultadoDto> CompletarContatoAsync(
        Guid pendenciaId,
        AtualizarContatoPendenciaDto dto,
        CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        var pendencia = await ObterPendenciaPendenteAsync(
            pendenciaId,
            TipoPendenciaUsuario.CompletarContatoAtletaDaPartida,
            usuario.Id,
            cancellationToken);

        if (!pendencia.AtletaId.HasValue)
        {
            throw new RegraNegocioException("A pendência informada não possui atleta vinculado.");
        }

        var atleta = pendencia.Atleta ?? throw new EntidadeNaoEncontradaException("Atleta não encontrado.");

        if (atleta.Usuario is not null)
        {
            ConcluirPendencia(pendencia, "Pendência encerrada porque o atleta já possui usuário vinculado.");
        }
        else
        {
            var emailNormalizado = NormalizarEmail(dto.Email);
            var usuarioExistente = await usuarioRepositorio.ObterPorEmailParaAtualizacaoAsync(emailNormalizado, cancellationToken);
            if (usuarioExistente is not null)
            {
                if (!usuarioExistente.Ativo || usuarioExistente.DadosAnonimizados)
                {
                    throw new RegraNegocioException("Este e-mail pertence a um usuário inativo.");
                }

                var atletaExistente = usuarioExistente.Atleta
                    ?? throw new RegraNegocioException("Este e-mail pertence a um usuário sem atleta vinculado.");

                return new AtualizarContatoPendenciaResultadoDto(
                    true,
                    pendencia.ParaDto(),
                    new UsuarioAtletaPendenciaDto(
                        usuarioExistente.Id,
                        atletaExistente.Id,
                        atletaExistente.Nome,
                        atletaExistente.Apelido));
            }

            await GarantirEmailUnicoNosGruposDoAtletaAsync(atleta.Id, emailNormalizado, cancellationToken);

            atleta.Email = emailNormalizado;
            atleta.AtualizarDataModificacao();
            await ConcluirPendenciasContatoAtletaAsync(
                atleta.Id,
                pendencia.PartidaId.HasValue
                    ? "Contato informado. A partida continua aguardando vínculo do atleta para liberar a aprovação."
                    : "Contato informado.",
                cancellationToken);
        }

        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        var pendenciaAtualizada = await pendenciaUsuarioRepositorio.ObterPorIdAsync(pendencia.Id, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Pendência não encontrada.");
        return new AtualizarContatoPendenciaResultadoDto(false, pendenciaAtualizada.ParaDto(), null);
    }

    public async Task<PendenciaUsuarioDto> ConfirmarVinculoAtletaCadastradoAsync(
        Guid pendenciaId,
        ConfirmarVinculoAtletaPendenciaDto dto,
        CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        PendenciaUsuarioDto? resultado = null;

        await unidadeTrabalho.ExecutarEmTransacaoAsync(async ct =>
        {
            var pendencia = await ObterPendenciaDoUsuarioAsync(
                pendenciaId,
                TipoPendenciaUsuario.CompletarContatoAtletaDaPartida,
                usuario.Id,
                ct);

            if (pendencia.Status != StatusPendenciaUsuario.Pendente)
            {
                resultado = pendencia.ParaDto();
                return;
            }

            if (!pendencia.AtletaId.HasValue)
            {
                throw new RegraNegocioException("A pendência informada não possui atleta vinculado.");
            }

            var usuarioEncontrado = await usuarioRepositorio.ObterPorIdParaAtualizacaoAsync(dto.UsuarioId, ct)
                ?? throw new EntidadeNaoEncontradaException("Usuário não encontrado.");
            if (!usuarioEncontrado.Ativo || usuarioEncontrado.DadosAnonimizados)
            {
                throw new RegraNegocioException("Este e-mail pertence a um usuário inativo.");
            }

            var atletaDestino = usuarioEncontrado.Atleta
                ?? throw new RegraNegocioException("Este e-mail pertence a um usuário sem atleta vinculado.");
            atletaDestino.Usuario = usuarioEncontrado;

            var atletaPendenteId = pendencia.AtletaId.Value;
            if (atletaPendenteId == atletaDestino.Id)
            {
                await ConcluirPendenciasContatoAtletaAsync(
                    atletaPendenteId,
                    "Pendência concluída porque o atleta já possui usuário vinculado.",
                    ct);
                await RecalcularStatusPartidaSeExistirAsync(pendencia.PartidaId, ct);
                await unidadeTrabalho.SalvarAlteracoesAsync(ct);
                resultado = pendencia.ParaDto();
                return;
            }

            var partidas = await ListarPartidasParaVinculoAsync(
                atletaPendenteId,
                usuario.Id,
                pendencia.PartidaId,
                ct);
            if (partidas.Count == 0)
            {
                throw new RegraNegocioException("Não há partidas pendentes para vincular este atleta.");
            }

            foreach (var partida in partidas)
            {
                await SubstituirAtletaNaPartidaAsync(partida, atletaPendenteId, atletaDestino, ct);
                await CriarAprovacaoSeNecessarioAsync(partida, atletaDestino, ct);
                await RecalcularStatusPartidaAsync(partida, ct);
            }

            await ConcluirPendenciasContatoDasPartidasAsync(
                atletaPendenteId,
                partidas.Select(x => x.Id).ToHashSet(),
                "Pendência concluída com vínculo ao atleta cadastrado.",
                ct);

            await unidadeTrabalho.SalvarAlteracoesAsync(ct);
            var pendenciaAtualizada = await pendenciaUsuarioRepositorio.ObterPorIdAsync(pendencia.Id, ct)
                ?? throw new EntidadeNaoEncontradaException("Pendência não encontrada.");
            resultado = pendenciaAtualizada.ParaDto();
        }, cancellationToken);

        return resultado ?? throw new EntidadeNaoEncontradaException("Pendência não encontrada.");
    }

    private async Task<PendenciaUsuario> ObterPendenciaDoUsuarioAsync(
        Guid pendenciaId,
        TipoPendenciaUsuario tipoEsperado,
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        var pendencia = await pendenciaUsuarioRepositorio.ObterPorIdAsync(pendenciaId, cancellationToken);
        if (pendencia is null)
        {
            throw new EntidadeNaoEncontradaException("Pendência não encontrada.");
        }

        if (pendencia.UsuarioId != usuarioId)
        {
            throw new RegraNegocioException("Você só pode atuar nas suas próprias pendências.");
        }

        if (pendencia.Tipo != tipoEsperado)
        {
            throw new RegraNegocioException("Tipo de pendência inválido para esta operação.");
        }

        return pendencia;
    }

    public async Task InicializarFluxoPartidaAsync(
        Partida partida,
        Guid usuarioRegistradorId,
        CancellationToken cancellationToken = default)
    {
        var pendenciasExistentes = await pendenciaUsuarioRepositorio.ListarPendentesPorPartidaAsync(partida.Id, cancellationToken);
        foreach (var pendencia in pendenciasExistentes)
        {
            CancelarPendencia(pendencia, "Cancelada para reconstruir o fluxo de aprovação da partida.");
        }

        var aprovacoesExistentes = await partidaAprovacaoRepositorio.ListarPorPartidaAsync(partida.Id, cancellationToken);
        if (aprovacoesExistentes.Count > 0)
        {
            partidaAprovacaoRepositorio.RemoverIntervalo(aprovacoesExistentes);
        }

        if (partida.Status != StatusPartida.Encerrada)
        {
            partida.StatusAprovacao = StatusAprovacaoPartida.Aprovada;
            partida.AtualizarDataModificacao();
            partidaRepositorio.Atualizar(partida);
            await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
            return;
        }

        foreach (var atleta in ObterAtletasDuplaValidante(partida))
        {
            if (atleta.Usuario is null)
            {
                await CriarPendenciaContatoAsync(usuarioRegistradorId, partida, atleta, cancellationToken);
                continue;
            }

            var aprovacao = new PartidaAprovacao
            {
                PartidaId = partida.Id,
                AtletaId = atleta.Id,
                UsuarioId = atleta.Usuario.Id,
                Status = StatusPartidaAprovacao.Pendente,
                DataSolicitacao = DateTime.UtcNow,
                Partida = partida,
                Atleta = atleta,
                Usuario = atleta.Usuario
            };

            await partidaAprovacaoRepositorio.AdicionarAsync(aprovacao, cancellationToken);
            await CriarPendenciaAprovacaoAsync(atleta.Usuario.Id, partida, atleta, cancellationToken);
        }

        await RecalcularStatusPartidaAsync(partida, cancellationToken);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
    }

    public async Task SincronizarAposVinculoAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default)
    {
        var partidas = await partidaRepositorio.ListarComPendenteDeVinculoPorAtletaAsync(atletaId, cancellationToken);
        if (partidas.Count == 0)
        {
            return;
        }

        foreach (var partida in partidas)
        {
            var atleta = ObterAtletasDuplaValidante(partida).FirstOrDefault(x => x.Id == atletaId);
            if (atleta?.Usuario is null)
            {
                continue;
            }

            var aprovacaoExistente = await partidaAprovacaoRepositorio.ObterPorPartidaEAtletaAsync(partida.Id, atletaId, cancellationToken);
            if (aprovacaoExistente is null)
            {
                var aprovacao = new PartidaAprovacao
                {
                    PartidaId = partida.Id,
                    AtletaId = atleta.Id,
                    UsuarioId = atleta.Usuario.Id,
                    Status = StatusPartidaAprovacao.Pendente,
                    DataSolicitacao = DateTime.UtcNow,
                    Partida = partida,
                    Atleta = atleta,
                    Usuario = atleta.Usuario
                };

                await partidaAprovacaoRepositorio.AdicionarAsync(aprovacao, cancellationToken);
                await CriarPendenciaAprovacaoAsync(atleta.Usuario.Id, partida, atleta, cancellationToken);
            }

            var pendenciasContato = await pendenciaUsuarioRepositorio.ListarPendentesPorPartidaAsync(partida.Id, cancellationToken);
            foreach (var pendenciaContato in pendenciasContato.Where(x =>
                         x.Tipo == TipoPendenciaUsuario.CompletarContatoAtletaDaPartida &&
                         x.AtletaId == atletaId))
            {
                ConcluirPendencia(pendenciaContato, "Pendência concluída porque o atleta agora possui usuário vinculado.");
            }

            await RecalcularStatusPartidaAsync(partida, cancellationToken);
        }

        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<Partida>> ListarPartidasParaVinculoAsync(
        Guid atletaPendenteId,
        Guid usuarioId,
        Guid? partidaAtualId,
        CancellationToken cancellationToken)
    {
        var partidas = await partidaRepositorio.ListarComPendenteDeVinculoPorAtletaAsync(
            atletaPendenteId,
            cancellationToken);

        return partidas
            .Where(x => x.CriadoPorUsuarioId == usuarioId || x.Id == partidaAtualId)
            .DistinctBy(x => x.Id)
            .ToList();
    }

    private async Task SubstituirAtletaNaPartidaAsync(
        Partida partida,
        Guid atletaPendenteId,
        Atleta atletaDestino,
        CancellationToken cancellationToken)
    {
        if (AtletaParticipaDaPartida(partida, atletaDestino.Id))
        {
            throw new RegraNegocioException("O atleta encontrado já participa desta partida.");
        }

        var duplaAOriginalId = partida.DuplaAId;
        var duplaBOriginalId = partida.DuplaBId;

        if (partida.DuplaA is not null &&
            (partida.DuplaA.Atleta1Id == atletaPendenteId || partida.DuplaA.Atleta2Id == atletaPendenteId))
        {
            var novaDupla = await SubstituirAtletaNaDuplaAsync(partida.DuplaA, atletaPendenteId, atletaDestino, cancellationToken);
            partida.DuplaAId = novaDupla.Id;
            partida.DuplaA = novaDupla;

            if (partida.DuplaVencedoraId == duplaAOriginalId)
            {
                partida.DuplaVencedoraId = novaDupla.Id;
                partida.DuplaVencedora = novaDupla;
            }
        }

        if (partida.DuplaB is not null &&
            (partida.DuplaB.Atleta1Id == atletaPendenteId || partida.DuplaB.Atleta2Id == atletaPendenteId))
        {
            var novaDupla = await SubstituirAtletaNaDuplaAsync(partida.DuplaB, atletaPendenteId, atletaDestino, cancellationToken);
            partida.DuplaBId = novaDupla.Id;
            partida.DuplaB = novaDupla;

            if (partida.DuplaVencedoraId == duplaBOriginalId)
            {
                partida.DuplaVencedoraId = novaDupla.Id;
                partida.DuplaVencedora = novaDupla;
            }
        }

        if (partida.GrupoId.HasValue)
        {
            await resolvedorAtletaDuplaServico.GarantirAtletaNoGrupoAsync(partida.GrupoId.Value, atletaDestino, cancellationToken);
        }

        partida.AtualizarDataModificacao();
        partidaRepositorio.Atualizar(partida);
    }

    private async Task<Dupla> SubstituirAtletaNaDuplaAsync(
        Dupla dupla,
        Guid atletaPendenteId,
        Atleta atletaDestino,
        CancellationToken cancellationToken)
    {
        var parceiro = dupla.Atleta1Id == atletaPendenteId
            ? dupla.Atleta2
            : dupla.Atleta2Id == atletaPendenteId
                ? dupla.Atleta1
                : null;

        if (parceiro is null)
        {
            throw new RegraNegocioException("A participação pendente não foi encontrada na dupla da partida.");
        }

        if (parceiro.Id == atletaDestino.Id)
        {
            throw new RegraNegocioException("Um atleta não pode formar dupla com ele mesmo.");
        }

        return await resolvedorAtletaDuplaServico.ObterOuCriarDuplaAsync(
            parceiro,
            atletaDestino,
            cancellationToken);
    }

    private async Task CriarAprovacaoSeNecessarioAsync(
        Partida partida,
        Atleta atleta,
        CancellationToken cancellationToken)
    {
        if (atleta.Usuario is null || !AtletaPertenceADuplaValidante(partida, atleta.Id))
        {
            return;
        }

        var aprovacaoExistente = await partidaAprovacaoRepositorio.ObterPorPartidaEAtletaAsync(
            partida.Id,
            atleta.Id,
            cancellationToken);
        if (aprovacaoExistente is not null)
        {
            return;
        }

        await partidaAprovacaoRepositorio.AdicionarAsync(new PartidaAprovacao
        {
            PartidaId = partida.Id,
            AtletaId = atleta.Id,
            UsuarioId = atleta.Usuario.Id,
            Status = StatusPartidaAprovacao.Pendente,
            DataSolicitacao = DateTime.UtcNow,
            Partida = partida,
            Atleta = atleta,
            Usuario = atleta.Usuario
        }, cancellationToken);

        await CriarPendenciaAprovacaoAsync(atleta.Usuario.Id, partida, atleta, cancellationToken);
    }

    private async Task ConcluirPendenciasContatoDasPartidasAsync(
        Guid atletaId,
        IReadOnlySet<Guid> partidaIds,
        string observacao,
        CancellationToken cancellationToken)
    {
        var pendencias = await pendenciaUsuarioRepositorio.ListarPendentesPorAtletaAsync(atletaId, cancellationToken);
        foreach (var pendencia in pendencias.Where(x =>
                     x.Tipo == TipoPendenciaUsuario.CompletarContatoAtletaDaPartida &&
                     x.PartidaId.HasValue &&
                     partidaIds.Contains(x.PartidaId.Value)))
        {
            ConcluirPendencia(pendencia, observacao);
        }
    }

    private async Task RecalcularStatusPartidaSeExistirAsync(Guid? partidaId, CancellationToken cancellationToken)
    {
        if (!partidaId.HasValue)
        {
            return;
        }

        var partida = await partidaRepositorio.ObterPorIdAsync(partidaId.Value, cancellationToken);
        if (partida is not null)
        {
            await RecalcularStatusPartidaAsync(partida, cancellationToken);
        }
    }

    private async Task<PendenciaUsuario> ObterPendenciaPendenteAsync(
        Guid pendenciaId,
        TipoPendenciaUsuario tipoEsperado,
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        var pendencia = await pendenciaUsuarioRepositorio.ObterPorIdAsync(pendenciaId, cancellationToken);
        if (pendencia is null)
        {
            throw new EntidadeNaoEncontradaException("Pendência não encontrada.");
        }

        if (pendencia.UsuarioId != usuarioId)
        {
            throw new RegraNegocioException("Você só pode atuar nas suas próprias pendências.");
        }

        if (pendencia.Status != StatusPendenciaUsuario.Pendente)
        {
            throw new RegraNegocioException("Esta pendência já foi concluída.");
        }

        if (pendencia.Tipo != tipoEsperado)
        {
            throw new RegraNegocioException("Tipo de pendência inválido para esta operação.");
        }

        return pendencia;
    }

    private async Task<PartidaAprovacao> ObterAprovacaoDaPendenciaAsync(
        PendenciaUsuario pendencia,
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        if (!pendencia.PartidaId.HasValue || !pendencia.AtletaId.HasValue)
        {
            throw new RegraNegocioException("A pendência informada não possui aprovação de partida vinculada.");
        }

        var aprovacao = await partidaAprovacaoRepositorio.ObterPorPartidaEAtletaAsync(
            pendencia.PartidaId.Value,
            pendencia.AtletaId.Value,
            cancellationToken);

        if (aprovacao is null || aprovacao.UsuarioId != usuarioId)
        {
            throw new RegraNegocioException("A aprovação desta partida não está disponível para o usuário atual.");
        }

        return aprovacao;
    }

    private async Task CriarPendenciaContatoAsync(
        Guid usuarioRegistradorId,
        Partida partida,
        Atleta atleta,
        CancellationToken cancellationToken)
    {
        if (StatusCadastroAtletaUtil.TemEmail(atleta))
        {
            return;
        }

        var pendenciaExistente = await pendenciaUsuarioRepositorio.ObterPendenteAsync(
            TipoPendenciaUsuario.CompletarContatoAtletaDaPartida,
            usuarioRegistradorId,
            partida.Id,
            atleta.Id,
            cancellationToken);
        if (pendenciaExistente is not null)
        {
            return;
        }

        await pendenciaUsuarioRepositorio.AdicionarAsync(new PendenciaUsuario
        {
            Tipo = TipoPendenciaUsuario.CompletarContatoAtletaDaPartida,
            UsuarioId = usuarioRegistradorId,
            AtletaId = atleta.Id,
            PartidaId = partida.Id,
            Status = StatusPendenciaUsuario.Pendente,
            Atleta = atleta,
            Partida = partida
        }, cancellationToken);
    }

    private async Task CriarPendenciaAprovacaoAsync(
        Guid usuarioId,
        Partida partida,
        Atleta atleta,
        CancellationToken cancellationToken)
    {
        var pendenciaExistente = await pendenciaUsuarioRepositorio.ObterPendenteAsync(
            TipoPendenciaUsuario.AprovarPartida,
            usuarioId,
            partida.Id,
            atleta.Id,
            cancellationToken);
        if (pendenciaExistente is not null)
        {
            return;
        }

        await pendenciaUsuarioRepositorio.AdicionarAsync(new PendenciaUsuario
        {
            Tipo = TipoPendenciaUsuario.AprovarPartida,
            UsuarioId = usuarioId,
            AtletaId = atleta.Id,
            PartidaId = partida.Id,
            Status = StatusPendenciaUsuario.Pendente,
            Atleta = atleta,
            Partida = partida
        }, cancellationToken);
    }

    private async Task CancelarPendenciasAprovacaoResolvidasAsync(
        Partida partida,
        Guid atletaRespondenteId,
        Guid pendenciaAtualId,
        CancellationToken cancellationToken)
    {
        var partidaDetalhada = await partidaRepositorio.ObterPorIdAsync(partida.Id, cancellationToken) ?? partida;
        var aprovacoes = await partidaAprovacaoRepositorio.ListarPorPartidaAsync(partida.Id, cancellationToken);
        var atletasDaDupla = ObterAtletasDaMesmaDupla(partidaDetalhada, atletaRespondenteId)
            .Select(x => x.Id)
            .ToHashSet();
        var partidaResolvida = DuplaValidantePossuiResposta(partidaDetalhada, aprovacoes);
        var pendencias = await pendenciaUsuarioRepositorio.ListarPendentesPorPartidaAsync(partida.Id, cancellationToken);

        foreach (var pendencia in pendencias.Where(x => x.Tipo == TipoPendenciaUsuario.AprovarPartida))
        {
            if (pendencia.Id == pendenciaAtualId)
            {
                continue;
            }

            if (partidaResolvida || (pendencia.AtletaId.HasValue && atletasDaDupla.Contains(pendencia.AtletaId.Value)))
            {
                CancelarPendencia(pendencia, "Cancelada porque a dupla já possui resposta para esta partida.");
            }
        }
    }

    private async Task ConcluirPendenciasContatoAtletaAsync(
        Guid atletaId,
        string observacao,
        CancellationToken cancellationToken)
    {
        var pendencias = await pendenciaUsuarioRepositorio.ListarPendentesPorAtletaAsync(atletaId, cancellationToken);
        foreach (var pendencia in pendencias.Where(x => x.Tipo == TipoPendenciaUsuario.CompletarContatoAtletaDaPartida))
        {
            ConcluirPendencia(pendencia, observacao);
        }
    }

    private async Task GarantirEmailUnicoNosGruposDoAtletaAsync(
        Guid atletaId,
        string emailNormalizado,
        CancellationToken cancellationToken)
    {
        var gruposDoAtleta = await grupoAtletaRepositorio.ListarPorAtletaAsync(atletaId, cancellationToken);
        foreach (var grupoDoAtleta in gruposDoAtleta)
        {
            var atletasDoGrupo = await grupoAtletaRepositorio.ListarPorGrupoAsync(grupoDoAtleta.GrupoId, cancellationToken);
            var emailDuplicado = atletasDoGrupo.Any(x =>
                x.AtletaId != atletaId &&
                !string.IsNullOrWhiteSpace(x.Atleta.Email) &&
                string.Equals(x.Atleta.Email, emailNormalizado, StringComparison.OrdinalIgnoreCase));

            if (emailDuplicado)
            {
                throw new RegraNegocioException("Já existe um atleta nesse grupo com este email.");
            }
        }
    }

    private async Task RecalcularStatusPartidaAsync(Partida partida, CancellationToken cancellationToken)
    {
        var partidaDetalhada = await partidaRepositorio.ObterPorIdAsync(partida.Id, cancellationToken) ?? partida;

        if (partidaDetalhada.Status != StatusPartida.Encerrada)
        {
            partidaDetalhada.StatusAprovacao = StatusAprovacaoPartida.Aprovada;
            partidaDetalhada.AtualizarDataModificacao();
            partidaRepositorio.Atualizar(partidaDetalhada);
            return;
        }

        var atletas = ObterAtletasDuplaValidante(partidaDetalhada);
        var aprovacoes = await partidaAprovacaoRepositorio.ListarPorPartidaAsync(partidaDetalhada.Id, cancellationToken);

        if (aprovacoes.Any(x =>
                AtletaPertenceADuplaValidante(partidaDetalhada, x.AtletaId) &&
                x.Status == StatusPartidaAprovacao.Contestada))
        {
            partidaDetalhada.StatusAprovacao = StatusAprovacaoPartida.Contestada;
        }
        else if (aprovacoes.Any(x =>
                     AtletaPertenceADuplaValidante(partidaDetalhada, x.AtletaId) &&
                     x.Status == StatusPartidaAprovacao.Aprovada))
        {
            partidaDetalhada.StatusAprovacao = StatusAprovacaoPartida.Aprovada;
        }
        else if (atletas.Count == 0 || atletas.Any(x => x.Usuario is null))
        {
            partidaDetalhada.StatusAprovacao = StatusAprovacaoPartida.PendenteDeVinculos;
        }
        else
        {
            partidaDetalhada.StatusAprovacao = StatusAprovacaoPartida.PendenteAprovacao;
        }

        partidaDetalhada.AtualizarDataModificacao();
        partidaRepositorio.Atualizar(partidaDetalhada);
    }

    private async Task GarantirPartidaAindaAguardandoRespostaAsync(
        Partida partida,
        Guid atletaRespondenteId,
        CancellationToken cancellationToken)
    {
        var partidaDetalhada = await partidaRepositorio.ObterPorIdAsync(partida.Id, cancellationToken) ?? partida;
        if (!AtletaPertenceADuplaValidante(partidaDetalhada, atletaRespondenteId))
        {
            throw new RegraNegocioException("Apenas atletas da Dupla 2 podem validar esta partida.");
        }

        var aprovacoes = await partidaAprovacaoRepositorio.ListarPorPartidaAsync(partida.Id, cancellationToken);
        if (DuplaValidantePossuiResposta(partidaDetalhada, aprovacoes))
        {
            throw new RegraNegocioException("Esta partida já foi resolvida.");
        }
    }

    private static IReadOnlyList<Atleta> ObterAtletasDuplaValidante(Partida partida)
    {
        return new[]
        {
            partida.DuplaB?.Atleta1,
            partida.DuplaB?.Atleta2
        }
        .OfType<Atleta>()
        .DistinctBy(x => x.Id)
        .ToList();
    }

    private static IReadOnlyList<Atleta> ObterAtletasDaMesmaDupla(Partida partida, Guid atletaId)
    {
        if (partida.DuplaA is not null &&
            (partida.DuplaA.Atleta1Id == atletaId || partida.DuplaA.Atleta2Id == atletaId))
        {
            return new[] { partida.DuplaA.Atleta1, partida.DuplaA.Atleta2 }
                .OfType<Atleta>()
                .ToList();
        }

        if (partida.DuplaB is not null &&
            (partida.DuplaB.Atleta1Id == atletaId || partida.DuplaB.Atleta2Id == atletaId))
        {
            return new[] { partida.DuplaB.Atleta1, partida.DuplaB.Atleta2 }
                .OfType<Atleta>()
                .ToList();
        }

        return [];
    }

    private static bool DuplaValidantePossuiResposta(
        Partida partida,
        IReadOnlyList<PartidaAprovacao> aprovacoes)
    {
        return DuplaPossuiResposta(partida.DuplaB, aprovacoes);
    }

    private static bool DuplaPossuiResposta(Dupla? dupla, IReadOnlyList<PartidaAprovacao> aprovacoes)
    {
        if (dupla is null)
        {
            return false;
        }

        return aprovacoes.Any(x =>
            (x.AtletaId == dupla.Atleta1Id || x.AtletaId == dupla.Atleta2Id) &&
            x.Status != StatusPartidaAprovacao.Pendente);
    }

    private static bool AtletaPertenceADuplaValidante(Partida partida, Guid atletaId)
    {
        return partida.DuplaB is not null &&
               (partida.DuplaB.Atleta1Id == atletaId || partida.DuplaB.Atleta2Id == atletaId);
    }

    private static bool AtletaParticipaDaPartida(Partida partida, Guid atletaId)
    {
        return (partida.DuplaA is not null &&
                (partida.DuplaA.Atleta1Id == atletaId || partida.DuplaA.Atleta2Id == atletaId)) ||
               (partida.DuplaB is not null &&
                (partida.DuplaB.Atleta1Id == atletaId || partida.DuplaB.Atleta2Id == atletaId));
    }

    private static bool PendenciaAindaAcionavel(PendenciaUsuario pendencia)
    {
        if (pendencia.Tipo != TipoPendenciaUsuario.CompletarContatoAtletaDaPartida)
        {
            return true;
        }

        return pendencia.Atleta is not null &&
               !StatusCadastroAtletaUtil.PossuiUsuarioVinculado(pendencia.Atleta) &&
               !StatusCadastroAtletaUtil.TemEmail(pendencia.Atleta);
    }

    private static void ConcluirPendencia(PendenciaUsuario pendencia, string? observacao)
    {
        pendencia.Status = StatusPendenciaUsuario.Concluida;
        pendencia.DataConclusao = DateTime.UtcNow;
        pendencia.Observacao = string.IsNullOrWhiteSpace(observacao) ? pendencia.Observacao : observacao.Trim();
        pendencia.AtualizarDataModificacao();
    }

    private static void CancelarPendencia(PendenciaUsuario pendencia, string observacao)
    {
        pendencia.Status = StatusPendenciaUsuario.Cancelada;
        pendencia.DataConclusao = DateTime.UtcNow;
        pendencia.Observacao = observacao;
        pendencia.AtualizarDataModificacao();
    }

    private static string NormalizarEmail(string email)
    {
        var emailNormalizado = NormalizadorNomeAtleta.NormalizarTexto(email).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(emailNormalizado))
        {
            throw new RegraNegocioException("E-mail é obrigatório.");
        }

        try
        {
            _ = new System.Net.Mail.MailAddress(emailNormalizado);
        }
        catch
        {
            throw new RegraNegocioException("E-mail inválido.");
        }

        return emailNormalizado;
    }
}
