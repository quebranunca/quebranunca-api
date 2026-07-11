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
    IGrupoRepositorio grupoRepositorio,
    IGrupoAtletaRepositorio grupoAtletaRepositorio,
    IPartidaRepositorio partidaRepositorio,
    IAutorizacaoUsuarioServico autorizacaoUsuarioServico
) : IRankingServico
{
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
        var partidas = FiltrarPartidasEsportivamenteValidas(
            await partidaRepositorio.ListarParaRankingGeralAsync(null, cancellationToken));

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
        var liga = await ligaRepositorio.ObterPorIdAsync(ligaId, cancellationToken);
        if (liga is null)
        {
            throw new EntidadeNaoEncontradaException("Liga não encontrada.");
        }

        var partidas = FiltrarPartidasEsportivamenteValidas(
            await partidaRepositorio.ListarParaRankingPorLigaAsync(ligaId, cancellationToken));
        var partidasSemCompeticaoOuCategoria = FiltrarPartidasEsportivamenteValidas(
            await partidaRepositorio.ListarParaRankingSemCompeticaoOuCategoriaAsync(
                null,
                cancellationToken));
        return MontarRankingLiga(ligaId, liga.Nome, partidas, partidasSemCompeticaoOuCategoria);
    }

    public async Task<RankingRegiaoFiltroDto> ListarRegioesDisponiveisAsync(
        CancellationToken cancellationToken = default)
    {
        var partidas = FiltrarPartidasEsportivamenteValidas(
            await partidaRepositorio.ListarParaRankingGeralAsync(null, cancellationToken));
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
        var partidas = FiltrarPartidasEsportivamenteValidas(
            await partidaRepositorio.ListarParaRankingGeralAsync(null, cancellationToken));
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
        var competicao = await competicaoRepositorio.ObterPorIdAsync(competicaoId, cancellationToken);
        if (competicao is null)
        {
            throw new EntidadeNaoEncontradaException("Competição não encontrada.");
        }

        var partidas = FiltrarPartidasEsportivamenteValidas(
            await partidaRepositorio.ListarParaRankingPorCompeticaoAsync(competicaoId, cancellationToken));
        return MontarRankingPorCategoria(partidas);
    }

    public async Task<IReadOnlyList<RankingCategoriaDto>> ListarAtletasPorGrupoAsync(
        Guid grupoId,
        CancellationToken cancellationToken = default)
    {
        var grupo = await grupoRepositorio.ObterPorIdAsync(grupoId, cancellationToken);
        if (grupo is null)
        {
            throw new EntidadeNaoEncontradaException("Grupo não encontrado.");
        }

        var membros = await grupoAtletaRepositorio.ListarPorGrupoAsync(grupoId, cancellationToken);
        var atletasBase = membros
            .Select(x => x.Atleta)
            .Where(x => x is not null)
            .DistinctBy(x => x.Id)
            .ToList();
        var partidas = FiltrarPartidasEsportivamenteValidas(
            await partidaRepositorio.ListarParaRankingPorGrupoAsync(grupoId, cancellationToken));
        var ranking = MontarRankingConsolidado(
            grupo.Id,
            grupo.Id,
            grupo.Nome,
            "Ranking do grupo",
            partidas,
            atletasBase);
        return ranking is null ? [] : [ranking];
    }

    public async Task<RankingPaginaDto<RankingDuplaItemDto>> ListarDuplasAsync(
        Guid? grupoId,
        string? periodo,
        int pagina,
        int tamanhoPagina,
        string? ordenacao,
        CancellationToken cancellationToken = default)
    {
        var partidas = await ObterPartidasRankingAsync(grupoId, periodo, cancellationToken);
        var ranking = OrdenarDuplas(MontarRankingDuplas(partidas), ordenacao)
            .Select((dupla, indice) => CriarRankingDuplaItem(dupla, indice + 1))
            .ToList();

        return Paginar(ranking, pagina, tamanhoPagina);
    }

    public async Task<RankingDuplaDetalheDto> ObterDuplaAsync(
        string id,
        Guid? grupoId,
        string? periodo,
        CancellationToken cancellationToken = default)
    {
        var partidas = await ObterPartidasRankingAsync(grupoId, periodo, cancellationToken);
        var ranking = OrdenarDuplas(MontarRankingDuplas(partidas), null)
            .Select((dupla, indice) => (Dupla: dupla, Posicao: indice + 1))
            .ToList();
        var item = ranking.FirstOrDefault(x => x.Dupla.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

        if (item.Dupla is null)
        {
            throw new EntidadeNaoEncontradaException("Dupla não encontrada no ranking.");
        }

        return new RankingDuplaDetalheDto(
            CriarRankingDuplaItem(item.Dupla, item.Posicao),
            item.Dupla.Participacoes
                .OrderByDescending(x => x.DataPartida)
                .ThenByDescending(x => x.PartidaId)
                .Take(5)
                .Select(CriarRankingDuplaJogo)
                .ToList(),
            MontarAdversariosDupla(item.Dupla),
            MontarGruposDupla(item.Dupla),
            item.Dupla.Participacoes
                .OrderByDescending(x => x.DataPartida)
                .ThenByDescending(x => x.PartidaId)
                .Select(CriarRankingDuplaJogo)
                .ToList());
    }

    public async Task<RankingPaginaDto<RankingGrupoItemDto>> ListarGruposAsync(
        Guid? grupoId,
        string? periodo,
        int pagina,
        int tamanhoPagina,
        string? ordenacao,
        CancellationToken cancellationToken = default)
    {
        var ranking = await MontarRankingGruposAsync(grupoId, periodo, cancellationToken);
        var ordenado = OrdenarGrupos(ranking, ordenacao)
            .Select((grupo, indice) => CriarRankingGrupoItem(grupo, indice + 1))
            .ToList();

        return Paginar(ordenado, pagina, tamanhoPagina);
    }

    public async Task<RankingGrupoDetalheDto> ObterGrupoAsync(
        Guid id,
        string? periodo,
        CancellationToken cancellationToken = default)
    {
        var grupos = await MontarRankingGruposAsync(id, periodo, cancellationToken);
        var rankingGrupos = OrdenarGrupos(grupos, null)
            .Select((grupo, indice) => (Grupo: grupo, Posicao: indice + 1))
            .ToList();
        var grupoRanking = rankingGrupos.FirstOrDefault(x => x.Grupo.GrupoId == id);

        if (grupoRanking.Grupo is null)
        {
            throw new EntidadeNaoEncontradaException("Grupo não encontrado no ranking.");
        }

        var partidas = grupoRanking.Grupo.Partidas
            .OrderByDescending(x => x.DataPartida ?? x.DataCriacao)
            .ThenByDescending(x => x.Id)
            .ToList();
        var rankingAtletas = MontarRankingConsolidado(
            grupoRanking.Grupo.GrupoId,
            grupoRanking.Grupo.GrupoId,
            grupoRanking.Grupo.Nome,
            "Ranking do grupo",
            partidas,
            grupoRanking.Grupo.Grupo.Atletas.Select(x => x.Atleta).OfType<Atleta>().ToList());
        var topDuplas = OrdenarDuplas(MontarRankingDuplas(partidas), null)
            .Select((dupla, indice) => CriarRankingDuplaItem(dupla, indice + 1))
            .Take(5)
            .ToList();

        return new RankingGrupoDetalheDto(
            grupoRanking.Grupo.GrupoId,
            grupoRanking.Grupo.Nome,
            grupoRanking.Grupo.FotoUrl,
            grupoRanking.Grupo.Cidade,
            grupoRanking.Grupo.Descricao,
            grupoRanking.Grupo.Administrador,
            grupoRanking.Grupo.Publico,
            grupoRanking.Grupo.QuantidadeAtletas,
            grupoRanking.Grupo.QuantidadePartidas,
            grupoRanking.Grupo.AtletasAtivos,
            grupoRanking.Grupo.PontuacaoRanking,
            rankingAtletas?.Atletas.Take(5).ToList() ?? [],
            topDuplas,
            partidas.Take(5).Select(CriarRankingGrupoJogo).ToList(),
            MontarEvolucaoMensalGrupo(partidas));
    }

    private async Task<IReadOnlyList<Partida>> ObterPartidasRankingAsync(
        Guid? grupoId,
        string? periodo,
        CancellationToken cancellationToken)
    {
        var partidas = grupoId.HasValue
            ? await partidaRepositorio.ListarParaRankingPorGrupoAsync(grupoId.Value, cancellationToken)
            : await partidaRepositorio.ListarParaRankingGeralAsync(null, cancellationToken);

        return FiltrarPorPeriodo(FiltrarPartidasEsportivamenteValidas(partidas), periodo);
    }

    private static RankingPaginaDto<T> Paginar<T>(
        IReadOnlyList<T> itens,
        int pagina,
        int tamanhoPagina)
    {
        var paginaNormalizada = Math.Max(1, pagina);
        var tamanhoNormalizado = Math.Clamp(tamanhoPagina, 1, 100);
        var total = itens.Count;
        var totalPaginas = total == 0
            ? 0
            : (int)Math.Ceiling(total / (decimal)tamanhoNormalizado);

        return new RankingPaginaDto<T>(
            itens
                .Skip((paginaNormalizada - 1) * tamanhoNormalizado)
                .Take(tamanhoNormalizado)
                .ToList(),
            paginaNormalizada,
            tamanhoNormalizado,
            total,
            totalPaginas);
    }

    private static IReadOnlyList<Partida> FiltrarPorPeriodo(IReadOnlyList<Partida> partidas, string? periodo)
    {
        var periodoNormalizado = NormalizarPeriodo(periodo);
        if (string.IsNullOrWhiteSpace(periodoNormalizado) || periodoNormalizado == "todos")
        {
            return partidas;
        }

        var agora = DateTime.UtcNow;
        DateTime? inicio = periodoNormalizado switch
        {
            "30" or "30d" or "ultimos30dias" or "ultimos30" => agora.AddDays(-30),
            "90" or "90d" or "ultimos90dias" or "ultimos90" => agora.AddDays(-90),
            "ano" or "anoatual" or "year" => new DateTime(agora.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            _ => null
        };

        if (!inicio.HasValue)
        {
            return partidas;
        }

        return partidas
            .Where(x => (x.DataPartida ?? x.DataCriacao) >= inicio.Value)
            .ToList();
    }

    private static string NormalizarPeriodo(string? periodo)
    {
        if (string.IsNullOrWhiteSpace(periodo))
        {
            return string.Empty;
        }

        var normalizado = periodo.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalizado.Length);
        foreach (var caractere in normalizado)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caractere) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(caractere))
            {
                builder.Append(char.ToLowerInvariant(caractere));
            }
        }

        return builder.ToString();
    }

    private static IReadOnlyList<DuplaRankingAcumulado> MontarRankingDuplas(IReadOnlyList<Partida> partidas)
    {
        var duplas = new Dictionary<string, DuplaRankingAcumulado>(StringComparer.OrdinalIgnoreCase);

        foreach (var partida in partidas)
        {
            if (partida.DuplaA is null || partida.DuplaB is null)
            {
                continue;
            }

            AcumularDupla(duplas, partida, partida.DuplaA, partida.DuplaB);
            AcumularDupla(duplas, partida, partida.DuplaB, partida.DuplaA);
        }

        return duplas.Values.ToList();
    }

    private static void AcumularDupla(
        IDictionary<string, DuplaRankingAcumulado> duplas,
        Partida partida,
        Dupla dupla,
        Dupla adversaria)
    {
        var chave = NormalizarChaveDupla(dupla.Atleta1Id, dupla.Atleta2Id);
        if (!duplas.TryGetValue(chave.Id, out var acumulado))
        {
            acumulado = new DuplaRankingAcumulado(
                chave.Id,
                CriarAtletaResumo(chave.MenorId == dupla.Atleta1Id ? dupla.Atleta1 : dupla.Atleta2),
                CriarAtletaResumo(chave.MaiorId == dupla.Atleta1Id ? dupla.Atleta1 : dupla.Atleta2));
            duplas[chave.Id] = acumulado;
        }

        var vencedoraId = partida.ObterDuplaVencedoraPorPlacar();
        var empate = partida.TerminouEmpatada();
        var venceu = vencedoraId == dupla.Id;
        var dataPartida = partida.DataPartida ?? partida.DataCriacao;
        var possuiPlacar = partida.PossuiPlacarDetalhado();
        var ehDuplaA = partida.DuplaAId == dupla.Id;
        var pontosPro = possuiPlacar
            ? ehDuplaA ? partida.PlacarDuplaA!.Value : partida.PlacarDuplaB!.Value
            : (int?)null;
        var pontosContra = possuiPlacar
            ? ehDuplaA ? partida.PlacarDuplaB!.Value : partida.PlacarDuplaA!.Value
            : (int?)null;
        var pontosRanking = empate
            ? 0m
            : venceu ? ObterPontosVitoriaRanking(partida) : ObterPontosDerrotaRanking(partida);
        var adversariaChave = NormalizarChaveDupla(adversaria.Atleta1Id, adversaria.Atleta2Id);

        acumulado.Participacoes.Add(new DuplaParticipacaoRanking(
            partida.Id,
            dataPartida,
            partida.GrupoId,
            partida.Grupo?.Nome ?? "Partidas avulsas",
            venceu,
            empate,
            pontosRanking,
            pontosPro,
            pontosContra,
            adversariaChave.Id,
            FormatarNomeDuplaRanking(adversaria),
            MontarConfrontoRanking(partida)));
    }

    private static IEnumerable<DuplaRankingAcumulado> OrdenarDuplas(
        IReadOnlyList<DuplaRankingAcumulado> duplas,
        string? ordenacao)
    {
        var ordenacaoNormalizada = NormalizarPeriodo(ordenacao);
        return ordenacaoNormalizada switch
        {
            "aproveitamento" => duplas
                .OrderByDescending(x => x.Aproveitamento)
                .ThenByDescending(x => x.Vitorias)
                .ThenByDescending(x => x.PontosRanking)
                .ThenBy(x => x.Nome),
            "vitorias" => duplas
                .OrderByDescending(x => x.Vitorias)
                .ThenByDescending(x => x.Aproveitamento)
                .ThenByDescending(x => x.PontosRanking)
                .ThenBy(x => x.Nome),
            "sequencia" => duplas
                .OrderByDescending(x => x.SequenciaAtual.Quantidade)
                .ThenByDescending(x => x.PontosRanking)
                .ThenByDescending(x => x.Vitorias)
                .ThenBy(x => x.Nome),
            "jogos" => duplas
                .OrderByDescending(x => x.Jogos)
                .ThenByDescending(x => x.PontosRanking)
                .ThenByDescending(x => x.Vitorias)
                .ThenBy(x => x.Nome),
            _ => duplas
                .OrderByDescending(x => x.PontosRanking)
                .ThenByDescending(x => x.Aproveitamento)
                .ThenByDescending(x => x.Vitorias)
                .ThenByDescending(x => x.SequenciaAtual.Tipo == "V" ? x.SequenciaAtual.Quantidade : 0)
                .ThenByDescending(x => x.Jogos)
                .ThenBy(x => x.Nome)
        };
    }

    private static RankingDuplaItemDto CriarRankingDuplaItem(DuplaRankingAcumulado dupla, int posicao)
    {
        var placar = dupla.EstatisticasPlacar;
        return new RankingDuplaItemDto(
            dupla.Id,
            posicao,
            dupla.Atleta1,
            dupla.Atleta2,
            dupla.Jogos,
            dupla.Vitorias,
            dupla.Derrotas,
            dupla.Aproveitamento,
            dupla.SequenciaAtual,
            dupla.PontosRanking,
            0,
            dupla.UltimoJogo,
            dupla.GrupoPrincipal,
            placar.JogosComPlacar > 0 ? placar.PontosPro : null,
            placar.JogosComPlacar > 0 ? placar.PontosContra : null,
            placar.JogosComPlacar > 0 ? placar.Saldo : null);
    }

    private static RankingDuplaJogoDto CriarRankingDuplaJogo(DuplaParticipacaoRanking participacao)
    {
        return new RankingDuplaJogoDto(
            participacao.PartidaId,
            participacao.DataPartida,
            participacao.NomeGrupo,
            participacao.NomeDuplaAdversaria,
            participacao.Empate ? "Empate" : participacao.Venceu ? "Vitória" : "Derrota",
            participacao.PossuiPlacar ? $"{participacao.PontosPro} x {participacao.PontosContra}" : null,
            participacao.PossuiPlacar);
    }

    private static IReadOnlyList<RankingDuplaAdversarioDto> MontarAdversariosDupla(DuplaRankingAcumulado dupla)
    {
        return dupla.Participacoes
            .GroupBy(x => x.AdversariaId)
            .Select(grupo =>
            {
                var participacoes = grupo.ToList();
                var jogos = participacoes.Count;
                var vitorias = participacoes.Count(x => x.Venceu);
                var derrotas = participacoes.Count(x => !x.Venceu && !x.Empate);
                var jogosComPlacar = participacoes.Count(x => x.PossuiPlacar);
                var pontosPro = participacoes.Sum(x => x.PontosPro ?? 0);
                var pontosContra = participacoes.Sum(x => x.PontosContra ?? 0);

                return new RankingDuplaAdversarioDto(
                    grupo.Key,
                    participacoes[0].NomeDuplaAdversaria,
                    jogos,
                    vitorias,
                    derrotas,
                    CalcularAproveitamento(vitorias, jogos),
                    participacoes.Max(x => x.DataPartida),
                    jogosComPlacar > 0 ? pontosPro : null,
                    jogosComPlacar > 0 ? pontosContra : null,
                    jogosComPlacar > 0 ? pontosPro - pontosContra : null);
            })
            .OrderByDescending(x => x.Jogos)
            .ThenByDescending(x => x.Vitorias)
            .ThenBy(x => x.Nome)
            .Take(5)
            .ToList();
    }

    private static IReadOnlyList<RankingDuplaGrupoDto> MontarGruposDupla(DuplaRankingAcumulado dupla)
    {
        return dupla.Participacoes
            .GroupBy(x => x.GrupoId)
            .Select(grupo =>
            {
                var participacoes = grupo.ToList();
                var jogos = participacoes.Count;
                var vitorias = participacoes.Count(x => x.Venceu);
                var derrotas = participacoes.Count(x => !x.Venceu && !x.Empate);
                var jogosComPlacar = participacoes.Count(x => x.PossuiPlacar);
                var pontosPro = participacoes.Sum(x => x.PontosPro ?? 0);
                var pontosContra = participacoes.Sum(x => x.PontosContra ?? 0);

                return new RankingDuplaGrupoDto(
                    grupo.Key,
                    participacoes[0].NomeGrupo,
                    jogos,
                    vitorias,
                    derrotas,
                    CalcularAproveitamento(vitorias, jogos),
                    participacoes.Sum(x => x.PontosRanking),
                    jogosComPlacar > 0 ? pontosPro : null,
                    jogosComPlacar > 0 ? pontosContra : null,
                    jogosComPlacar > 0 ? pontosPro - pontosContra : null);
            })
            .OrderByDescending(x => x.Jogos)
            .ThenByDescending(x => x.PontosRanking)
            .ThenBy(x => x.Nome)
            .ToList();
    }

    private async Task<IReadOnlyList<GrupoRankingAcumulado>> MontarRankingGruposAsync(
        Guid? grupoId,
        string? periodo,
        CancellationToken cancellationToken)
    {
        var grupos = (await grupoRepositorio.ListarAsync(cancellationToken))
            .Where(x => !EhGrupoPartidasAvulsas(x))
            .Where(x => !grupoId.HasValue || x.Id == grupoId.Value)
            .ToDictionary(x => x.Id);

        if (grupoId.HasValue && !grupos.ContainsKey(grupoId.Value))
        {
            var grupo = await grupoRepositorio.ObterPorIdAsync(grupoId.Value, cancellationToken);
            if (grupo is not null && !EhGrupoPartidasAvulsas(grupo))
            {
                grupos[grupo.Id] = grupo;
            }
        }

        var partidas = await ObterPartidasRankingAsync(grupoId, periodo, cancellationToken);
        foreach (var grupoPartida in partidas.Select(x => x.Grupo).OfType<Grupo>())
        {
            if (!EhGrupoPartidasAvulsas(grupoPartida))
            {
                grupos.TryAdd(grupoPartida.Id, grupoPartida);
            }
        }

        return grupos.Values
            .Where(GrupoAtivo)
            .Select(grupo =>
            {
                var partidasGrupo = partidas
                    .Where(x => x.GrupoId == grupo.Id)
                    .ToList();
                return new GrupoRankingAcumulado(grupo, partidasGrupo);
            })
            .ToList();
    }

    private static bool EhGrupoPartidasAvulsas(Grupo grupo)
        => string.Equals(grupo.Nome?.Trim(), "Partidas avulsas", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(grupo.Nome?.Trim(), "Ranking Geral", StringComparison.OrdinalIgnoreCase);

    private static bool GrupoAtivo(Grupo grupo)
        => !grupo.DataFim.HasValue || grupo.DataFim.Value.Date >= DateTime.UtcNow.Date;

    private static IEnumerable<GrupoRankingAcumulado> OrdenarGrupos(
        IReadOnlyList<GrupoRankingAcumulado> grupos,
        string? ordenacao)
    {
        var ordenacaoNormalizada = NormalizarPeriodo(ordenacao);
        return ordenacaoNormalizada switch
        {
            "partidas" or "jogos" => grupos
                .OrderByDescending(x => x.QuantidadePartidas)
                .ThenByDescending(x => x.PontuacaoRanking)
                .ThenBy(x => x.Nome),
            "atletas" => grupos
                .OrderByDescending(x => x.QuantidadeAtletas)
                .ThenByDescending(x => x.AtletasAtivos)
                .ThenByDescending(x => x.PontuacaoRanking)
                .ThenBy(x => x.Nome),
            "ativos" => grupos
                .OrderByDescending(x => x.AtletasAtivos)
                .ThenByDescending(x => x.PontuacaoRanking)
                .ThenBy(x => x.Nome),
            _ => grupos
                .OrderByDescending(x => x.PontuacaoRanking)
                .ThenByDescending(x => x.QuantidadePartidas)
                .ThenByDescending(x => x.AtletasAtivos)
                .ThenBy(x => x.Nome)
        };
    }

    private static RankingGrupoItemDto CriarRankingGrupoItem(GrupoRankingAcumulado grupo, int posicao)
    {
        return new RankingGrupoItemDto(
            grupo.GrupoId,
            posicao,
            grupo.Nome,
            grupo.FotoUrl,
            grupo.Cidade,
            grupo.QuantidadeAtletas,
            grupo.QuantidadePartidas,
            grupo.AtletasAtivos,
            grupo.PontuacaoRanking,
            0,
            grupo.UltimaPartida);
    }

    private static RankingGrupoJogoDto CriarRankingGrupoJogo(Partida partida)
    {
        var vencedora = ObterDuplaVencedora(partida);
        var resultado = vencedora is null
            ? "Empate"
            : $"{FormatarNomeDuplaRanking(vencedora)} venceu";

        return new RankingGrupoJogoDto(
            partida.Id,
            partida.DataPartida ?? partida.DataCriacao,
            FormatarNomeDuplaRanking(partida.DuplaA),
            FormatarNomeDuplaRanking(partida.DuplaB),
            resultado,
            partida.PossuiPlacarDetalhado() ? $"{partida.PlacarDuplaA} x {partida.PlacarDuplaB}" : null,
            partida.PossuiPlacarDetalhado());
    }

    private static IReadOnlyList<RankingGrupoEvolucaoMensalDto> MontarEvolucaoMensalGrupo(IReadOnlyList<Partida> partidas)
    {
        var agora = DateTime.UtcNow;
        var meses = Enumerable.Range(0, 6)
            .Select(offset => new DateTime(agora.Year, agora.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-offset))
            .OrderBy(x => x)
            .ToList();

        return meses
            .Select(mes =>
            {
                var partidasMes = partidas
                    .Where(x =>
                    {
                        var data = x.DataPartida ?? x.DataCriacao;
                        return data.Year == mes.Year && data.Month == mes.Month;
                    })
                    .ToList();
                var atletasAtivos = partidasMes
                    .SelectMany(EnumerarAtletasRanking)
                    .Select(x => x.Id)
                    .Distinct()
                    .Count();

                return new RankingGrupoEvolucaoMensalDto(
                    mes.Year,
                    mes.Month,
                    partidasMes.Count,
                    atletasAtivos,
                    CalcularPontuacaoGrupo(partidasMes.Count, atletasAtivos));
            })
            .ToList();
    }

    private static (Guid MenorId, Guid MaiorId, string Id) NormalizarChaveDupla(Guid atleta1Id, Guid atleta2Id)
    {
        var menorId = atleta1Id.CompareTo(atleta2Id) <= 0 ? atleta1Id : atleta2Id;
        var maiorId = menorId == atleta1Id ? atleta2Id : atleta1Id;
        return (menorId, maiorId, $"{menorId:N}_{maiorId:N}");
    }

    private static RankingAtletaResumoDto CriarAtletaResumo(Atleta atleta)
    {
        return new RankingAtletaResumoDto(
            atleta.Id,
            atleta.Nome,
            atleta.Apelido,
            FotoPerfilAtletaUtil.ObterUrlPublica(atleta));
    }

    private static decimal CalcularAproveitamento(int vitorias, int jogos)
        => jogos == 0 ? 0m : Math.Round(vitorias * 100m / jogos, 1);

    private static RankingSequenciaDto CalcularSequencia(IReadOnlyList<DuplaParticipacaoRanking> participacoes)
    {
        var ordenadas = participacoes
            .OrderByDescending(x => x.DataPartida)
            .ThenByDescending(x => x.PartidaId)
            .ToList();

        if (ordenadas.Count == 0)
        {
            return new RankingSequenciaDto("nenhuma", 0, "Sem jogos");
        }

        var tipo = ordenadas[0].Empate ? "E" : ordenadas[0].Venceu ? "V" : "D";
        var quantidade = ordenadas.TakeWhile(x => (x.Empate ? "E" : x.Venceu ? "V" : "D") == tipo).Count();
        var texto = tipo switch
        {
            "V" => quantidade == 1 ? "1 vitória seguida" : $"{quantidade} vitórias seguidas",
            "D" => quantidade == 1 ? "1 derrota seguida" : $"{quantidade} derrotas seguidas",
            _ => quantidade == 1 ? "1 empate seguido" : $"{quantidade} empates seguidos"
        };

        return new RankingSequenciaDto(tipo, quantidade, texto);
    }

    private static RankingEstatisticasPlacarAcumulado CalcularEstatisticasPlacar(
        IReadOnlyList<DuplaParticipacaoRanking> participacoes)
    {
        var comPlacar = participacoes.Where(x => x.PossuiPlacar).ToList();
        var pontosPro = comPlacar.Sum(x => x.PontosPro ?? 0);
        var pontosContra = comPlacar.Sum(x => x.PontosContra ?? 0);
        return new RankingEstatisticasPlacarAcumulado(
            comPlacar.Count,
            pontosPro,
            pontosContra,
            pontosPro - pontosContra);
    }

    private static decimal CalcularPontuacaoGrupo(int partidasValidas, int atletasAtivos)
        => partidasValidas * 10m + atletasAtivos * 3m;

    private static decimal ObterPontosVitoriaRanking(Partida partida)
    {
        if (partida.DuplaVencedoraId is null)
        {
            return 0m;
        }

        if (partida.CategoriaCompeticao?.Competicao is { } competicao)
        {
            var pontos = competicao.ObterPontosVitoria();
            if (partida.StatusAprovacao == StatusAprovacaoPartida.Aprovada)
            {
                pontos += Partida.PontosBonusAprovacaoVitoriaRanking;
            }

            return pontos * partida.CategoriaCompeticao.PesoRanking;
        }

        return partida.CalcularPontosRankingVitoria(partida.CategoriaCompeticao?.PesoRanking);
    }

    private static IReadOnlyList<Partida> FiltrarPartidasEsportivamenteValidas(IReadOnlyList<Partida> partidas)
        => partidas
            .Where(x => x.Status == StatusPartida.Encerrada)
            .Where(x => !x.Cancelada)
            .Where(x => x.ExcluidaDefinitivamenteEm is null)
            .Where(x => x.DuplaAId.HasValue && x.DuplaBId.HasValue)
            .Where(x => x.DuplaA is not null && x.DuplaB is not null)
            .ToList();

    private static decimal ObterPontosDerrotaRanking(Partida partida)
    {
        if (partida.CategoriaCompeticao?.Competicao is not { } competicao)
        {
            return Partida.PontosDerrotaRanking;
        }

        return competicao.ObterPontosDerrota() * partida.CategoriaCompeticao.PesoRanking;
    }

    private static decimal ObterBonusAprovacaoPendenteRanking(Partida partida)
    {
        return partida.CalcularBonusAprovacaoPendenteRanking(partida.CategoriaCompeticao?.PesoRanking);
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
        IReadOnlyList<Partida> partidas,
        IReadOnlyList<Atleta>? atletasBase = null)
    {
        var atletas = new Dictionary<Guid, RankingAtletaAcumulado>();
        foreach (var atleta in atletasBase ?? [])
        {
            atletas.TryAdd(atleta.Id, CriarRankingAtletaAcumulado(atleta));
        }

        if (partidas.Count == 0 && atletas.Count == 0)
        {
            return null;
        }

        var participacoesOficiaisAplicadas = new HashSet<(Guid AtletaId, Guid ReferenciaId)>();
        var participacoesPendentesAplicadas = new HashSet<(Guid AtletaId, Guid ReferenciaId)>();

        foreach (var partida in OrdenarPartidasParaPontuacao(partidas))
        {
            var categoria = partida.CategoriaCompeticao;
            var competicao = categoria?.Competicao;
            var dataPartida = partida.DataPartida ?? partida.DataCriacao;
            var pontuacaoPendente = PontuacaoDaPartidaPendente(partida);
            var peso = categoria?.PesoRanking ?? 1m;
            var referenciaParticipacaoId = competicao?.Id ?? partida.GrupoId ?? competicaoId;
            var nomeCompeticaoPartida = competicao?.Nome ?? partida.Grupo?.Nome ?? nomeCompeticao;
            var nomeCategoriaPartida = categoria?.Nome ?? nomeCategoria;
            var pontosParticipacao = competicao is null ? 0m : competicao.ObterPontosParticipacao() * peso;
            var pontosVitoria = ObterPontosVitoriaRanking(partida);
            var pontosBonusAprovacaoPendente = ObterBonusAprovacaoPendenteRanking(partida);
            var pontosDerrota = ObterPontosDerrotaRanking(partida);
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
                referenciaParticipacaoId,
                pontosParticipacao,
                empate,
                vencedoraId == duplaA.Id,
                pontosVitoria,
                pontosBonusAprovacaoPendente,
                pontosDerrota,
                pontuacaoPendente,
                partida.Id,
                confronto,
                dataPartida,
                nomeCompeticaoPartida,
                nomeCategoriaPartida);
            Acumular(
                atletas,
                participacoesOficiaisAplicadas,
                participacoesPendentesAplicadas,
                duplaA.Atleta2,
                referenciaParticipacaoId,
                pontosParticipacao,
                empate,
                vencedoraId == duplaA.Id,
                pontosVitoria,
                pontosBonusAprovacaoPendente,
                pontosDerrota,
                pontuacaoPendente,
                partida.Id,
                confronto,
                dataPartida,
                nomeCompeticaoPartida,
                nomeCategoriaPartida);
            Acumular(
                atletas,
                participacoesOficiaisAplicadas,
                participacoesPendentesAplicadas,
                duplaB.Atleta1,
                referenciaParticipacaoId,
                pontosParticipacao,
                empate,
                vencedoraId == duplaB.Id,
                pontosVitoria,
                pontosBonusAprovacaoPendente,
                pontosDerrota,
                pontuacaoPendente,
                partida.Id,
                confronto,
                dataPartida,
                nomeCompeticaoPartida,
                nomeCategoriaPartida);
            Acumular(
                atletas,
                participacoesOficiaisAplicadas,
                participacoesPendentesAplicadas,
                duplaB.Atleta2,
                referenciaParticipacaoId,
                pontosParticipacao,
                empate,
                vencedoraId == duplaB.Id,
                pontosVitoria,
                pontosBonusAprovacaoPendente,
                pontosDerrota,
                pontuacaoPendente,
                partida.Id,
                confronto,
                dataPartida,
                nomeCompeticaoPartida,
                nomeCategoriaPartida);
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
            var competicao = categoria?.Competicao;
            var dataPartida = partida.DataPartida ?? partida.DataCriacao;
            var pontuacaoPendente = PontuacaoDaPartidaPendente(partida);
            var peso = categoria?.PesoRanking ?? 1m;
            var referenciaParticipacaoId = competicao?.Id ?? partida.GrupoId ?? Guid.Empty;
            var nomeCompeticaoPartida = competicao?.Nome ?? partida.Grupo?.Nome ?? "Jogos sem liga";
            var nomeCategoriaPartida = categoria?.Nome ?? "Partidas sem competição/categoria";
            var pontosParticipacao = competicao is null ? 0m : competicao.ObterPontosParticipacao() * peso;
            var pontosVitoria = ObterPontosVitoriaRanking(partida);
            var pontosBonusAprovacaoPendente = ObterBonusAprovacaoPendenteRanking(partida);
            var pontosDerrota = ObterPontosDerrotaRanking(partida);
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
                referenciaParticipacaoId,
                pontosParticipacao,
                empate,
                vencedoraId == duplaA.Id,
                pontosVitoria,
                pontosBonusAprovacaoPendente,
                pontosDerrota,
                pontuacaoPendente,
                partida.Id,
                confronto,
                dataPartida,
                nomeCompeticaoPartida,
                nomeCategoriaPartida);
            AcumularSeAtletaElegivel(
                atletas,
                participacoesOficiaisAplicadas,
                participacoesPendentesAplicadas,
                duplaA.Atleta2,
                AtletaElegivel,
                referenciaParticipacaoId,
                pontosParticipacao,
                empate,
                vencedoraId == duplaA.Id,
                pontosVitoria,
                pontosBonusAprovacaoPendente,
                pontosDerrota,
                pontuacaoPendente,
                partida.Id,
                confronto,
                dataPartida,
                nomeCompeticaoPartida,
                nomeCategoriaPartida);
            AcumularSeAtletaElegivel(
                atletas,
                participacoesOficiaisAplicadas,
                participacoesPendentesAplicadas,
                duplaB.Atleta1,
                AtletaElegivel,
                referenciaParticipacaoId,
                pontosParticipacao,
                empate,
                vencedoraId == duplaB.Id,
                pontosVitoria,
                pontosBonusAprovacaoPendente,
                pontosDerrota,
                pontuacaoPendente,
                partida.Id,
                confronto,
                dataPartida,
                nomeCompeticaoPartida,
                nomeCategoriaPartida);
            AcumularSeAtletaElegivel(
                atletas,
                participacoesOficiaisAplicadas,
                participacoesPendentesAplicadas,
                duplaB.Atleta2,
                AtletaElegivel,
                referenciaParticipacaoId,
                pontosParticipacao,
                empate,
                vencedoraId == duplaB.Id,
                pontosVitoria,
                pontosBonusAprovacaoPendente,
                pontosDerrota,
                pontuacaoPendente,
                partida.Id,
                confronto,
                dataPartida,
                nomeCompeticaoPartida,
                nomeCategoriaPartida);
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
            if (categoria is null)
            {
                continue;
            }

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
            var pontosBonusAprovacaoPendente = ObterBonusAprovacaoPendenteRanking(partida);
            var pontosDerrota = ObterPontosDerrotaRanking(partida);
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
                pontosBonusAprovacaoPendente,
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
                pontosBonusAprovacaoPendente,
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
                pontosBonusAprovacaoPendente,
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
                pontosBonusAprovacaoPendente,
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
            if (partidasCategoria.Key.HasValue &&
                acumuladoPorCategoria.TryGetValue(partidasCategoria.Key.Value, out var categoriaAcumulada))
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
        var competicao = categoria?.Competicao;
        if (categoria is null || competicao is null)
        {
            return;
        }

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
        if (!partida.PossuiPlacarDetalhado())
        {
            var vencedora = ObterDuplaVencedora(partida);
            var perdedora = ObterDuplaPerdedora(partida);
            if (vencedora is not null && perdedora is not null)
            {
                return $"{FormatarNomeDuplaRanking(vencedora)} venceu {FormatarNomeDuplaRanking(perdedora)} sem placar detalhado";
            }

            return $"{FormatarNomeDuplaRanking(partida.DuplaA)} x {FormatarNomeDuplaRanking(partida.DuplaB)} sem placar detalhado";
        }

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
                atleta.FotoPerfilUrl,
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
        decimal pontosBonusAprovacaoPendente,
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
            pontosBonusAprovacaoPendente,
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
        decimal pontosBonusAprovacaoPendente,
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
                item.Jogos++;
                item.Vitorias++;
                item.Pontos += pontosPartida;
                item.PontosPendentes += pontosBonusAprovacaoPendente;
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

    private sealed class DuplaRankingAcumulado(
        string id,
        RankingAtletaResumoDto atleta1,
        RankingAtletaResumoDto atleta2)
    {
        public string Id { get; } = id;
        public RankingAtletaResumoDto Atleta1 { get; } = atleta1;
        public RankingAtletaResumoDto Atleta2 { get; } = atleta2;
        public List<DuplaParticipacaoRanking> Participacoes { get; } = [];
        public string Nome => $"{ObterNomeExibicao(Atleta1)} / {ObterNomeExibicao(Atleta2)}";
        public int Jogos => Participacoes.Count;
        public int Vitorias => Participacoes.Count(x => x.Venceu);
        public int Derrotas => Participacoes.Count(x => !x.Venceu && !x.Empate);
        public decimal Aproveitamento => CalcularAproveitamento(Vitorias, Jogos);
        public decimal PontosRanking => Participacoes.Sum(x => x.PontosRanking);
        public DateTime? UltimoJogo => Participacoes.Count == 0 ? null : Participacoes.Max(x => x.DataPartida);
        public RankingSequenciaDto SequenciaAtual => CalcularSequencia(Participacoes);
        public RankingEstatisticasPlacarAcumulado EstatisticasPlacar => CalcularEstatisticasPlacar(Participacoes);
        public string? GrupoPrincipal => Participacoes
            .GroupBy(x => x.NomeGrupo)
            .OrderByDescending(x => x.Count())
            .ThenByDescending(x => x.Max(item => item.DataPartida))
            .Select(x => x.Key)
            .FirstOrDefault();

        private static string ObterNomeExibicao(RankingAtletaResumoDto atleta)
            => string.IsNullOrWhiteSpace(atleta.Apelido) ? atleta.Nome : atleta.Apelido!;
    }

    private sealed record DuplaParticipacaoRanking(
        Guid PartidaId,
        DateTime DataPartida,
        Guid? GrupoId,
        string NomeGrupo,
        bool Venceu,
        bool Empate,
        decimal PontosRanking,
        int? PontosPro,
        int? PontosContra,
        string AdversariaId,
        string NomeDuplaAdversaria,
        string Confronto)
    {
        public bool PossuiPlacar => PontosPro.HasValue && PontosContra.HasValue;
    }

    private sealed record RankingEstatisticasPlacarAcumulado(
        int JogosComPlacar,
        int PontosPro,
        int PontosContra,
        int Saldo);

    private sealed class GrupoRankingAcumulado(Grupo grupo, IReadOnlyList<Partida> partidas)
    {
        public Grupo Grupo { get; } = grupo;
        public IReadOnlyList<Partida> Partidas { get; } = partidas;
        public Guid GrupoId => Grupo.Id;
        public string Nome => Grupo.Nome;
        public string? FotoUrl => Grupo.ImagemUrl;
        public string? Cidade => Grupo.Arena?.Cidade ?? Grupo.LocalPrincipal;
        public string? Descricao => Grupo.Descricao;
        public string? Administrador => Grupo.UsuarioOrganizador?.Nome;
        public bool Publico => Grupo.Publico;
        public int QuantidadeAtletas => Grupo.Atletas.Select(x => x.AtletaId).Distinct().Count();
        public int QuantidadePartidas => Partidas.Count;
        public int AtletasAtivos => Partidas
            .SelectMany(EnumerarAtletasRanking)
            .Select(x => x.Id)
            .Distinct()
            .Count();
        public decimal PontuacaoRanking => CalcularPontuacaoGrupo(QuantidadePartidas, AtletasAtivos);
        public DateTime? UltimaPartida => Partidas.Count == 0 ? null : Partidas.Max(x => x.DataPartida ?? x.DataCriacao);
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
        string statusPendencia,
        string? fotoPerfilUrl)
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
        public string? FotoPerfilUrl { get; } = fotoPerfilUrl;
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
            StatusCadastroAtletaUtil.ObterStatusPendencia(atleta),
            FotoPerfilAtletaUtil.ObterUrlPublica(atleta));
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
