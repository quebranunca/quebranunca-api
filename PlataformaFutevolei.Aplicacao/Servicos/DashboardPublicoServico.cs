using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Aplicacao.Utilitarios;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Aplicacao.Servicos;

public class DashboardPublicoServico(
    IAtletaRepositorio atletaRepositorio,
    ICompeticaoRepositorio competicaoRepositorio,
    IGrupoRepositorio grupoRepositorio,
    IPartidaRepositorio partidaRepositorio,
    IRankingServico rankingServico
) : IDashboardPublicoServico
{
    private const int LimitePartidasRecentes = 8;
    private const int LimiteRanking = 10;
    private const int LimiteDestaques = 6;
    private const int LimiteGrupos = 6;
    private const int LimiteCampeonatos = 4;
    private const int LimiteRegioes = 6;

    public async Task<DashboardPublicoDto> ObterDashboardAsync(CancellationToken cancellationToken = default)
    {
        var agora = DateTime.UtcNow;
        var inicioHoje = agora.Date;
        var fimHoje = inicioHoje.AddDays(1);

        var atletas = await atletaRepositorio.ListarAsync(cancellationToken);
        var competicoes = await competicaoRepositorio.ListarAsync(cancellationToken);
        var grupos = await grupoRepositorio.ListarAsync(cancellationToken);
        var partidas = await partidaRepositorio.ListarParaRankingGeralAsync(null, cancellationToken);
        var rankingCategorias = await rankingServico.ListarAtletasGeralAsync(cancellationToken);

        var partidasValidas = partidas
            .Where(PartidaValida)
            .OrderByDescending(ObterDataPartida)
            .ToList();
        var ranking = rankingCategorias
            .FirstOrDefault()?
            .Atletas
            .Take(LimiteRanking)
            .Select(x => new DashboardPublicoRankingAtletaDto(
                x.Posicao,
                x.AtletaId,
                x.NomeAtleta,
                x.ApelidoAtleta,
                x.Jogos,
                x.Vitorias,
                x.Derrotas,
                CalcularAproveitamento(x.Vitorias, x.Jogos),
                x.Pontos,
                CalcularSequenciaAtual(x.AtletaId, partidasValidas),
                x.FotoPerfilUrl))
            .ToList() ?? [];

        var totalCampeonatos = competicoes.Count(x => x.Tipo == TipoCompeticao.Campeonato);
        var partidasHoje = partidasValidas.Count(x =>
        {
            var data = ObterDataPartida(x);
            return data >= inicioHoje && data < fimHoje;
        });
        var cidadesAtivas = atletas
            .Where(x => !string.IsNullOrWhiteSpace(x.Cidade))
            .Select(x => $"{x.Cidade!.Trim().ToLowerInvariant()}|{x.Estado?.Trim().ToLowerInvariant()}")
            .Distinct()
            .Count();
        var atletasOnline = ContarAtletasEmPartidasRecentes(partidasValidas, agora.AddHours(-24));

        var resumo = new DashboardPublicoResumoDto(
            partidasValidas.Count,
            atletas.Count,
            grupos.Count,
            totalCampeonatos,
            partidasHoje,
            atletasOnline,
            cidadesAtivas);

        return new DashboardPublicoDto(
            resumo,
            MontarMetricas(resumo, partidasValidas, ranking),
            MontarUltimasPartidas(partidasValidas, agora),
            ranking,
            MontarAtletasDestaque(ranking, partidasValidas),
            MontarGrupos(grupos, partidasValidas),
            MontarCampeonatos(competicoes, partidasValidas, agora),
            MontarRegioes(atletas, partidasValidas),
            MontarInsights(resumo, ranking, partidasValidas));
    }

    private static bool PartidaValida(Partida partida)
    {
        return partida.EsportivamenteValida() &&
            partida.DuplaA is not null &&
            partida.DuplaB is not null &&
            partida.DuplaVencedoraId.HasValue;
    }

    private static DateTime ObterDataPartida(Partida partida)
    {
        return partida.DataPartida ?? partida.DataCriacao;
    }

    private static decimal CalcularAproveitamento(int vitorias, int jogos)
    {
        return jogos == 0 ? 0 : decimal.Round(vitorias * 100m / jogos, 1);
    }

    private static IReadOnlyList<DashboardPublicoMetricaDto> MontarMetricas(
        DashboardPublicoResumoDto resumo,
        IReadOnlyList<Partida> partidas,
        IReadOnlyList<DashboardPublicoRankingAtletaDto> ranking)
    {
        var maiorSequencia = ranking.Count == 0 ? 0 : ranking.Max(x => x.SequenciaAtual);
        var mediaPartidasDia = CalcularMediaPartidasDia(partidas);
        var partidasComPlacar = partidas.Where(x => x.PossuiPlacarDetalhado()).ToList();
        var partidaMaisDisputada = partidasComPlacar
            .OrderBy(x => Math.Abs(x.PlacarDuplaA!.Value - x.PlacarDuplaB!.Value))
            .ThenByDescending(ObterDataPartida)
            .FirstOrDefault();
        var placarMaisElastico = partidasComPlacar
            .OrderByDescending(x => Math.Abs(x.PlacarDuplaA!.Value - x.PlacarDuplaB!.Value))
            .FirstOrDefault();

        return
        [
            new("partidas", "Partidas registradas", resumo.TotalPartidas.ToString("N0"), "histórico da comunidade", "partidas"),
            new("atletas", "Atletas", resumo.TotalAtletas.ToString("N0"), "nomes no ranking", "atletas"),
            new("grupos", "Grupos", resumo.TotalGrupos.ToString("N0"), "comunidades ativas", "grupos"),
            new("campeonatos", "Campeonatos", resumo.TotalCampeonatos.ToString("N0"), "eventos cadastrados", "trofeu"),
            new("hoje", "Partidas hoje", resumo.PartidasHoje.ToString("N0"), "movimento das últimas horas", "raio"),
            new("online", "Atletas ativos", resumo.AtletasOnline.ToString("N0"), "com jogo nas últimas 24h", "ativo"),
            new("cidades", "Cidades", resumo.CidadesAtivas.ToString("N0"), "regiões com atletas", "mapa"),
            new("media", "Média diária", mediaPartidasDia.ToString("N1"), "partidas por dia", "grafico"),
            new("sequencia", "Maior sequência", maiorSequencia.ToString("N0"), "vitórias seguidas", "fogo"),
            new("disputada", "Mais disputada", partidaMaisDisputada is null ? "0" : $"{partidaMaisDisputada.PlacarDuplaA} x {partidaMaisDisputada.PlacarDuplaB}", "menor diferença", "placar"),
            new("elastica", "Placar elástico", placarMaisElastico is null ? "0" : $"{placarMaisElastico.PlacarDuplaA} x {placarMaisElastico.PlacarDuplaB}", "maior diferença", "impacto")
        ];
    }

    private static decimal CalcularMediaPartidasDia(IReadOnlyList<Partida> partidas)
    {
        if (partidas.Count == 0)
        {
            return 0;
        }

        var datas = partidas.Select(ObterDataPartida).ToList();
        var dias = Math.Max(1, (int)Math.Ceiling((datas.Max().Date - datas.Min().Date).TotalDays) + 1);
        return decimal.Round(partidas.Count / (decimal)dias, 1);
    }

    private static IReadOnlyList<DashboardPublicoPartidaDto> MontarUltimasPartidas(
        IReadOnlyList<Partida> partidas,
        DateTime agora)
    {
        return partidas
            .Take(LimitePartidasRecentes)
            .Select(partida =>
            {
                var data = ObterDataPartida(partida);
                return new DashboardPublicoPartidaDto(
                    partida.Id,
                    data,
                    partida.Grupo?.Nome,
                    partida.CategoriaCompeticao?.Competicao?.Nome,
                    FormatarDupla(partida.DuplaA),
                    FormatarDupla(partida.DuplaB),
                    partida.PlacarDuplaA,
                    partida.PlacarDuplaB,
                    partida.DuplaVencedoraId == partida.DuplaAId ? FormatarDupla(partida.DuplaA) : FormatarDupla(partida.DuplaB),
                    Math.Max(0, (int)(agora - data).TotalMinutes));
            })
            .ToList();
    }

    private static IReadOnlyList<DashboardPublicoAtletaDestaqueDto> MontarAtletasDestaque(
        IReadOnlyList<DashboardPublicoRankingAtletaDto> ranking,
        IReadOnlyList<Partida> partidas)
    {
        var destaques = new List<DashboardPublicoAtletaDestaqueDto>();
        var maiorSequencia = ranking.OrderByDescending(x => x.SequenciaAtual).FirstOrDefault();
        if (maiorSequencia is not null && maiorSequencia.SequenciaAtual > 0)
        {
            destaques.Add(new(maiorSequencia.AtletaId, maiorSequencia.Nome, maiorSequencia.Apelido, "Maior sequência", $"{maiorSequencia.SequenciaAtual}", "vitórias seguidas", maiorSequencia.FotoPerfilUrl));
        }

        var maisVitorias = ranking.OrderByDescending(x => x.Vitorias).FirstOrDefault();
        if (maisVitorias is not null)
        {
            destaques.Add(new(maisVitorias.AtletaId, maisVitorias.Nome, maisVitorias.Apelido, "Mais vitórias", $"{maisVitorias.Vitorias}", "vitórias registradas", maisVitorias.FotoPerfilUrl));
        }

        var maisAtivo = ranking.OrderByDescending(x => x.Jogos).FirstOrDefault();
        if (maisAtivo is not null)
        {
            destaques.Add(new(maisAtivo.AtletaId, maisAtivo.Nome, maisAtivo.Apelido, "Mais ativo", $"{maisAtivo.Jogos}", "jogos no histórico", maisAtivo.FotoPerfilUrl));
        }

        var emAlta = ranking
            .Where(x => x.Jogos >= 3)
            .OrderByDescending(x => x.Aproveitamento)
            .ThenByDescending(x => x.Jogos)
            .FirstOrDefault();
        if (emAlta is not null)
        {
            destaques.Add(new(emAlta.AtletaId, emAlta.Nome, emAlta.Apelido, "Em alta", $"{emAlta.Aproveitamento:N0}%", "aproveitamento", emAlta.FotoPerfilUrl));
        }

        return destaques
            .DistinctBy(x => x.AtletaId)
            .Take(LimiteDestaques)
            .ToList();
    }

    private static IReadOnlyList<DashboardPublicoGrupoDto> MontarGrupos(
        IReadOnlyList<Grupo> grupos,
        IReadOnlyList<Partida> partidas)
    {
        return grupos
            .Select(grupo =>
            {
                var partidasGrupo = partidas.Where(x => x.GrupoId == grupo.Id).ToList();
                return new DashboardPublicoGrupoDto(
                    grupo.Id,
                    grupo.Nome,
                    partidasGrupo.Count,
                    ContarAtletasDistintos(partidasGrupo),
                    partidasGrupo.Count == 0 ? null : partidasGrupo.Max(ObterDataPartida));
            })
            .Where(x => x.Partidas > 0)
            .OrderByDescending(x => x.Partidas)
            .ThenByDescending(x => x.UltimaAtividade)
            .Take(LimiteGrupos)
            .ToList();
    }

    private static IReadOnlyList<DashboardPublicoCampeonatoDto> MontarCampeonatos(
        IReadOnlyList<Competicao> competicoes,
        IReadOnlyList<Partida> partidas,
        DateTime agora)
    {
        return competicoes
            .Where(x => x.Tipo == TipoCompeticao.Campeonato)
            .OrderByDescending(x => !x.DataFim.HasValue || x.DataFim.Value >= agora.Date)
            .ThenByDescending(x => x.DataInicio)
            .Take(LimiteCampeonatos)
            .Select(competicao =>
            {
                var partidasCampeonato = partidas
                    .Where(x => x.CategoriaCompeticao?.CompeticaoId == competicao.Id)
                    .ToList();
                return new DashboardPublicoCampeonatoDto(
                    competicao.Id,
                    competicao.Nome,
                    ObterStatusCampeonato(competicao, agora),
                    partidasCampeonato.Count,
                    null,
                    competicao.Arena?.Nome);
            })
            .ToList();
    }

    private static IReadOnlyList<DashboardPublicoRegiaoDto> MontarRegioes(
        IReadOnlyList<Atleta> atletas,
        IReadOnlyList<Partida> partidas)
    {
        var partidasPorAtleta = partidas
            .SelectMany(EnumerarAtletasPartida)
            .GroupBy(x => x.Id)
            .ToDictionary(x => x.Key, x => x.Count());

        return atletas
            .Where(x => !string.IsNullOrWhiteSpace(x.Cidade))
            .GroupBy(x => new
            {
                Cidade = x.Cidade!.Trim(),
                Estado = string.IsNullOrWhiteSpace(x.Estado) ? null : x.Estado.Trim()
            })
            .Select(x => new DashboardPublicoRegiaoDto(
                x.Key.Cidade,
                x.Key.Estado,
                x.Sum(atleta => partidasPorAtleta.GetValueOrDefault(atleta.Id))))
            .OrderByDescending(x => x.Partidas)
            .ThenBy(x => x.Cidade)
            .Take(LimiteRegioes)
            .ToList();
    }

    private static IReadOnlyList<string> MontarInsights(
        DashboardPublicoResumoDto resumo,
        IReadOnlyList<DashboardPublicoRankingAtletaDto> ranking,
        IReadOnlyList<Partida> partidas)
    {
        var insights = new List<string>();

        if (resumo.PartidasHoje > 0)
        {
            insights.Add($"{resumo.PartidasHoje} partida(s) já movimentaram a plataforma hoje.");
        }

        var lider = ranking.FirstOrDefault();
        if (lider is not null)
        {
            insights.Add($"{ObterNomeExibicao(lider.Nome, lider.Apelido)} lidera o ranking geral com {lider.Pontos:N0} ponto(s).");
        }

        var maiorSequencia = ranking.OrderByDescending(x => x.SequenciaAtual).FirstOrDefault();
        if (maiorSequencia is not null && maiorSequencia.SequenciaAtual > 1)
        {
            insights.Add($"{ObterNomeExibicao(maiorSequencia.Nome, maiorSequencia.Apelido)} está em sequência de {maiorSequencia.SequenciaAtual} vitórias.");
        }

        if (partidas.Count > 0)
        {
            insights.Add($"A comunidade já transformou {partidas.Count:N0} jogos em histórico, ranking e estatística.");
        }

        return insights.Take(4).ToList();
    }

    private static int ContarAtletasEmPartidasRecentes(IReadOnlyList<Partida> partidas, DateTime desde)
    {
        return partidas
            .Where(x => ObterDataPartida(x) >= desde)
            .SelectMany(EnumerarAtletasPartida)
            .Select(x => x.Id)
            .Distinct()
            .Count();
    }

    private static int ContarAtletasDistintos(IReadOnlyList<Partida> partidas)
    {
        return partidas
            .SelectMany(EnumerarAtletasPartida)
            .Select(x => x.Id)
            .Distinct()
            .Count();
    }

    private static IEnumerable<Atleta> EnumerarAtletasPartida(Partida partida)
    {
        if (partida.DuplaA is not null)
        {
            yield return partida.DuplaA.Atleta1;
            yield return partida.DuplaA.Atleta2;
        }

        if (partida.DuplaB is not null)
        {
            yield return partida.DuplaB.Atleta1;
            yield return partida.DuplaB.Atleta2;
        }
    }

    private static int CalcularSequenciaAtual(Guid atletaId, IReadOnlyList<Partida> partidas)
    {
        var sequencia = 0;
        foreach (var partida in partidas.Where(x => AtletaParticipou(x, atletaId)).OrderByDescending(ObterDataPartida))
        {
            if (AtletaVenceu(partida, atletaId))
            {
                sequencia++;
                continue;
            }

            break;
        }

        return sequencia;
    }

    private static bool AtletaParticipou(Partida partida, Guid atletaId)
    {
        return partida.DuplaA is not null &&
            partida.DuplaB is not null &&
            (partida.DuplaA.Atleta1Id == atletaId ||
             partida.DuplaA.Atleta2Id == atletaId ||
             partida.DuplaB.Atleta1Id == atletaId ||
             partida.DuplaB.Atleta2Id == atletaId);
    }

    private static bool AtletaVenceu(Partida partida, Guid atletaId)
    {
        if (!partida.DuplaVencedoraId.HasValue)
        {
            return false;
        }

        return (partida.DuplaAId == partida.DuplaVencedoraId &&
                partida.DuplaA is not null &&
                (partida.DuplaA.Atleta1Id == atletaId || partida.DuplaA.Atleta2Id == atletaId)) ||
            (partida.DuplaBId == partida.DuplaVencedoraId &&
             partida.DuplaB is not null &&
             (partida.DuplaB.Atleta1Id == atletaId || partida.DuplaB.Atleta2Id == atletaId));
    }

    private static string ObterStatusCampeonato(Competicao competicao, DateTime agora)
    {
        if (competicao.DataFim.HasValue && competicao.DataFim.Value.Date < agora.Date)
        {
            return "Encerrado";
        }

        if (competicao.DataInicio.Date > agora.Date)
        {
            return "Em breve";
        }

        return "Em andamento";
    }

    private static string FormatarDupla(Dupla? dupla)
    {
        if (dupla is null)
        {
            return "Dupla indefinida";
        }

        return $"{ObterNomeExibicao(dupla.Atleta1.Nome, dupla.Atleta1.Apelido)} / {ObterNomeExibicao(dupla.Atleta2.Nome, dupla.Atleta2.Apelido)}";
    }

    private static string ObterNomeExibicao(string nome, string? apelido)
    {
        return string.IsNullOrWhiteSpace(apelido) ? nome : apelido;
    }
}
