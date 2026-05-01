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
    IUnidadeTrabalho unidadeTrabalho,
    IAutorizacaoUsuarioServico autorizacaoUsuarioServico
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

    public async Task<PendenciaUsuarioDto> CompletarContatoAsync(
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
            atleta.Email = NormalizarEmail(dto.Email);
            atleta.AtualizarDataModificacao();
            await ConcluirPendenciasContatoAtletaAsync(
                atleta.Id,
                "Contato informado. A partida continua aguardando vínculo do atleta para liberar a aprovação.",
                cancellationToken);
        }

        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        var pendenciaAtualizada = await pendenciaUsuarioRepositorio.ObterPorIdAsync(pendencia.Id, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Pendência não encontrada.");
        return pendenciaAtualizada.ParaDto();
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

        var atletas = ObterAtletasParaAprovacao(partida, usuarioRegistradorId);

        foreach (var atleta in atletas)
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
            var atleta = ObterAtletasPartida(partida).FirstOrDefault(x => x.Id == atletaId);
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
        var partidaResolvida = PartidaPossuiRespostaDasDuasDuplas(partidaDetalhada, aprovacoes);
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

        var atletas = ObterAtletasPartida(partidaDetalhada);
        var aprovacoes = await partidaAprovacaoRepositorio.ListarPorPartidaAsync(partidaDetalhada.Id, cancellationToken);

        if (atletas.Any(x => x.Usuario is null))
        {
            partidaDetalhada.StatusAprovacao = StatusAprovacaoPartida.PendenteDeVinculos;
        }
        else if (PartidaPossuiRespostaDasDuasDuplas(partidaDetalhada, aprovacoes))
        {
            partidaDetalhada.StatusAprovacao = StatusAprovacaoPartida.Aprovada;
        }
        else
        {
            partidaDetalhada.StatusAprovacao = StatusAprovacaoPartida.PendenteAprovacao;
        }

        partidaDetalhada.AtualizarDataModificacao();
        partidaRepositorio.Atualizar(partidaDetalhada);
    }

    private static IReadOnlyList<Atleta> ObterAtletasPartida(Partida partida)
    {
        return new[]
        {
            partida.DuplaA?.Atleta1,
            partida.DuplaA?.Atleta2,
            partida.DuplaB?.Atleta1,
            partida.DuplaB?.Atleta2
        }
        .OfType<Atleta>()
        .DistinctBy(x => x.Id)
        .ToList();
    }

    private static IReadOnlyList<Atleta> ObterAtletasParaAprovacao(Partida partida, Guid usuarioRegistradorId)
    {
        if (partida.CriadoPorUsuarioId != usuarioRegistradorId || partida.DuplaB is null)
        {
            return ObterAtletasPartida(partida);
        }

        return new[]
        {
            partida.DuplaB.Atleta1,
            partida.DuplaB.Atleta2
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

    private static bool PartidaPossuiRespostaDasDuasDuplas(
        Partida partida,
        IReadOnlyList<PartidaAprovacao> aprovacoes)
    {
        return DuplaPossuiResposta(partida.DuplaA, aprovacoes) &&
               DuplaPossuiResposta(partida.DuplaB, aprovacoes);
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
