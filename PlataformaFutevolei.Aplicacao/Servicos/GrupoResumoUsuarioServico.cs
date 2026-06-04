using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Interfaces.Seguranca;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Aplicacao.Utilitarios;
using PlataformaFutevolei.Dominio.Entidades;

namespace PlataformaFutevolei.Aplicacao.Servicos;

public class GrupoResumoUsuarioServico(
    IGrupoRepositorio grupoRepositorio,
    IPartidaRepositorio partidaRepositorio,
    IRankingServico rankingServico,
    IAutorizacaoUsuarioServico autorizacaoUsuarioServico
) : IGrupoResumoUsuarioServico
{
    private const int LimiteGruposResumoHome = 6;

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

        return await MontarResumoGrupoAsync(grupo, usuario.AtletaId, cancellationToken);
    }

    public async Task<IReadOnlyList<GrupoResumoUsuarioDto>> ListarMeusResumosAsync(
        CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        var grupos = await grupoRepositorio.ListarResumosUsuarioAsync(
            usuario.Id,
            usuario.AtletaId,
            LimiteGruposResumoHome,
            cancellationToken);

        var resumos = new List<GrupoResumoUsuarioDto>(grupos.Count);
        foreach (var grupo in grupos)
        {
            resumos.Add(await MontarResumoGrupoAsync(grupo, usuario.AtletaId, cancellationToken));
        }

        return resumos;
    }

    public async Task<GrupoDashboardUsuarioDto> ObterDashboardAsync(CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        var grupos = await grupoRepositorio.ListarDashboardUsuarioAsync(
            usuario.Id,
            usuario.AtletaId,
            cancellationToken);

        var itens = new List<GrupoDashboardItemDto>(grupos.Count);
        foreach (var grupo in grupos)
        {
            var ranking = await rankingServico.ListarAtletasPorGrupoAsync(grupo.Id, cancellationToken);
            itens.Add(new GrupoDashboardItemDto(
                grupo.Id,
                grupo.Nome,
                grupo.ImagemUrl,
                grupo.Publico ? "Público" : "Privado",
                grupo.UsuarioOrganizadorId,
                grupo.UsuarioOrganizador?.Nome,
                grupo.Atletas.Select(x => x.AtletaId).Distinct().Count(),
                grupo.Partidas.Count,
                ContarPendenciasGrupo(grupo),
                ObterUltimaAtividade(grupo),
                MontarRankingResumo(ranking, usuario.AtletaId)));
        }

        var totais = new GrupoDashboardTotaisDto(
            itens.Count,
            grupos
                .SelectMany(x => x.Atletas)
                .Select(x => x.AtletaId)
                .Distinct()
                .Count(),
            itens.Sum(x => x.QuantidadePartidas),
            itens.Sum(x => x.Pendencias));

        return new GrupoDashboardUsuarioDto(totais, itens);
    }

    private async Task<GrupoResumoUsuarioDto> MontarResumoGrupoAsync(
        Grupo grupo,
        Guid? atletaUsuarioId,
        CancellationToken cancellationToken)
    {
        var ultimoJogo = atletaUsuarioId.HasValue
            ? await partidaRepositorio.ObterUltimaDoAtletaNoGrupoAsync(grupo.Id, atletaUsuarioId.Value, cancellationToken)
            : await partidaRepositorio.ObterUltimaDoGrupoAsync(grupo.Id, cancellationToken);
        var ranking = await rankingServico.ListarAtletasPorGrupoAsync(grupo.Id, cancellationToken);

        return new GrupoResumoUsuarioDto(
            grupo.Id,
            grupo.Nome,
            ultimoJogo is null ? null : MontarUltimoJogo(ultimoJogo),
            MontarRankingResumo(ranking, atletaUsuarioId));
    }

    private static GrupoResumoUltimoJogoDto MontarUltimoJogo(Partida partida)
    {
        return new GrupoResumoUltimoJogoDto(
            partida.DataPartida ?? partida.DataCriacao,
            ObterAtletas(partida.DuplaA),
            ObterAtletas(partida.DuplaB),
            partida.PlacarDuplaA,
            partida.PlacarDuplaB,
            (int)partida.Status,
            (int)partida.StatusAprovacao);
    }

    private static IReadOnlyList<GrupoResumoAtletaDto> ObterAtletas(Dupla? dupla)
    {
        if (dupla is null)
        {
            return [];
        }

        return new[] { dupla.Atleta1, dupla.Atleta2 }
            .Where(x => x is not null)
            .Select(x => new GrupoResumoAtletaDto(
                x.Id,
                x.Nome,
                x.Apelido,
                FotoPerfilAtletaUtil.ObterUrlPublica(x)))
            .ToList();
    }

    private static int ContarPendenciasGrupo(Grupo grupo)
        => grupo.Atletas.Count(x =>
            x.Atleta is not null &&
            x.Atleta.Usuario is null &&
            string.IsNullOrWhiteSpace(x.Atleta.Email));

    private static DateTime? ObterUltimaAtividade(Grupo grupo)
    {
        var ultimaPartida = grupo.Partidas
            .Select(x => x.DataPartida ?? x.DataCriacao)
            .DefaultIfEmpty()
            .Max();

        if (ultimaPartida != default)
        {
            return ultimaPartida;
        }

        return grupo.DataAtualizacao != default
            ? grupo.DataAtualizacao
            : grupo.DataCriacao;
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
                ApelidoAtleta = x.First().ApelidoAtleta,
                FotoPerfilUrl = x.First().FotoPerfilUrl,
                Pontuacao = x.Sum(atleta => atleta.Pontos)
            })
            .OrderByDescending(x => x.Pontuacao)
            .ThenBy(x => x.NomeAtleta)
            .Select((x, indice) => new GrupoResumoRankingAtletaDto(
                indice + 1,
                x.AtletaId,
                x.NomeAtleta,
                x.ApelidoAtleta,
                x.Pontuacao,
                atletaUsuarioId.HasValue && x.AtletaId == atletaUsuarioId.Value,
                x.FotoPerfilUrl))
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
