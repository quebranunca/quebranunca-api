using Microsoft.Extensions.Logging;
using PlataformaFutevolei.Aplicacao.Configuracoes;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Interfaces.Seguranca;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Aplicacao.Servicos;

public class PontuacaoBeneficioServico(
    IPontuacaoBeneficioRepositorio pontuacaoRepositorio,
    IUsuarioRepositorio usuarioRepositorio,
    IUnidadeTrabalho unidadeTrabalho,
    IAutorizacaoUsuarioServico autorizacaoUsuarioServico,
    ILogger<PontuacaoBeneficioServico> logger
) : IPontuacaoBeneficioServico
{
    private static readonly IReadOnlyCollection<TipoEventoPontuacaoBeneficio> EventosPartida =
    [
        TipoEventoPontuacaoBeneficio.PartidaParticipante
    ];

    private static readonly IReadOnlyCollection<TipoEventoPontuacaoBeneficio> EventosCompartilhamento =
    [
        TipoEventoPontuacaoBeneficio.CompartilhamentoPartida,
        TipoEventoPontuacaoBeneficio.CompartilhamentoRanking,
        TipoEventoPontuacaoBeneficio.CompartilhamentoScoutAtleta,
        TipoEventoPontuacaoBeneficio.CompartilhamentoScoutDupla
    ];

    public async Task<GamificacaoResumoDto> ObterResumoAsync(CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        var atletaId = usuario.AtletaId;
        var saldo = atletaId.HasValue
            ? await pontuacaoRepositorio.ObterSaldoPorAtletaAsync(atletaId.Value, cancellationToken)
            : null;

        var resumoPontuacao = MontarResumoPontuacao(atletaId, saldo);
        var nivel = MontarNivel(resumoPontuacao.TotalAcumulado);
        var beneficios = await ListarBeneficiosAsync(null, true, null, cancellationToken);
        var missoes = atletaId.HasValue
            ? await ListarMissoesAsync(cancellationToken)
            : [];
        var conquistas = atletaId.HasValue
            ? await ListarConquistasAsync(cancellationToken)
            : [];

        var inicioMes = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var fimMes = inicioMes.AddMonths(1);
        var (inicioSemana, fimSemana) = ObterIntervaloSemanaUtc(DateTime.UtcNow);
        var atividade = atletaId.HasValue
            ? new GamificacaoResumoAtividadeDto(
                await pontuacaoRepositorio.ContarEventosAsync(atletaId.Value, EventosPartida, inicioMes, fimMes, cancellationToken),
                await pontuacaoRepositorio.ContarEventosAsync(atletaId.Value, EventosCompartilhamento, inicioSemana, fimSemana, cancellationToken),
                await pontuacaoRepositorio.ContarEventosAsync(atletaId.Value, [TipoEventoPontuacaoBeneficio.PartidaParticipante], inicioSemana, fimSemana, cancellationToken),
                null)
            : new GamificacaoResumoAtividadeDto(0, 0, 0, null);

        return new GamificacaoResumoDto(
            resumoPontuacao,
            nivel,
            atividade,
            beneficios.OrderBy(x => x.PontosFaltantes).ThenBy(x => x.PontosNecessarios).Take(3).ToList(),
            missoes.Take(4).ToList(),
            conquistas.Where(x => x.Desbloqueada).Take(4).ToList());
    }

    public async Task<ExtratoPontuacaoBeneficioListaDto> ListarExtratoAsync(
        TipoEventoPontuacaoBeneficio? tipo,
        DateTime? dataInicial,
        DateTime? dataFinal,
        int pagina,
        int quantidadePorPagina,
        CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        if (!usuario.AtletaId.HasValue)
        {
            return new ExtratoPontuacaoBeneficioListaDto(1, 20, []);
        }

        var paginaNormalizada = Math.Max(1, pagina);
        var quantidadeNormalizada = Math.Clamp(quantidadePorPagina, 1, 50);
        var extrato = await pontuacaoRepositorio.ListarExtratoAsync(
            usuario.AtletaId.Value,
            tipo,
            dataInicial,
            dataFinal,
            (paginaNormalizada - 1) * quantidadeNormalizada,
            quantidadeNormalizada,
            cancellationToken);

        return new ExtratoPontuacaoBeneficioListaDto(
            paginaNormalizada,
            quantidadeNormalizada,
            extrato.Select(MapearExtrato).ToList());
    }

    public async Task<IReadOnlyList<BeneficioPontuacaoDto>> ListarBeneficiosAsync(
        TipoBeneficioPontuacao? tipo,
        bool? disponivel,
        bool? destaque,
        CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualAsync(cancellationToken);
        var saldoAtual = 0;
        if (usuario?.AtletaId is Guid atletaId)
        {
            var saldo = await pontuacaoRepositorio.ObterSaldoPorAtletaAsync(atletaId, cancellationToken);
            saldoAtual = saldo?.SaldoAtual ?? 0;
        }

        var beneficios = await pontuacaoRepositorio.ListarBeneficiosAtivosAsync(tipo, disponivel, destaque, cancellationToken);
        return beneficios.Select(x => MapearBeneficio(x, saldoAtual)).ToList();
    }

    public async Task<ResgateBeneficioPontuacaoDto> SolicitarResgateAsync(
        Guid beneficioId,
        SolicitarResgateBeneficioDto dto,
        CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        var atletaId = usuario.AtletaId ?? throw new RegraNegocioException("Seu usuário precisa estar vinculado a um atleta para resgatar benefícios.");
        var beneficio = await pontuacaoRepositorio.ObterBeneficioPorIdAsync(beneficioId, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Benefício não encontrado.");
        if (!beneficio.Ativo || beneficio.QuantidadeDisponivel == 0)
        {
            throw new RegraNegocioException("Benefício indisponível para resgate.");
        }

        if (await pontuacaoRepositorio.ExisteResgateSolicitadoAsync(atletaId, beneficioId, cancellationToken))
        {
            throw new RegraNegocioException("Já existe um resgate solicitado para este benefício.");
        }

        var saldo = await ObterOuCriarSaldoParaAtualizacaoAsync(atletaId, cancellationToken);
        if (saldo.SaldoAtual < beneficio.PontosNecessarios)
        {
            throw new RegraNegocioException("Saldo insuficiente para este benefício.");
        }

        ResgateBeneficioPontuacao? resgateCriado = null;
        await unidadeTrabalho.ExecutarEmTransacaoAsync(async ct =>
        {
            var resgate = new ResgateBeneficioPontuacao
            {
                AtletaId = atletaId,
                BeneficioId = beneficio.Id,
                PontosUtilizados = beneficio.PontosNecessarios,
                Status = StatusResgateBeneficioPontuacao.Solicitado,
                ObservacaoAtleta = dto.ObservacaoAtleta?.Trim(),
                SolicitadoEm = DateTime.UtcNow,
                AtualizadoPorUsuarioId = usuario.Id
            };

            await pontuacaoRepositorio.AdicionarResgateAsync(resgate, ct);
            await RegistrarMovimentacaoAsync(
                atletaId,
                -beneficio.PontosNecessarios,
                TipoEventoPontuacaoBeneficio.ResgateBeneficio,
                $"Resgate solicitado: {beneficio.Titulo}",
                "Resgate",
                $"RESGATE:{resgate.Id}:ATLETA:{atletaId}",
                null,
                null,
                resgate.Id,
                usuario.Id,
                ct,
                salvarAoFinal: false);

            await unidadeTrabalho.SalvarAlteracoesAsync(ct);
            resgate.Beneficio = beneficio;
            resgateCriado = resgate;
        }, cancellationToken);

        logger.LogInformation("Resgate de benefício Pontos QN solicitado. ResgateId={ResgateId} AtletaId={AtletaId} BeneficioId={BeneficioId}", resgateCriado?.Id, atletaId, beneficioId);
        var resgatePersistido = await pontuacaoRepositorio.ObterResgatePorIdAsync(resgateCriado!.Id, cancellationToken)
            ?? resgateCriado;
        resgatePersistido.Beneficio ??= beneficio;
        return MapearResgate(resgatePersistido);
    }

    public async Task<IReadOnlyList<ResgateBeneficioPontuacaoDto>> ListarMeusResgatesAsync(CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        if (!usuario.AtletaId.HasValue)
        {
            return [];
        }

        var resgates = await pontuacaoRepositorio.ListarResgatesPorAtletaAsync(usuario.AtletaId.Value, cancellationToken);
        return resgates.Select(MapearResgate).ToList();
    }

    public async Task<IReadOnlyList<ResgateBeneficioPontuacaoDto>> ListarResgatesAdministracaoAsync(CancellationToken cancellationToken = default)
    {
        await autorizacaoUsuarioServico.GarantirAdministradorAsync(cancellationToken);
        var resgates = await pontuacaoRepositorio.ListarResgatesAdministracaoAsync(cancellationToken);
        return resgates.Select(MapearResgate).ToList();
    }

    public Task<ResgateBeneficioPontuacaoDto> AprovarResgateAsync(
        Guid resgateId,
        AtualizarStatusResgateBeneficioDto dto,
        CancellationToken cancellationToken = default)
    {
        return AtualizarStatusResgateAsync(
            resgateId,
            dto,
            StatusResgateBeneficioPontuacao.Aprovado,
            cancellationToken);
    }

    public Task<ResgateBeneficioPontuacaoDto> RejeitarResgateAsync(
        Guid resgateId,
        AtualizarStatusResgateBeneficioDto dto,
        CancellationToken cancellationToken = default)
    {
        return AtualizarStatusResgateAsync(
            resgateId,
            dto,
            StatusResgateBeneficioPontuacao.Rejeitado,
            cancellationToken);
    }

    public Task<ResgateBeneficioPontuacaoDto> CancelarResgateAsync(
        Guid resgateId,
        AtualizarStatusResgateBeneficioDto dto,
        CancellationToken cancellationToken = default)
    {
        return AtualizarStatusResgateAsync(
            resgateId,
            dto,
            StatusResgateBeneficioPontuacao.Cancelado,
            cancellationToken);
    }

    public async Task<IReadOnlyList<MissaoPontuacaoDto>> ListarMissoesAsync(CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        if (!usuario.AtletaId.HasValue)
        {
            return [];
        }

        var atletaId = usuario.AtletaId.Value;
        var (inicioSemana, fimSemana) = ObterIntervaloSemanaUtc(DateTime.UtcNow);
        var partidas = await pontuacaoRepositorio.ContarEventosAsync(atletaId, [TipoEventoPontuacaoBeneficio.PartidaParticipante], inicioSemana, fimSemana, cancellationToken);
        var compartilhamentos = await pontuacaoRepositorio.ContarEventosAsync(atletaId, EventosCompartilhamento, inicioSemana, fimSemana, cancellationToken);
        var pendencias = await pontuacaoRepositorio.ContarEventosAsync(atletaId, [TipoEventoPontuacaoBeneficio.PendenciaResolvida], inicioSemana, fimSemana, cancellationToken);
        var placares = await pontuacaoRepositorio.ContarEventosAsync(atletaId, [TipoEventoPontuacaoBeneficio.PartidaPlacarCompleto], inicioSemana, fimSemana, cancellationToken);

        return
        [
            new("jogar-3-partidas", "Jogue 3 partidas", "Participe de partidas válidas na semana.", Math.Min(partidas, 3), 3, PontuacaoBeneficioRegras.SequenciaSemanal, partidas >= 3, inicioSemana, fimSemana),
            new("jogar-5-partidas", "Jogue 5 partidas", "Mantenha ritmo forte na semana.", Math.Min(partidas, 5), 5, PontuacaoBeneficioRegras.FrequenciaCincoPartidasSemana, partidas >= 5, inicioSemana, fimSemana),
            new("compartilhar-2-resultados", "Compartilhe 2 resultados", "Compartilhe resultados, ranking ou scout.", Math.Min(compartilhamentos, 2), 2, PontuacaoBeneficioRegras.Compartilhamento, compartilhamentos >= 2, inicioSemana, fimSemana),
            new("resolver-1-pendencia", "Resolva 1 pendência", "Ajude a comunidade corrigindo dados pendentes.", Math.Min(pendencias, 1), 1, PontuacaoBeneficioRegras.PendenciaResolvida, pendencias >= 1, inicioSemana, fimSemana),
            new("registrar-1-placar", "Registre 1 partida com placar completo", "Inclua o placar para melhorar a qualidade dos dados.", Math.Min(placares, 1), 1, PontuacaoBeneficioRegras.PartidaPlacarCompleto, placares >= 1, inicioSemana, fimSemana)
        ];
    }

    public async Task<IReadOnlyList<ConquistaAtletaDto>> ListarConquistasAsync(CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        if (!usuario.AtletaId.HasValue)
        {
            return [];
        }

        var atletaId = usuario.AtletaId.Value;
        var saldo = await pontuacaoRepositorio.ObterSaldoPorAtletaAsync(atletaId, cancellationToken);
        var inicio = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var fim = DateTime.UtcNow.AddYears(10);
        var partidas = await pontuacaoRepositorio.ContarEventosAsync(atletaId, [TipoEventoPontuacaoBeneficio.PartidaParticipante], inicio, fim, cancellationToken);
        var compartilhamentos = await pontuacaoRepositorio.ContarEventosAsync(atletaId, EventosCompartilhamento, inicio, fim, cancellationToken);
        var (inicioSemana, fimSemana) = ObterIntervaloSemanaUtc(DateTime.UtcNow);
        var partidasSemana = await pontuacaoRepositorio.ContarEventosAsync(atletaId, [TipoEventoPontuacaoBeneficio.PartidaParticipante], inicioSemana, fimSemana, cancellationToken);
        var totalAcumulado = saldo?.TotalAcumulado ?? 0;

        return
        [
            CriarConquista("primeira-partida", "Primeira Partida", "Participou da primeira partida válida.", partidas, 1),
            CriarConquista("sequencia-7-dias", "Sequência de 7 dias", "Manteve presença constante na semana.", partidasSemana, 7),
            CriarConquista("top-10-grupo", "Top 10 do grupo", "Chegue ao Top 10 de um ranking de grupo.", 0, 1),
            CriarConquista("mestre-compartilhamento", "Mestre do Compartilhamento", "Compartilhou 10 resultados, rankings ou scouts.", compartilhamentos, 10),
            CriarConquista("maratonista", "Maratonista", "Participou de 30 partidas válidas.", partidas, 30),
            CriarConquista("invicto", "Invicto", "Sequência esportiva sem derrotas será ligada ao scout em versão futura.", 0, 1),
            CriarConquista("lenda-qn", "Lenda QN", "Acumulou 4000 Pontos QN.", totalAcumulado, 4000)
        ];
    }

    public async Task RegistrarCompartilhamentoAsync(
        RegistrarCompartilhamentoGamificacaoDto dto,
        CancellationToken cancellationToken = default)
    {
        ValidarCompartilhamento(dto);
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        var atletaId = usuario.AtletaId ?? throw new RegraNegocioException("Seu usuário precisa estar vinculado a um atleta para pontuar compartilhamentos.");
        var evento = PontuacaoBeneficioRegras.ObterEventoCompartilhamento(dto.Tipo);
        var hoje = DateTime.UtcNow.Date;
        var amanha = hoje.AddDays(1);
        var (inicioSemana, fimSemana) = ObterIntervaloSemanaUtc(DateTime.UtcNow);

        var eventosHoje = await pontuacaoRepositorio.ContarEventosAsync(atletaId, [evento], hoje, amanha, cancellationToken);
        var eventosSemana = await pontuacaoRepositorio.ContarEventosAsync(atletaId, EventosCompartilhamento, inicioSemana, fimSemana, cancellationToken);
        if (eventosHoje >= PontuacaoBeneficioRegras.LimiteCompartilhamentoDiarioPorTipo ||
            eventosSemana >= PontuacaoBeneficioRegras.LimiteCompartilhamentoSemanalTotal)
        {
            logger.LogInformation("Compartilhamento Pontos QN ignorado por limite. AtletaId={AtletaId} Tipo={Tipo}", atletaId, dto.Tipo);
            return;
        }

        var chave = MontarChaveCompartilhamento(dto, atletaId, hoje);
        await RegistrarMovimentacaoAsync(
            atletaId,
            PontuacaoBeneficioRegras.Compartilhamento,
            evento,
            MontarDescricaoCompartilhamento(dto.Tipo),
            "Compartilhamento",
            chave,
            dto.GrupoId,
            dto.PartidaId,
            null,
            usuario.Id,
            cancellationToken);
    }

    private static void ValidarCompartilhamento(RegistrarCompartilhamentoGamificacaoDto dto)
    {
        if (dto.Tipo == TipoCompartilhamentoGamificacao.Partida && !dto.PartidaId.HasValue)
        {
            throw new RegraNegocioException("Informe a partida compartilhada.");
        }

        if (dto.Tipo == TipoCompartilhamentoGamificacao.ScoutAtleta && !dto.AtletaId.HasValue)
        {
            throw new RegraNegocioException("Informe o atleta do scout compartilhado.");
        }

        if (dto.Tipo == TipoCompartilhamentoGamificacao.ScoutDupla && !dto.DuplaId.HasValue)
        {
            throw new RegraNegocioException("Informe a dupla do scout compartilhado.");
        }
    }

    public async Task PontuarPartidaValidadaAsync(
        Partida partida,
        Guid? usuarioRegistradorId,
        CancellationToken cancellationToken = default)
    {
        if (!PartidaPodePontuar(partida))
        {
            return;
        }

        var atletas = ObterAtletasPartida(partida).Distinct().ToList();
        foreach (var atletaId in atletas)
        {
            await RegistrarMovimentacaoAsync(
                atletaId,
                PontuacaoBeneficioRegras.PartidaParticipante,
                TipoEventoPontuacaoBeneficio.PartidaParticipante,
                "Participou de partida validada",
                "Partida",
                $"PARTIDA_PARTICIPANTE:{partida.Id}:ATLETA:{atletaId}",
                partida.GrupoId,
                partida.Id,
                null,
                usuarioRegistradorId,
                cancellationToken);
        }

        foreach (var atletaId in ObterAtletasDuplaVencedora(partida))
        {
            await RegistrarMovimentacaoAsync(
                atletaId,
                PontuacaoBeneficioRegras.PartidaVitoria,
                TipoEventoPontuacaoBeneficio.PartidaVitoria,
                "Venceu partida validada",
                "Partida",
                $"PARTIDA_VITORIA:{partida.Id}:ATLETA:{atletaId}",
                partida.GrupoId,
                partida.Id,
                null,
                usuarioRegistradorId,
                cancellationToken);
        }

        var atletaRegistradorId = await ObterAtletaRegistradorAsync(usuarioRegistradorId, cancellationToken);
        if (!atletaRegistradorId.HasValue)
        {
            return;
        }

        await RegistrarMovimentacaoAsync(
            atletaRegistradorId.Value,
            PontuacaoBeneficioRegras.PartidaRegistrador,
            TipoEventoPontuacaoBeneficio.PartidaRegistrador,
            "Registrou partida válida",
            "Partida",
            $"PARTIDA_REGISTRADOR:{partida.Id}:USUARIO:{usuarioRegistradorId}:ATLETA:{atletaRegistradorId.Value}",
            partida.GrupoId,
            partida.Id,
            null,
            usuarioRegistradorId,
            cancellationToken);

        if (partida.PossuiPlacarDetalhado())
        {
            await RegistrarMovimentacaoAsync(
                atletaRegistradorId.Value,
                PontuacaoBeneficioRegras.PartidaPlacarCompleto,
                TipoEventoPontuacaoBeneficio.PartidaPlacarCompleto,
                "Registrou partida com placar completo",
                "Partida",
                $"PARTIDA_PLACAR_COMPLETO:{partida.Id}:ATLETA:{atletaRegistradorId.Value}",
                partida.GrupoId,
                partida.Id,
                null,
                usuarioRegistradorId,
                cancellationToken);
        }
    }

    public Task PontuarConfirmacaoAprovacaoPartidaAsync(
        Guid partidaId,
        Guid atletaId,
        Guid pendenciaId,
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        return RegistrarMovimentacaoAsync(
            atletaId,
            PontuacaoBeneficioRegras.ConfirmacaoAprovacaoPartida,
            TipoEventoPontuacaoBeneficio.ConfirmacaoAprovacaoPartida,
            "Confirmou aprovação de partida",
            "Pendencia",
            $"PARTIDA_CONFIRMACAO_APROVACAO:{partidaId}:PENDENCIA:{pendenciaId}:ATLETA:{atletaId}",
            null,
            partidaId,
            null,
            usuarioId,
            cancellationToken);
    }

    public async Task EstornarPartidaAsync(Guid partidaId, CancellationToken cancellationToken = default)
    {
        var extratos = await pontuacaoRepositorio.ListarExtratoPorPartidaAsync(partidaId, cancellationToken);
        foreach (var extrato in extratos.Where(x => x.Pontos > 0 && x.TipoEvento != TipoEventoPontuacaoBeneficio.EstornoPartida))
        {
            await RegistrarMovimentacaoAsync(
                extrato.AtletaId,
                -extrato.Pontos,
                TipoEventoPontuacaoBeneficio.EstornoPartida,
                $"Estorno: {extrato.Descricao}",
                "Partida",
                $"ESTORNO_PARTIDA:{partidaId}:EXTRATO:{extrato.Id}",
                extrato.GrupoId,
                partidaId,
                null,
                null,
                cancellationToken);
        }
    }

    public Task PontuarPendenciaResolvidaAsync(
        Guid pendenciaId,
        Guid atletaId,
        Guid? partidaId,
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        return RegistrarMovimentacaoAsync(
            atletaId,
            PontuacaoBeneficioRegras.PendenciaResolvida,
            TipoEventoPontuacaoBeneficio.PendenciaResolvida,
            "Resolveu pendência de dados",
            "Pendencia",
            $"PENDENCIA_RESOLVIDA:{pendenciaId}:ATLETA:{atletaId}",
            null,
            partidaId,
            null,
            usuarioId,
            cancellationToken);
    }

    public Task PontuarPerfilCompletoAsync(
        Atleta atleta,
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        if (!PerfilCompleto(atleta))
        {
            return Task.CompletedTask;
        }

        return RegistrarMovimentacaoAsync(
            atleta.Id,
            PontuacaoBeneficioRegras.PerfilCompleto,
            TipoEventoPontuacaoBeneficio.PerfilCompleto,
            "Completou o perfil de atleta",
            "Perfil",
            $"PERFIL_COMPLETO:ATLETA:{atleta.Id}",
            null,
            null,
            null,
            usuarioId,
            cancellationToken);
    }

    public async Task<RecalculoSaldoInicialPontuacaoResultadoDto> RecalcularSaldoInicialRetroativoAsync(
        bool dryRun,
        CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        await autorizacaoUsuarioServico.GarantirAdministradorAsync(cancellationToken);

        var calculos = await pontuacaoRepositorio.CalcularSaldosIniciaisRetroativosAsync(cancellationToken);
        var atletasComSaldoInicial = await pontuacaoRepositorio.ListarAtletasComSaldoInicialRetroativoAsync(cancellationToken);
        var calculosComStatus = calculos
            .Select(x => x with { JaPossuiaSaldoInicial = atletasComSaldoInicial.Contains(x.AtletaId) })
            .ToList();
        var candidatos = calculosComStatus
            .Where(x => x.TotalCalculado > 0 && !x.JaPossuiaSaldoInicial)
            .ToList();
        var avisos = new List<string>
        {
            dryRun
                ? "Dry run executado: nenhum saldo foi alterado."
                : "Saldo inicial retroativo aplicado apenas para atletas sem lançamento anterior."
        };

        if (!dryRun && candidatos.Count > 0)
        {
            await unidadeTrabalho.ExecutarEmTransacaoAsync(async ct =>
            {
                foreach (var candidato in candidatos)
                {
                    await RegistrarMovimentacaoAsync(
                        candidato.AtletaId,
                        candidato.TotalCalculado,
                        TipoEventoPontuacaoBeneficio.SaldoInicialRetroativo,
                        MontarDescricaoSaldoInicial(candidato),
                        "Backfill",
                        MontarChaveSaldoInicialRetroativo(candidato.AtletaId),
                        null,
                        null,
                        null,
                        usuario.Id,
                        ct,
                        salvarAoFinal: false);
                }

                await unidadeTrabalho.SalvarAlteracoesAsync(ct);
            }, cancellationToken);

            logger.LogInformation(
                "Saldo inicial retroativo Pontos QN aplicado. Atletas={Atletas} TotalPontos={TotalPontos}",
                candidatos.Count,
                candidatos.Sum(x => x.TotalCalculado));
        }

        return new RecalculoSaldoInicialPontuacaoResultadoDto(
            dryRun,
            !dryRun,
            calculosComStatus.Count,
            candidatos.Count,
            calculosComStatus.Count(x => x.JaPossuiaSaldoInicial),
            calculosComStatus.Count(x => x.PerfilCompleto),
            candidatos.Sum(x => x.TotalCalculado),
            calculosComStatus
                .OrderByDescending(x => x.TotalCalculado)
                .ThenBy(x => x.NomeAtleta)
                .Take(10)
                .ToList(),
            avisos);
    }

    private async Task<ResgateBeneficioPontuacaoDto> AtualizarStatusResgateAsync(
        Guid resgateId,
        AtualizarStatusResgateBeneficioDto dto,
        StatusResgateBeneficioPontuacao novoStatus,
        CancellationToken cancellationToken)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        await autorizacaoUsuarioServico.GarantirAdministradorAsync(cancellationToken);
        var resgate = await pontuacaoRepositorio.ObterResgatePorIdParaAtualizacaoAsync(resgateId, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Resgate não encontrado.");
        if (resgate.Status != StatusResgateBeneficioPontuacao.Solicitado)
        {
            throw new RegraNegocioException("Apenas resgates solicitados podem ser atualizados.");
        }

        resgate.Status = novoStatus;
        resgate.ObservacaoAdmin = dto.ObservacaoAdmin?.Trim();
        resgate.CodigoCupom = dto.CodigoCupom?.Trim();
        resgate.AtualizadoPorUsuarioId = usuario.Id;

        if (novoStatus == StatusResgateBeneficioPontuacao.Aprovado)
        {
            resgate.AprovadoEm = DateTime.UtcNow;
        }
        else if (novoStatus == StatusResgateBeneficioPontuacao.Rejeitado)
        {
            resgate.RejeitadoEm = DateTime.UtcNow;
            await RegistrarEstornoResgateAsync(resgate, usuario.Id, cancellationToken, salvarAoFinal: false);
        }
        else if (novoStatus == StatusResgateBeneficioPontuacao.Cancelado)
        {
            resgate.CanceladoEm = DateTime.UtcNow;
            await RegistrarEstornoResgateAsync(resgate, usuario.Id, cancellationToken, salvarAoFinal: false);
        }

        resgate.AtualizarDataModificacao();
        pontuacaoRepositorio.AtualizarResgate(resgate);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        logger.LogInformation("Status de resgate Pontos QN atualizado. ResgateId={ResgateId} Status={Status}", resgateId, novoStatus);
        return MapearResgate(resgate);
    }

    private Task RegistrarEstornoResgateAsync(
        ResgateBeneficioPontuacao resgate,
        Guid usuarioId,
        CancellationToken cancellationToken,
        bool salvarAoFinal)
    {
        return RegistrarMovimentacaoAsync(
            resgate.AtletaId,
            resgate.PontosUtilizados,
            TipoEventoPontuacaoBeneficio.EstornoResgate,
            $"Estorno de resgate: {resgate.Beneficio?.Titulo ?? "benefício"}",
            "Resgate",
            $"ESTORNO_RESGATE:{resgate.Id}:ATLETA:{resgate.AtletaId}",
            null,
            null,
            resgate.Id,
            usuarioId,
            cancellationToken,
            salvarAoFinal);
    }

    private async Task RegistrarMovimentacaoAsync(
        Guid atletaId,
        int pontos,
        TipoEventoPontuacaoBeneficio tipoEvento,
        string descricao,
        string origem,
        string chaveIdempotencia,
        Guid? grupoId,
        Guid? partidaId,
        Guid? resgateId,
        Guid? criadoPorUsuarioId,
        CancellationToken cancellationToken,
        bool salvarAoFinal = true)
    {
        if (pontos == 0 || await pontuacaoRepositorio.ExisteExtratoPorChaveAsync(chaveIdempotencia, cancellationToken))
        {
            return;
        }

        var saldo = await ObterOuCriarSaldoParaAtualizacaoAsync(atletaId, cancellationToken);
        if (saldo.SaldoAtual + pontos < 0)
        {
            throw new RegraNegocioException("Saldo insuficiente para esta movimentação.");
        }

        saldo.SaldoAtual += pontos;
        if (pontos > 0 && tipoEvento != TipoEventoPontuacaoBeneficio.EstornoResgate)
        {
            saldo.TotalAcumulado += pontos;
        }

        if (tipoEvento == TipoEventoPontuacaoBeneficio.ResgateBeneficio)
        {
            saldo.TotalResgatado += Math.Abs(pontos);
        }
        else if (tipoEvento == TipoEventoPontuacaoBeneficio.EstornoResgate)
        {
            saldo.TotalResgatado = Math.Max(0, saldo.TotalResgatado - pontos);
        }

        saldo.AtualizarDataModificacao();
        pontuacaoRepositorio.AtualizarSaldo(saldo);

        await pontuacaoRepositorio.AdicionarExtratoAsync(
            new ExtratoPontuacaoBeneficio
            {
                AtletaId = atletaId,
                GrupoId = grupoId,
                PartidaId = partidaId,
                ResgateId = resgateId,
                TipoEvento = tipoEvento,
                Pontos = pontos,
                Descricao = descricao,
                Origem = origem,
                ChaveIdempotencia = chaveIdempotencia,
                CriadoPorUsuarioId = criadoPorUsuarioId
            },
            cancellationToken);

        if (salvarAoFinal)
        {
            await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
        }
    }

    private async Task<PontuacaoBeneficioAtleta> ObterOuCriarSaldoParaAtualizacaoAsync(
        Guid atletaId,
        CancellationToken cancellationToken)
    {
        var saldo = await pontuacaoRepositorio.ObterSaldoPorAtletaParaAtualizacaoAsync(atletaId, cancellationToken);
        if (saldo is not null)
        {
            return saldo;
        }

        saldo = new PontuacaoBeneficioAtleta { AtletaId = atletaId };
        await pontuacaoRepositorio.AdicionarSaldoAsync(saldo, cancellationToken);
        return saldo;
    }

    private async Task<Guid?> ObterAtletaRegistradorAsync(
        Guid? usuarioRegistradorId,
        CancellationToken cancellationToken)
    {
        if (!usuarioRegistradorId.HasValue || usuarioRegistradorId.Value == Guid.Empty)
        {
            return null;
        }

        var usuario = await usuarioRepositorio.ObterPorIdAsync(usuarioRegistradorId.Value, cancellationToken);
        return usuario?.AtletaId;
    }

    private static PontuacaoBeneficioResumoDto MontarResumoPontuacao(
        Guid? atletaId,
        PontuacaoBeneficioAtleta? saldo)
    {
        return new PontuacaoBeneficioResumoDto(
            atletaId.HasValue,
            atletaId,
            saldo?.SaldoAtual ?? 0,
            saldo?.TotalAcumulado ?? 0,
            saldo?.TotalResgatado ?? 0);
    }

    private static NivelPontuacaoBeneficioDto MontarNivel(int totalAcumulado)
    {
        var faixa = PontuacaoBeneficioRegras.Faixas
            .Last(x => totalAcumulado >= x.PontosMinimos);
        var proxima = faixa.PontosProximaFaixa;
        if (!proxima.HasValue)
        {
            return new NivelPontuacaoBeneficioDto(faixa.Nome, faixa.PontosMinimos, null, 100, 0);
        }

        var baseFaixa = faixa.PontosMinimos;
        var progresso = totalAcumulado <= baseFaixa
            ? 0
            : (int)Math.Clamp(((decimal)(totalAcumulado - baseFaixa) / (proxima.Value - baseFaixa)) * 100, 0, 100);

        return new NivelPontuacaoBeneficioDto(
            faixa.Nome,
            faixa.PontosMinimos,
            proxima,
            progresso,
            Math.Max(0, proxima.Value - totalAcumulado));
    }

    private static ExtratoPontuacaoBeneficioDto MapearExtrato(ExtratoPontuacaoBeneficio extrato)
    {
        return new ExtratoPontuacaoBeneficioDto(
            extrato.Id,
            extrato.AtletaId,
            extrato.GrupoId,
            extrato.PartidaId,
            extrato.ResgateId,
            extrato.TipoEvento,
            NomeEvento(extrato.TipoEvento),
            extrato.Pontos,
            extrato.Descricao,
            extrato.Origem,
            extrato.DataCriacao);
    }

    private static BeneficioPontuacaoDto MapearBeneficio(BeneficioPontuacao beneficio, int saldoAtual)
    {
        return new BeneficioPontuacaoDto(
            beneficio.Id,
            beneficio.Titulo,
            beneficio.Descricao,
            beneficio.Tipo,
            NomeTipoBeneficio(beneficio.Tipo),
            beneficio.PontosNecessarios,
            beneficio.Ativo,
            beneficio.QuantidadeDisponivel,
            beneficio.ImagemUrl,
            beneficio.Ordem,
            beneficio.Destaque,
            saldoAtual >= beneficio.PontosNecessarios,
            Math.Max(0, beneficio.PontosNecessarios - saldoAtual));
    }

    private static ResgateBeneficioPontuacaoDto MapearResgate(ResgateBeneficioPontuacao resgate)
    {
        return new ResgateBeneficioPontuacaoDto(
            resgate.Id,
            resgate.AtletaId,
            resgate.BeneficioId,
            resgate.Beneficio?.Titulo ?? "Benefício",
            resgate.Beneficio?.Tipo ?? TipoBeneficioPontuacao.Outro,
            resgate.PontosUtilizados,
            resgate.Status,
            NomeStatusResgate(resgate.Status),
            resgate.CodigoCupom,
            resgate.ObservacaoAtleta,
            resgate.ObservacaoAdmin,
            resgate.SolicitadoEm,
            resgate.AprovadoEm,
            resgate.RejeitadoEm,
            resgate.CanceladoEm,
            resgate.UtilizadoEm);
    }

    private static ConquistaAtletaDto CriarConquista(
        string codigo,
        string titulo,
        string descricao,
        int progressoAtual,
        int meta)
    {
        return new ConquistaAtletaDto(
            codigo,
            titulo,
            descricao,
            progressoAtual >= meta,
            Math.Min(progressoAtual, meta),
            meta);
    }

    private static bool PartidaPodePontuar(Partida partida)
    {
        return partida is
        {
            Ativa: true,
            Status: StatusPartida.Encerrada,
            StatusAprovacao: StatusAprovacaoPartida.Aprovada,
            DuplaA: not null,
            DuplaB: not null,
            DuplaVencedoraId: not null
        };
    }

    private static IReadOnlyList<Guid> ObterAtletasPartida(Partida partida)
    {
        if (partida.DuplaA is null || partida.DuplaB is null)
        {
            return [];
        }

        return
        [
            partida.DuplaA.Atleta1Id,
            partida.DuplaA.Atleta2Id,
            partida.DuplaB.Atleta1Id,
            partida.DuplaB.Atleta2Id
        ];
    }

    private static IReadOnlyList<Guid> ObterAtletasDuplaVencedora(Partida partida)
    {
        if (partida.DuplaVencedoraId == partida.DuplaAId && partida.DuplaA is not null)
        {
            return [partida.DuplaA.Atleta1Id, partida.DuplaA.Atleta2Id];
        }

        if (partida.DuplaVencedoraId == partida.DuplaBId && partida.DuplaB is not null)
        {
            return [partida.DuplaB.Atleta1Id, partida.DuplaB.Atleta2Id];
        }

        return [];
    }

    private static (DateTime Inicio, DateTime Fim) ObterIntervaloSemanaUtc(DateTime referencia)
    {
        var data = referencia.Date;
        var diff = ((int)data.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        var inicio = DateTime.SpecifyKind(data.AddDays(-diff), DateTimeKind.Utc);
        return (inicio, inicio.AddDays(7));
    }

    private static string MontarChaveCompartilhamento(
        RegistrarCompartilhamentoGamificacaoDto dto,
        Guid atletaId,
        DateTime dataUtc)
    {
        var data = dataUtc.ToString("yyyyMMdd");
        return dto.Tipo switch
        {
            TipoCompartilhamentoGamificacao.Partida => $"COMPARTILHAMENTO_PARTIDA:{dto.PartidaId}:ATLETA:{atletaId}:{data}",
            TipoCompartilhamentoGamificacao.Ranking => $"COMPARTILHAMENTO_RANKING:{dto.GrupoId?.ToString() ?? "GERAL"}:ATLETA:{atletaId}:{data}",
            TipoCompartilhamentoGamificacao.ScoutAtleta => $"COMPARTILHAMENTO_SCOUT:{dto.AtletaId?.ToString() ?? atletaId.ToString()}:ATLETA:{atletaId}:{data}",
            TipoCompartilhamentoGamificacao.ScoutDupla => $"COMPARTILHAMENTO_SCOUT_DUPLA:{dto.DuplaId?.ToString() ?? "GERAL"}:ATLETA:{atletaId}:{data}",
            _ => $"COMPARTILHAMENTO:{dto.Tipo}:ATLETA:{atletaId}:{data}"
        };
    }

    private static string MontarDescricaoCompartilhamento(TipoCompartilhamentoGamificacao tipo)
    {
        return tipo switch
        {
            TipoCompartilhamentoGamificacao.Partida => "Compartilhou resultado de partida",
            TipoCompartilhamentoGamificacao.Ranking => "Compartilhou ranking",
            TipoCompartilhamentoGamificacao.ScoutAtleta => "Compartilhou scout de atleta",
            TipoCompartilhamentoGamificacao.ScoutDupla => "Compartilhou scout de dupla",
            _ => "Compartilhou conteúdo"
        };
    }

    private static string MontarChaveSaldoInicialRetroativo(Guid atletaId)
        => $"SALDO_INICIAL_RETROATIVO:ATLETA:{atletaId}";

    private static string MontarDescricaoSaldoInicial(SaldoInicialRetroativoAtletaDto saldo)
    {
        return "Saldo inicial calculado pelo historico da plataforma: " +
            $"{saldo.PartidasParticipadas} partidas, " +
            $"{saldo.PartidasRegistradas} registros, " +
            $"{saldo.PartidasComPlacar} placares, " +
            $"{saldo.Vitorias} vitorias, " +
            $"{saldo.Grupos} grupos, " +
            $"{saldo.PendenciasResolvidas} pendencias, " +
            $"perfil completo: {(saldo.PerfilCompleto ? "sim" : "nao")}.";
    }

    private static bool PerfilCompleto(Atleta atleta)
    {
        return !string.IsNullOrWhiteSpace(atleta.Nome) &&
            !string.IsNullOrWhiteSpace(atleta.Email) &&
            atleta.Nivel.HasValue &&
            atleta.Sexo.HasValue &&
            atleta.DataNascimento.HasValue;
    }

    private static string NomeEvento(TipoEventoPontuacaoBeneficio tipo)
    {
        return tipo switch
        {
            TipoEventoPontuacaoBeneficio.PerfilCompleto => "Perfil completo",
            TipoEventoPontuacaoBeneficio.PartidaParticipante => "Participação em partida",
            TipoEventoPontuacaoBeneficio.PartidaRegistrador => "Registro de partida",
            TipoEventoPontuacaoBeneficio.PartidaPlacarCompleto => "Placar completo",
            TipoEventoPontuacaoBeneficio.PartidaVitoria => "Vitória em partida",
            TipoEventoPontuacaoBeneficio.ConfirmacaoAprovacaoPartida => "Confirmação de partida",
            TipoEventoPontuacaoBeneficio.EntradaGrupo => "Entrada em grupo",
            TipoEventoPontuacaoBeneficio.CompartilhamentoPartida => "Compartilhamento de partida",
            TipoEventoPontuacaoBeneficio.CompartilhamentoRanking => "Compartilhamento de ranking",
            TipoEventoPontuacaoBeneficio.CompartilhamentoScoutAtleta => "Compartilhamento de scout",
            TipoEventoPontuacaoBeneficio.CompartilhamentoScoutDupla => "Compartilhamento de scout de dupla",
            TipoEventoPontuacaoBeneficio.PendenciaResolvida => "Pendência resolvida",
            TipoEventoPontuacaoBeneficio.SequenciaSemanal => "Sequência semanal",
            TipoEventoPontuacaoBeneficio.ConviteAtletaCadastro => "Convite com cadastro",
            TipoEventoPontuacaoBeneficio.ConviteAtletaPrimeiraPartida => "Convite convertido",
            TipoEventoPontuacaoBeneficio.ResgateBeneficio => "Resgate de benefício",
            TipoEventoPontuacaoBeneficio.SaldoInicialRetroativo => "Saldo inicial retroativo",
            TipoEventoPontuacaoBeneficio.EstornoPartida => "Estorno de partida",
            TipoEventoPontuacaoBeneficio.EstornoResgate => "Estorno de resgate",
            _ => tipo.ToString()
        };
    }

    private static string NomeTipoBeneficio(TipoBeneficioPontuacao tipo)
    {
        return tipo switch
        {
            TipoBeneficioPontuacao.DescontoLoja => "Desconto na loja",
            TipoBeneficioPontuacao.Brinde => "Brinde",
            TipoBeneficioPontuacao.Experiencia => "Experiência",
            TipoBeneficioPontuacao.Produto => "Produto",
            _ => "Outro"
        };
    }

    private static string NomeStatusResgate(StatusResgateBeneficioPontuacao status)
    {
        return status switch
        {
            StatusResgateBeneficioPontuacao.Solicitado => "Solicitado",
            StatusResgateBeneficioPontuacao.Aprovado => "Aprovado",
            StatusResgateBeneficioPontuacao.Rejeitado => "Rejeitado",
            StatusResgateBeneficioPontuacao.Cancelado => "Cancelado",
            StatusResgateBeneficioPontuacao.Utilizado => "Utilizado",
            _ => status.ToString()
        };
    }
}
