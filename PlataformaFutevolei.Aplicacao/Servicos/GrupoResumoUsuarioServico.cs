using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Interfaces.Seguranca;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Dominio.Entidades;

namespace PlataformaFutevolei.Aplicacao.Servicos;

public class GrupoResumoUsuarioServico(
    IGrupoRepositorio grupoRepositorio,
    IPartidaRepositorio partidaRepositorio,
    IRankingServico rankingServico,
    IAutorizacaoUsuarioServico autorizacaoUsuarioServico
) : IGrupoResumoUsuarioServico
{
    public async Task<GrupoResumoUsuarioDto?> ObterMeuResumoAsync(CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        var grupo = await grupoRepositorio.ObterResumoUsuarioAsync(
            usuario.Id,
            usuario.AtletaId,
            cancellationToken);

        if (grupo is null)
        {
            return null;
        }

        var ultimoJogo = await partidaRepositorio.ObterUltimaDoGrupoAsync(grupo.Id, cancellationToken);
        var ranking = await rankingServico.ListarAtletasPorGrupoAsync(grupo.Id, cancellationToken);

        return new GrupoResumoUsuarioDto(
            grupo.Id,
            grupo.Nome,
            ultimoJogo is null ? null : MontarUltimoJogo(ultimoJogo),
            MontarRankingResumo(ranking, usuario.AtletaId));
    }

    private static GrupoResumoUltimoJogoDto MontarUltimoJogo(Partida partida)
    {
        return new GrupoResumoUltimoJogoDto(
            partida.DataPartida ?? partida.DataCriacao,
            ObterNomes(partida.DuplaA),
            ObterNomes(partida.DuplaB),
            $"{partida.PlacarDuplaA} x {partida.PlacarDuplaB}");
    }

    private static IReadOnlyList<string> ObterNomes(Dupla? dupla)
    {
        if (dupla is null)
        {
            return [];
        }

        return [dupla.Atleta1.Nome, dupla.Atleta2.Nome];
    }

    private static IReadOnlyList<GrupoResumoRankingAtletaDto> MontarRankingResumo(
        IReadOnlyList<RankingCategoriaDto> ranking,
        Guid? atletaUsuarioId)
    {
        var rankingOrdenado = ranking
            .SelectMany(x => x.Atletas)
            .GroupBy(x => x.AtletaId)
            .Select(x => new
            {
                AtletaId = x.Key,
                NomeAtleta = x.First().NomeAtleta,
                Pontuacao = x.Sum(atleta => atleta.Pontos)
            })
            .OrderByDescending(x => x.Pontuacao)
            .ThenBy(x => x.NomeAtleta)
            .Select((x, indice) => new GrupoResumoRankingAtletaDto(
                indice + 1,
                x.AtletaId,
                x.NomeAtleta,
                x.Pontuacao))
            .ToList();

        if (rankingOrdenado.Count == 0)
        {
            return [];
        }

        var indiceAtleta = atletaUsuarioId.HasValue
            ? rankingOrdenado.FindIndex(x => x.AtletaId == atletaUsuarioId.Value)
            : -1;

        if (indiceAtleta <= 0)
        {
            return rankingOrdenado.Take(3).ToList();
        }

        var inicio = Math.Max(indiceAtleta - 1, 0);
        var quantidade = Math.Min(3, rankingOrdenado.Count - inicio);

        return rankingOrdenado
            .Skip(inicio)
            .Take(quantidade)
            .ToList();
    }
}
