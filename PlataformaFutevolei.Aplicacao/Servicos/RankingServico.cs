using System.Globalization;
using System.Text;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Interfaces.Seguranca;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Aplicacao.Utilitarios;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Aplicacao.Servicos;

public class RankingServico(
    ILigaRepositorio ligaRepositorio,
    ICompeticaoRepositorio competicaoRepositorio,
    IPartidaRepositorio partidaRepositorio,
    IAutorizacaoUsuarioServico autorizacaoUsuarioServico
) : IRankingServico
{
    private const string NomeCompeticaoPartidasAvulsas = "Partidas avulsas";

    public async Task<RankingFiltroInicialDto> ObterFiltroInicialAsync(
        CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualAsync(cancellationToken);

        if (usuario is null)
        {
            return new RankingFiltroInicialDto("geral", null);
        }

        if (usuario.Perfil != PerfilUsuario.Atleta)
        {
            return new RankingFiltroInicialDto("geral", null);
        }

        Guid? competicaoId = usuario.Perfil switch
        {
            PerfilUsuario.Atleta when usuario.AtletaId.HasValue => await partidaRepositorio.ObterUltimaCompeticaoComPartidaEncerradaAsync(
                null,
                usuario.AtletaId.Value,
                cancellationToken),
            PerfilUsuario.Organizador => await partidaRepositorio.ObterUltimaCompeticaoComPartidaEncerradaAsync(
                usuario.Id,
                null,
                cancellationToken),
            _ => await partidaRepositorio.ObterUltimaCompeticaoComPartidaEncerradaAsync(
                null,
                null,
                cancellationToken)
        };

        return new RankingFiltroInicialDto(
            competicaoId.HasValue ? "competicao" : null,
            competicaoId);
    }

    public async Task<IReadOnlyList<RankingCategoriaDto>> ListarAtletasGeralAsync(
        CancellationToken cancellationToken = default)
    {
        var partidas = await partidaRepositorio.ListarParaRankingGeralAsync(null, cancellationToken);

        var rankingGeral = MontarRankingConsolidado(
            Guid.Empty,
            Guid.Empty,
            "Todas as competições",
            "Ranking geral",
            partidas);

        return rankingGeral is null ? [] : [rankingGeral];
    }

    public async Task<IReadOnlyList<RankingCategoriaDto>> ListarAtletasPorLigaAsync(
        Guid ligaId,
        CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualAsync(cancellationToken);
        if (usuario?.Perfil == PerfilUsuario.Atleta)
        {
            throw new RegraNegocioException("Usuários com perfil atleta só podem visualizar o ranking dos grupos em que participam.");
        }

        var liga = await ligaRepositorio.ObterPorIdAsync(ligaId, cancellationToken);
        if (liga is null)
        {
            throw new EntidadeNaoEncontradaException("Liga não encontrada.");
        }

        var partidas = await partidaRepositorio.ListarParaRankingPorLigaAsync(ligaId, cancellationToken);
        var partidasSemCompeticaoOuCategoria = await partidaRepositorio.ListarParaRankingSemCompeticaoOuCategoriaAsync(
            usuario?.Perfil == PerfilUsuario.Organizador ? usuario.Id : null,
            cancellationToken);
        return MontarRankingLiga(ligaId, liga.Nome, partidas, partidasSemCompeticaoOuCategoria);
    }

    public async Task<RankingRegiaoFiltroDto> ListarRegioesDisponiveisAsync(
        CancellationToken cancellationToken = default)
    {
        var partidas = await partidaRepositorio.ListarParaRankingGeralAsync(null, cancellationToken);
        var atletas = partidas
            .SelectMany(EnumerarAtletasRanking)
            .Where(EhCadastroCompleto)
            .DistinctBy(x => x.Id)
            .ToList();

        var estados = atletas
            .Select(x => x.Estado)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();

        var cidades = atletas
            .Where(x => !string.IsNullOrWhiteSpace(x.Estado) && !string.IsNullOrWhiteSpace(x.Cidade))
            .GroupBy(x => $"{NormalizarChaveRegiao(x.Estado)}|{NormalizarChaveRegiao(x.Cidade)}")
            .Select(x => new RankingRegiaoCidadeDto(x.First().Estado!, x.First().Cidade!))
            .OrderBy(x => x.Estado)
            .ThenBy(x => x.Cidade)
            .ToList();

        var bairros = atletas
            .Where(x =>
                !string.IsNullOrWhiteSpace(x.Estado) &&
                !string.IsNullOrWhiteSpace(x.Cidade) &&
                !string.IsNullOrWhiteSpace(x.Bairro))
            .GroupBy(x => $"{NormalizarChaveRegiao(x.Estado)}|{NormalizarChaveRegiao(x.Cidade)}|{NormalizarChaveRegiao(x.Bairro)}")
            .Select(x => new RankingRegiaoBairroDto(x.First().Estado!, x.First().Cidade!, x.First().Bairro!))
            .OrderBy(x => x.Estado)
            .ThenBy(x => x.Cidade)
            .ThenBy(x => x.Bairro)
            .ToList();

        return new RankingRegiaoFiltroDto(estados, cidades, bairros);
    }

    public async Task<IReadOnlyList<RankingCategoriaDto>> ListarAtletasPorRegiaoAsync(
        string? estado,
        string? cidade,
        string? bairro,
        CancellationToken cancellationToken = default)
    {
        var partidas = await partidaRepositorio.ListarParaRankingGeralAsync(null, cancellationToken);
        var ranking = MontarRankingRegiao(
            partidas,
            NormalizarFiltroRegiao(estado),
            NormalizarFiltroRegiao(cidade),
            NormalizarFiltroRegiao(bairro));

        return ranking is null ? [] : [ranking];
    }

    public async Task<IReadOnlyList<RankingCategoriaDto>> ListarAtletasPorCompeticaoAsync(
        Guid competicaoId,
        CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualAsync(cancellationToken);
        var competicao = await competicaoRepositorio.ObterPorIdAsync(competicaoId, cancellationToken);
        if (competicao is null)
        {
            throw new EntidadeNaoEncontradaException("Competição não encontrada.");
        }

        if (usuario?.Perfil == PerfilUsuario.Atleta)
        {
            var competicaoPartidasAvulsas = EhCompeticaoPartidasAvulsas(competicao);

            if (competicao.Tipo != TipoCompeticao.Grupo)
            {
                throw new RegraNegocioException("Usuários com perfil atleta só podem visualizar o ranking dos grupos em que participam.");
            }

            var usuarioEhDonoDoGrupo = competicao.UsuarioOrganizadorId == usuario.Id;

            if (!competicaoPartidasAvulsas && !usuarioEhDonoDoGrupo && !usuario.AtletaId.HasValue)
            {
                throw new RegraNegocioException("Seu usuário não possui atleta vinculado para consultar o ranking do grupo.");
            }

            if (!competicaoPartidasAvulsas && !usuarioEhDonoDoGrupo)
            {
                var possuiAcessoAoGrupo = await competicaoRepositorio.AtletaPossuiAcessoAsync(
                    competicaoId,
                    usuario.Id,
                    usuario.AtletaId!.Value,
                    cancellationToken);

                if (!possuiAcessoAoGrupo)
                {
                    throw new RegraNegocioException("Você só pode visualizar o ranking dos grupos em que participa.");
                }
            }
        }

        var partidas = await partidaRepositorio.ListarParaRankingPorCompeticaoAsync(competicaoId, cancellationToken);
        return MontarRankingPorCategoria(partidas);
    }

    private static bool EhCompeticaoPartidasAvulsas(Competicao competicao)
        => competicao.Tipo == TipoCompeticao.Grupo &&
           string.Equals(
               competicao.Nome?.Trim(),
               NomeCompeticaoPartidasAvulsas,
               StringComparison.OrdinalIgnoreCase);

    private static decimal ObterPontosVitoriaRanking(Partida partida)
    {
        var categoria = partida.CategoriaCompeticao;
        var competicao = categoria.Competicao;
        var peso = categoria.PesoRanking;

        if (competicao.Tipo == TipoCompeticao.Grupo || EhCompeticaoPartidasAvulsas(competicao))
        {
            return 1m;
        }

        return competicao.ObterPontosVitoria() * peso;
    }

    private static IReadOnlyList<RankingCategoriaDto> MontarRankingLiga(
        Guid ligaId,
        string nomeLiga,
        IReadOnlyList<Partida> partidasLiga,
        IReadOnlyList<Partida> partidasSemCompeticaoOuCategoria)
    {
        var categorias = new List<RankingCategoriaDto>();
        var rankingLiga = MontarRankingConsolidado(
            ligaId,
            ligaId,
            nomeLiga,
            "Ranking geral da liga",
            partidasLiga);
        if (rankingLiga is not null)
        {
            categorias.Add(rankingLiga);
        }

        var rankingSemCompeticaoOuCategoria = MontarRankingConsolidado(
            Guid.Empty,
            Guid.Empty,
            "Jogos sem liga",
            "Partidas sem competição/categoria",
            partidasSemCompeticaoOuCategoria);
        if (rankingSemCompeticaoOuCategoria is not null)
        {
            categorias.Add(rankingSemCompeticaoOuCategoria);
        }

        return categorias;
    }

    private static RankingCategoriaDto? MontarRankingConsolidado(
        Guid categoriaId,
        Guid competicaoId,
        string nomeCompeticao,
        string nomeCategoria,
        IReadOnlyList<Partida> partidas)
    {
        if (partidas.Count == 0)
        {
            return null;
        }

        var atletas = new Dictionary<Guid, RankingAtletaAcumulado>();
        var participacoesOficiaisAplicadas = new HashSet<(Guid AtletaId, Guid ReferenciaId)>();
        var participacoesPendentesAplicadas = new HashSet<(Guid AtletaId, Guid ReferenciaId)>();

        foreach (var partida in OrdenarPartidasParaPontuacao(partidas))
        {
            var categoria = partida.CategoriaCompeticao;
            var competicao = categoria.Competicao;
            var dataPartida = partida.DataPartida ?? partida.DataCriacao;
            var pontuacaoPendente = PontuacaoDaPartidaPendente(partida);
            var peso = categoria.PesoRanking;
            var pontosParticipacao = competicao.ObterPontosParticipacao() * peso;
            var pontosVitoria = ObterPontosVitoriaRanking(partida);
            var pontosDerrota = competicao.ObterPontosDerrota() * peso;
            var empate = partida.TerminouEmpatada();
            var vencedoraId = partida.ObterDuplaVencedoraPorPlacar();
            var confronto = MontarConfrontoRanking(partida);
            var duplaA = partida.DuplaA!;
            var duplaB = partida.DuplaB!;

            Acumular(
                atletas,
                participacoesOficiaisAplicadas,
                participacoesPendentesAplicadas,
                duplaA.Atleta1,
                competicao.Id,
                pontosParticipacao,
                empate,
                vencedoraId == duplaA.Id,
                pontosVitoria,
                pontosDerrota,
                pontuacaoPendente,
                partida.Id,
                confronto,
                dataPartida,
                competicao.Nome,
                categoria.Nome);
            Acumular(
                atletas,
                participacoesOficiaisAplicadas,
                participacoesPendentesAplicadas,
                duplaA.Atleta2,
                competicao.Id,
                pontosParticipacao,
                empate,
                vencedoraId == duplaA.Id,
                pontosVitoria,
                pontosDerrota,
                pontuacaoPendente,
                partida.Id,
                confronto,
                dataPartida,
                competicao.Nome,
                categoria.Nome);
            Acumular(
                atletas,
                participacoesOficiaisAplicadas,
                participacoesPendentesAplicadas,
                duplaB.Atleta1,
                competicao.Id,
                pontosParticipacao,
                empate,
                vencedoraId == duplaB.Id,
                pontosVitoria,
                pontosDerrota,
                pontuacaoPendente,
                partida.Id,
                confronto,
                dataPartida,
                competicao.Nome,
                categoria.Nome);
            Acumular(
                atletas,
                participacoesOficiaisAplicadas,
                participacoesPendentesAplicadas,
                duplaB.Atleta2,
                competicao.Id,
                pontosParticipacao,
                empate,
                vencedoraId == duplaB.Id,
                pontosVitoria,
                pontosDerrota,
                pontuacaoPendente,
                partida.Id,
                confronto,
                dataPartida,
                competicao.Nome,
                categoria.Nome);
        }

        foreach (var partidasCategoria in partidas.GroupBy(x => x.CategoriaCompeticaoId))
        {
            AplicarPontuacaoColocacao(partidasCategoria.ToList(), atletas);
        }

        return new RankingCategoriaDto(
            categoriaId,
            competicaoId,
            nomeCompeticao,
            nomeCategoria,
            null,
            OrdenarAtletas(atletas));
    }

    private static RankingCategoriaDto? MontarRankingRegiao(
        IReadOnlyList<Partida> partidas,
        string? estado,
        string? cidade,
        string? bairro)
    {
        if (partidas.Count == 0)
        {
            return null;
        }

        var atletas = new Dictionary<Guid, RankingAtletaAcumulado>();
        var participacoesOficiaisAplicadas = new HashSet<(Guid AtletaId, Guid ReferenciaId)>();
        var participacoesPendentesAplicadas = new HashSet<(Guid AtletaId, Guid ReferenciaId)>();
        bool AtletaElegivel(Atleta atleta) => EhAtletaElegivelRegiao(atleta, estado, cidade, bairro);

        foreach (var partida in OrdenarPartidasParaPontuacao(partidas))
        {
            var categoria = partida.CategoriaCompeticao;
            var competicao = categoria.Competicao;
            var dataPartida = partida.DataPartida ?? partida.DataCriacao;
            var pontuacaoPendente = PontuacaoDaPartidaPendente(partida);
            var peso = categoria.PesoRanking;
            var pontosParticipacao = competicao.ObterPontosParticipacao() * peso;
            var pontosVitoria = ObterPontosVitoriaRanking(partida);
            var pontosDerrota = competicao.ObterPontosDerrota() * peso;
            var empate = partida.TerminouEmpatada();
            var vencedoraId = partida.ObterDuplaVencedoraPorPlacar();
            var confronto = MontarConfrontoRanking(partida);
            var duplaA = partida.DuplaA!;
            var duplaB = partida.DuplaB!;

            AcumularSeAtletaElegivel(
                atletas,
                participacoesOficiaisAplicadas,
                participacoesPendentesAplicadas,
                duplaA.Atleta1,
                AtletaElegivel,
                competicao.Id,
                pontosParticipacao,
                empate,
                vencedoraId == duplaA.Id,
                pontosVitoria,
                pontosDerrota,
                pontuacaoPendente,
                partida.Id,
                confronto,
                dataPartida,
                competicao.Nome,
                categoria.Nome);
            AcumularSeAtletaElegivel(
                atletas,
                participacoesOficiaisAplicadas,
                participacoesPendentesAplicadas,
                duplaA.Atleta2,
                AtletaElegivel,
                competicao.Id,
                pontosParticipacao,
                empate,
                vencedoraId == duplaA.Id,
                pontosVitoria,
                pontosDerrota,
                pontuacaoPendente,
                partida.Id,
                confronto,
                dataPartida,
                competicao.Nome,
                categoria.Nome);
            AcumularSeAtletaElegivel(
                atletas,
                participacoesOficiaisAplicadas,
                participacoesPendentesAplicadas,
                duplaB.Atleta1,
                AtletaElegivel,
                competicao.Id,
                pontosParticipacao,
                empate,
                vencedoraId == duplaB.Id,
                pontosVitoria,
                pontosDerrota,
                pontuacaoPendente,
                partida.Id,
                confronto,
                dataPartida,
                competicao.Nome,
                categoria.Nome);
            AcumularSeAtletaElegivel(
                atletas,
                participacoesOficiaisAplicadas,
                participacoesPendentesAplicadas,
                duplaB.Atleta2,
                AtletaElegivel,
                competicao.Id,
                pontosParticipacao,
                empate,
                vencedoraId == duplaB.Id,
                pontosVitoria,
                pontosDerrota,
                pontuacaoPendente,
                partida.Id,
                confronto,
                dataPartida,
                competicao.Nome,
                categoria.Nome);
        }

        foreach (var partidasCategoria in partidas.GroupBy(x => x.CategoriaCompeticaoId))
        {
            AplicarPontuacaoColocacao(partidasCategoria.ToList(), atletas, AtletaElegivel);
        }

        if (atletas.Count == 0)
        {
            return null;
        }

        return new RankingCategoriaDto(
            Guid.Empty,
            Guid.Empty,
            "Ranking por região",
            MontarNomeRegiao(estado, cidade, bairro),
            null,
            OrdenarAtletas(atletas));
    }

    private static IReadOnlyList<RankingCategoriaDto> MontarRankingPorCategoria(IReadOnlyList<Partida> partidas)
    {
        var acumuladoPorCategoria = new Dictionary<Guid, RankingCategoriaAcumulado>();
        var participacoesOficiaisAplicadas = new HashSet<(Guid AtletaId, Guid ReferenciaId)>();
        var participacoesPendentesAplicadas = new HashSet<(Guid AtletaId, Guid ReferenciaId)>();

        foreach (var partida in OrdenarPartidasParaPontuacao(partidas))
        {
            var categoria = partida.CategoriaCompeticao;
            var competicao = categoria.Competicao;
            var dataPartida = partida.DataPartida ?? partida.DataCriacao;
            var pontuacaoPendente = PontuacaoDaPartidaPendente(partida);
            if (!acumuladoPorCategoria.TryGetValue(categoria.Id, out var categoriaAcumulada))
            {
                categoriaAcumulada = new RankingCategoriaAcumulado(
                    categoria.Id,
                    categoria.CompeticaoId,
                    competicao.Nome,
                    categoria.Nome,
                    categoria.Genero);
                acumuladoPorCategoria[categoria.Id] = categoriaAcumulada;
            }

            var peso = categoria.PesoRanking;
            var pontosParticipacao = competicao.ObterPontosParticipacao() * peso;
            var pontosVitoria = ObterPontosVitoriaRanking(partida);
            var pontosDerrota = competicao.ObterPontosDerrota() * peso;
            var empate = partida.TerminouEmpatada();
            var vencedoraId = partida.ObterDuplaVencedoraPorPlacar();
            var confronto = MontarConfrontoRanking(partida);
            var duplaA = partida.DuplaA!;
            var duplaB = partida.DuplaB!;

            Acumular(
                categoriaAcumulada.Atletas,
                participacoesOficiaisAplicadas,
                participacoesPendentesAplicadas,
                duplaA.Atleta1,
                categoria.Id,
                pontosParticipacao,
                empate,
                vencedoraId == duplaA.Id,
                pontosVitoria,
                pontosDerrota,
                pontuacaoPendente,
                partida.Id,
                confronto,
                dataPartida,
                competicao.Nome,
                categoria.Nome);
            Acumular(
                categoriaAcumulada.Atletas,
                participacoesOficiaisAplicadas,
                participacoesPendentesAplicadas,
                duplaA.Atleta2,
                categoria.Id,
                pontosParticipacao,
                empate,
                vencedoraId == duplaA.Id,
                pontosVitoria,
                pontosDerrota,
                pontuacaoPendente,
                partida.Id,
                confronto,
                dataPartida,
                competicao.Nome,
                categoria.Nome);
            Acumular(
                categoriaAcumulada.Atletas,
                participacoesOficiaisAplicadas,
                participacoesPendentesAplicadas,
                duplaB.Atleta1,
                categoria.Id,
                pontosParticipacao,
                empate,
                vencedoraId == duplaB.Id,
                pontosVitoria,
                pontosDerrota,
                pontuacaoPendente,
                partida.Id,
                confronto,
                dataPartida,
                competicao.Nome,
                categoria.Nome);
            Acumular(
                categoriaAcumulada.Atletas,
                participacoesOficiaisAplicadas,
                participacoesPendentesAplicadas,
                duplaB.Atleta2,
                categoria.Id,
                pontosParticipacao,
                empate,
                vencedoraId == duplaB.Id,
                pontosVitoria,
                pontosDerrota,
                pontuacaoPendente,
                partida.Id,
                confronto,
                dataPartida,
                competicao.Nome,
                categoria.Nome);
        }

        foreach (var partidasCategoria in partidas.GroupBy(x => x.CategoriaCompeticaoId))
        {
            if (acumuladoPorCategoria.TryGetValue(partidasCategoria.Key, out var categoriaAcumulada))
            {
                AplicarPontuacaoColocacao(partidasCategoria.ToList(), categoriaAcumulada.Atletas);
            }
        }

        return acumuladoPorCategoria.Values
            .OrderBy(x => x.NomeCompeticao)
            .ThenBy(x => x.Genero)
            .ThenBy(x => x.NomeCategoria)
            .Select(x => new RankingCategoriaDto(
                x.CategoriaId,
                x.CompeticaoId,
                x.NomeCompeticao,
                x.NomeCategoria,
                x.Genero,
                OrdenarAtletas(x.Atletas)))
            .ToList();
    }

    private static void AplicarPontuacaoColocacao(
        IReadOnlyList<Partida> partidasCategoria,
        IDictionary<Guid, RankingAtletaAcumulado> atletas,
        Func<Atleta, bool>? filtroAtleta = null)
    {
        if (partidasCategoria.Count == 0)
        {
            return;
        }

        var categoria = partidasCategoria[0].CategoriaCompeticao;
        var competicao = categoria.Competicao;
        if (competicao.Tipo != TipoCompeticao.Campeonato)
        {
            return;
        }

        var peso = categoria.PesoRanking;
        var final = partidasCategoria
            .Where(EhFaseFinal)
            .OrderByDescending(x => x.DataPartida ?? x.DataCriacao)
            .FirstOrDefault();

        if (final is not null)
        {
            AdicionarPontuacaoColocacao(
                atletas,
                ObterDuplaVencedora(final),
                competicao.ObterPontosPrimeiroLugar() * peso,
                final,
                PontuacaoDaPartidaPendente(final),
                competicao.Nome,
                categoria.Nome,
                "1º lugar",
                filtroAtleta);

            AdicionarPontuacaoColocacao(
                atletas,
                ObterDuplaPerdedora(final),
                competicao.ObterPontosSegundoLugar() * peso,
                final,
                PontuacaoDaPartidaPendente(final),
                competicao.Nome,
                categoria.Nome,
                "2º lugar",
                filtroAtleta);
        }

        var disputaTerceiro = partidasCategoria
            .Where(EhFaseTerceiroLugar)
            .OrderByDescending(x => x.DataPartida ?? x.DataCriacao)
            .FirstOrDefault();

        if (disputaTerceiro is null)
        {
            return;
        }

        AdicionarPontuacaoColocacao(
            atletas,
            ObterDuplaVencedora(disputaTerceiro),
            competicao.ObterPontosTerceiroLugar() * peso,
            disputaTerceiro,
            PontuacaoDaPartidaPendente(disputaTerceiro),
            competicao.Nome,
            categoria.Nome,
            "3º lugar",
            filtroAtleta);
    }

    private static void AdicionarPontuacaoColocacao(
        IDictionary<Guid, RankingAtletaAcumulado> atletas,
        Dupla? dupla,
        decimal pontos,
        Partida partida,
        bool pontuacaoPendente,
        string nomeCompeticao,
        string nomeCategoria,
        string colocacao,
        Func<Atleta, bool>? filtroAtleta = null)
    {
        if (dupla is null || pontos <= 0)
        {
            return;
        }

        var confronto = $"Pontuação por colocação: {MontarConfrontoRanking(partida)}";
        var dataPartida = partida.DataPartida ?? partida.DataCriacao;
        AdicionarPontuacaoColocacaoAtleta(
            atletas,
            dupla.Atleta1,
            pontos,
            partida.Id,
            confronto,
            dataPartida,
            nomeCompeticao,
            nomeCategoria,
            colocacao,
            pontuacaoPendente,
            filtroAtleta);
        AdicionarPontuacaoColocacaoAtleta(
            atletas,
            dupla.Atleta2,
            pontos,
            partida.Id,
            confronto,
            dataPartida,
            nomeCompeticao,
            nomeCategoria,
            colocacao,
            pontuacaoPendente,
            filtroAtleta);
    }

    private static void AdicionarPontuacaoColocacaoAtleta(
        IDictionary<Guid, RankingAtletaAcumulado> atletas,
        Atleta atleta,
        decimal pontos,
        Guid partidaId,
        string confronto,
        DateTime dataPartida,
        string nomeCompeticao,
        string nomeCategoria,
        string colocacao,
        bool pontuacaoPendente,
        Func<Atleta, bool>? filtroAtleta = null)
    {
        if (filtroAtleta is not null && !filtroAtleta(atleta))
        {
            return;
        }

        if (!atletas.TryGetValue(atleta.Id, out var item))
        {
            item = CriarRankingAtletaAcumulado(atleta);
            atletas[atleta.Id] = item;
        }

        if (pontuacaoPendente)
        {
            item.PontosPendentes += pontos;
        }
        else
        {
            item.Pontos += pontos;
        }

        item.Partidas.Add(new RankingPartidaDto(
            partidaId,
            confronto,
            dataPartida,
            nomeCompeticao,
            nomeCategoria,
            pontuacaoPendente ? $"{colocacao} pendente" : colocacao,
            pontos));
    }

    private static Dupla? ObterDuplaVencedora(Partida partida)
    {
        if (partida.DuplaVencedoraId == partida.DuplaAId)
        {
            return partida.DuplaA;
        }

        if (partida.DuplaVencedoraId == partida.DuplaBId)
        {
            return partida.DuplaB;
        }

        return null;
    }

    private static Dupla? ObterDuplaPerdedora(Partida partida)
    {
        if (partida.DuplaVencedoraId == partida.DuplaAId)
        {
            return partida.DuplaB;
        }

        if (partida.DuplaVencedoraId == partida.DuplaBId)
        {
            return partida.DuplaA;
        }

        return null;
    }

    private static bool EhFaseFinal(Partida partida)
    {
        var fase = NormalizarFaseRanking(partida.FaseCampeonato);
        return fase is "FINAL" or "FINAIS" or "GRANDEFINAL";
    }

    private static string MontarConfrontoRanking(Partida partida)
    {
        return $"{FormatarNomeDuplaRanking(partida.DuplaA)} {partida.PlacarDuplaA} x {partida.PlacarDuplaB} {FormatarNomeDuplaRanking(partida.DuplaB)}";
    }

    private static string FormatarNomeDuplaRanking(Dupla? dupla)
    {
        if (dupla is null)
        {
            return "A definir";
        }

        return $"{FormatarNomeAtletaRanking(dupla.Atleta1)} / {FormatarNomeAtletaRanking(dupla.Atleta2)}";
    }

    private static string FormatarNomeAtletaRanking(Atleta? atleta)
    {
        if (atleta is null)
        {
            return "A definir";
        }

        if (!string.IsNullOrWhiteSpace(atleta.Apelido))
        {
            return atleta.Apelido.Trim();
        }

        return atleta.Nome
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault() ?? atleta.Nome;
    }

    private static bool EhFaseTerceiroLugar(Partida partida)
    {
        var fase = NormalizarFaseRanking(partida.FaseCampeonato);
        return fase.Contains("3LUGAR", StringComparison.Ordinal) ||
            fase.Contains("TERCEIROLUGAR", StringComparison.Ordinal);
    }

    private static string NormalizarFaseRanking(string? faseCampeonato)
    {
        if (string.IsNullOrWhiteSpace(faseCampeonato))
        {
            return string.Empty;
        }

        var faseNormalizada = faseCampeonato.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(faseNormalizada.Length);

        foreach (var caractere in faseNormalizada)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caractere) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(caractere))
            {
                builder.Append(char.ToUpperInvariant(caractere));
            }
        }

        return builder.ToString();
    }

    private static IReadOnlyList<RankingAtletaDto> OrdenarAtletas(
        IDictionary<Guid, RankingAtletaAcumulado> atletas)
    {
        return atletas.Values
            .OrderByDescending(atleta => atleta.Pontos)
            .ThenByDescending(atleta => atleta.Vitorias)
            .ThenBy(atleta => atleta.NomeAtleta)
            .Select((atleta, indice) => new RankingAtletaDto(
                indice + 1,
                atleta.AtletaId,
                atleta.NomeAtleta,
                atleta.ApelidoAtleta,
                atleta.Bairro,
                atleta.Cidade,
                atleta.Estado,
                atleta.Lado,
                atleta.PossuiUsuarioVinculado,
                atleta.CadastroPendente,
                atleta.TemEmail,
                atleta.StatusPendencia,
                atleta.Jogos,
                atleta.Vitorias,
                atleta.Derrotas,
                atleta.Empates,
                atleta.Pontos,
                atleta.PontosPendentes,
                atleta.Partidas
                    .OrderByDescending(partida => partida.DataPartida)
                    .ToList()))
            .ToList();
    }

    private static void AcumularSeAtletaElegivel(
        IDictionary<Guid, RankingAtletaAcumulado> acumulado,
        ISet<(Guid AtletaId, Guid ReferenciaId)> participacoesOficiaisAplicadas,
        ISet<(Guid AtletaId, Guid ReferenciaId)> participacoesPendentesAplicadas,
        Atleta atleta,
        Func<Atleta, bool> atletaElegivel,
        Guid referenciaParticipacaoId,
        decimal pontosParticipacao,
        bool empate,
        bool venceu,
        decimal pontosVitoria,
        decimal pontosDerrota,
        bool pontuacaoPendente,
        Guid partidaId,
        string confronto,
        DateTime dataPartida,
        string nomeCompeticao,
        string nomeCategoria)
    {
        if (!atletaElegivel(atleta))
        {
            return;
        }

        Acumular(
            acumulado,
            participacoesOficiaisAplicadas,
            participacoesPendentesAplicadas,
            atleta,
            referenciaParticipacaoId,
            pontosParticipacao,
            empate,
            venceu,
            pontosVitoria,
            pontosDerrota,
            pontuacaoPendente,
            partidaId,
            confronto,
            dataPartida,
            nomeCompeticao,
            nomeCategoria);
    }

    private static void Acumular(
        IDictionary<Guid, RankingAtletaAcumulado> acumulado,
        ISet<(Guid AtletaId, Guid ReferenciaId)> participacoesOficiaisAplicadas,
        ISet<(Guid AtletaId, Guid ReferenciaId)> participacoesPendentesAplicadas,
        Atleta atleta,
        Guid referenciaParticipacaoId,
        decimal pontosParticipacao,
        bool empate,
        bool venceu,
        decimal pontosVitoria,
        decimal pontosDerrota,
        bool pontuacaoPendente,
        Guid partidaId,
        string confronto,
        DateTime dataPartida,
        string nomeCompeticao,
        string nomeCategoria)
    {
        if (!acumulado.TryGetValue(atleta.Id, out var item))
        {
            item = CriarRankingAtletaAcumulado(atleta);
            acumulado[atleta.Id] = item;
        }

        var pontosPartida = ObterPontosParticipacaoUnica(
            participacoesOficiaisAplicadas,
            participacoesPendentesAplicadas,
            atleta.Id,
            referenciaParticipacaoId,
            pontosParticipacao,
            pontuacaoPendente);

        if (empate)
        {
            if (pontuacaoPendente)
            {
                item.PontosPendentes += pontosPartida;
                item.Partidas.Add(new RankingPartidaDto(
                    partidaId,
                    confronto,
                    dataPartida,
                    nomeCompeticao,
                    nomeCategoria,
                    "Empate pendente",
                    pontosPartida));
                return;
            }

            item.Jogos++;
            item.Empates++;
            item.Pontos += pontosPartida;
            item.Partidas.Add(new RankingPartidaDto(
                partidaId,
                confronto,
                dataPartida,
                nomeCompeticao,
                nomeCategoria,
                "Empate",
                pontosPartida));
            return;
        }

        if (venceu)
        {
            pontosPartida += pontosVitoria;

            if (pontuacaoPendente)
            {
                item.PontosPendentes += pontosPartida;
                item.Partidas.Add(new RankingPartidaDto(
                    partidaId,
                    confronto,
                    dataPartida,
                    nomeCompeticao,
                    nomeCategoria,
                    "Vitória pendente",
                    pontosPartida));
                return;
            }

            item.Jogos++;
            item.Vitorias++;
            item.Pontos += pontosPartida;
            item.Partidas.Add(new RankingPartidaDto(
                partidaId,
                confronto,
                dataPartida,
                nomeCompeticao,
                nomeCategoria,
                "Vitória",
                pontosPartida));
            return;
        }

        pontosPartida += pontosDerrota;

        if (pontuacaoPendente)
        {
            item.PontosPendentes += pontosPartida;
            item.Partidas.Add(new RankingPartidaDto(
                partidaId,
                confronto,
                dataPartida,
                nomeCompeticao,
                nomeCategoria,
                "Derrota pendente",
                pontosPartida));
            return;
        }

        item.Jogos++;
        item.Derrotas++;
        item.Pontos += pontosPartida;
        item.Partidas.Add(new RankingPartidaDto(
            partidaId,
            confronto,
            dataPartida,
            nomeCompeticao,
            nomeCategoria,
            "Derrota",
            pontosPartida));
    }

    private static decimal ObterPontosParticipacaoUnica(
        ISet<(Guid AtletaId, Guid ReferenciaId)> participacoesOficiaisAplicadas,
        ISet<(Guid AtletaId, Guid ReferenciaId)> participacoesPendentesAplicadas,
        Guid atletaId,
        Guid referenciaParticipacaoId,
        decimal pontosParticipacao,
        bool pontuacaoPendente)
    {
        var chave = (atletaId, referenciaParticipacaoId);

        if (!pontuacaoPendente)
        {
            participacoesPendentesAplicadas.Remove(chave);
            return participacoesOficiaisAplicadas.Add(chave)
                ? pontosParticipacao
                : 0m;
        }

        if (participacoesOficiaisAplicadas.Contains(chave))
        {
            return 0m;
        }

        return participacoesPendentesAplicadas.Add(chave)
            ? pontosParticipacao
            : 0m;
    }

    private static IEnumerable<Partida> OrdenarPartidasParaPontuacao(IReadOnlyList<Partida> partidas)
    {
        return partidas
            .OrderBy(x => PontuacaoDaPartidaPendente(x))
            .ThenByDescending(x => x.DataPartida ?? x.DataCriacao);
    }

    private static bool PontuacaoDaPartidaPendente(Partida partida)
    {
        return partida.StatusAprovacao != StatusAprovacaoPartida.Aprovada;
    }

    private sealed class RankingAtletaAcumulado(
        Guid atletaId,
        string nomeAtleta,
        string? apelidoAtleta,
        string? bairro,
        string? cidade,
        string? estado,
        LadoAtleta lado,
        bool possuiUsuarioVinculado,
        bool cadastroPendente,
        bool temEmail,
        string statusPendencia)
    {
        public Guid AtletaId { get; } = atletaId;
        public string NomeAtleta { get; } = nomeAtleta;
        public string? ApelidoAtleta { get; } = apelidoAtleta;
        public string? Bairro { get; } = bairro;
        public string? Cidade { get; } = cidade;
        public string? Estado { get; } = estado;
        public LadoAtleta Lado { get; } = lado;
        public bool PossuiUsuarioVinculado { get; } = possuiUsuarioVinculado;
        public bool CadastroPendente { get; } = cadastroPendente;
        public bool TemEmail { get; } = temEmail;
        public string StatusPendencia { get; } = statusPendencia;
        public int Jogos { get; set; }
        public int Vitorias { get; set; }
        public int Derrotas { get; set; }
        public int Empates { get; set; }
        public decimal Pontos { get; set; }
        public decimal PontosPendentes { get; set; }
        public List<RankingPartidaDto> Partidas { get; } = [];
    }

    private static RankingAtletaAcumulado CriarRankingAtletaAcumulado(Atleta atleta)
    {
        return new RankingAtletaAcumulado(
            atleta.Id,
            atleta.Nome,
            atleta.Apelido,
            atleta.Bairro,
            atleta.Cidade,
            atleta.Estado,
            atleta.Lado,
            StatusCadastroAtletaUtil.PossuiUsuarioVinculado(atleta),
            atleta.CadastroPendente,
            StatusCadastroAtletaUtil.TemEmail(atleta),
            StatusCadastroAtletaUtil.ObterStatusPendencia(atleta));
    }

    private static IEnumerable<Atleta> EnumerarAtletasRanking(Partida partida)
    {
        return new[]
        {
            partida.DuplaA?.Atleta1,
            partida.DuplaA?.Atleta2,
            partida.DuplaB?.Atleta1,
            partida.DuplaB?.Atleta2
        }.OfType<Atleta>();
    }

    private static bool EhAtletaElegivelRegiao(
        Atleta atleta,
        string? estado,
        string? cidade,
        string? bairro)
    {
        if (!EhCadastroCompleto(atleta))
        {
            return false;
        }

        return MesmoTextoRegiao(atleta.Estado, estado) &&
            MesmoTextoRegiao(atleta.Cidade, cidade) &&
            MesmoTextoRegiao(atleta.Bairro, bairro);
    }

    private static bool EhCadastroCompleto(Atleta atleta)
    {
        return !atleta.CadastroPendente;
    }

    private static bool MesmoTextoRegiao(string? valorAtleta, string? filtro)
    {
        return string.IsNullOrWhiteSpace(filtro) ||
            string.Equals(valorAtleta, filtro, StringComparison.OrdinalIgnoreCase);
    }

    private static string? NormalizarFiltroRegiao(string? valor)
    {
        var normalizado = NormalizadorNomeAtleta.NormalizarTexto(valor);
        return string.IsNullOrWhiteSpace(normalizado) ? null : normalizado;
    }

    private static string NormalizarChaveRegiao(string? valor)
    {
        return NormalizarFiltroRegiao(valor)?.ToUpperInvariant() ?? string.Empty;
    }

    private static string MontarNomeRegiao(string? estado, string? cidade, string? bairro)
    {
        var partes = new[] { estado, cidade, bairro }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        return partes.Count == 0
            ? "Todas as regiões"
            : string.Join(" / ", partes);
    }

    private sealed class RankingCategoriaAcumulado(
        Guid categoriaId,
        Guid competicaoId,
        string nomeCompeticao,
        string nomeCategoria,
        GeneroCategoria genero)
    {
        public Guid CategoriaId { get; } = categoriaId;
        public Guid CompeticaoId { get; } = competicaoId;
        public string NomeCompeticao { get; } = nomeCompeticao;
        public string NomeCategoria { get; } = nomeCategoria;
        public GeneroCategoria Genero { get; } = genero;
        public Dictionary<Guid, RankingAtletaAcumulado> Atletas { get; } = new();
    }
}
