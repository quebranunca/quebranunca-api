using System.Globalization;
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
    IAutorizacaoUsuarioServico autorizacaoUsuarioServico
) : IDashboardAtletaServico
{
    private const int QuantidadeMesesEvolucao = 6;
    private const int QuantidadeDiasHeatmap = 112;
    private const int QuantidadeUltimasPartidas = 5;
    private const int QuantidadeRelacoes = 8;

    private sealed record DashboardAtletaContexto(
        Atleta Atleta,
        IReadOnlyList<Partida> PartidasValidas);

    public async Task<DashboardAtletaDto> ObterDashboardAsync(CancellationToken cancellationToken = default)
    {
        var contexto = await CarregarContextoAsync(cancellationToken);
        var atleta = contexto.Atleta;
        var partidasValidas = contexto.PartidasValidas;

        var resumo = MontarResumo(atleta.Id, partidasValidas);
        var posicaoRanking = await ObterPosicaoRankingAsync(atleta.Id, cancellationToken);
        var parceiros = MontarRelacoes(atleta.Id, partidasValidas, obterParceiros: true);
        var rivais = MontarRelacoes(atleta.Id, partidasValidas, obterParceiros: false);
        var parceirosRecentes = MontarRelacoesRecentes(parceiros);
        var rivaisRecentes = MontarRelacoesRecentes(rivais);
        var melhorParceiro = parceiros.FirstOrDefault();
        var rivalMaisFrequente = rivais.FirstOrDefault();
        var resumoComNomes = resumo with
        {
            MelhorParceiro = ObterNomeExibicao(melhorParceiro?.Nome, melhorParceiro?.Apelido),
            RivalMaisFrequente = ObterNomeExibicao(rivalMaisFrequente?.Nome, rivalMaisFrequente?.Apelido)
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
            MontarInsights(resumoComNomes, melhorParceiro, rivais.FirstOrDefault(), partidasValidas));
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
            MontarTextoSequencia(resumo.SequenciaAtual),
            FotoPerfilAtletaUtil.ObterUrlPublica(contexto.Atleta));
    }

    public async Task<DashboardAtletaResumoDto> ObterResumoAsync(CancellationToken cancellationToken = default)
    {
        var contexto = await CarregarContextoAsync(cancellationToken);
        var resumo = MontarResumo(contexto.Atleta.Id, contexto.PartidasValidas);
        var parceiros = MontarRelacoes(contexto.Atleta.Id, contexto.PartidasValidas, obterParceiros: true);
        var rivais = MontarRelacoes(contexto.Atleta.Id, contexto.PartidasValidas, obterParceiros: false);

        return resumo with
        {
            MelhorParceiro = ObterNomeExibicao(parceiros.FirstOrDefault()?.Nome, parceiros.FirstOrDefault()?.Apelido),
            RivalMaisFrequente = ObterNomeExibicao(rivais.FirstOrDefault()?.Nome, rivais.FirstOrDefault()?.Apelido)
        };
    }

    public async Task<IReadOnlyList<string>> ObterInsightsAsync(CancellationToken cancellationToken = default)
    {
        var contexto = await CarregarContextoAsync(cancellationToken);
        var resumo = MontarResumo(contexto.Atleta.Id, contexto.PartidasValidas);
        var parceiros = MontarRelacoes(contexto.Atleta.Id, contexto.PartidasValidas, obterParceiros: true);
        var rivais = MontarRelacoes(contexto.Atleta.Id, contexto.PartidasValidas, obterParceiros: false);
        var resumoComNomes = resumo with
        {
            MelhorParceiro = ObterNomeExibicao(parceiros.FirstOrDefault()?.Nome, parceiros.FirstOrDefault()?.Apelido),
            RivalMaisFrequente = ObterNomeExibicao(rivais.FirstOrDefault()?.Nome, rivais.FirstOrDefault()?.Apelido)
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

    public async Task<DashboardAtletaConexoesDto> ObterConexoesAsync(CancellationToken cancellationToken = default)
    {
        var contexto = await CarregarContextoAsync(cancellationToken);
        var parceiros = MontarRelacoes(contexto.Atleta.Id, contexto.PartidasValidas, obterParceiros: true);
        var rivais = MontarRelacoes(contexto.Atleta.Id, contexto.PartidasValidas, obterParceiros: false);

        return new DashboardAtletaConexoesDto(
            parceiros.Take(QuantidadeRelacoes).ToList(),
            rivais.Take(QuantidadeRelacoes).ToList(),
            MontarRelacoesRecentes(parceiros).Take(QuantidadeRelacoes).ToList(),
            MontarRelacoesRecentes(rivais).Take(QuantidadeRelacoes).ToList());
    }

    public async Task<IReadOnlyList<DashboardAtletaHeatmapDiaDto>> ObterFrequenciaAsync(CancellationToken cancellationToken = default)
    {
        var contexto = await CarregarContextoAsync(cancellationToken);
        return MontarHeatmap(contexto.PartidasValidas);
    }

    private async Task<DashboardAtletaContexto> CarregarContextoAsync(CancellationToken cancellationToken)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        if (!usuario.AtletaId.HasValue)
        {
            throw new RegraNegocioException("Seu usuário precisa estar vinculado a um atleta para visualizar o dashboard.");
        }

        var atleta = await atletaRepositorio.ObterPorIdAsync(usuario.AtletaId.Value, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Atleta não encontrado.");
        var partidas = await partidaRepositorio.ListarPorAtletaAsync(atleta.Id, cancellationToken);
        var partidasValidas = partidas
            .Where(PartidaContaParaDashboard)
            .OrderByDescending(ObterDataReferencia)
            .ThenByDescending(x => x.DataCriacao)
            .ToList();

        return new DashboardAtletaContexto(atleta, partidasValidas);
    }

    private static bool PartidaContaParaDashboard(Partida partida)
    {
        return partida.Status == StatusPartida.Encerrada &&
            partida.StatusAprovacao != StatusAprovacaoPartida.Contestada &&
            partida.DuplaVencedoraId.HasValue &&
            partida.DuplaA is not null &&
            partida.DuplaB is not null;
    }

    private static DashboardAtletaResumoDto MontarResumo(Guid atletaId, IReadOnlyList<Partida> partidas)
    {
        var total = partidas.Count;
        var vitorias = partidas.Count(x => AtletaVenceu(atletaId, x));
        var derrotas = total - vitorias;
        var aproveitamento = total == 0 ? 0 : decimal.Round(vitorias * 100m / total, 1);
        var saldoPontos = partidas.Sum(x => ObterPontosAtleta(atletaId, x) - ObterPontosAdversario(atletaId, x));
        var sequenciaAtual = CalcularSequenciaAtual(atletaId, partidas);

        return new DashboardAtletaResumoDto(
            total,
            vitorias,
            derrotas,
            aproveitamento,
            saldoPontos,
            sequenciaAtual,
            null,
            null);
    }

    private static IReadOnlyList<DashboardAtletaMetricaDto> MontarMetricas(DashboardAtletaResumoDto resumo)
    {
        return
        [
            new("partidas", "Partidas", resumo.TotalPartidas.ToString(CultureInfo.InvariantCulture), null, "jogos"),
            new("vitorias", "Vitórias", resumo.Vitorias.ToString(CultureInfo.InvariantCulture), null, "vitorias", true),
            new("derrotas", "Derrotas", resumo.Derrotas.ToString(CultureInfo.InvariantCulture), null, "derrotas"),
            new("aproveitamento", "Aproveitamento", $"{resumo.Aproveitamento:0.#}%", null, "aproveitamento", true),
            new("saldo", "Saldo de pontos", resumo.SaldoPontos.ToString("+0;-0;0", CultureInfo.InvariantCulture), null, "saldo"),
            new("sequencia", "Sequência", resumo.SequenciaAtual.ToString(CultureInfo.InvariantCulture), "vitórias", "sequencia"),
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

                return new DashboardAtletaEvolucaoDto(
                    cultura.DateTimeFormat.GetAbbreviatedMonthName(mes.Month).TrimEnd('.'),
                    mes.Year,
                    mes.Month,
                    total,
                    vitorias,
                    aproveitamento,
                    null);
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
            ObterPontosAdversario(atletaId, partida));
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
                var partidasJuntos = grupo.Count();
                var vitorias = grupo.Count(x => x.Vitoria);
                var derrotas = partidasJuntos - vitorias;
                var aproveitamento = partidasJuntos == 0 ? 0 : decimal.Round(vitorias * 100m / partidasJuntos, 1);
                var ultimaPartida = grupo.Max(x => ObterDataReferencia(x.Partida));

                return new DashboardAtletaRelacaoDto(
                    primeiro.Id,
                    primeiro.Nome,
                    primeiro.Apelido,
                    partidasJuntos,
                    vitorias,
                    derrotas,
                    aproveitamento,
                    ultimaPartida,
                    FotoPerfilAtletaUtil.ObterUrlPublica(primeiro));
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

    private static int CalcularSequenciaAtual(Guid atletaId, IReadOnlyList<Partida> partidas)
    {
        var sequencia = 0;
        foreach (var partida in partidas)
        {
            if (!AtletaVenceu(atletaId, partida))
            {
                break;
            }

            sequencia++;
        }

        return sequencia;
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

    private static int ObterPontosAtleta(Guid atletaId, Partida partida)
    {
        return ReferenceEquals(ObterDuplaDoAtleta(atletaId, partida), partida.DuplaA)
            ? partida.PlacarDuplaA
            : partida.PlacarDuplaB;
    }

    private static int ObterPontosAdversario(Guid atletaId, Partida partida)
    {
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

    private static DateTime ObterDataReferencia(Partida partida)
    {
        return partida.DataPartida ?? partida.DataCriacao;
    }

    private static string ObterNomeExibicao(string? nome, string? apelido)
    {
        return !string.IsNullOrWhiteSpace(apelido) ? apelido.Trim() : nome?.Trim() ?? "Atleta";
    }

}
