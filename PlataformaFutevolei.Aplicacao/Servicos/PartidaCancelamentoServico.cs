using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Interfaces.Seguranca;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Aplicacao.Mapeadores;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Aplicacao.Servicos;

public class PartidaCancelamentoServico(
    IPartidaRepositorio partidaRepositorio,
    ISolicitacaoCancelamentoPartidaRepositorio solicitacaoRepositorio,
    IPendenciaUsuarioRepositorio pendenciaUsuarioRepositorio,
    IHistoricoPartidaRepositorio historicoPartidaRepositorio,
    IUnidadeTrabalho unidadeTrabalho,
    IAutorizacaoUsuarioServico autorizacaoUsuarioServico,
    IPontuacaoBeneficioServico pontuacaoBeneficioServico,
    ILogger<PartidaCancelamentoServico> logger
) : IPartidaCancelamentoServico
{
    private const int LimiteObservacao = 200;
    private const int LimiteMotivoExclusao = 200;
    private const int LimiteMotivoCancelamentoDireto = 200;

    public async Task<SolicitacaoCancelamentoPartidaDto> SolicitarAsync(
        Guid partidaId,
        SolicitarCancelamentoPartidaDto dto,
        CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        var motivo = ValidarMotivo(dto);
        var observacao = NormalizarObservacao(dto.Observacao, motivo);
        SolicitacaoCancelamentoPartida? solicitacaoCriada = null;

        await unidadeTrabalho.ExecutarEmTransacaoAsync(async ct =>
        {
            var partida = await partidaRepositorio.ObterPorIdParaAtualizacaoAsync(partidaId, ct)
                ?? throw new EntidadeNaoEncontradaException("Partida não encontrada.");

            ValidarPartidaPodeReceberSolicitacao(partida);

            if (await solicitacaoRepositorio.ObterPendentePorPartidaAsync(partida.Id, ct) is not null)
            {
                throw new ConflitoEstadoException("Já existe uma solicitação de cancelamento pendente para esta partida.");
            }

            var atletaSolicitanteId = usuario.AtletaId
                ?? throw new AcessoNegadoException("Somente atletas participantes da partida podem solicitar cancelamento.");
            var (duplaSolicitante, duplaAdversaria) = ObterDuplasDoSolicitante(partida, atletaSolicitanteId);
            var atletasAdversarios = ObterAtletasDaDuplaAdversaria(duplaAdversaria);

            var solicitacao = new SolicitacaoCancelamentoPartida
            {
                PartidaId = partida.Id,
                Partida = partida,
                SolicitadaPorUsuarioId = usuario.Id,
                SolicitadaPorUsuario = usuario,
                SolicitadaEm = DateTime.UtcNow,
                DuplaSolicitanteId = duplaSolicitante.Id,
                DuplaSolicitante = duplaSolicitante,
                DuplaAdversariaId = duplaAdversaria.Id,
                DuplaAdversaria = duplaAdversaria,
                Motivo = motivo,
                Observacao = observacao,
                Status = StatusSolicitacaoCancelamentoPartida.Pendente
            };

            await solicitacaoRepositorio.AdicionarAsync(solicitacao, ct);
            foreach (var atleta in atletasAdversarios)
            {
                var pendencia = new PendenciaUsuario
                {
                    Tipo = TipoPendenciaUsuario.ResponderCancelamentoPartida,
                    UsuarioId = atleta.Usuario!.Id,
                    Usuario = atleta.Usuario,
                    AtletaId = atleta.Id,
                    Atleta = atleta,
                    PartidaId = partida.Id,
                    Partida = partida,
                    SolicitacaoCancelamentoPartidaId = solicitacao.Id,
                    SolicitacaoCancelamentoPartida = solicitacao,
                    Status = StatusPendenciaUsuario.Pendente
                };

                solicitacao.Pendencias.Add(pendencia);
                await pendenciaUsuarioRepositorio.AdicionarAsync(pendencia, ct);
            }

            await RegistrarHistoricoAsync(partida, "SolicitacaoCancelamentoCriada", usuario, ObterTextoMotivo(motivo, observacao), ct);
            await unidadeTrabalho.SalvarAlteracoesAsync(ct);
            solicitacaoCriada = solicitacao;
        }, cancellationToken);

        logger.LogInformation(
            "Solicitação de cancelamento criada. PartidaId={PartidaId} SolicitacaoId={SolicitacaoId} UsuarioId={UsuarioId}",
            partidaId,
            solicitacaoCriada?.Id,
            usuario.Id);

        return solicitacaoCriada!.ParaDto(usuario);
    }

    public async Task<SolicitacaoCancelamentoPartidaDto?> ObterAtualAsync(
        Guid partidaId,
        CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualAsync(cancellationToken);
        var solicitacoes = await solicitacaoRepositorio.ListarPorPartidaAsync(partidaId, cancellationToken);
        var atual = solicitacoes
            .OrderByDescending(x => x.Status == StatusSolicitacaoCancelamentoPartida.Pendente)
            .ThenByDescending(x => x.DataCriacao)
            .FirstOrDefault();

        return atual?.ParaDto(usuario);
    }

    public Task<SolicitacaoCancelamentoPartidaDto> AprovarAsync(
        Guid partidaId,
        Guid solicitacaoId,
        CancellationToken cancellationToken = default)
    {
        return ResponderAsync(partidaId, solicitacaoId, aprovar: true, cancellationToken);
    }

    public Task<SolicitacaoCancelamentoPartidaDto> RecusarAsync(
        Guid partidaId,
        Guid solicitacaoId,
        CancellationToken cancellationToken = default)
    {
        return ResponderAsync(partidaId, solicitacaoId, aprovar: false, cancellationToken);
    }

    public async Task<SolicitacaoCancelamentoPartidaDto> CancelarSolicitacaoAsync(
        Guid partidaId,
        Guid solicitacaoId,
        CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        SolicitacaoCancelamentoPartida? solicitacaoAtualizada = null;

        await unidadeTrabalho.ExecutarEmTransacaoAsync(async ct =>
        {
            var partida = await partidaRepositorio.ObterPorIdParaAtualizacaoAsync(partidaId, ct)
                ?? throw new EntidadeNaoEncontradaException("Partida não encontrada.");
            var solicitacao = await ObterSolicitacaoPendenteParaTransicaoAsync(partida.Id, solicitacaoId, ct);

            if (solicitacao.SolicitadaPorUsuarioId != usuario.Id)
            {
                throw new AcessoNegadoException("Apenas quem solicitou pode cancelar esta solicitação.");
            }

            solicitacao.Status = StatusSolicitacaoCancelamentoPartida.CanceladaPeloSolicitante;
            solicitacao.CanceladaPeloSolicitanteEm = DateTime.UtcNow;
            solicitacao.AtualizarDataModificacao();
            EncerrarPendenciasCancelamento(solicitacao, null, StatusPendenciaUsuario.Cancelada, "Cancelada porque o solicitante encerrou a solicitação.");
            solicitacaoRepositorio.Atualizar(solicitacao);
            await RegistrarHistoricoAsync(partida, "SolicitacaoCancelamentoCancelada", usuario, "Solicitação cancelada pelo solicitante.", ct);
            await unidadeTrabalho.SalvarAlteracoesAsync(ct);
            solicitacaoAtualizada = solicitacao;
        }, cancellationToken);

        logger.LogInformation(
            "Solicitação de cancelamento cancelada pelo solicitante. PartidaId={PartidaId} SolicitacaoId={SolicitacaoId} UsuarioId={UsuarioId}",
            partidaId,
            solicitacaoId,
            usuario.Id);

        return solicitacaoAtualizada!.ParaDto(usuario);
    }

    public async Task<PartidaDto> CancelarDiretamenteAsync(
        Guid partidaId,
        CancelarPartidaDto dto,
        CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        var motivo = NormalizarMotivoCancelamentoDireto(dto);
        Partida? partidaAtualizada = null;

        await unidadeTrabalho.ExecutarEmTransacaoAsync(async ct =>
        {
            var partida = await partidaRepositorio.ObterPorIdParaAtualizacaoAsync(partidaId, ct)
                ?? throw new EntidadeNaoEncontradaException("Partida não encontrada.");

            ValidarUsuarioPodeGerenciarPartida(partida, usuario, "cancelar");

            if (partida.Cancelada)
            {
                partidaAtualizada = partida;
                return;
            }

            var solicitacaoPendente = await solicitacaoRepositorio.ObterPendentePorPartidaAsync(partida.Id, ct);
            if (solicitacaoPendente is not null)
            {
                EncerrarSolicitacaoPendentePorAcaoDireta(
                    solicitacaoPendente,
                    "Encerrada porque a partida foi cancelada diretamente.");
                solicitacaoRepositorio.Atualizar(solicitacaoPendente);
            }

            partida.Cancelada = true;
            partida.CanceladaEm = DateTime.UtcNow;
            partida.AtualizarDataModificacao();
            partidaRepositorio.Atualizar(partida);

            await EncerrarPendenciasOperacionaisDaPartidaCanceladaAsync(partida.Id, solicitacaoPendente?.Id ?? Guid.Empty, ct);
            await pontuacaoBeneficioServico.EstornarPartidaAsync(partida.Id, ct);
            await RegistrarHistoricoAsync(partida, "CancelamentoDireto", usuario, motivo, ct);
            await unidadeTrabalho.SalvarAlteracoesAsync(ct);
            partidaAtualizada = partida;
        }, cancellationToken);

        logger.LogWarning(
            "Partida cancelada diretamente. PartidaId={PartidaId} UsuarioId={UsuarioId}",
            partidaId,
            usuario.Id);

        return partidaAtualizada!.ParaDto(usuario);
    }

    public async Task ExcluirDefinitivamenteAsync(
        Guid partidaId,
        ExcluirPartidaDefinitivamenteDto dto,
        CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        var motivo = NormalizarMotivoExclusao(dto);

        await unidadeTrabalho.ExecutarEmTransacaoAsync(async ct =>
        {
            var partida = await partidaRepositorio.ObterPorIdParaAtualizacaoAsync(partidaId, ct)
                ?? throw new EntidadeNaoEncontradaException("Partida não encontrada.");

            ValidarUsuarioPodeGerenciarPartida(partida, usuario, "excluir definitivamente");

            if (partida.ExcluidaDefinitivamenteEm.HasValue)
            {
                throw new ConflitoEstadoException("Esta partida já foi excluída definitivamente.");
            }

            var solicitacaoPendente = await solicitacaoRepositorio.ObterPendentePorPartidaAsync(partida.Id, ct);
            if (solicitacaoPendente is not null)
            {
                EncerrarSolicitacaoPendentePorAcaoDireta(
                    solicitacaoPendente,
                    "Encerrada porque a partida foi excluída definitivamente.");
                solicitacaoRepositorio.Atualizar(solicitacaoPendente);
            }

            await EncerrarPendenciasOperacionaisDaPartidaCanceladaAsync(partida.Id, solicitacaoPendente?.Id ?? Guid.Empty, ct);

            await pontuacaoBeneficioServico.EstornarPartidaAsync(partida.Id, ct);
            partida.Ativa = false;
            partida.ExcluidaDefinitivamenteEm = DateTime.UtcNow;
            partida.ExcluidaDefinitivamentePorUsuarioId = usuario.Id;
            partida.MotivoExclusaoDefinitiva = motivo;
            partida.AtualizarDataModificacao();
            partidaRepositorio.Atualizar(partida);
            await RegistrarHistoricoAsync(partida, "ExclusaoDefinitiva", usuario, motivo, ct);
            await unidadeTrabalho.SalvarAlteracoesAsync(ct);
        }, cancellationToken);

        logger.LogWarning(
            "Partida excluída definitivamente de forma lógica. PartidaId={PartidaId} UsuarioId={UsuarioId} Motivo={Motivo}",
            partidaId,
            usuario.Id,
            motivo);
    }

    private async Task<SolicitacaoCancelamentoPartidaDto> ResponderAsync(
        Guid partidaId,
        Guid solicitacaoId,
        bool aprovar,
        CancellationToken cancellationToken)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        SolicitacaoCancelamentoPartida? solicitacaoAtualizada = null;

        await unidadeTrabalho.ExecutarEmTransacaoAsync(async ct =>
        {
            var partida = await partidaRepositorio.ObterPorIdParaAtualizacaoAsync(partidaId, ct)
                ?? throw new EntidadeNaoEncontradaException("Partida não encontrada.");
            var solicitacao = await ObterSolicitacaoPendenteParaTransicaoAsync(partida.Id, solicitacaoId, ct);
            var pendenciaUsuario = ValidarUsuarioPodeResponder(solicitacao, usuario);

            solicitacao.Status = aprovar
                ? StatusSolicitacaoCancelamentoPartida.Aprovada
                : StatusSolicitacaoCancelamentoPartida.Recusada;
            solicitacao.RespondidaPorUsuarioId = usuario.Id;
            solicitacao.RespondidaEm = DateTime.UtcNow;
            solicitacao.AtualizarDataModificacao();

            EncerrarPendenciasCancelamento(
                solicitacao,
                pendenciaUsuario.Id,
                StatusPendenciaUsuario.Concluida,
                aprovar ? "Cancelamento aprovado por atleta adversário." : "Cancelamento recusado por atleta adversário.");

            solicitacaoRepositorio.Atualizar(solicitacao);

            if (aprovar)
            {
                if (partida.Cancelada)
                {
                    throw new ConflitoEstadoException("Esta partida já foi cancelada.");
                }

                partida.Cancelada = true;
                partida.CanceladaEm = DateTime.UtcNow;
                partida.SolicitacaoCancelamentoOrigemId = solicitacao.Id;
                partida.AtualizarDataModificacao();
                partidaRepositorio.Atualizar(partida);
                await EncerrarPendenciasOperacionaisDaPartidaCanceladaAsync(partida.Id, solicitacao.Id, ct);
                await pontuacaoBeneficioServico.EstornarPartidaAsync(partida.Id, ct);
            }

            await RegistrarHistoricoAsync(
                partida,
                aprovar ? "SolicitacaoCancelamentoAprovada" : "SolicitacaoCancelamentoRecusada",
                usuario,
                aprovar ? "Cancelamento aprovado por atleta adversário." : "Cancelamento recusado por atleta adversário.",
                ct);
            await unidadeTrabalho.SalvarAlteracoesAsync(ct);
            solicitacaoAtualizada = solicitacao;
        }, cancellationToken);

        logger.LogInformation(
            "Solicitação de cancelamento respondida. PartidaId={PartidaId} SolicitacaoId={SolicitacaoId} UsuarioId={UsuarioId} Aprovada={Aprovada}",
            partidaId,
            solicitacaoId,
            usuario.Id,
            aprovar);

        return solicitacaoAtualizada!.ParaDto(usuario);
    }

    private async Task<SolicitacaoCancelamentoPartida> ObterSolicitacaoPendenteParaTransicaoAsync(
        Guid partidaId,
        Guid solicitacaoId,
        CancellationToken cancellationToken)
    {
        var solicitacao = await solicitacaoRepositorio.ObterPorIdParaAtualizacaoAsync(solicitacaoId, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Solicitação de cancelamento não encontrada.");

        if (solicitacao.PartidaId != partidaId)
        {
            throw new EntidadeNaoEncontradaException("Solicitação de cancelamento não encontrada para esta partida.");
        }

        if (solicitacao.Status != StatusSolicitacaoCancelamentoPartida.Pendente)
        {
            throw new ConflitoEstadoException("Esta solicitação de cancelamento já foi processada.");
        }

        if (solicitacao.Partida.ExcluidaDefinitivamenteEm.HasValue)
        {
            throw new EntidadeNaoEncontradaException("Partida não encontrada.");
        }

        if (solicitacao.Partida.Cancelada)
        {
            throw new ConflitoEstadoException("Esta partida já foi cancelada.");
        }

        return solicitacao;
    }

    private static PendenciaUsuario ValidarUsuarioPodeResponder(
        SolicitacaoCancelamentoPartida solicitacao,
        Usuario usuario)
    {
        var atletaId = usuario.AtletaId
            ?? throw new AcessoNegadoException("Somente atletas da dupla adversária podem responder ao cancelamento.");

        if (!DuplaContemAtleta(solicitacao.DuplaAdversaria, atletaId))
        {
            throw new AcessoNegadoException("Somente atletas da dupla adversária podem responder ao cancelamento.");
        }

        if (DuplaContemAtleta(solicitacao.DuplaSolicitante, atletaId))
        {
            throw new AcessoNegadoException("O solicitante e sua dupla não podem responder à própria solicitação.");
        }

        var pendencia = solicitacao.Pendencias.FirstOrDefault(x =>
            x.UsuarioId == usuario.Id &&
            x.AtletaId == atletaId &&
            x.Status == StatusPendenciaUsuario.Pendente);

        return pendencia
            ?? throw new ConflitoEstadoException("Esta pendência de cancelamento já foi processada.");
    }

    private static void ValidarPartidaPodeReceberSolicitacao(Partida partida)
    {
        if (partida.ExcluidaDefinitivamenteEm.HasValue)
        {
            throw new EntidadeNaoEncontradaException("Partida não encontrada.");
        }

        if (partida.Cancelada)
        {
            throw new ConflitoEstadoException("Esta partida já foi cancelada.");
        }

        if (partida.Status != StatusPartida.Encerrada)
        {
            throw new RegraNegocioException("Somente partidas encerradas podem receber solicitação de cancelamento.");
        }

        if (partida.DuplaA is null || partida.DuplaB is null)
        {
            throw new RegraNegocioException("A partida precisa ter as duas duplas definidas para solicitar cancelamento.");
        }
    }

    private static (Dupla DuplaSolicitante, Dupla DuplaAdversaria) ObterDuplasDoSolicitante(
        Partida partida,
        Guid atletaSolicitanteId)
    {
        if (DuplaContemAtleta(partida.DuplaA, atletaSolicitanteId) && partida.DuplaB is not null)
        {
            return (partida.DuplaA!, partida.DuplaB);
        }

        if (DuplaContemAtleta(partida.DuplaB, atletaSolicitanteId) && partida.DuplaA is not null)
        {
            return (partida.DuplaB!, partida.DuplaA);
        }

        throw new AcessoNegadoException("Somente atletas participantes da partida podem solicitar cancelamento.");
    }

    private static IReadOnlyList<Atleta> ObterAtletasDaDuplaAdversaria(Dupla duplaAdversaria)
    {
        var atletas = new[] { duplaAdversaria.Atleta1, duplaAdversaria.Atleta2 }
            .OfType<Atleta>()
            .DistinctBy(x => x.Id)
            .ToList();

        if (atletas.Count != 2 || atletas.Any(x => x.Usuario is null))
        {
            throw new RegraNegocioException("A dupla adversária precisa ter dois atletas com usuário vinculado para responder ao cancelamento.");
        }

        return atletas;
    }

    private async Task EncerrarPendenciasOperacionaisDaPartidaCanceladaAsync(
        Guid partidaId,
        Guid solicitacaoCancelamentoId,
        CancellationToken cancellationToken)
    {
        var pendencias = await pendenciaUsuarioRepositorio.ListarPendentesPorPartidaAsync(partidaId, cancellationToken);
        foreach (var pendencia in pendencias.Where(x =>
                     x.SolicitacaoCancelamentoPartidaId != solicitacaoCancelamentoId ||
                     x.Tipo != TipoPendenciaUsuario.ResponderCancelamentoPartida))
        {
            pendencia.Status = StatusPendenciaUsuario.Cancelada;
            pendencia.DataConclusao = DateTime.UtcNow;
            pendencia.Observacao = "Cancelada porque a partida foi cancelada.";
            pendencia.AtualizarDataModificacao();
            pendenciaUsuarioRepositorio.Atualizar(pendencia);
        }
    }

    private static void EncerrarPendenciasCancelamento(
        SolicitacaoCancelamentoPartida solicitacao,
        Guid? pendenciaRespondidaId,
        StatusPendenciaUsuario statusPendenciaRespondida,
        string observacaoRespondida)
    {
        foreach (var pendencia in solicitacao.Pendencias.Where(x => x.Status == StatusPendenciaUsuario.Pendente))
        {
            if (pendenciaRespondidaId.HasValue && pendencia.Id == pendenciaRespondidaId.Value)
            {
                pendencia.Status = statusPendenciaRespondida;
                pendencia.Observacao = observacaoRespondida;
            }
            else
            {
                pendencia.Status = StatusPendenciaUsuario.Cancelada;
                pendencia.Observacao = pendenciaRespondidaId.HasValue
                    ? "Cancelada porque a solicitação de cancelamento já foi respondida por outro atleta da dupla."
                    : observacaoRespondida;
            }

            pendencia.DataConclusao = DateTime.UtcNow;
            pendencia.AtualizarDataModificacao();
        }
    }

    private static MotivoCancelamentoPartida ValidarMotivo(SolicitarCancelamentoPartidaDto dto)
    {
        if (!dto.Motivo.HasValue || !Enum.IsDefined(typeof(MotivoCancelamentoPartida), dto.Motivo.Value))
        {
            throw new RegraNegocioException("Informe o motivo do cancelamento.");
        }

        return dto.Motivo.Value;
    }

    private static string? NormalizarObservacao(string? observacao, MotivoCancelamentoPartida motivo)
    {
        var texto = observacao?.Trim();
        if (string.IsNullOrWhiteSpace(texto))
        {
            if (motivo == MotivoCancelamentoPartida.Outro)
            {
                throw new RegraNegocioException("Descreva o motivo do cancelamento.");
            }

            return null;
        }

        if (texto.Length > LimiteObservacao)
        {
            throw new RegraNegocioException($"A observação deve ter no máximo {LimiteObservacao} caracteres.");
        }

        return texto;
    }

    private static string NormalizarMotivoCancelamentoDireto(CancelarPartidaDto dto)
    {
        var motivo = dto.Motivo?.Trim();
        if (string.IsNullOrWhiteSpace(motivo))
        {
            throw new RegraNegocioException("Informe o motivo do cancelamento.");
        }

        if (motivo.Length > LimiteMotivoCancelamentoDireto)
        {
            throw new RegraNegocioException($"O motivo deve ter no máximo {LimiteMotivoCancelamentoDireto} caracteres.");
        }

        return motivo;
    }

    private static string NormalizarMotivoExclusao(ExcluirPartidaDefinitivamenteDto dto)
    {
        var motivo = dto.Motivo?.Trim();
        if (string.IsNullOrWhiteSpace(motivo))
        {
            throw new RegraNegocioException("Informe o motivo da exclusão definitiva.");
        }

        if (motivo.Length > LimiteMotivoExclusao)
        {
            throw new RegraNegocioException($"O motivo deve ter no máximo {LimiteMotivoExclusao} caracteres.");
        }

        return motivo;
    }

    private static void ValidarUsuarioPodeGerenciarPartida(Partida partida, Usuario usuario, string acao)
    {
        if (usuario.Perfil == PerfilUsuario.Administrador || partida.CriadoPorUsuarioId == usuario.Id)
        {
            return;
        }

        throw new AcessoNegadoException($"Você não tem permissão para {acao} esta partida.");
    }

    private static void EncerrarSolicitacaoPendentePorAcaoDireta(
        SolicitacaoCancelamentoPartida solicitacao,
        string observacao)
    {
        if (solicitacao.Status != StatusSolicitacaoCancelamentoPartida.Pendente)
        {
            return;
        }

        solicitacao.Status = StatusSolicitacaoCancelamentoPartida.CanceladaPeloSolicitante;
        solicitacao.CanceladaPeloSolicitanteEm = DateTime.UtcNow;
        solicitacao.AtualizarDataModificacao();
        EncerrarPendenciasCancelamento(solicitacao, null, StatusPendenciaUsuario.Cancelada, observacao);
    }

    private async Task RegistrarHistoricoAsync(
        Partida partida,
        string acao,
        Usuario usuario,
        string? motivo,
        CancellationToken cancellationToken)
    {
        await historicoPartidaRepositorio.AdicionarAsync(
            new HistoricoPartida
            {
                PartidaIdOriginal = partida.Id,
                Acao = acao,
                UsuarioResponsavelId = usuario.Id,
                DataHoraUtc = DateTime.UtcNow,
                Motivo = motivo,
                SnapshotJson = CriarSnapshotPartida(partida),
                CorrelationId = Activity.Current?.TraceId.ToString()
            },
            cancellationToken);
    }

    private static string CriarSnapshotPartida(Partida partida)
    {
        var snapshot = new
        {
            partida.Id,
            partida.GrupoId,
            GrupoNome = partida.Grupo?.Nome,
            partida.CategoriaCompeticaoId,
            CategoriaNome = partida.CategoriaCompeticao?.Nome,
            partida.CriadoPorUsuarioId,
            CriadoPorUsuarioNome = partida.CriadoPorUsuario?.Nome,
            partida.Status,
            partida.StatusAprovacao,
            partida.Ativa,
            partida.Cancelada,
            partida.CanceladaEm,
            partida.ExcluidaDefinitivamenteEm,
            partida.PlacarDuplaA,
            partida.PlacarDuplaB,
            partida.DuplaVencedoraId,
            partida.TipoRegistroResultado,
            partida.DataPartida,
            partida.DataCriacao,
            partida.DataAtualizacao,
            DuplaA = CriarSnapshotDupla(partida.DuplaA),
            DuplaB = CriarSnapshotDupla(partida.DuplaB)
        };

        return JsonSerializer.Serialize(snapshot);
    }

    private static object? CriarSnapshotDupla(Dupla? dupla)
    {
        if (dupla is null)
        {
            return null;
        }

        return new
        {
            dupla.Id,
            dupla.Nome,
            dupla.Atleta1Id,
            Atleta1Nome = dupla.Atleta1?.Nome,
            dupla.Atleta2Id,
            Atleta2Nome = dupla.Atleta2?.Nome
        };
    }

    private static string ObterTextoMotivo(MotivoCancelamentoPartida motivo, string? observacao)
        => string.IsNullOrWhiteSpace(observacao)
            ? motivo.ToString()
            : $"{motivo}: {observacao}";

    private static bool DuplaContemAtleta(Dupla? dupla, Guid atletaId)
        => dupla is not null &&
           (dupla.Atleta1Id == atletaId || dupla.Atleta2Id == atletaId);
}
