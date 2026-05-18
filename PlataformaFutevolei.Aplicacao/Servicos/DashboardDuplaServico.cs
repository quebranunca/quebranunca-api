using System.Globalization;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Aplicacao.Servicos;

public class DashboardDuplaServico(
    IAtletaRepositorio atletaRepositorio,
    IPartidaRepositorio partidaRepositorio
) : IDashboardDuplaServico
{
    private const int QuantidadeMesesEvolucao = 6;

    public async Task<DashboardDuplaDto> ObterDashboardAsync(
        Guid atleta1Id,
        Guid atleta2Id,
        CancellationToken cancellationToken = default)
    {
        if (atleta1Id == atleta2Id)
        {
            throw new RegraNegocioException("Informe dois atletas diferentes para visualizar o dashboard da dupla.");
        }

        var atleta1 = await atletaRepositorio.ObterPorIdAsync(atleta1Id, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Atleta não encontrado.");
        var atleta2 = await atletaRepositorio.ObterPorIdAsync(atleta2Id, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Atleta não encontrado.");

        var partidas = await partidaRepositorio.ListarPorAtletaAsync(atleta1Id, cancellationToken);
        var partidasValidas = partidas
            .Where(PartidaContaParaDashboard)
            .Where(x => DuplaEstaNaPartida(atleta1Id, atleta2Id, x))
            .OrderByDescending(ObterDataReferencia)
            .ThenByDescending(x => x.DataCriacao)
            .ToList();

        var resumo = MontarResumo(atleta1Id, atleta2Id, partidasValidas);
        var adversarios = MontarAdversarios(atleta1Id, atleta2Id, partidasValidas);

        return new DashboardDuplaDto(
            new DashboardDuplaDadosDto(
                ParaAtletaDto(atleta1),
                ParaAtletaDto(atleta2),
                $"{ObterNomeExibicao(atleta1.Nome, atleta1.Apelido)} e {ObterNomeExibicao(atleta2.Nome, atleta2.Apelido)}",
                ObterCategoriaPrincipal(partidasValidas)),
            resumo,
            MontarMetricas(resumo),
            partidasValidas.Take(6).Select(x => MontarPartidaRecente(atleta1Id, atleta2Id, x)).ToList(),
            adversarios.Take(8).ToList(),
            MontarEvolucao(atleta1Id, atleta2Id, partidasValidas),
            MontarInsights(atleta1Id, atleta2Id, resumo, adversarios.FirstOrDefault(), partidasValidas));
    }

    private static bool PartidaContaParaDashboard(Partida partida)
    {
        return partida.Status == StatusPartida.Encerrada &&
            partida.StatusAprovacao != StatusAprovacaoPartida.Contestada &&
            partida.DuplaVencedoraId.HasValue &&
            partida.DuplaA is not null &&
            partida.DuplaB is not null;
    }

    private static DashboardDuplaResumoDto MontarResumo(Guid atleta1Id, Guid atleta2Id, IReadOnlyList<Partida> partidas)
    {
        var total = partidas.Count;
        var vitorias = partidas.Count(x => DuplaVenceu(atleta1Id, atleta2Id, x));
        var derrotas = total - vitorias;
        var aproveitamento = total == 0 ? 0 : decimal.Round(vitorias * 100m / total, 1);
        var pontosPro = partidas.Sum(x => ObterPontosDupla(atleta1Id, atleta2Id, x));
        var pontosContra = partidas.Sum(x => ObterPontosAdversario(atleta1Id, atleta2Id, x));

        return new DashboardDuplaResumoDto(
            total,
            vitorias,
            derrotas,
            aproveitamento,
            pontosPro,
            pontosContra,
            pontosPro - pontosContra,
            CalcularMaiorSequenciaVitorias(atleta1Id, atleta2Id, partidas),
            CalcularSequenciaAtual(atleta1Id, atleta2Id, partidas));
    }

    private static IReadOnlyList<DashboardDuplaMetricaDto> MontarMetricas(DashboardDuplaResumoDto resumo)
    {
        return
        [
            new("partidas", "Partidas juntos", resumo.TotalPartidas.ToString(CultureInfo.InvariantCulture), null, "jogos"),
            new("vitorias", "Vitórias", resumo.Vitorias.ToString(CultureInfo.InvariantCulture), null, "vitorias", true),
            new("derrotas", "Derrotas", resumo.Derrotas.ToString(CultureInfo.InvariantCulture), null, "derrotas"),
            new("aproveitamento", "Aproveitamento", $"{resumo.Aproveitamento:0.#}%", null, "aproveitamento", true),
            new("saldo", "Saldo de pontos", resumo.SaldoPontos.ToString("+0;-0;0", CultureInfo.InvariantCulture), null, "saldo"),
            new("sequencia", "Sequência atual", resumo.SequenciaAtual.ToString(CultureInfo.InvariantCulture), "vitórias", "sequencia")
        ];
    }

    private static DashboardDuplaPartidaDto MontarPartidaRecente(Guid atleta1Id, Guid atleta2Id, Partida partida)
    {
        var adversarios = ObterDuplaAdversaria(atleta1Id, atleta2Id, partida);

        return new DashboardDuplaPartidaDto(
            partida.Id,
            DuplaVenceu(atleta1Id, atleta2Id, partida) ? "Vitória" : "Derrota",
            partida.DataPartida,
            partida.GrupoId,
            partida.CategoriaCompeticaoId,
            partida.Grupo?.Nome,
            partida.CategoriaCompeticao?.Nome,
            partida.CategoriaCompeticao?.Competicao?.Nome,
            partida.Status.ToString(),
            (int)partida.StatusAprovacao,
            ObterPontosDupla(atleta1Id, atleta2Id, partida),
            ObterPontosAdversario(atleta1Id, atleta2Id, partida),
            adversarios is null ? [] : ObterAtletas(adversarios).Select(ParaAtletaDto).ToList());
    }

    private static IReadOnlyList<DashboardDuplaAdversarioDto> MontarAdversarios(
        Guid atleta1Id,
        Guid atleta2Id,
        IReadOnlyList<Partida> partidas)
    {
        return partidas
            .Select(partida => new
            {
                Dupla = ObterDuplaAdversaria(atleta1Id, atleta2Id, partida),
                Vitoria = DuplaVenceu(atleta1Id, atleta2Id, partida)
            })
            .Where(x => x.Dupla is not null)
            .GroupBy(x => NormalizarChaveDupla(x.Dupla!.Atleta1Id, x.Dupla.Atleta2Id))
            .Select(grupo =>
            {
                var dupla = grupo.First().Dupla!;
                var total = grupo.Count();
                var vitorias = grupo.Count(x => x.Vitoria);
                var aproveitamento = total == 0 ? 0 : decimal.Round(vitorias * 100m / total, 1);

                return new DashboardDuplaAdversarioDto(
                    ObterAtletas(dupla).Select(ParaAtletaDto).ToList(),
                    total,
                    vitorias,
                    total - vitorias,
                    aproveitamento);
            })
            .OrderByDescending(x => x.Partidas)
            .ThenByDescending(x => x.Aproveitamento)
            .ThenBy(x => string.Join(" ", x.Atletas.Select(atleta => ObterNomeExibicao(atleta.Nome, atleta.Apelido))))
            .ToList();
    }

    private static IReadOnlyList<DashboardDuplaEvolucaoDto> MontarEvolucao(
        Guid atleta1Id,
        Guid atleta2Id,
        IReadOnlyList<Partida> partidas)
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
                var vitorias = partidasMes.Count(x => DuplaVenceu(atleta1Id, atleta2Id, x));
                var derrotas = total - vitorias;
                var aproveitamento = total == 0 ? 0 : decimal.Round(vitorias * 100m / total, 1);

                return new DashboardDuplaEvolucaoDto(
                    cultura.DateTimeFormat.GetAbbreviatedMonthName(mes.Month).TrimEnd('.'),
                    mes.Year,
                    mes.Month,
                    total,
                    vitorias,
                    derrotas,
                    aproveitamento);
            })
            .ToList();
    }

    private static IReadOnlyList<string> MontarInsights(
        Guid atleta1Id,
        Guid atleta2Id,
        DashboardDuplaResumoDto resumo,
        DashboardDuplaAdversarioDto? rivalMaisFrequente,
        IReadOnlyList<Partida> partidas)
    {
        var insights = new List<string>();
        var ultimas = partidas.Take(5).ToList();
        var vitoriasRecentes = ultimas.Count(x => DuplaVenceu(atleta1Id, atleta2Id, x));

        if (ultimas.Count > 0)
        {
            insights.Add($"Dupla venceu {vitoriasRecentes} dos últimos {ultimas.Count} jogos.");
        }

        if (rivalMaisFrequente is not null)
        {
            insights.Add($"Maior rivalidade contra {FormatarDupla(rivalMaisFrequente.Atletas)}.");
        }

        if (resumo.SequenciaAtual > 0)
        {
            insights.Add($"Sequência atual de {resumo.SequenciaAtual} vitórias.");
        }

        insights.Add(resumo.SaldoPontos >= 0
            ? $"Saldo positivo de {resumo.SaldoPontos} pontos."
            : $"Saldo negativo de {Math.Abs(resumo.SaldoPontos)} pontos.");

        if (insights.Count == 0)
        {
            insights.Add("Registre partidas dessa dupla para acompanhar a evolução.");
        }

        return insights.Take(4).ToList();
    }

    private static int CalcularSequenciaAtual(Guid atleta1Id, Guid atleta2Id, IReadOnlyList<Partida> partidas)
    {
        var sequencia = 0;
        foreach (var partida in partidas)
        {
            if (!DuplaVenceu(atleta1Id, atleta2Id, partida))
            {
                break;
            }

            sequencia++;
        }

        return sequencia;
    }

    private static int CalcularMaiorSequenciaVitorias(Guid atleta1Id, Guid atleta2Id, IReadOnlyList<Partida> partidas)
    {
        var maior = 0;
        var atual = 0;

        foreach (var partida in partidas.OrderBy(ObterDataReferencia).ThenBy(x => x.DataCriacao))
        {
            if (DuplaVenceu(atleta1Id, atleta2Id, partida))
            {
                atual++;
                maior = Math.Max(maior, atual);
                continue;
            }

            atual = 0;
        }

        return maior;
    }

    private static string? ObterCategoriaPrincipal(IReadOnlyList<Partida> partidas)
    {
        return partidas
            .Where(x => !string.IsNullOrWhiteSpace(x.CategoriaCompeticao?.Nome))
            .GroupBy(x => x.CategoriaCompeticao!.Nome)
            .OrderByDescending(x => x.Count())
            .ThenBy(x => x.Key)
            .Select(x => x.Key)
            .FirstOrDefault();
    }

    private static bool DuplaVenceu(Guid atleta1Id, Guid atleta2Id, Partida partida)
    {
        return ObterDuplaAlvo(atleta1Id, atleta2Id, partida)?.Id == partida.DuplaVencedoraId;
    }

    private static bool DuplaEstaNaPartida(Guid atleta1Id, Guid atleta2Id, Partida partida)
    {
        return ObterDuplaAlvo(atleta1Id, atleta2Id, partida) is not null;
    }

    private static Dupla? ObterDuplaAlvo(Guid atleta1Id, Guid atleta2Id, Partida partida)
    {
        if (EhMesmaDupla(partida.DuplaA, atleta1Id, atleta2Id))
        {
            return partida.DuplaA;
        }

        if (EhMesmaDupla(partida.DuplaB, atleta1Id, atleta2Id))
        {
            return partida.DuplaB;
        }

        return null;
    }

    private static Dupla? ObterDuplaAdversaria(Guid atleta1Id, Guid atleta2Id, Partida partida)
    {
        var duplaAlvo = ObterDuplaAlvo(atleta1Id, atleta2Id, partida);
        if (duplaAlvo is null)
        {
            return null;
        }

        return ReferenceEquals(duplaAlvo, partida.DuplaA) ? partida.DuplaB : partida.DuplaA;
    }

    private static int ObterPontosDupla(Guid atleta1Id, Guid atleta2Id, Partida partida)
    {
        return ReferenceEquals(ObterDuplaAlvo(atleta1Id, atleta2Id, partida), partida.DuplaA)
            ? partida.PlacarDuplaA
            : partida.PlacarDuplaB;
    }

    private static int ObterPontosAdversario(Guid atleta1Id, Guid atleta2Id, Partida partida)
    {
        return ReferenceEquals(ObterDuplaAlvo(atleta1Id, atleta2Id, partida), partida.DuplaA)
            ? partida.PlacarDuplaB
            : partida.PlacarDuplaA;
    }

    private static bool EhMesmaDupla(Dupla? dupla, Guid atleta1Id, Guid atleta2Id)
    {
        return dupla is not null &&
            ((dupla.Atleta1Id == atleta1Id && dupla.Atleta2Id == atleta2Id) ||
             (dupla.Atleta1Id == atleta2Id && dupla.Atleta2Id == atleta1Id));
    }

    private static string NormalizarChaveDupla(Guid atleta1Id, Guid atleta2Id)
    {
        return string.CompareOrdinal(atleta1Id.ToString("N"), atleta2Id.ToString("N")) <= 0
            ? $"{atleta1Id:N}:{atleta2Id:N}"
            : $"{atleta2Id:N}:{atleta1Id:N}";
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

    private static string FormatarDupla(IEnumerable<DashboardDuplaAtletaDto> atletas)
    {
        return string.Join(" e ", atletas.Select(x => ObterNomeExibicao(x.Nome, x.Apelido)));
    }
}
