using System.Globalization;
using Microsoft.Extensions.Logging;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Interfaces.Seguranca;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Aplicacao.Utilitarios;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Aplicacao.Servicos;

public class DashboardAtletaServico(
    IAtletaRepositorio atletaRepositorio,
    IPartidaRepositorio partidaRepositorio,
    IRankingServico rankingServico,
    IAutorizacaoUsuarioServico autorizacaoUsuarioServico,
    ILogger<DashboardAtletaServico> logger
) : IDashboardAtletaServico
{
    private const int QuantidadeMesesEvolucao = 6;
    private const int QuantidadeDiasHeatmap = 112;
    private const int QuantidadeUltimasPartidas = 5;
    private const int QuantidadeRelacoes = 8;
    private const int QuantidadeFormaRecente = 5;
    private const int MinimoJogosDestaqueRelacao = 3;

    private sealed record DashboardAtletaContexto(
        Guid UsuarioId,
        Atleta Atleta,
        IReadOnlyList<Partida> PartidasValidas);

    public async Task<DashboardAtletaDto> ObterDashboardAsync(CancellationToken cancellationToken = default)
    {
        var contexto = await CarregarContextoAsync(cancellationToken);
        var atleta = contexto.Atleta;
        var partidasValidas = contexto.PartidasValidas;

        var resumo = MontarResumo(atleta.Id, partidasValidas);
        var sequencia = CalcularSequencia(atleta.Id, partidasValidas);
        var estatisticasPontos = MontarEstatisticasPontos(atleta.Id, partidasValidas);
        var posicaoRanking = await ObterPosicaoRankingAsync(atleta.Id, cancellationToken);
        var parceiros = MontarRelacoes(atleta.Id, partidasValidas, obterParceiros: true);
        var rivais = MontarRelacoes(atleta.Id, partidasValidas, obterParceiros: false);
        var parceirosRecentes = MontarRelacoesRecentes(parceiros);
        var rivaisRecentes = MontarRelacoesRecentes(rivais);
        var desempenhoPorGrupo = await MontarDesempenhoPorGrupoAsync(atleta.Id, partidasValidas, cancellationToken);
        var melhorParceiro = parceiros.FirstOrDefault();
        var rivalMaisFrequente = rivais.FirstOrDefault();
        var resumoComNomes = resumo with
        {
            MelhorParceiro = melhorParceiro is null ? null : ObterNomeExibicao(melhorParceiro.Nome, melhorParceiro.Apelido),
            RivalMaisFrequente = rivalMaisFrequente is null ? null : ObterNomeExibicao(rivalMaisFrequente.Nome, rivalMaisFrequente.Apelido)
        };

        return new DashboardAtletaDto(
            new DashboardAtletaPerfilDto(
                atleta.Id,
                atleta.Nome,
                atleta.Apelido,
                atleta.Nivel?.ToString() ?? "Geral",
                posicaoRanking,
                resumoComNomes.Aproveitamento,
                resumoComNomes.SequenciaAtual,
                MontarTextoSequencia(resumoComNomes.SequenciaAtual),
                FotoPerfilAtletaUtil.ObterUrlPublica(atleta)),
            resumoComNomes,
            MontarMetricas(resumoComNomes),
            MontarEvolucao(atleta.Id, partidasValidas),
            partidasValidas.Take(QuantidadeUltimasPartidas).Select(x => MontarPartidaRecente(atleta.Id, x)).ToList(),
            parceiros.Take(QuantidadeRelacoes).ToList(),
            rivais.Take(QuantidadeRelacoes).ToList(),
            parceirosRecentes.Take(QuantidadeRelacoes).ToList(),
            rivaisRecentes.Take(QuantidadeRelacoes).ToList(),
            MontarHeatmap(partidasValidas),
            MontarInsights(resumoComNomes, melhorParceiro, rivais.FirstOrDefault(), partidasValidas),
            sequencia,
            estatisticasPontos,
            MontarFormaRecente(atleta.Id, partidasValidas),
            desempenhoPorGrupo,
            MontarDuplasDisponiveis(parceiros));
    }

    public async Task<DashboardAtletaPerfilDto> ObterPerfilAsync(CancellationToken cancellationToken = default)
    {
        var contexto = await CarregarContextoAsync(cancellationToken);
        var resumo = MontarResumo(contexto.Atleta.Id, contexto.PartidasValidas);
        var posicaoRanking = await ObterPosicaoRankingAsync(contexto.Atleta.Id, cancellationToken);

        return new DashboardAtletaPerfilDto(
            contexto.Atleta.Id,
            contexto.Atleta.Nome,
            contexto.Atleta.Apelido,
            contexto.Atleta.Nivel?.ToString() ?? "Geral",
            posicaoRanking,
            resumo.Aproveitamento,
            resumo.SequenciaAtual,
            resumo.TextoSequenciaAtual ?? MontarTextoSequencia("vitoria", resumo.SequenciaAtual),
            FotoPerfilAtletaUtil.ObterUrlPublica(contexto.Atleta));
    }

    public async Task<DashboardAtletaResumoDto> ObterResumoAsync(CancellationToken cancellationToken = default)
    {
        Guid? usuarioIdLog = null;
        Guid? atletaIdLog = null;

        try
        {
            var contexto = await CarregarContextoAsync(cancellationToken, "/api/dashboard/atleta/resumo");
            usuarioIdLog = contexto.UsuarioId;
            atletaIdLog = contexto.Atleta.Id;
            var resumo = MontarResumo(contexto.Atleta.Id, contexto.PartidasValidas);
            var parceiros = MontarRelacoes(contexto.Atleta.Id, contexto.PartidasValidas, obterParceiros: true);
            var rivais = MontarRelacoes(contexto.Atleta.Id, contexto.PartidasValidas, obterParceiros: false);
            var melhorParceiro = parceiros.FirstOrDefault();
            var rivalMaisFrequente = rivais.FirstOrDefault();

            return resumo with
            {
                MelhorParceiro = melhorParceiro is null ? null : ObterNomeExibicao(melhorParceiro.Nome, melhorParceiro.Apelido),
                RivalMaisFrequente = rivalMaisFrequente is null ? null : ObterNomeExibicao(rivalMaisFrequente.Nome, rivalMaisFrequente.Apelido)
            };
        }
        catch (Exception ex) when (ex is not RegraNegocioException and not EntidadeNaoEncontradaException)
        {
            logger.LogError(
                ex,
                "Erro ao montar resumo do dashboard do atleta. Endpoint: {Endpoint}. UsuarioId: {UsuarioId}. AtletaId: {AtletaId}.",
                "/api/dashboard/atleta/resumo",
                usuarioIdLog,
                atletaIdLog);
            throw;
        }
    }

    public async Task<IReadOnlyList<string>> ObterInsightsAsync(CancellationToken cancellationToken = default)
    {
        var contexto = await CarregarContextoAsync(cancellationToken);
        var resumo = MontarResumo(contexto.Atleta.Id, contexto.PartidasValidas);
        var parceiros = MontarRelacoes(contexto.Atleta.Id, contexto.PartidasValidas, obterParceiros: true);
        var rivais = MontarRelacoes(contexto.Atleta.Id, contexto.PartidasValidas, obterParceiros: false);
        var resumoComNomes = resumo with
        {
            MelhorParceiro = parceiros.FirstOrDefault() is { } melhorParceiro
                ? ObterNomeExibicao(melhorParceiro.Nome, melhorParceiro.Apelido)
                : null,
            RivalMaisFrequente = rivais.FirstOrDefault() is { } rivalMaisFrequente
                ? ObterNomeExibicao(rivalMaisFrequente.Nome, rivalMaisFrequente.Apelido)
                : null
        };

        return MontarInsights(resumoComNomes, parceiros.FirstOrDefault(), rivais.FirstOrDefault(), contexto.PartidasValidas);
    }

    public async Task<IReadOnlyList<DashboardAtletaPartidaDto>> ListarUltimasPartidasAsync(CancellationToken cancellationToken = default)
    {
        var contexto = await CarregarContextoAsync(cancellationToken);
        return contexto.PartidasValidas
            .Take(QuantidadeUltimasPartidas)
            .Select(x => MontarPartidaRecente(contexto.Atleta.Id, x))
            .ToList();
    }

    public async Task<DashboardAtletaJogosDto> ListarJogosAsync(
        int pagina = 1,
        int tamanhoPagina = 20,
        string? resultado = null,
        string? tipoRegistro = null,
        Guid? grupoId = null,
        string? periodo = null,
        CancellationToken cancellationToken = default)
    {
        var contexto = await CarregarContextoAsync(cancellationToken);
        var paginaNormalizada = Math.Max(1, pagina);
        var tamanhoNormalizado = Math.Clamp(tamanhoPagina, 1, 50);
        var partidasFiltradas = AplicarFiltrosJogos(
                contexto.Atleta.Id,
                contexto.PartidasValidas,
                resultado,
                tipoRegistro,
                grupoId,
                periodo)
            .ToList();
        var total = partidasFiltradas.Count;
        var itens = partidasFiltradas
            .Skip((paginaNormalizada - 1) * tamanhoNormalizado)
            .Take(tamanhoNormalizado)
            .Select(x => MontarPartidaRecente(contexto.Atleta.Id, x))
            .ToList();

        return new DashboardAtletaJogosDto(
            itens,
            total,
            paginaNormalizada,
            tamanhoNormalizado,
            paginaNormalizada * tamanhoNormalizado < total);
    }

    public async Task<DashboardAtletaConexoesDto> ObterConexoesAsync(CancellationToken cancellationToken = default)
    {
        var contexto = await CarregarContextoAsync(cancellationToken);
        var parceiros = MontarRelacoes(contexto.Atleta.Id, contexto.PartidasValidas, obterParceiros: true);
        var rivais = MontarRelacoes(contexto.Atleta.Id, contexto.PartidasValidas, obterParceiros: false);

        return new DashboardAtletaConexoesDto(
            parceiros.Take(QuantidadeRelacoes).ToList(),
            rivais.Take(QuantidadeRelacoes).ToList(),
            MontarRelacoesRecentes(parceiros).Take(QuantidadeRelacoes).ToList(),
            MontarRelacoesRecentes(rivais).Take(QuantidadeRelacoes).ToList(),
            parceiros.FirstOrDefault(),
            parceiros.OrderByDescending(x => x.Vitorias).ThenByDescending(x => x.Partidas).FirstOrDefault(),
            parceiros.Where(x => x.Partidas >= MinimoJogosDestaqueRelacao)
                .OrderByDescending(x => x.Aproveitamento)
                .ThenByDescending(x => x.Partidas)
                .FirstOrDefault(),
            rivais.FirstOrDefault(),
            rivais.OrderByDescending(x => x.Vitorias).ThenByDescending(x => x.Partidas).FirstOrDefault(),
            rivais.Where(x => x.Partidas >= MinimoJogosDestaqueRelacao)
                .OrderBy(x => x.Aproveitamento)
                .ThenByDescending(x => x.Partidas)
                .FirstOrDefault());
    }

    public async Task<IReadOnlyList<DashboardAtletaGrupoDto>> ObterDesempenhoPorGrupoAsync(CancellationToken cancellationToken = default)
    {
        var contexto = await CarregarContextoAsync(cancellationToken);
        return await MontarDesempenhoPorGrupoAsync(contexto.Atleta.Id, contexto.PartidasValidas, cancellationToken);
    }

    public async Task<IReadOnlyList<DashboardDuplaParceiroDto>> ListarDuplasDisponiveisAsync(CancellationToken cancellationToken = default)
    {
        var contexto = await CarregarContextoAsync(cancellationToken);
        var parceiros = MontarRelacoes(contexto.Atleta.Id, contexto.PartidasValidas, obterParceiros: true);
        return MontarDuplasDisponiveis(parceiros);
    }

    public async Task<IReadOnlyList<DashboardAtletaHeatmapDiaDto>> ObterFrequenciaAsync(CancellationToken cancellationToken = default)
    {
        var contexto = await CarregarContextoAsync(cancellationToken);
        return MontarHeatmap(contexto.PartidasValidas);
    }

    private async Task<DashboardAtletaContexto> CarregarContextoAsync(
        CancellationToken cancellationToken,
        string? endpoint = null)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        if (!usuario.AtletaId.HasValue)
        {
            logger.LogWarning(
                "Dashboard do atleta solicitado por usuário sem atleta vinculado. Endpoint: {Endpoint}. UsuarioId: {UsuarioId}.",
                endpoint ?? "dashboard-atleta",
                usuario.Id);
            throw new RegraNegocioException("Seu usuário precisa estar vinculado a um atleta para visualizar o dashboard.");
        }

        var atleta = await atletaRepositorio.ObterPorIdAsync(usuario.AtletaId.Value, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Atleta não encontrado.");
        var partidas = await partidaRepositorio.ListarPorAtletaAsync(atleta.Id, cancellationToken);
        var partidasValidas = partidas
            .Where(partida => PartidaContaParaDashboard(atleta.Id, partida, endpoint))
            .OrderByDescending(ObterDataReferencia)
            .ThenByDescending(x => x.DataCriacao)
            .ToList();

        return new DashboardAtletaContexto(usuario.Id, atleta, partidasValidas);
    }

    private bool PartidaContaParaDashboard(Guid atletaId, Partida partida, string? endpoint)
    {
        var partidaTemDadosBasicos = partida.EsportivamenteValida() &&
            partida.DuplaVencedoraId.HasValue &&
            partida.DuplaA is not null &&
            partida.DuplaB is not null;

        if (!partidaTemDadosBasicos)
        {
            return false;
        }

        var duplaAtleta = ObterDuplaDoAtleta(atletaId, partida);
        if (duplaAtleta is null)
        {
            logger.LogWarning(
                "Partida ignorada no dashboard porque o atleta não foi encontrado nas duplas carregadas. Endpoint: {Endpoint}. AtletaId: {AtletaId}. PartidaId: {PartidaId}.",
                endpoint ?? "dashboard-atleta",
                atletaId,
                partida.Id);
            return false;
        }

        var duplaAdversaria = ReferenceEquals(duplaAtleta, partida.DuplaA) ? partida.DuplaB : partida.DuplaA;
        if (ObterAtletas(duplaAtleta).Count != 2 || duplaAdversaria is null || ObterAtletas(duplaAdversaria).Count != 2)
        {
            logger.LogWarning(
                "Partida ignorada no dashboard por dados incompletos de atletas nas duplas. Endpoint: {Endpoint}. AtletaId: {AtletaId}. PartidaId: {PartidaId}.",
                endpoint ?? "dashboard-atleta",
                atletaId,
                partida.Id);
            return false;
        }

        return true;
    }

    private static DashboardAtletaResumoDto MontarResumo(Guid atletaId, IReadOnlyList<Partida> partidas)
    {
        var total = partidas.Count;
        var vitorias = partidas.Count(x => AtletaVenceu(atletaId, x));
        var derrotas = total - vitorias;
        var aproveitamento = total == 0 ? 0 : decimal.Round(vitorias * 100m / total, 1);
        var estatisticasPontos = MontarEstatisticasPontos(atletaId, partidas);
        var sequencia = CalcularSequencia(atletaId, partidas);

        return new DashboardAtletaResumoDto(
            total,
            vitorias,
            derrotas,
            aproveitamento,
            estatisticasPontos.Saldo ?? 0,
            sequencia.Quantidade,
            null,
            null,
            estatisticasPontos.PontosPro ?? 0,
            estatisticasPontos.PontosContra ?? 0,
            estatisticasPontos.PartidasComPlacar,
            sequencia.MelhorSequenciaVitorias,
            sequencia.Tipo,
            sequencia.Texto);
    }

    private static IReadOnlyList<DashboardAtletaMetricaDto> MontarMetricas(DashboardAtletaResumoDto resumo)
    {
        return
        [
            new("partidas", "Partidas", resumo.TotalPartidas.ToString(CultureInfo.InvariantCulture), null, "jogos"),
            new("vitorias", "Vitórias", resumo.Vitorias.ToString(CultureInfo.InvariantCulture), null, "vitorias", true),
            new("derrotas", "Derrotas", resumo.Derrotas.ToString(CultureInfo.InvariantCulture), null, "derrotas"),
            new("aproveitamento", "Aproveitamento", $"{resumo.Aproveitamento:0.#}%", null, "aproveitamento", true),
            new("saldo", "Saldo de pontos", resumo.PartidasComPlacar > 0 ? resumo.SaldoPontos.ToString("+0;-0;0", CultureInfo.InvariantCulture) : "-", resumo.PartidasComPlacar > 0 ? null : "sem placar", "saldo"),
            new("sequencia", "Sequência", resumo.SequenciaAtual.ToString(CultureInfo.InvariantCulture), resumo.TipoSequenciaAtual == "derrota" ? "derrotas" : "vitórias", "sequencia"),
            new("parceiro", "Melhor parceiro", resumo.MelhorParceiro ?? "-", null, "parceiro"),
            new("rival", "Rival frequente", resumo.RivalMaisFrequente ?? "-", null, "rival")
        ];
    }

    private static IReadOnlyList<DashboardAtletaEvolucaoDto> MontarEvolucao(Guid atletaId, IReadOnlyList<Partida> partidas)
    {
        var hoje = DateTime.UtcNow;
        var inicio = new DateTime(hoje.Year, hoje.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-(QuantidadeMesesEvolucao - 1));
        var cultura = CultureInfo.GetCultureInfo("pt-BR");

        return Enumerable.Range(0, QuantidadeMesesEvolucao)
            .Select(indice => inicio.AddMonths(indice))
            .Select(mes =>
            {
                var partidasMes = partidas
                    .Where(x => ObterDataReferencia(x).Year == mes.Year && ObterDataReferencia(x).Month == mes.Month)
                    .ToList();
                var total = partidasMes.Count;
                var vitorias = partidasMes.Count(x => AtletaVenceu(atletaId, x));
                var aproveitamento = total == 0 ? 0 : decimal.Round(vitorias * 100m / total, 1);
                decimal? aproveitamentoDados = total == 0 ? null : aproveitamento;

                return new DashboardAtletaEvolucaoDto(
                    cultura.DateTimeFormat.GetAbbreviatedMonthName(mes.Month).TrimEnd('.'),
                    mes.Year,
                    mes.Month,
                    total,
                    vitorias,
                    aproveitamento,
                    null,
                    aproveitamentoDados,
                    total > 0);
            })
            .ToList();
    }

    private static DashboardAtletaPartidaDto MontarPartidaRecente(Guid atletaId, Partida partida)
    {
        var duplaAtleta = ObterDuplaDoAtleta(atletaId, partida);
        var duplaAdversaria = ReferenceEquals(duplaAtleta, partida.DuplaA) ? partida.DuplaB! : partida.DuplaA!;
        var parceiro = ObterAtletas(duplaAtleta!)
            .Where(x => x.Id != atletaId)
            .Select(x => ObterNomeExibicao(x.Nome, x.Apelido))
            .FirstOrDefault() ?? "Parceiro";
        var adversarios = string.Join(" e ", ObterAtletas(duplaAdversaria).Select(x => ObterNomeExibicao(x.Nome, x.Apelido)));

        return new DashboardAtletaPartidaDto(
            partida.Id,
            partida.CriadoPorUsuarioId,
            AtletaVenceu(atletaId, partida) ? "Vitória" : "Derrota",
            partida.DataPartida,
            partida.GrupoId,
            partida.CategoriaCompeticaoId,
            partida.Grupo?.Nome,
            partida.CategoriaCompeticao?.Nome,
            partida.CategoriaCompeticao?.Competicao?.Nome,
            partida.Status.ToString(),
            (int)partida.StatusAprovacao,
            partida.DuplaAId,
            partida.DuplaA?.Atleta1Id,
            partida.DuplaA?.Atleta1 is null ? null : ObterNomeExibicao(partida.DuplaA.Atleta1.Nome, partida.DuplaA.Atleta1.Apelido),
            partida.DuplaA?.Atleta2Id,
            partida.DuplaA?.Atleta2 is null ? null : ObterNomeExibicao(partida.DuplaA.Atleta2.Nome, partida.DuplaA.Atleta2.Apelido),
            partida.DuplaBId,
            partida.DuplaB?.Atleta1Id,
            partida.DuplaB?.Atleta1 is null ? null : ObterNomeExibicao(partida.DuplaB.Atleta1.Nome, partida.DuplaB.Atleta1.Apelido),
            partida.DuplaB?.Atleta2Id,
            partida.DuplaB?.Atleta2 is null ? null : ObterNomeExibicao(partida.DuplaB.Atleta2.Nome, partida.DuplaB.Atleta2.Apelido),
            parceiro,
            adversarios,
            ObterPontosAtleta(atletaId, partida),
            ObterPontosAdversario(atletaId, partida),
            partida.PossuiPlacarDetalhado(),
            partida.TipoRegistroResultado.ToString(),
            partida.Grupo?.Nome ?? "Partidas avulsas",
            ObterAtletas(duplaAtleta!).Select(ParaAtletaDto).ToList(),
            ObterAtletas(duplaAdversaria).Select(ParaAtletaDto).ToList());
    }

    private static IReadOnlyList<DashboardAtletaRelacaoDto> MontarRelacoes(
        Guid atletaId,
        IReadOnlyList<Partida> partidas,
        bool obterParceiros)
    {
        return partidas
            .SelectMany(partida => ObterAtletasRelacao(atletaId, partida, obterParceiros)
                .Select(atleta => new { Atleta = atleta, Partida = partida, Vitoria = AtletaVenceu(atletaId, partida) }))
            .GroupBy(x => x.Atleta.Id)
            .Select(grupo =>
            {
                var primeiro = grupo.First().Atleta;
                var partidasRelacao = grupo.Select(x => x.Partida)
                    .OrderByDescending(ObterDataReferencia)
                    .ThenByDescending(x => x.DataCriacao)
                    .ToList();
                var partidasJuntos = grupo.Count();
                var vitorias = grupo.Count(x => x.Vitoria);
                var derrotas = partidasJuntos - vitorias;
                var aproveitamento = partidasJuntos == 0 ? 0 : decimal.Round(vitorias * 100m / partidasJuntos, 1);
                var ultimaPartida = grupo.Max(x => ObterDataReferencia(x.Partida));
                var sequencia = CalcularSequencia(atletaId, partidasRelacao);
                var estatisticasPontos = MontarEstatisticasPontos(atletaId, partidasRelacao);

                return new DashboardAtletaRelacaoDto(
                    primeiro.Id,
                    primeiro.Nome,
                    primeiro.Apelido,
                    partidasJuntos,
                    vitorias,
                    derrotas,
                    aproveitamento,
                    ultimaPartida,
                    FotoPerfilAtletaUtil.ObterUrlPublica(primeiro),
                    sequencia.Tipo,
                    sequencia.Quantidade,
                    estatisticasPontos.PontosPro ?? 0,
                    estatisticasPontos.PontosContra ?? 0,
                    estatisticasPontos.Saldo ?? 0,
                    estatisticasPontos.PartidasComPlacar);
            })
            .OrderByDescending(x => x.Partidas)
            .ThenByDescending(x => x.Aproveitamento)
            .ThenBy(x => ObterNomeExibicao(x.Nome, x.Apelido))
            .ToList();
    }

    private static IReadOnlyList<DashboardAtletaRelacaoDto> MontarRelacoesRecentes(
        IReadOnlyList<DashboardAtletaRelacaoDto> relacoes)
    {
        return relacoes
            .Where(x => x.UltimaPartida.HasValue)
            .OrderByDescending(x => x.UltimaPartida)
            .ThenByDescending(x => x.Partidas)
            .ThenBy(x => ObterNomeExibicao(x.Nome, x.Apelido))
            .ToList();
    }

    private static IReadOnlyList<DashboardAtletaHeatmapDiaDto> MontarHeatmap(IReadOnlyList<Partida> partidas)
    {
        var inicio = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-(QuantidadeDiasHeatmap - 1)));
        var grupos = partidas
            .GroupBy(x => DateOnly.FromDateTime(ObterDataReferencia(x).Date))
            .ToDictionary(x => x.Key, x => x.Count());

        return Enumerable.Range(0, QuantidadeDiasHeatmap)
            .Select(offset =>
            {
                var data = inicio.AddDays(offset);
                return new DashboardAtletaHeatmapDiaDto(data, grupos.GetValueOrDefault(data));
            })
            .ToList();
    }

    private static IReadOnlyList<string> MontarInsights(
        DashboardAtletaResumoDto resumo,
        DashboardAtletaRelacaoDto? melhorParceiro,
        DashboardAtletaRelacaoDto? rivalMaisFrequente,
        IReadOnlyList<Partida> partidas)
    {
        var ultimas10 = partidas.Take(10).ToList();
        var insights = new List<string>();

        if (resumo.SequenciaAtual > 1)
        {
            insights.Add($"Você está em uma sequência de {resumo.SequenciaAtual} vitórias.");
        }

        if (ultimas10.Count > 0)
        {
            insights.Add($"Você venceu {resumo.Vitorias} de {resumo.TotalPartidas} partidas no histórico válido.");
        }

        if (melhorParceiro is not null)
        {
            insights.Add($"Seu parceiro mais frequente é {ObterNomeExibicao(melhorParceiro.Nome, melhorParceiro.Apelido)}.");
        }

        if (rivalMaisFrequente is not null)
        {
            insights.Add($"Você enfrentou {ObterNomeExibicao(rivalMaisFrequente.Nome, rivalMaisFrequente.Apelido)} em {rivalMaisFrequente.Partidas} jogos.");
        }

        if (insights.Count == 0)
        {
            insights.Add("Registre mais partidas para acompanhar sua evolução.");
        }

        return insights.Take(4).ToList();
    }

    private async Task<int?> ObterPosicaoRankingAsync(Guid atletaId, CancellationToken cancellationToken)
    {
        var rankings = await rankingServico.ListarAtletasGeralAsync(cancellationToken);
        return rankings
            .SelectMany(x => x.Atletas)
            .FirstOrDefault(x => x.AtletaId == atletaId)
            ?.Posicao;
    }

    private static IEnumerable<Partida> AplicarFiltrosJogos(
        Guid atletaId,
        IReadOnlyList<Partida> partidas,
        string? resultado,
        string? tipoRegistro,
        Guid? grupoId,
        string? periodo)
    {
        var consulta = partidas.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(resultado))
        {
            var resultadoNormalizado = resultado.Trim().ToLowerInvariant();
            consulta = resultadoNormalizado switch
            {
                "vitorias" or "vitórias" or "vitoria" or "vitória" => consulta.Where(x => AtletaVenceu(atletaId, x)),
                "derrotas" or "derrota" => consulta.Where(x => !AtletaVenceu(atletaId, x)),
                _ => consulta
            };
        }

        if (!string.IsNullOrWhiteSpace(tipoRegistro))
        {
            var tipoNormalizado = tipoRegistro.Trim().ToLowerInvariant();
            consulta = tipoNormalizado switch
            {
                "com-placar" or "placar" => consulta.Where(x => x.PossuiPlacarDetalhado()),
                "apenas-vencedor" or "sem-placar" => consulta.Where(x => !x.PossuiPlacarDetalhado()),
                _ => consulta
            };
        }

        if (grupoId.HasValue)
        {
            consulta = consulta.Where(x => x.GrupoId == grupoId.Value);
        }

        if (!string.IsNullOrWhiteSpace(periodo))
        {
            var hoje = DateTime.UtcNow.Date;
            var inicio = periodo.Trim().ToLowerInvariant() switch
            {
                "30d" or "30" => hoje.AddDays(-30),
                "90d" or "90" => hoje.AddDays(-90),
                "ano" or "ano-atual" => new DateTime(hoje.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                _ => (DateTime?)null
            };

            if (inicio.HasValue)
            {
                consulta = consulta.Where(x => ObterDataReferencia(x).Date >= inicio.Value);
            }
        }

        return consulta;
    }

    private async Task<IReadOnlyList<DashboardAtletaGrupoDto>> MontarDesempenhoPorGrupoAsync(
        Guid atletaId,
        IReadOnlyList<Partida> partidas,
        CancellationToken cancellationToken)
    {
        var gruposRanking = new Dictionary<Guid, RankingAtletaDto?>();

        async Task<RankingAtletaDto?> ObterRankingAtletaGrupoAsync(Guid grupoId)
        {
            if (!gruposRanking.TryGetValue(grupoId, out var rankingAtleta))
            {
                var ranking = await rankingServico.ListarAtletasPorGrupoAsync(grupoId, cancellationToken);
                rankingAtleta = ranking
                    .SelectMany(x => x.Atletas)
                    .FirstOrDefault(x => x.AtletaId == atletaId);
                gruposRanking[grupoId] = rankingAtleta;
            }

            return rankingAtleta;
        }

        var grupos = new List<DashboardAtletaGrupoDto>();
        foreach (var grupo in partidas.GroupBy(x => x.GrupoId))
        {
            var partidasGrupo = grupo
                .OrderByDescending(ObterDataReferencia)
                .ThenByDescending(x => x.DataCriacao)
                .ToList();
            var jogos = partidasGrupo.Count;
            var vitorias = partidasGrupo.Count(x => AtletaVenceu(atletaId, x));
            var derrotas = jogos - vitorias;
            var aproveitamento = jogos == 0 ? 0 : decimal.Round(vitorias * 100m / jogos, 1);
            var sequencia = CalcularSequencia(atletaId, partidasGrupo);
            RankingAtletaDto? rankingAtleta = null;

            if (grupo.Key.HasValue)
            {
                rankingAtleta = await ObterRankingAtletaGrupoAsync(grupo.Key.Value);
            }

            grupos.Add(new DashboardAtletaGrupoDto(
                grupo.Key,
                grupo.Key.HasValue ? partidasGrupo.FirstOrDefault()?.Grupo?.Nome ?? "Grupo" : "Partidas avulsas",
                !grupo.Key.HasValue,
                jogos,
                vitorias,
                derrotas,
                aproveitamento,
                sequencia.Tipo,
                sequencia.Quantidade,
                rankingAtleta?.Posicao,
                rankingAtleta?.Pontos,
                MontarEstatisticasPontos(atletaId, partidasGrupo)));
        }

        return grupos
            .OrderBy(x => x.PartidasAvulsas)
            .ThenByDescending(x => x.Jogos)
            .ThenBy(x => x.Nome)
            .ToList();
    }

    private static IReadOnlyList<DashboardDuplaParceiroDto> MontarDuplasDisponiveis(IReadOnlyList<DashboardAtletaRelacaoDto> parceiros)
    {
        return parceiros
            .Select(parceiro => new DashboardDuplaParceiroDto(
                parceiro.AtletaId,
                parceiro.Nome,
                parceiro.Apelido,
                parceiro.FotoPerfilUrl,
                parceiro.Partidas,
                parceiro.Vitorias,
                parceiro.Derrotas,
                parceiro.Aproveitamento,
                parceiro.UltimaPartida))
            .ToList();
    }

    private static IReadOnlyList<DashboardScoutResultadoRecenteDto> MontarFormaRecente(Guid atletaId, IReadOnlyList<Partida> partidas)
    {
        return partidas
            .Take(QuantidadeFormaRecente)
            .Select(partida => new DashboardScoutResultadoRecenteDto(
                partida.Id,
                AtletaVenceu(atletaId, partida) ? "V" : "D",
                partida.DataPartida))
            .ToList();
    }

    private static DashboardScoutSequenciaDto CalcularSequencia(Guid atletaId, IReadOnlyList<Partida> partidas)
    {
        if (partidas.Count == 0)
        {
            return new DashboardScoutSequenciaDto("sem-jogos", 0, "Sem partidas", 0);
        }

        var primeiraEhVitoria = AtletaVenceu(atletaId, partidas[0]);
        var quantidadeAtual = 0;

        foreach (var partida in partidas)
        {
            if (AtletaVenceu(atletaId, partida) != primeiraEhVitoria)
            {
                break;
            }

            quantidadeAtual++;
        }

        var tipo = primeiraEhVitoria ? "vitoria" : "derrota";
        return new DashboardScoutSequenciaDto(
            tipo,
            quantidadeAtual,
            MontarTextoSequencia(tipo, quantidadeAtual),
            CalcularMelhorSequenciaVitorias(atletaId, partidas));
    }

    private static int CalcularMelhorSequenciaVitorias(Guid atletaId, IReadOnlyList<Partida> partidas)
    {
        var melhor = 0;
        var atual = 0;

        foreach (var partida in partidas.OrderBy(ObterDataReferencia).ThenBy(x => x.DataCriacao))
        {
            if (AtletaVenceu(atletaId, partida))
            {
                atual++;
                melhor = Math.Max(melhor, atual);
                continue;
            }

            atual = 0;
        }

        return melhor;
    }

    private static DashboardScoutEstatisticasPontosDto MontarEstatisticasPontos(Guid atletaId, IReadOnlyList<Partida> partidas)
    {
        var jogosComPlacar = partidas
            .Where(x => x.PossuiPlacarDetalhado())
            .Select(partida => new
            {
                Partida = partida,
                Pro = ObterPontosAtleta(atletaId, partida),
                Contra = ObterPontosAdversario(atletaId, partida),
                Vitoria = AtletaVenceu(atletaId, partida)
            })
            .Where(x => x.Pro.HasValue && x.Contra.HasValue)
            .ToList();

        if (jogosComPlacar.Count == 0)
        {
            return new DashboardScoutEstatisticasPontosDto(
                false,
                0,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                0);
        }

        var pontosPro = jogosComPlacar.Sum(x => x.Pro!.Value);
        var pontosContra = jogosComPlacar.Sum(x => x.Contra!.Value);
        var saldo = pontosPro - pontosContra;
        var maiorVitoria = jogosComPlacar
            .Where(x => x.Vitoria)
            .Select(x => x.Pro!.Value - x.Contra!.Value)
            .DefaultIfEmpty()
            .Max();
        var derrotaMaisApertada = jogosComPlacar
            .Where(x => !x.Vitoria)
            .Select(x => x.Contra!.Value - x.Pro!.Value)
            .Where(x => x > 0)
            .DefaultIfEmpty()
            .Min();

        return new DashboardScoutEstatisticasPontosDto(
            true,
            jogosComPlacar.Count,
            pontosPro,
            pontosContra,
            saldo,
            decimal.Round(pontosPro / (decimal)jogosComPlacar.Count, 1),
            decimal.Round(pontosContra / (decimal)jogosComPlacar.Count, 1),
            decimal.Round(saldo / (decimal)jogosComPlacar.Count, 1),
            maiorVitoria == 0 ? null : maiorVitoria,
            derrotaMaisApertada == 0 ? null : derrotaMaisApertada,
            jogosComPlacar.Count(x => Math.Abs(x.Pro!.Value - x.Contra!.Value) <= 2));
    }

    private static string MontarTextoSequencia(string tipo, int sequencia)
    {
        return tipo switch
        {
            "vitoria" when sequencia == 1 => "1 vitória seguida",
            "vitoria" when sequencia > 1 => $"{sequencia} vitórias seguidas",
            "derrota" when sequencia == 1 => "1 derrota seguida",
            "derrota" when sequencia > 1 => $"{sequencia} derrotas seguidas",
            _ => "Sem sequência"
        };
    }

    private static string MontarTextoSequencia(int sequencia)
    {
        return sequencia > 1 ? $"{sequencia} vitórias seguidas" : "Buscando sequência";
    }

    private static bool AtletaVenceu(Guid atletaId, Partida partida)
    {
        var duplaAtleta = ObterDuplaDoAtleta(atletaId, partida);
        return duplaAtleta?.Id == partida.DuplaVencedoraId;
    }

    private static int? ObterPontosAtleta(Guid atletaId, Partida partida)
    {
        if (!partida.PossuiPlacarDetalhado())
        {
            return null;
        }

        return ReferenceEquals(ObterDuplaDoAtleta(atletaId, partida), partida.DuplaA)
            ? partida.PlacarDuplaA
            : partida.PlacarDuplaB;
    }

    private static int? ObterPontosAdversario(Guid atletaId, Partida partida)
    {
        if (!partida.PossuiPlacarDetalhado())
        {
            return null;
        }

        return ReferenceEquals(ObterDuplaDoAtleta(atletaId, partida), partida.DuplaA)
            ? partida.PlacarDuplaB
            : partida.PlacarDuplaA;
    }

    private static Dupla? ObterDuplaDoAtleta(Guid atletaId, Partida partida)
    {
        if (partida.DuplaA is not null && ObterAtletas(partida.DuplaA).Any(x => x.Id == atletaId))
        {
            return partida.DuplaA;
        }

        if (partida.DuplaB is not null && ObterAtletas(partida.DuplaB).Any(x => x.Id == atletaId))
        {
            return partida.DuplaB;
        }

        return null;
    }

    private static IEnumerable<Atleta> ObterAtletasRelacao(Guid atletaId, Partida partida, bool obterParceiros)
    {
        var duplaAtleta = ObterDuplaDoAtleta(atletaId, partida);
        if (duplaAtleta is null)
        {
            return [];
        }

        if (obterParceiros)
        {
            return ObterAtletas(duplaAtleta).Where(x => x.Id != atletaId);
        }

        var duplaAdversaria = ReferenceEquals(duplaAtleta, partida.DuplaA) ? partida.DuplaB : partida.DuplaA;
        return duplaAdversaria is null ? [] : ObterAtletas(duplaAdversaria);
    }

    private static IReadOnlyList<Atleta> ObterAtletas(Dupla dupla)
    {
        return new[] { dupla.Atleta1, dupla.Atleta2 }
            .Where(x => x is not null)
            .Cast<Atleta>()
            .ToList();
    }

    private static DashboardDuplaAtletaDto ParaAtletaDto(Atleta atleta)
    {
        return new DashboardDuplaAtletaDto(atleta.Id, atleta.Nome, atleta.Apelido);
    }

    private static DateTime ObterDataReferencia(Partida partida)
    {
        return partida.DataPartida ?? partida.DataCriacao;
    }

    private static string ObterNomeExibicao(string? nome, string? apelido)
    {
        return !string.IsNullOrWhiteSpace(apelido) ? apelido.Trim() : nome?.Trim() ?? "Atleta";
    }

}
