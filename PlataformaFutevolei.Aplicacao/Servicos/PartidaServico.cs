using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Interfaces.Seguranca;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Aplicacao.Mapeadores;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Aplicacao.Servicos;

public class PartidaServico(
    IPartidaRepositorio partidaRepositorio,
    ICategoriaCompeticaoRepositorio categoriaRepositorio,
    ICompeticaoRepositorio competicaoRepositorio,
    IGrupoAtletaRepositorio grupoAtletaRepositorio,
    IDuplaRepositorio duplaRepositorio,
    IInscricaoCampeonatoRepositorio inscricaoRepositorio,
    IUnidadeTrabalho unidadeTrabalho,
    IAutorizacaoUsuarioServico autorizacaoUsuarioServico,
    IResolvedorAtletaDuplaServico resolvedorAtletaDuplaServico,
    IPendenciaServico pendenciaServico
) : IPartidaServico
{
    private const string MarcadorMetadadosChave = "[[chave:";
    private const string MarcadorMetadadosRodada = "[[rodada:";
    private const string MarcadorMetadadosLados = "[[lados:";
    private const string NomeFaseFinal = "Final";
    private const string NomeFaseTerceiroLugar = "Disputa de 3º lugar";
    private const string NomeFaseFinalReset = "Final de reset";
    private const string NomeFaseChaveVencedores = "Chave dos vencedores";
    private const string NomeFaseChavePerdedores = "Chave dos perdedores";
    private const string NomeFaseEliminatoriaGrupos = "Fase eliminatória";
    private const string NomeCategoriaSemCategoria = "Sem categoria";
    private const string NomeCompeticaoPartidasAvulsas = "Partidas avulsas";
    private const string SecaoChaveVencedores = "VENCEDORES";
    private const string SecaoChavePerdedores = "PERDEDORES";
    private const string SecaoChaveFinal = "FINAL";
    private const string SecaoChaveReset = "RESET";
    private const string SecaoEliminatoriaGrupos = "ELIMINATORIA_GRUPOS";

    public async Task<IReadOnlyList<PartidaDto>> ListarPorCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default)
    {
        await ObterCompeticaoGrupoComAcessoParaConsultaAsync(competicaoId, cancellationToken);
        var partidas = await partidaRepositorio.ListarPorCompeticaoAsync(competicaoId, cancellationToken);
        return partidas.Select(x => x.ParaDto()).ToList();
    }

    public async Task<IReadOnlyList<PartidaDto>> ListarPorCategoriaAsync(Guid categoriaId, CancellationToken cancellationToken = default)
    {
        await ObterCategoriaComAcessoParaConsultaAsync(categoriaId, cancellationToken);
        var partidas = await partidaRepositorio.ListarPorCategoriaAsync(categoriaId, cancellationToken);
        return partidas.Select(x => x.ParaDto()).ToList();
    }

    public async Task<IReadOnlyList<RodadaEstruturaCompeticaoDto>> ListarEstruturaPorCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default)
    {
        await ObterCompeticaoGrupoComAcessoParaConsultaAsync(competicaoId, cancellationToken);
        var partidas = await partidaRepositorio.ListarPorCompeticaoAsync(competicaoId, cancellationToken);
        return MontarEstruturaRodadasPadrao(partidas);
    }

    public async Task<IReadOnlyList<RodadaEstruturaCompeticaoDto>> ListarEstruturaPorCategoriaAsync(Guid categoriaId, CancellationToken cancellationToken = default)
    {
        var categoria = await ObterCategoriaComAcessoParaConsultaAsync(categoriaId, cancellationToken);
        var partidas = await partidaRepositorio.ListarPorCategoriaAsync(categoriaId, cancellationToken);
        return MontarEstruturaRodadasCategoria(categoria, partidas);
    }

    public async Task<ChaveamentoCategoriaDto> ObterChaveamentoPorCategoriaAsync(Guid categoriaId, CancellationToken cancellationToken = default)
    {
        var categoria = await ObterCategoriaComAcessoParaConsultaAsync(categoriaId, cancellationToken);
        var partidas = await partidaRepositorio.ListarPorCategoriaAsync(categoriaId, cancellationToken);

        return new ChaveamentoCategoriaDto(
            categoria.Id,
            categoria.Nome,
            categoria.Competicao.PossuiFinalReset,
            partidas.Select(x => x.ParaDto()).ToList());
    }

    public async Task<IReadOnlyList<SituacaoDuplaCompeticaoDto>> ListarSituacaoDuplasPorCategoriaAsync(Guid categoriaId, CancellationToken cancellationToken = default)
    {
        var categoria = await ObterCategoriaComAcessoParaConsultaAsync(categoriaId, cancellationToken);
        if (!EhCategoriaComChaveDuplaEliminacao(categoria))
        {
            throw new RegraNegocioException("A situação das duplas está disponível apenas para categorias com chave de dupla eliminação.");
        }

        var inscricoes = await inscricaoRepositorio.ListarPorCampeonatoAsync(
            categoria.CompeticaoId,
            categoriaId,
            cancellationToken);
        var duplasInscritas = await ResolverDuplasInscritasAsync(inscricoes, cancellationToken);
        var partidasCategoria = await partidaRepositorio.ListarPorCategoriaAsync(categoriaId, cancellationToken);
        return await MontarSituacaoDuplasChaveDuplaEliminacaoAsync(categoria, duplasInscritas, partidasCategoria, cancellationToken);
    }

    public async Task<PartidaDto> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        var partida = await partidaRepositorio.ObterPorIdAsync(id, cancellationToken);
        if (partida is null)
        {
            throw new EntidadeNaoEncontradaException("Partida não encontrada.");
        }

        if (usuario.Perfil == PerfilUsuario.Atleta)
        {
            if (partida.CategoriaCompeticao.Competicao.Tipo != TipoCompeticao.Grupo)
            {
                throw new RegraNegocioException("Atletas só podem visualizar partidas de grupos.");
            }

            await GarantirAcessoAtletaAoGrupoAsync(
                usuario,
                partida.CategoriaCompeticao.CompeticaoId,
                cancellationToken);
        }
        else
        {
            await autorizacaoUsuarioServico.GarantirGestaoCompeticaoAsync(partida.CategoriaCompeticao.CompeticaoId, cancellationToken);
        }

        return partida.ParaDto();
    }

    private async Task<Competicao> ObterCompeticaoGrupoComAcessoParaConsultaAsync(Guid competicaoId, CancellationToken cancellationToken)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        var competicao = await competicaoRepositorio.ObterPorIdAsync(competicaoId, cancellationToken);
        if (competicao is null)
        {
            throw new EntidadeNaoEncontradaException("Competição não encontrada.");
        }

        if (competicao.Tipo != TipoCompeticao.Grupo)
        {
            throw new RegraNegocioException("A consulta por competição está disponível apenas para grupos.");
        }

        if (usuario.Perfil == PerfilUsuario.Atleta)
        {
            await GarantirAcessoAtletaAoGrupoAsync(usuario, competicao.Id, cancellationToken);
            return competicao;
        }

        await autorizacaoUsuarioServico.GarantirGestaoCompeticaoAsync(competicao.Id, cancellationToken);
        return competicao;
    }

    private async Task<CategoriaCompeticao> ObterCategoriaComAcessoParaConsultaAsync(Guid categoriaId, CancellationToken cancellationToken)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        var categoria = await categoriaRepositorio.ObterPorIdAsync(categoriaId, cancellationToken);
        if (categoria is null)
        {
            throw new EntidadeNaoEncontradaException("Categoria não encontrada.");
        }

        if (usuario.Perfil == PerfilUsuario.Atleta)
        {
            if (categoria.Competicao.Tipo != TipoCompeticao.Grupo)
            {
                throw new RegraNegocioException("Atletas só podem visualizar partidas de grupos.");
            }

            await GarantirAcessoAtletaAoGrupoAsync(usuario, categoria.CompeticaoId, cancellationToken);
        }
        else
        {
            await autorizacaoUsuarioServico.GarantirGestaoCompeticaoAsync(categoria.CompeticaoId, cancellationToken);
        }

        return categoria;
    }

    private async Task GarantirAcessoAtletaAoGrupoAsync(
        Usuario usuario,
        Guid competicaoId,
        CancellationToken cancellationToken)
    {
        if (!usuario.AtletaId.HasValue)
        {
            throw new RegraNegocioException("Seu usuário não possui atleta vinculado para consultar partidas do grupo.");
        }

        var grupoAtleta = await grupoAtletaRepositorio.ObterPorCompeticaoEAtletaAsync(
            competicaoId,
            usuario.AtletaId.Value,
            cancellationToken);

        if (grupoAtleta is null)
        {
            throw new RegraNegocioException("Você só pode visualizar partidas dos grupos em que participa.");
        }
    }

    public async Task<GeracaoTabelaCategoriaDto> GerarTabelaCategoriaAsync(
        Guid categoriaId,
        GerarTabelaCategoriaDto dto,
        CancellationToken cancellationToken = default)
    {
        var categoria = await categoriaRepositorio.ObterPorIdAsync(categoriaId, cancellationToken);
        if (categoria is null)
        {
            throw new EntidadeNaoEncontradaException("Categoria não encontrada.");
        }

        await GarantirEdicaoPartidasAsync(categoria.Competicao, cancellationToken);

        if (categoria.Competicao.Tipo == TipoCompeticao.Grupo)
        {
            throw new RegraNegocioException("O sorteio automático de jogos está disponível apenas para categorias de campeonato ou evento.");
        }

        var formatoCategoria = ObterFormatoCampeonatoEfetivo(categoria);
        if (categoria.Competicao.InscricoesAbertas && !categoria.InscricoesEncerradas)
        {
            throw new RegraNegocioException("Feche as inscrições da competição ou encerre as inscrições desta categoria antes de gerar o chaveamento.");
        }

        var partidasExistentes = await partidaRepositorio.ListarPorCategoriaAsync(categoriaId, cancellationToken);
        ValidarTabelaPodeSerSubstituida(partidasExistentes);

        if (partidasExistentes.Count > 0 && !dto.SubstituirTabelaExistente)
        {
            throw new RegraNegocioException("A categoria já possui uma tabela de jogos gerada. Use a substituição para gerar novamente.");
        }

        if (partidasExistentes.Count > 0)
        {
            await RemoverPartidasCategoriaAsync(partidasExistentes, cancellationToken);
        }

        categoria.LimparAprovacaoTabelaJogos();

        var inscricoes = await inscricaoRepositorio.ListarPorCampeonatoAsync(
            categoria.CompeticaoId,
            categoriaId,
            cancellationToken);

        var duplasInscritas = await ResolverDuplasInscritasAsync(inscricoes, cancellationToken);
        if (duplasInscritas.Count < 4)
        {
            throw new RegraNegocioException("A categoria precisa ter ao menos quatro duplas inscritas para sortear os jogos.");
        }

        var partidasGeradas = GerarPartidasCategoria(categoria, duplasInscritas);
        foreach (var partida in partidasGeradas)
        {
            await partidaRepositorio.AdicionarAsync(partida, cancellationToken);
        }

        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        var partidasAtualizadas = await partidaRepositorio.ListarPorCategoriaAsync(categoriaId, cancellationToken);
        return new GeracaoTabelaCategoriaDto(
            categoria.Id,
            categoria.Nome,
            partidasGeradas.Count,
            partidasExistentes.Count > 0,
            MontarResumoGeracao(categoria, formatoCategoria, duplasInscritas.Count, partidasGeradas),
            partidasAtualizadas.Select(x => x.ParaDto()).ToList());
    }

    public async Task<RemocaoTabelaCategoriaDto> RemoverTabelaCategoriaAsync(
        Guid categoriaId,
        CancellationToken cancellationToken = default)
    {
        var categoria = await categoriaRepositorio.ObterPorIdAsync(categoriaId, cancellationToken);
        if (categoria is null)
        {
            throw new EntidadeNaoEncontradaException("Categoria não encontrada.");
        }

        await GarantirEdicaoPartidasAsync(categoria.Competicao, cancellationToken);

        if (categoria.Competicao.Tipo == TipoCompeticao.Grupo)
        {
            throw new RegraNegocioException("A exclusão em lote dos jogos está disponível apenas para categorias de campeonato ou evento.");
        }

        var partidasExistentes = await partidaRepositorio.ListarPorCategoriaAsync(categoriaId, cancellationToken);
        if (partidasExistentes.Count == 0)
        {
            throw new RegraNegocioException("A categoria não possui jogos cadastrados para excluir.");
        }

        ValidarTabelaPodeSerSubstituida(partidasExistentes);
        await RemoverPartidasCategoriaAsync(partidasExistentes, cancellationToken);
        categoria.LimparAprovacaoTabelaJogos();
        categoriaRepositorio.Atualizar(categoria);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        return new RemocaoTabelaCategoriaDto(
            categoria.Id,
            categoria.Nome,
            partidasExistentes.Count,
            $"Tabela removida com {partidasExistentes.Count} jogo(s) excluído(s) da categoria {categoria.Nome}.");
    }

    public async Task<PartidaDto> CriarAsync(CriarPartidaDto dto, CancellationToken cancellationToken = default)
    {
        var usuarioAtual = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        var (categoria, duplaA, duplaB, metadadosLados) = await ValidarRelacionamentosAsync(
            dto.CompeticaoId,
            dto.NomeGrupo,
            dto.CategoriaCompeticaoId,
            dto.DuplaAId,
            dto.DuplaBId,
            dto.DuplaAAtleta1Id,
            dto.DuplaAAtleta1Nome,
            dto.DuplaAAtleta2Id,
            dto.DuplaAAtleta2Nome,
            dto.DuplaBAtleta1Id,
            dto.DuplaBAtleta1Nome,
            dto.DuplaBAtleta2Id,
            dto.DuplaBAtleta2Nome,
            null,
            cancellationToken
        );

        var partida = new Partida
        {
            CategoriaCompeticaoId = categoria.Id,
            CategoriaCompeticao = categoria,
            CriadoPorUsuarioId = usuarioAtual.Id,
            CriadoPorUsuario = usuarioAtual,
            DuplaAId = duplaA.Id,
            DuplaA = duplaA,
            DuplaBId = duplaB.Id,
            DuplaB = duplaB,
            FaseCampeonato = NormalizarFaseCampeonato(dto.FaseCampeonato),
            Status = dto.Status,
            DataPartida = dto.DataPartida.HasValue ? NormalizarParaUtc(dto.DataPartida.Value) : DateTime.UtcNow,
            Observacoes = MontarObservacoesPartida(dto.Observacoes?.Trim(), null, null, metadadosLados)
        };

        ValidarTabelaAprovadaParaResultado(categoria, dto.Status);
        AplicarStatusEResultado(partida, dto.Status, dto.PlacarDuplaA, dto.PlacarDuplaB, dataAtualPadraoUtc: DateTime.UtcNow);
        AtualizarNavegacoesPartida(partida, categoria, duplaA, duplaB, usuarioAtual);
        ValidarPartida(partida, categoria.Competicao);

        await partidaRepositorio.AdicionarAsync(partida, cancellationToken);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
        var partidaPersistida = await partidaRepositorio.ObterPorIdAsync(partida.Id, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Partida não encontrada após o cadastro.");
        await pendenciaServico.InicializarFluxoPartidaAsync(partidaPersistida, usuarioAtual.Id, cancellationToken);
        await ProcessarAvancoChaveAsync(categoria, cancellationToken);
        await ProcessarAvancoRodadasAsync(categoria, cancellationToken);
        var partidaCriada = await partidaRepositorio.ObterPorIdAsync(partida.Id, cancellationToken);
        return partidaCriada!.ParaDto();
    }

    public async Task<PartidaDto> AtualizarAsync(Guid id, AtualizarPartidaDto dto, CancellationToken cancellationToken = default)
    {
        var partida = await partidaRepositorio.ObterPorIdAsync(id, cancellationToken);
        if (partida is null)
        {
            throw new EntidadeNaoEncontradaException("Partida não encontrada.");
        }

        await GarantirEdicaoPartidasAsync(partida.CategoriaCompeticao.Competicao, cancellationToken);
        var categoriaOriginalId = partida.CategoriaCompeticaoId;
        var duplaOriginalAId = partida.DuplaAId;
        var duplaOriginalBId = partida.DuplaBId;
        var faseOriginal = NormalizarFaseCampeonato(partida.FaseCampeonato);
        var statusOriginal = partida.Status;
        var placarOriginalA = partida.PlacarDuplaA;
        var placarOriginalB = partida.PlacarDuplaB;
        var vencedorOriginalId = partida.DuplaVencedoraId;
        var metadadosChave = ExtrairMetadadosChave(partida.Observacoes);
        var metadadosRodada = ExtrairMetadadosRodada(partida.Observacoes);
        var metadadosLados = ExtrairMetadadosLados(partida.Observacoes);

        var (categoria, duplaA, duplaB, metadadosLadosAtualizados) = await ValidarRelacionamentosAsync(
            dto.CompeticaoId,
            dto.NomeGrupo,
            dto.CategoriaCompeticaoId,
            dto.DuplaAId,
            dto.DuplaBId,
            dto.DuplaAAtleta1Id,
            dto.DuplaAAtleta1Nome,
            dto.DuplaAAtleta2Id,
            dto.DuplaAAtleta2Nome,
            dto.DuplaBAtleta1Id,
            dto.DuplaBAtleta1Nome,
            dto.DuplaBAtleta2Id,
            dto.DuplaBAtleta2Nome,
            metadadosLados,
            cancellationToken
        );

        var faseAtualizada = NormalizarFaseCampeonato(dto.FaseCampeonato);
        await ValidarEdicaoPartidaGerenciadaPorChaveDuplaEliminacaoAsync(
            partida,
            categoria,
            categoriaOriginalId,
            duplaOriginalAId,
            duplaOriginalBId,
            faseOriginal,
            statusOriginal,
            placarOriginalA,
            placarOriginalB,
            vencedorOriginalId,
            faseAtualizada,
            dto.Status,
            dto.PlacarDuplaA,
            dto.PlacarDuplaB,
            duplaA,
            duplaB,
            metadadosChave,
            cancellationToken);

        partida.CategoriaCompeticaoId = categoria.Id;
        partida.DuplaAId = duplaA.Id;
        partida.DuplaBId = duplaB.Id;
        partida.CriadoPorUsuarioId ??= (await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken)).Id;
        partida.FaseCampeonato = faseAtualizada;
        partida.Status = dto.Status;
        partida.DataPartida = dto.DataPartida.HasValue ? NormalizarParaUtc(dto.DataPartida.Value) : DateTime.UtcNow;
        partida.Observacoes = dto.Observacoes?.Trim();
        ValidarTabelaAprovadaParaResultado(categoria, dto.Status);
        AplicarStatusEResultado(partida, dto.Status, dto.PlacarDuplaA, dto.PlacarDuplaB, partida.DataPartida ?? DateTime.UtcNow);
        AtualizarNavegacoesPartida(partida, categoria, duplaA, duplaB);
        ValidarPartida(partida, categoria.Competicao);
        partida.AtualizarDataModificacao();
        partida.Observacoes = MontarObservacoesPartida(dto.Observacoes?.Trim(), metadadosChave, metadadosRodada, metadadosLadosAtualizados);

        partidaRepositorio.Atualizar(partida);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
        await pendenciaServico.InicializarFluxoPartidaAsync(
            partida,
            partida.CriadoPorUsuarioId ?? Guid.Empty,
            cancellationToken);
        await ProcessarAvancoChaveAsync(categoria, cancellationToken);
        await ProcessarAvancoRodadasAsync(categoria, cancellationToken);
        var partidaAtualizada = await partidaRepositorio.ObterPorIdAsync(id, cancellationToken);
        return partidaAtualizada!.ParaDto();
    }

    public async Task RemoverAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var partida = await partidaRepositorio.ObterPorIdAsync(id, cancellationToken);
        if (partida is null)
        {
            throw new EntidadeNaoEncontradaException("Partida não encontrada.");
        }

        await GarantirEdicaoPartidasAsync(partida.CategoriaCompeticao.Competicao, cancellationToken);

        partidaRepositorio.Remover(partida);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
    }

    private async Task RemoverPartidasCategoriaAsync(
        IReadOnlyList<Partida> partidasExistentes,
        CancellationToken cancellationToken)
    {
        foreach (var partidaExistente in partidasExistentes)
        {
            partidaRepositorio.Remover(partidaExistente);
        }

        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
    }

    private static void ValidarTabelaPodeSerSubstituida(IReadOnlyList<Partida> partidasExistentes)
    {
        if (partidasExistentes.Any(x => x.Status == StatusPartida.Encerrada))
        {
            throw new RegraNegocioException("A categoria já possui partidas encerradas. Remova ou ajuste a tabela manualmente antes de gerar novamente.");
        }
    }

    private static void ValidarTabelaAprovadaParaResultado(CategoriaCompeticao categoria, StatusPartida status)
    {
        if (categoria.Competicao.Tipo == TipoCompeticao.Grupo || status != StatusPartida.Encerrada)
        {
            return;
        }

        if (!categoria.TabelaJogosAprovada)
        {
            throw new RegraNegocioException("A tabela de jogos precisa ser aprovada antes de preencher resultados.");
        }
    }

    private static DateTime NormalizarParaUtc(DateTime data)
    {
        return data.Kind switch
        {
            DateTimeKind.Utc => data,
            DateTimeKind.Local => data.ToUniversalTime(),
            _ => DateTime.SpecifyKind(data, DateTimeKind.Utc)
        };
    }

    private async Task<(CategoriaCompeticao categoria, Dupla duplaA, Dupla duplaB, MetadadosLados? metadadosLados)> ValidarRelacionamentosAsync(
        Guid? competicaoId,
        string? nomeGrupo,
        Guid? categoriaCompeticaoId,
        Guid? duplaAId,
        Guid? duplaBId,
        Guid? duplaAAtleta1Id,
        string? duplaAAtleta1Nome,
        Guid? duplaAAtleta2Id,
        string? duplaAAtleta2Nome,
        Guid? duplaBAtleta1Id,
        string? duplaBAtleta1Nome,
        Guid? duplaBAtleta2Id,
        string? duplaBAtleta2Nome,
        MetadadosLados? metadadosLadosExistentes,
        CancellationToken cancellationToken
    )
    {
        CategoriaCompeticao categoria;
        if (categoriaCompeticaoId.HasValue)
        {
            categoria = await categoriaRepositorio.ObterPorIdAsync(categoriaCompeticaoId.Value, cancellationToken)
                ?? throw new RegraNegocioException("Categoria não encontrada.");

            if (competicaoId.HasValue && categoria.CompeticaoId != competicaoId.Value)
            {
                throw new RegraNegocioException("A categoria informada não pertence à competição selecionada.");
            }
        }
        else
        {
            var competicaoExistente = competicaoId.HasValue && competicaoId.Value != Guid.Empty
                ? await competicaoRepositorio.ObterPorIdAsync(competicaoId.Value, cancellationToken)
                : null;
            var competicao = competicaoExistente
                ?? await ObterOuCriarCompeticaoPartidasAvulsasAsync(nomeGrupo, cancellationToken);

            if (competicao.Tipo != TipoCompeticao.Grupo)
            {
                throw new RegraNegocioException("Toda partida de campeonato ou evento deve pertencer a uma categoria.");
            }

            categoria = await ObterOuCriarCategoriaSemCategoriaGrupoAsync(competicao, cancellationToken);
        }

        await GarantirEdicaoPartidasAsync(categoria.Competicao, cancellationToken);

        Dupla duplaA;
        Dupla duplaB;
        MetadadosLados? metadadosLados = metadadosLadosExistentes;
        if (categoria.Competicao.Tipo == TipoCompeticao.Grupo)
        {
            var usuarioAtual = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
            var atletaDuplaA1 = await ResolverAtletaPartidaGrupoAsync(duplaAAtleta1Id, duplaAAtleta1Nome, cancellationToken);
            var atletaDuplaA2 = await ResolverAtletaPartidaGrupoAsync(duplaAAtleta2Id, duplaAAtleta2Nome, cancellationToken);
            var atletaDuplaB1 = await ResolverAtletaPartidaGrupoAsync(duplaBAtleta1Id, duplaBAtleta1Nome, cancellationToken);
            var atletaDuplaB2 = await ResolverAtletaPartidaGrupoAsync(duplaBAtleta2Id, duplaBAtleta2Nome, cancellationToken);

            ValidarAtletaUsuarioNaPartidaGrupo(usuarioAtual, atletaDuplaA1, atletaDuplaA2);
            ValidarAtletasGrupo(atletaDuplaA1, atletaDuplaA2, atletaDuplaB1, atletaDuplaB2);

            foreach (var atleta in new[] { atletaDuplaA1, atletaDuplaA2, atletaDuplaB1, atletaDuplaB2 }.DistinctBy(x => x.Id))
            {
                await resolvedorAtletaDuplaServico.GarantirAtletaNoGrupoAsync(categoria.CompeticaoId, atleta, cancellationToken);
            }

            duplaA = await resolvedorAtletaDuplaServico.ObterOuCriarDuplaAsync(atletaDuplaA1, atletaDuplaA2, cancellationToken);
            duplaB = await resolvedorAtletaDuplaServico.ObterOuCriarDuplaAsync(atletaDuplaB1, atletaDuplaB2, cancellationToken);
            metadadosLados = new MetadadosLados(
                atletaDuplaA1.Id,
                atletaDuplaA2.Id,
                atletaDuplaB1.Id,
                atletaDuplaB2.Id);
        }
        else
        {
            duplaA = await ResolverDuplaPartidaCategoriaAsync(
                duplaAId,
                duplaAAtleta1Id,
                duplaAAtleta1Nome,
                duplaAAtleta2Id,
                duplaAAtleta2Nome,
                cancellationToken);
            duplaB = await ResolverDuplaPartidaCategoriaAsync(
                duplaBId,
                duplaBAtleta1Id,
                duplaBAtleta1Nome,
                duplaBAtleta2Id,
                duplaBAtleta2Nome,
                cancellationToken);

            if (categoria.Competicao.Tipo is TipoCompeticao.Campeonato or TipoCompeticao.Evento)
            {
                await ValidarInscricaoCompeticaoAsync(categoria.Id, duplaA, cancellationToken);
                await ValidarInscricaoCompeticaoAsync(categoria.Id, duplaB, cancellationToken);
            }
        }

        if (duplaA.Id == duplaB.Id)
        {
            throw new RegraNegocioException("Uma partida não pode ter a mesma dupla em ambos os lados.");
        }

        ValidarAtletasDuplicadosNaPartida(duplaA, duplaB);

        return (categoria, duplaA, duplaB, metadadosLados);
    }

    private async Task<CategoriaCompeticao> ObterOuCriarCategoriaSemCategoriaGrupoAsync(
        Competicao competicao,
        CancellationToken cancellationToken)
    {
        var categorias = await categoriaRepositorio.ListarPorCompeticaoAsync(competicao.Id, cancellationToken);
        var categoriaExistente = categorias.FirstOrDefault(categoria =>
            string.Equals(categoria.Nome, NomeCategoriaSemCategoria, StringComparison.OrdinalIgnoreCase));
        if (categoriaExistente is not null)
        {
            return await categoriaRepositorio.ObterPorIdAsync(categoriaExistente.Id, cancellationToken)
                ?? categoriaExistente;
        }

        var categoria = new CategoriaCompeticao
        {
            CompeticaoId = competicao.Id,
            Competicao = competicao,
            Nome = NomeCategoriaSemCategoria,
            Genero = GeneroCategoria.Misto,
            Nivel = NivelCategoria.Livre,
            PesoRanking = 1m
        };

        await categoriaRepositorio.AdicionarAsync(categoria, cancellationToken);
        return categoria;
    }

    private async Task<Competicao> ObterOuCriarCompeticaoPartidasAvulsasAsync(
        string? nomeGrupo,
        CancellationToken cancellationToken)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        var usuarioOrganizadorId = usuario.Perfil is PerfilUsuario.Organizador or PerfilUsuario.Atleta
            ? (Guid?)usuario.Id
            : null;
        var nomeCompeticao = string.IsNullOrWhiteSpace(nomeGrupo)
            ? NomeCompeticaoPartidasAvulsas
            : nomeGrupo.Trim();

        var competicaoExistente = (await competicaoRepositorio.ListarAsync(cancellationToken))
            .FirstOrDefault(competicao =>
                competicao.Tipo == TipoCompeticao.Grupo &&
                competicao.UsuarioOrganizadorId == usuarioOrganizadorId &&
                string.Equals(competicao.Nome, nomeCompeticao, StringComparison.OrdinalIgnoreCase));

        if (competicaoExistente is not null)
        {
            return await competicaoRepositorio.ObterPorIdAsync(competicaoExistente.Id, cancellationToken)
                ?? competicaoExistente;
        }

        var competicao = new Competicao
        {
            Nome = nomeCompeticao,
            Tipo = TipoCompeticao.Grupo,
            Descricao = string.Equals(nomeCompeticao, NomeCompeticaoPartidasAvulsas, StringComparison.OrdinalIgnoreCase)
                ? "Criada automaticamente para lançamento de partidas sem contexto prévio."
                : "Criada automaticamente a partir do registro rápido de partidas.",
            DataInicio = DateTime.UtcNow,
            UsuarioOrganizadorId = usuarioOrganizadorId,
            ContaRankingLiga = false,
            InscricoesAbertas = false,
            PossuiFinalReset = false
        };

        await competicaoRepositorio.AdicionarAsync(competicao, cancellationToken);
        return competicao;
    }

    private async Task ValidarInscricaoCompeticaoAsync(
        Guid categoriaId,
        Dupla dupla,
        CancellationToken cancellationToken)
    {
        var inscricao = await inscricaoRepositorio.ObterDuplicadaAsync(
            categoriaId,
            dupla.Id,
            cancellationToken);

        if (inscricao is null)
        {
            throw new RegraNegocioException($"A dupla {dupla.Nome} precisa estar inscrita nesta categoria da competição.");
        }

        if (inscricao.Status != StatusInscricaoCampeonato.Ativa)
        {
            throw new RegraNegocioException($"A inscrição da dupla {dupla.Nome} precisa estar aprovada para registrar partidas nesta categoria.");
        }
    }

    private async Task<Atleta> ResolverAtletaPartidaGrupoAsync(
        Guid? atletaId,
        string? nomeCompleto,
        CancellationToken cancellationToken)
    {
        return await resolvedorAtletaDuplaServico.ResolverAtletaAsync(
            atletaId,
            nomeCompleto,
            null,
            "Informe os quatro atletas da partida do grupo.",
            true,
            cancellationToken);
    }

    private async Task<Dupla> ResolverDuplaPartidaCategoriaAsync(
        Guid? duplaId,
        Guid? atleta1Id,
        string? atleta1Nome,
        Guid? atleta2Id,
        string? atleta2Nome,
        CancellationToken cancellationToken)
    {
        if (duplaId.HasValue && duplaId.Value != Guid.Empty)
        {
            return await duplaRepositorio.ObterPorIdAsync(duplaId.Value, cancellationToken)
                ?? throw new RegraNegocioException("As duplas da partida devem estar cadastradas.");
        }

        var atleta1 = await resolvedorAtletaDuplaServico.ResolverAtletaAsync(
            atleta1Id,
            atleta1Nome,
            null,
            "Informe os dois atletas de cada dupla ou selecione uma dupla já inscrita.",
            true,
            cancellationToken);
        var atleta2 = await resolvedorAtletaDuplaServico.ResolverAtletaAsync(
            atleta2Id,
            atleta2Nome,
            null,
            "Informe os dois atletas de cada dupla ou selecione uma dupla já inscrita.",
            true,
            cancellationToken);

        return await resolvedorAtletaDuplaServico.ObterOuCriarDuplaAsync(atleta1, atleta2, cancellationToken);
    }

    private static void ValidarAtletasGrupo(Atleta atletaDuplaA1, Atleta atletaDuplaA2, Atleta atletaDuplaB1, Atleta atletaDuplaB2)
    {
        if (atletaDuplaA1.Id == atletaDuplaA2.Id || atletaDuplaB1.Id == atletaDuplaB2.Id)
        {
            throw new RegraNegocioException("Uma dupla não pode ter o mesmo atleta duas vezes.");
        }

        if (atletaDuplaA1.Id == atletaDuplaB1.Id ||
            atletaDuplaA1.Id == atletaDuplaB2.Id ||
            atletaDuplaA2.Id == atletaDuplaB1.Id ||
            atletaDuplaA2.Id == atletaDuplaB2.Id)
        {
            throw new RegraNegocioException("Um mesmo atleta não pode jogar pelos dois lados da partida.");
        }
    }

    private static void ValidarAtletaUsuarioNaPartidaGrupo(Usuario usuario, Atleta atletaDuplaA1, Atleta atletaDuplaA2)
    {
        if (usuario.Perfil != PerfilUsuario.Atleta)
        {
            return;
        }

        if (!usuario.AtletaId.HasValue)
        {
            throw new RegraNegocioException("Você precisa ter um atleta vinculado para registrar partidas no grupo.");
        }

        if (usuario.AtletaId.Value != atletaDuplaA1.Id && usuario.AtletaId.Value != atletaDuplaA2.Id)
        {
            throw new RegraNegocioException("O atleta vinculado ao seu usuário precisa compor a primeira dupla.");
        }
    }

    private static void ValidarAtletasDuplicadosNaPartida(Dupla duplaA, Dupla duplaB)
    {
        if (duplaA.Atleta1Id == duplaB.Atleta1Id ||
            duplaA.Atleta1Id == duplaB.Atleta2Id ||
            duplaA.Atleta2Id == duplaB.Atleta1Id ||
            duplaA.Atleta2Id == duplaB.Atleta2Id)
        {
            throw new RegraNegocioException("Um mesmo atleta não pode jogar pelos dois lados da partida.");
        }
    }

    private static void AplicarStatusEResultado(
        Partida partida,
        StatusPartida status,
        int? placarDuplaA,
        int? placarDuplaB,
        DateTime dataAtualPadraoUtc)
    {
        if (status == StatusPartida.Agendada)
        {
            partida.PlacarDuplaA = 0;
            partida.PlacarDuplaB = 0;
            partida.DuplaVencedoraId = null;
            return;
        }

        if (!partida.Ativa || !partida.PossuiParticipantesDefinidos())
        {
            throw new RegraNegocioException("A partida ainda não possui as duas duplas definidas para receber resultado.");
        }

        if (!placarDuplaA.HasValue || !placarDuplaB.HasValue)
        {
            throw new RegraNegocioException("Informe o placar das duas duplas para encerrar a partida.");
        }

        partida.PlacarDuplaA = placarDuplaA.Value;
        partida.PlacarDuplaB = placarDuplaB.Value;
        partida.DuplaVencedoraId = partida.ObterDuplaVencedoraPorPlacar();
        partida.DataPartida ??= dataAtualPadraoUtc;
    }

    private static void AtualizarNavegacoesPartida(
        Partida partida,
        CategoriaCompeticao categoria,
        Dupla duplaA,
        Dupla duplaB,
        Usuario? usuarioCriador = null)
    {
        partida.CategoriaCompeticao = categoria;
        partida.DuplaA = duplaA;
        partida.DuplaB = duplaB;

        if (usuarioCriador is not null)
        {
            partida.CriadoPorUsuario = usuarioCriador;
        }

        partida.DuplaVencedora = partida.DuplaVencedoraId switch
        {
            var id when id == duplaA.Id => duplaA,
            var id when id == duplaB.Id => duplaB,
            _ => null
        };
    }

    private static void ValidarPartida(Partida partida, Competicao competicao)
    {
        var exigeFaseCampeonato = ExigeFaseCampeonato(partida, competicao);
        if (exigeFaseCampeonato && string.IsNullOrWhiteSpace(partida.FaseCampeonato))
        {
            throw new RegraNegocioException("Informe a fase da partida para jogos de campeonato ou evento com formato vinculado.");
        }

        if (!exigeFaseCampeonato && !string.IsNullOrWhiteSpace(partida.FaseCampeonato))
        {
            throw new RegraNegocioException("Fase da partida só deve ser informada para jogos de campeonato ou evento com formato vinculado.");
        }

        if (partida.Status == StatusPartida.Encerrada && !partida.PossuiParticipantesDefinidos())
        {
            throw new RegraNegocioException("A partida precisa ter as duas duplas definidas antes do encerramento.");
        }

        if (!partida.Ativa && partida.Status == StatusPartida.Encerrada)
        {
            throw new RegraNegocioException("A partida ainda não está ativa no chaveamento para receber resultado.");
        }

        if (partida.Status == StatusPartida.Agendada)
        {
            if (partida.DuplaVencedoraId.HasValue)
            {
                throw new RegraNegocioException("Partidas agendadas não devem informar dupla vencedora.");
            }

            if (!partida.PossuiParticipantesDefinidos() && !partida.LadoDaChave.HasValue)
            {
                throw new RegraNegocioException("Informe as duas duplas da partida.");
            }

            return;
        }

        if (partida.PlacarDuplaA < 0 || partida.PlacarDuplaB < 0)
        {
            throw new RegraNegocioException("Placar não pode ser negativo.");
        }

        if (!partida.DataPartida.HasValue)
        {
            throw new RegraNegocioException("Informe a data da partida encerrada.");
        }

        if (partida.TerminouEmpatada())
        {
            if (!competicao.ObterPermiteEmpate())
            {
                throw new RegraNegocioException("A partida não pode terminar empatada.");
            }

            if (partida.DuplaVencedoraId.HasValue)
            {
                throw new RegraNegocioException("Partidas empatadas não devem informar dupla vencedora.");
            }

            if (partida.ObterMaiorPlacar() < competicao.ObterPontosMinimosPartida())
            {
                throw new RegraNegocioException($"Em caso de empate, a partida deve atingir no mínimo {competicao.ObterPontosMinimosPartida()} pontos.");
            }

            return;
        }

        if (partida.ObterMaiorPlacar() < competicao.ObterPontosMinimosPartida())
        {
            throw new RegraNegocioException($"A dupla vencedora deve alcançar no mínimo {competicao.ObterPontosMinimosPartida()} pontos.");
        }

        if (partida.ObterDiferencaPlacar() < competicao.ObterDiferencaMinimaPartida())
        {
            throw new RegraNegocioException($"A partida deve terminar com diferença mínima de {competicao.ObterDiferencaMinimaPartida()} pontos.");
        }

        if (partida.ObterDuplaVencedoraPorPlacar() != partida.DuplaVencedoraId)
        {
            throw new RegraNegocioException("A dupla vencedora deve ser coerente com o placar informado.");
        }
    }

    private static bool ExigeFaseCampeonato(Partida partida, Competicao competicao)
    {
        if (competicao.Tipo == TipoCompeticao.Campeonato)
        {
            return true;
        }

        return competicao.Tipo == TipoCompeticao.Evento &&
               partida.CategoriaCompeticao is not null &&
               ObterFormatoCampeonatoEfetivo(partida.CategoriaCompeticao) is not null;
    }

    private async Task<IReadOnlyList<Dupla>> ResolverDuplasInscritasAsync(
        IReadOnlyList<InscricaoCampeonato> inscricoes,
        CancellationToken cancellationToken)
    {
        var duplas = new List<Dupla>();

        foreach (var inscricao in inscricoes.Where(x => x.Status == StatusInscricaoCampeonato.Ativa))
        {
            var dupla = await duplaRepositorio.ObterPorIdAsync(inscricao.DuplaId, cancellationToken);

            if (dupla is null)
            {
                throw new RegraNegocioException($"A dupla da inscrição {inscricao.Dupla?.Nome ?? inscricao.DuplaId.ToString()} não foi encontrada no cadastro.");
            }

            if (duplas.All(x => x.Id != dupla.Id))
            {
                duplas.Add(dupla);
            }
        }

        return duplas;
    }

    private static List<Partida> GerarPartidasCategoria(CategoriaCompeticao categoria, IReadOnlyList<Dupla> duplasInscritas)
    {
        var duplasSorteadas = duplasInscritas
            .OrderBy(_ => Random.Shared.Next())
            .ToList();

        var formato = ObterFormatoCampeonatoEfetivo(categoria);
        if (categoria.Competicao.Tipo != TipoCompeticao.Grupo && formato is not null)
        {
            if (!formato.Ativo)
            {
                throw new RegraNegocioException("O formato vinculado à categoria ou à competição está inativo.");
            }

            return formato.TipoFormato switch
            {
                TipoFormatoCampeonato.PontosCorridos => GerarPartidasPontosCorridos(categoria, duplasSorteadas, formato.TurnoEVolta),
                TipoFormatoCampeonato.FaseDeGrupos => GerarPartidasFaseDeGrupos(categoria, duplasSorteadas, formato),
                TipoFormatoCampeonato.Chave => GerarPartidasChave(categoria, duplasSorteadas, formato),
                _ => throw new RegraNegocioException("O formato da categoria é inválido para geração da tabela.")
            };
        }

        return GerarPrimeiraRodadaRoundRobin(categoria, duplasSorteadas, null, false);
    }

    private static List<Partida> GerarPartidasPontosCorridos(
        CategoriaCompeticao categoria,
        IReadOnlyList<Dupla> duplas,
        bool turnoEVolta)
    {
        return GerarPrimeiraRodadaRoundRobin(categoria, duplas, "Fase classificatória", turnoEVolta);
    }

    private static List<Partida> GerarPartidasFaseDeGrupos(
        CategoriaCompeticao categoria,
        IReadOnlyList<Dupla> duplas,
        FormatoCampeonato formato)
    {
        var quantidadeGrupos = formato.QuantidadeGrupos
            ?? throw new RegraNegocioException("O formato em fase de grupos precisa informar a quantidade de grupos.");

        if (quantidadeGrupos <= 0)
        {
            throw new RegraNegocioException("Quantidade de grupos inválida para gerar a tabela.");
        }

        if (duplas.Count < quantidadeGrupos * 2)
        {
            throw new RegraNegocioException("É necessário ter ao menos duas duplas por grupo para gerar a fase de grupos.");
        }

        var grupos = DistribuirDuplasEmGrupos(duplas, quantidadeGrupos);
        var partidas = new List<Partida>();

        for (var indiceGrupo = 0; indiceGrupo < grupos.Count; indiceGrupo++)
        {
            var nomeGrupo = $"Grupo {(char)('A' + indiceGrupo)}";
            partidas.AddRange(GerarPrimeiraRodadaRoundRobin(categoria, grupos[indiceGrupo], nomeGrupo, formato.TurnoEVolta));
        }

        return partidas;
    }

    private static List<Partida> GerarPartidasChave(
        CategoriaCompeticao categoria,
        IReadOnlyList<Dupla> duplas,
        FormatoCampeonato formato)
    {
        if (formato.QuantidadeDerrotasParaEliminacao == 2)
        {
            return GerarPartidasChaveDuplaEliminacao(categoria, duplas);
        }

        var (chaveA, chaveB) = DistribuirDuplasEmDuasChaves(duplas);

        if (chaveA.Count < 2 || chaveB.Count < 2)
        {
            throw new RegraNegocioException("A chave precisa de ao menos duas duplas em cada lado para gerar os jogos iniciais.");
        }

        var partidas = new List<Partida>();
        partidas.AddRange(GerarPartidasRodadaChave(categoria, chaveA, "A", 1));
        partidas.AddRange(GerarPartidasRodadaChave(categoria, chaveB, "B", 1));
        return partidas;
    }

    private static List<Partida> GerarPartidasChaveDuplaEliminacao(
        CategoriaCompeticao categoria,
        IReadOnlyList<Dupla> duplas)
    {
        return GerarChaveDuplaEliminacaoCompleta(categoria, duplas);
    }

    private static (List<Dupla> chaveA, List<Dupla> chaveB) DistribuirDuplasEmDuasChaves(IReadOnlyList<Dupla> duplas)
    {
        var metadeSuperior = (int)Math.Ceiling(duplas.Count / 2d);
        var chaveA = duplas.Take(metadeSuperior).ToList();
        var chaveB = duplas.Skip(metadeSuperior).ToList();
        return (chaveA, chaveB);
    }

    private static List<List<Dupla>> DistribuirDuplasEmGrupos(IReadOnlyList<Dupla> duplas, int quantidadeGrupos)
    {
        var grupos = Enumerable.Range(0, quantidadeGrupos)
            .Select(_ => new List<Dupla>())
            .ToList();

        foreach (var item in duplas.Select((dupla, indice) => new { dupla, indice }))
        {
            var ciclo = item.indice / quantidadeGrupos;
            var deslocamento = item.indice % quantidadeGrupos;
            var indiceGrupo = ciclo % 2 == 0
                ? deslocamento
                : quantidadeGrupos - 1 - deslocamento;

            grupos[indiceGrupo].Add(item.dupla);
        }

        return grupos;
    }

    private static List<Partida> GerarPrimeiraRodadaRoundRobin(
        CategoriaCompeticao categoria,
        IReadOnlyList<Dupla> duplas,
        string? nomeFaseBase,
        bool turnoEVolta)
    {
        var ordemDuplas = duplas.Select(x => x.Id).ToList();
        var rodadasBase = GerarRodadasRoundRobin(duplas);
        return rodadasBase.Count == 0
            ? []
            : CriarPartidasDaRodadaRoundRobin(categoria, rodadasBase, 1, nomeFaseBase, turnoEVolta, ordemDuplas);
    }

    private static List<RodadaRoundRobin> GerarRodadasRoundRobin(IReadOnlyList<Dupla> duplas)
    {
        var trabalho = duplas.ToList();
        var usaFolga = trabalho.Count % 2 != 0;
        if (usaFolga)
        {
            trabalho.Add(null!);
        }

        var quantidadeEquipes = trabalho.Count;
        var quantidadeRodadas = quantidadeEquipes - 1;
        var jogosPorRodada = quantidadeEquipes / 2;
        var rodadas = new List<RodadaRoundRobin>();

        for (var numeroRodada = 1; numeroRodada <= quantidadeRodadas; numeroRodada++)
        {
            var confrontos = new List<ConfrontoRoundRobin>();

            for (var indiceJogo = 0; indiceJogo < jogosPorRodada; indiceJogo++)
            {
                var duplaA = trabalho[indiceJogo];
                var duplaB = trabalho[quantidadeEquipes - 1 - indiceJogo];

                if (duplaA is null || duplaB is null)
                {
                    continue;
                }

                confrontos.Add(new ConfrontoRoundRobin(duplaA, duplaB));
            }

            rodadas.Add(new RodadaRoundRobin(numeroRodada, confrontos));

            var ultimo = trabalho[^1];
            for (var indice = quantidadeEquipes - 1; indice > 1; indice--)
            {
                trabalho[indice] = trabalho[indice - 1];
            }

            trabalho[1] = ultimo;
        }

        return rodadas;
    }

    private static List<Partida> CriarPartidasDaRodadaRoundRobin(
        CategoriaCompeticao categoria,
        IReadOnlyList<RodadaRoundRobin> rodadasBase,
        int numeroRodada,
        string? nomeFaseBase,
        bool turnoEVolta,
        IReadOnlyList<Guid> ordemDuplas)
    {
        if (numeroRodada <= 0)
        {
            return [];
        }

        var quantidadeRodadasBase = rodadasBase.Count;
        var quantidadeRodadasTotal = turnoEVolta ? quantidadeRodadasBase * 2 : quantidadeRodadasBase;
        if (numeroRodada > quantidadeRodadasTotal)
        {
            return [];
        }

        var rodadaBaseIndice = numeroRodada <= quantidadeRodadasBase
            ? numeroRodada - 1
            : numeroRodada - quantidadeRodadasBase - 1;
        var ehRodadaRetorno = turnoEVolta && numeroRodada > quantidadeRodadasBase;
        var rodadaBase = rodadasBase[rodadaBaseIndice];
        var metadados = new MetadadosRodada(
            nomeFaseBase,
            numeroRodada,
            0,
            turnoEVolta,
            ordemDuplas);

        return rodadaBase.Confrontos
            .Select((confronto, indice) => CriarPartidaAgendada(
                categoria,
                ehRodadaRetorno ? confronto.DuplaB : confronto.DuplaA,
                ehRodadaRetorno ? confronto.DuplaA : confronto.DuplaB,
                MontarNomeFaseRodada(categoria, nomeFaseBase, numeroRodada),
                null,
                metadados with { OrdemConfronto = indice + 1 }))
            .ToList();
    }

    private static string? MontarNomeFaseRodada(CategoriaCompeticao categoria, string? nomeFaseBase, int numeroRodada)
    {
        if (string.IsNullOrWhiteSpace(nomeFaseBase))
        {
            return categoria.Competicao.Tipo is TipoCompeticao.Campeonato or TipoCompeticao.Evento
                ? $"Rodada {numeroRodada:00}"
                : null;
        }

        return $"{nomeFaseBase} - Rodada {numeroRodada:00}";
    }

    private static Partida CriarPartidaAgendada(
        CategoriaCompeticao categoria,
        Dupla duplaA,
        Dupla duplaB,
        string? faseCampeonato,
        MetadadosChave? metadadosChave = null,
        MetadadosRodada? metadadosRodada = null)
    {
        return new Partida
        {
            CategoriaCompeticaoId = categoria.Id,
            CategoriaCompeticao = categoria,
            DuplaAId = duplaA.Id,
            DuplaA = duplaA,
            DuplaBId = duplaB.Id,
            DuplaB = duplaB,
            FaseCampeonato = faseCampeonato,
            Status = StatusPartida.Agendada,
            PlacarDuplaA = 0,
            PlacarDuplaB = 0,
            DuplaVencedoraId = null,
            DataPartida = null,
            Observacoes = MontarObservacoesPartida("Tabela gerada automaticamente.", metadadosChave, metadadosRodada)
        };
    }

    private static string MontarResumoGeracao(
        CategoriaCompeticao categoria,
        FormatoCampeonato? formato,
        int quantidadeDuplas,
        IReadOnlyList<Partida> partidasGeradas)
    {
        if (formato is null)
        {
            return $"Jogos iniciais sorteados com {partidasGeradas.Count} confronto(s) para {quantidadeDuplas} duplas na categoria {categoria.Nome}. As próximas rodadas serão abertas conforme os resultados.";
        }

        if (formato.TipoFormato == TipoFormatoCampeonato.FaseDeGrupos && formato.GeraMataMataAposGrupos)
        {
            return $"Primeira rodada da fase de grupos sorteada com {partidasGeradas.Count} jogo(s) para {quantidadeDuplas} duplas. As próximas rodadas serão abertas conforme os resultados.";
        }

        if (formato.TipoFormato == TipoFormatoCampeonato.Chave && formato.QuantidadeDerrotasParaEliminacao == 2)
        {
            return categoria.Competicao.PossuiFinalReset
                ? $"Chaveamento completo gerado para {quantidadeDuplas} duplas na categoria {categoria.Nome}, com winners, losers, final e finalíssima pendente."
                : $"Chaveamento completo gerado para {quantidadeDuplas} duplas na categoria {categoria.Nome}, com winners, losers e final.";
        }

        if (formato.TipoFormato == TipoFormatoCampeonato.Chave)
        {
            return $"Jogos iniciais sorteados em duas chaves para {quantidadeDuplas} duplas na categoria {categoria.Nome}. As próximas partidas serão abertas conforme os resultados.";
        }

        return $"Jogos iniciais sorteados com {partidasGeradas.Count} confronto(s) para {quantidadeDuplas} duplas na categoria {categoria.Nome}. As próximas rodadas serão abertas conforme os resultados.";
    }

    private static string? NormalizarFaseCampeonato(string? faseCampeonato)
    {
        if (string.IsNullOrWhiteSpace(faseCampeonato))
        {
            return null;
        }

        return string.Join(
            ' ',
            faseCampeonato.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private async Task GarantirEdicaoPartidasAsync(Competicao competicao, CancellationToken cancellationToken)
    {
        if (competicao.Tipo == TipoCompeticao.Grupo)
        {
            var usuarioAtual = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
            if (usuarioAtual.Perfil == PerfilUsuario.Administrador)
            {
                return;
            }

            if (usuarioAtual.Perfil == PerfilUsuario.Organizador)
            {
                if (competicao.UsuarioOrganizadorId != usuarioAtual.Id)
                {
                    throw new RegraNegocioException("O organizador só pode alterar competições vinculadas ao próprio usuário.");
                }

                return;
            }

            if (usuarioAtual.Perfil == PerfilUsuario.Atleta)
            {
                if (competicao.UsuarioOrganizadorId != usuarioAtual.Id)
                {
                    throw new RegraNegocioException("Você só pode alterar grupos vinculados ao próprio usuário.");
                }

                return;
            }

            throw new RegraNegocioException("Apenas administradores, organizadores ou o atleta dono do grupo podem gerenciar competições.");
        }

        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        if (usuario.Perfil == PerfilUsuario.Administrador)
        {
            return;
        }

        if (usuario.Perfil != PerfilUsuario.Organizador || competicao.UsuarioOrganizadorId != usuario.Id)
        {
            throw new RegraNegocioException("Somente administradores ou o organizador da competição podem sortear jogos, preencher resultados ou alterar confrontos.");
        }
    }

    private async Task ProcessarAvancoRodadasAsync(CategoriaCompeticao categoria, CancellationToken cancellationToken)
    {
        var formato = ObterFormatoCampeonatoEfetivo(categoria);
        if (categoria.Competicao.Tipo == TipoCompeticao.Grupo ||
            formato?.TipoFormato == TipoFormatoCampeonato.Chave)
        {
            return;
        }

        var partidasCategoria = await partidaRepositorio.ListarPorCategoriaAsync(categoria.Id, cancellationToken);
        var partidasPorFase = partidasCategoria
            .Select(partida => new { Partida = partida, Metadados = ExtrairMetadadosRodada(partida.Observacoes) })
            .Where(x => x.Metadados is not null)
            .Select(x => new PartidaRodada(x.Partida, x.Metadados!))
            .GroupBy(x => x.Metadados.NomeFaseBase ?? string.Empty)
            .ToList();

        if (partidasPorFase.Count == 0)
        {
            return;
        }

        var novasPartidas = new List<Partida>();

        foreach (var grupo in partidasPorFase)
        {
            var referencia = grupo.First().Metadados;
            var rodadaAtual = grupo.Max(x => x.Metadados.NumeroRodada);
            var partidasRodadaAtual = grupo
                .Where(x => x.Metadados.NumeroRodada == rodadaAtual)
                .OrderBy(x => x.Metadados.OrdemConfronto)
                .ToList();

            if (partidasRodadaAtual.Any(x => x.Partida.Status != StatusPartida.Encerrada))
            {
                continue;
            }

            if (grupo.Any(x => x.Metadados.NumeroRodada == rodadaAtual + 1))
            {
                continue;
            }

            var duplasOrdenadas = await ResolverDuplasPorIdsAsync(referencia.OrdemDuplas, cancellationToken);
            var rodadasBase = GerarRodadasRoundRobin(duplasOrdenadas);
            novasPartidas.AddRange(CriarPartidasDaRodadaRoundRobin(
                categoria,
                rodadasBase,
                rodadaAtual + 1,
                referencia.NomeFaseBase,
                referencia.TurnoEVolta,
                referencia.OrdemDuplas));
        }

        if (novasPartidas.Count > 0)
        {
            foreach (var novaPartida in novasPartidas)
            {
                await partidaRepositorio.AdicionarAsync(novaPartida, cancellationToken);
            }

            await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
            partidasCategoria = await partidaRepositorio.ListarPorCategoriaAsync(categoria.Id, cancellationToken);
        }

        if (formato?.TipoFormato == TipoFormatoCampeonato.FaseDeGrupos && formato.GeraMataMataAposGrupos)
        {
            await ProcessarAvancoFaseDeGruposParaEliminatoriaAsync(categoria, formato, partidasCategoria, cancellationToken);
        }
    }

    private async Task ProcessarAvancoFaseDeGruposParaEliminatoriaAsync(
        CategoriaCompeticao categoria,
        FormatoCampeonato formato,
        IReadOnlyList<Partida> partidasCategoria,
        CancellationToken cancellationToken)
    {
        if (!formato.ClassificadosPorGrupo.HasValue || formato.ClassificadosPorGrupo.Value <= 0)
        {
            throw new RegraNegocioException("O formato em fase de grupos precisa informar quantos classificados avançam para o mata-mata.");
        }

        var partidasGrupo = partidasCategoria
            .Select(partida => new { Partida = partida, Metadados = ExtrairMetadadosRodada(partida.Observacoes) })
            .Where(x => x.Metadados is not null && EhNomeFaseGrupo(x.Metadados!.NomeFaseBase))
            .Select(x => new PartidaRodada(x.Partida, x.Metadados!))
            .ToList();

        if (partidasGrupo.Count == 0)
        {
            return;
        }

        var classificacaoPorGrupo = await MontarClassificacaoFaseDeGruposAsync(
            categoria,
            partidasGrupo,
            formato.ClassificadosPorGrupo.Value,
            cancellationToken);

        if (classificacaoPorGrupo.Count == 0)
        {
            return;
        }

        var classificadosOrdenados = MontarClassificadosOrdenadosParaEliminatoria(classificacaoPorGrupo, formato.ClassificadosPorGrupo.Value);
        if (classificadosOrdenados.Count <= 1)
        {
            return;
        }

        var partidasEliminatoria = partidasCategoria
            .Select(partida => new { Partida = partida, Metadados = ExtrairMetadadosChave(partida.Observacoes) })
            .Where(x => x.Metadados is not null &&
                        string.Equals(x.Metadados!.Lado, SecaoEliminatoriaGrupos, StringComparison.OrdinalIgnoreCase))
            .Select(x => new PartidaChave(x.Partida, x.Metadados!))
            .ToList();

        if (partidasEliminatoria.Count == 0)
        {
            var primeiraRodada = await CriarPrimeiraRodadaEliminatoriaFaseDeGruposAsync(
                categoria,
                classificadosOrdenados,
                cancellationToken);

            if (primeiraRodada.Count == 0)
            {
                return;
            }

            foreach (var partida in primeiraRodada)
            {
                await partidaRepositorio.AdicionarAsync(partida, cancellationToken);
            }

            await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
            return;
        }

        var participantesAtuais = classificadosOrdenados
            .Select(x => x.DuplaId)
            .ToList();

        var quantidadeRodadas = CalcularQuantidadeRodadasChave(participantesAtuais.Count);
        EstadoRodadaChave? estadoAnterior = null;

        for (var rodada = 1; rodada <= quantidadeRodadas; rodada++)
        {
            var partidasRodada = ObterPartidasChavePorSecaoERodada(partidasEliminatoria, SecaoEliminatoriaGrupos, rodada);
            var estado = rodada == 1
                ? AvaliarPrimeiraRodadaEliminatoriaFaseDeGrupos(classificadosOrdenados, partidasRodada)
                : estadoAnterior is null
                    ? null
                    : AvaliarRodadaChave(SecaoEliminatoriaGrupos, rodada, estadoAnterior.Classificados, partidasRodada);

            if (estado is null)
            {
                return;
            }

            if (partidasRodada.Count == 0)
            {
                var novasPartidas = rodada == 1
                    ? await CriarPrimeiraRodadaEliminatoriaFaseDeGruposAsync(categoria, classificadosOrdenados, cancellationToken)
                    : await CriarPartidasRodadaEliminatoriaFaseDeGruposAsync(
                        categoria,
                        rodada,
                        estado.Participantes,
                        cancellationToken);

                if (novasPartidas.Count == 0)
                {
                    return;
                }

                foreach (var partida in novasPartidas)
                {
                    await partidaRepositorio.AdicionarAsync(partida, cancellationToken);
                }

                await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
                return;
            }

            if (!estado.Concluida)
            {
                return;
            }

            if (estado.Classificados.Count <= 1)
            {
                return;
            }

            var proximaRodada = ObterPartidasChavePorSecaoERodada(partidasEliminatoria, SecaoEliminatoriaGrupos, rodada + 1);
            if (proximaRodada.Count == 0)
            {
                var novasPartidas = await CriarPartidasRodadaEliminatoriaFaseDeGruposAsync(
                    categoria,
                    rodada + 1,
                    estado.Classificados,
                    cancellationToken);

                if (novasPartidas.Count == 0)
                {
                    return;
                }

                foreach (var partida in novasPartidas)
                {
                    await partidaRepositorio.AdicionarAsync(partida, cancellationToken);
                }

                await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
                return;
            }

            estadoAnterior = estado;
        }
    }

    private async Task ProcessarAvancoChaveAsync(CategoriaCompeticao categoria, CancellationToken cancellationToken)
    {
        var formato = ObterFormatoCampeonatoEfetivo(categoria);
        if (categoria.Competicao.Tipo == TipoCompeticao.Grupo ||
            formato is null ||
            formato.TipoFormato != TipoFormatoCampeonato.Chave)
        {
            return;
        }

        if (formato.QuantidadeDerrotasParaEliminacao == 2)
        {
            await SincronizarChaveDuplaEliminacaoCompletaAsync(categoria, cancellationToken);
            return;
        }

        var partidasCategoria = await partidaRepositorio.ListarPorCategoriaAsync(categoria.Id, cancellationToken);
        var partidasChave = partidasCategoria
            .Select(partida => new { Partida = partida, Metadados = ExtrairMetadadosChave(partida.Observacoes) })
            .Where(x => x.Metadados is not null)
            .Select(x => new PartidaChave(x.Partida, x.Metadados!))
            .ToList();

        var novasPartidas = new List<Partida>();

        novasPartidas.AddRange(await GerarProximaRodadaChaveSeNecessarioAsync(categoria, partidasChave, "A", cancellationToken));
        novasPartidas.AddRange(await GerarProximaRodadaChaveSeNecessarioAsync(categoria, partidasChave, "B", cancellationToken));

        var campeaoChaveA = await ObterCampeaoChaveAsync(partidasChave, "A", cancellationToken);
        var campeaoChaveB = await ObterCampeaoChaveAsync(partidasChave, "B", cancellationToken);
        if (campeaoChaveA is not null &&
            campeaoChaveB is not null &&
            partidasCategoria.All(x => !string.Equals(x.FaseCampeonato, NomeFaseFinal, StringComparison.OrdinalIgnoreCase)) &&
            novasPartidas.All(x => !string.Equals(x.FaseCampeonato, NomeFaseFinal, StringComparison.OrdinalIgnoreCase)))
        {
            novasPartidas.Add(CriarPartidaAgendada(categoria, campeaoChaveA, campeaoChaveB, NomeFaseFinal));
        }

        if (formato.DisputaTerceiroLugar &&
            partidasCategoria.All(x => !string.Equals(x.FaseCampeonato, NomeFaseTerceiroLugar, StringComparison.OrdinalIgnoreCase)) &&
            novasPartidas.All(x => !string.Equals(x.FaseCampeonato, NomeFaseTerceiroLugar, StringComparison.OrdinalIgnoreCase)))
        {
            var viceChaveA = await ObterViceCampeaoChaveAsync(partidasChave, "A", cancellationToken);
            var viceChaveB = await ObterViceCampeaoChaveAsync(partidasChave, "B", cancellationToken);

            if (viceChaveA is not null && viceChaveB is not null)
            {
                novasPartidas.Add(CriarPartidaAgendada(categoria, viceChaveA, viceChaveB, NomeFaseTerceiroLugar));
            }
        }

        if (novasPartidas.Count == 0)
        {
            return;
        }

        foreach (var novaPartida in novasPartidas)
        {
            await partidaRepositorio.AdicionarAsync(novaPartida, cancellationToken);
        }

        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<Partida>> GerarProximaRodadaChaveSeNecessarioAsync(
        CategoriaCompeticao categoria,
        IReadOnlyList<PartidaChave> partidasChave,
        string lado,
        CancellationToken cancellationToken)
    {
        var partidasLado = partidasChave
            .Where(x => string.Equals(x.Metadados.Lado, lado, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (partidasLado.Count == 0)
        {
            return [];
        }

        var rodadaAtual = partidasLado.Max(x => x.Metadados.Rodada);
        var partidasRodadaAtual = partidasLado
            .Where(x => x.Metadados.Rodada == rodadaAtual)
            .OrderBy(x => x.Metadados.Ordem)
            .ToList();

        if (partidasRodadaAtual.Any(x => x.Partida.Status != StatusPartida.Encerrada || !x.Partida.DuplaVencedoraId.HasValue))
        {
            return [];
        }

        var idsDuplasEmEspera = partidasRodadaAtual
            .SelectMany(x => x.Metadados.DuplasEmEspera)
            .Distinct()
            .ToList();

        var duplasEmEspera = await ResolverDuplasPorIdsAsync(idsDuplasEmEspera, cancellationToken);
        var vencedores = await ResolverDuplasPorIdsAsync(
            partidasRodadaAtual
                .Select(x => x.Partida.DuplaVencedoraId!.Value)
                .Distinct()
                .ToList(),
            cancellationToken);

        var classificados = duplasEmEspera
            .Concat(vencedores)
            .ToList();

        if (classificados.Count <= 1)
        {
            return [];
        }

        return GerarPartidasRodadaChave(categoria, classificados, lado, rodadaAtual + 1);
    }

    private async Task<Dupla?> ObterCampeaoChaveAsync(
        IReadOnlyList<PartidaChave> partidasChave,
        string lado,
        CancellationToken cancellationToken)
    {
        var rodadaFinal = ObterRodadaMaisAltaConcluida(partidasChave, lado);
        if (rodadaFinal is null)
        {
            return null;
        }

        var idsClassificados = rodadaFinal
            .SelectMany(x => x.Metadados.DuplasEmEspera)
            .Concat(rodadaFinal
                .Where(x => x.Partida.DuplaVencedoraId.HasValue)
                .Select(x => x.Partida.DuplaVencedoraId!.Value))
            .Distinct()
            .ToList();

        if (idsClassificados.Count != 1)
        {
            return null;
        }

        var duplas = await ResolverDuplasPorIdsAsync(idsClassificados, cancellationToken);
        return duplas.SingleOrDefault();
    }

    private async Task<Dupla?> ObterViceCampeaoChaveAsync(
        IReadOnlyList<PartidaChave> partidasChave,
        string lado,
        CancellationToken cancellationToken)
    {
        var rodadaFinal = ObterRodadaMaisAltaConcluida(partidasChave, lado);
        if (rodadaFinal is null || rodadaFinal.Count != 1)
        {
            return null;
        }

        var partidaFinal = rodadaFinal[0].Partida;
        if (!partidaFinal.DuplaVencedoraId.HasValue)
        {
            return null;
        }

        var duplaViceId = partidaFinal.DuplaAId == partidaFinal.DuplaVencedoraId.Value
            ? partidaFinal.DuplaBId
            : partidaFinal.DuplaAId;

        if (!duplaViceId.HasValue)
        {
            return null;
        }

        var duplas = await ResolverDuplasPorIdsAsync([duplaViceId.Value], cancellationToken);
        return duplas.SingleOrDefault();
    }

    private async Task ProcessarAvancoChaveDuplaEliminacaoAsync(
        CategoriaCompeticao categoria,
        CancellationToken cancellationToken)
    {
        var partidasCategoria = await partidaRepositorio.ListarPorCategoriaAsync(categoria.Id, cancellationToken);
        var partidasChave = partidasCategoria
            .Select(partida => new { Partida = partida, Metadados = ExtrairMetadadosChave(partida.Observacoes) })
            .Where(x => x.Metadados is not null)
            .Select(x => new PartidaChave(x.Partida, x.Metadados!))
            .ToList();

        var participantesIniciais = ObterParticipantesIniciaisChaveDuplaEliminacao(partidasChave);
        if (participantesIniciais.Count == 0)
        {
            return;
        }

        var controleDuplas = ConstruirControleDuplasChaveDuplaEliminacao(participantesIniciais, partidasChave);
        ValidarIntegridadeControleDuplasChaveDuplaEliminacao(controleDuplas);

        var quantidadeRodadasVencedores = CalcularQuantidadeRodadasChave(participantesIniciais.Count);
        var quantidadeRodadasPerdedores = Math.Max(0, (quantidadeRodadasVencedores * 2) - 2);

        var estadosVencedores = new Dictionary<int, EstadoRodadaChave>();
        var participantesRodadaVencedores = participantesIniciais;

        for (var rodada = 1; rodada <= quantidadeRodadasVencedores; rodada++)
        {
            var estado = AvaliarRodadaChave(
                SecaoChaveVencedores,
                rodada,
                participantesRodadaVencedores,
                ObterPartidasChavePorSecaoERodada(partidasChave, SecaoChaveVencedores, rodada));

            estadosVencedores[rodada] = estado;

            if (!estado.Concluida)
            {
                break;
            }

            participantesRodadaVencedores = estado.Classificados;
        }

        var estadosPerdedores = new Dictionary<int, EstadoRodadaChave>();

        for (var rodada = 1; rodada <= quantidadeRodadasPerdedores; rodada++)
        {
            IReadOnlyList<Guid>? participantesRodadaPerdedores = rodada switch
            {
                1 => estadosVencedores.GetValueOrDefault(1)?.Concluida == true
                    ? estadosVencedores[1].Derrotados
                    : null,
                _ when rodada % 2 != 0 => estadosPerdedores.GetValueOrDefault(rodada - 1)?.Concluida == true
                    ? estadosPerdedores[rodada - 1].Classificados
                    : null,
                _ => estadosPerdedores.GetValueOrDefault(rodada - 1)?.Concluida == true &&
                     estadosVencedores.GetValueOrDefault((rodada / 2) + 1)?.Concluida == true
                    ? estadosPerdedores[rodada - 1].Classificados
                        .Concat(estadosVencedores[(rodada / 2) + 1].Derrotados)
                        .ToList()
                    : null
            };

            if (participantesRodadaPerdedores is null)
            {
                break;
            }

            var estado = AvaliarRodadaChave(
                SecaoChavePerdedores,
                rodada,
                participantesRodadaPerdedores,
                ObterPartidasChavePorSecaoERodada(partidasChave, SecaoChavePerdedores, rodada));

            estadosPerdedores[rodada] = estado;

            if (!estado.Concluida)
            {
                break;
            }
        }

        var novasPartidas = new List<Partida>();
        var duplasComPartidaPendente = controleDuplas.Values
            .Where(x => x.PartidasPendentesIds.Count > 0)
            .Select(x => x.DuplaId)
            .ToHashSet();

        foreach (var estado in estadosVencedores.Values.OrderBy(x => x.Rodada))
        {
            novasPartidas.AddRange(await CriarPartidasPendentesRodadaChaveAsync(
                categoria,
                estado,
                controleDuplas,
                duplasComPartidaPendente,
                cancellationToken));
        }

        foreach (var estado in estadosPerdedores.Values.OrderBy(x => x.Rodada))
        {
            novasPartidas.AddRange(await CriarPartidasPendentesRodadaChaveAsync(
                categoria,
                estado,
                controleDuplas,
                duplasComPartidaPendente,
                cancellationToken));
        }

        var campeaoChaveVencedoresId = ObterCampeaoSecao(estadosVencedores, quantidadeRodadasVencedores);
        var campeaoChavePerdedoresId = ObterCampeaoSecao(estadosPerdedores, quantidadeRodadasPerdedores);

        var finalExistente = ObterPartidaUnicaPorSecao(partidasChave, SecaoChaveFinal);
        var resetExistente = ObterPartidaUnicaPorSecao(partidasChave, SecaoChaveReset);

        if (campeaoChaveVencedoresId.HasValue &&
            campeaoChavePerdedoresId.HasValue &&
            finalExistente is null &&
            partidasCategoria.All(x => !string.Equals(x.FaseCampeonato, NomeFaseFinal, StringComparison.OrdinalIgnoreCase)) &&
            novasPartidas.All(x => !string.Equals(x.FaseCampeonato, NomeFaseFinal, StringComparison.OrdinalIgnoreCase)))
        {
            ValidarDuplaDisponivelParaNovaPartida(controleDuplas, duplasComPartidaPendente, campeaoChaveVencedoresId.Value, NomeFaseFinal);
            ValidarDuplaDisponivelParaNovaPartida(controleDuplas, duplasComPartidaPendente, campeaoChavePerdedoresId.Value, NomeFaseFinal);
            var duplasFinal = await ResolverDuplasPorIdsAsync(
                [campeaoChaveVencedoresId.Value, campeaoChavePerdedoresId.Value],
                cancellationToken);

            var duplasPorId = duplasFinal.ToDictionary(x => x.Id);
            novasPartidas.Add(CriarPartidaAgendada(
                categoria,
                duplasPorId[campeaoChaveVencedoresId.Value],
                duplasPorId[campeaoChavePerdedoresId.Value],
                NomeFaseFinal,
                new MetadadosChave(SecaoChaveFinal, 1, 1, [campeaoChaveVencedoresId.Value])));
            duplasComPartidaPendente.Add(campeaoChaveVencedoresId.Value);
            duplasComPartidaPendente.Add(campeaoChavePerdedoresId.Value);
        }

        if (categoria.Competicao.PossuiFinalReset &&
            finalExistente?.Partida.Status == StatusPartida.Encerrada &&
            finalExistente.Partida.DuplaVencedoraId.HasValue &&
            resetExistente is null &&
            partidasCategoria.All(x => !string.Equals(x.FaseCampeonato, NomeFaseFinalReset, StringComparison.OrdinalIgnoreCase)) &&
            novasPartidas.All(x => !string.Equals(x.FaseCampeonato, NomeFaseFinalReset, StringComparison.OrdinalIgnoreCase)))
        {
            var duplaInvictaId = finalExistente.Metadados.DuplasEmEspera.FirstOrDefault();

            if (duplaInvictaId == Guid.Empty)
            {
                duplaInvictaId = finalExistente.Partida.DuplaAId ?? Guid.Empty;
            }

            if (finalExistente.Partida.DuplaVencedoraId.Value != duplaInvictaId)
            {
                var duplaFinalAId = finalExistente.Partida.DuplaAId
                    ?? throw new RegraNegocioException("A final precisa ter as duas duplas definidas para avaliar a finalíssima.");
                var duplaFinalBId = finalExistente.Partida.DuplaBId
                    ?? throw new RegraNegocioException("A final precisa ter as duas duplas definidas para avaliar a finalíssima.");

                ValidarDuplaDisponivelParaNovaPartida(controleDuplas, duplasComPartidaPendente, duplaFinalAId, NomeFaseFinalReset);
                ValidarDuplaDisponivelParaNovaPartida(controleDuplas, duplasComPartidaPendente, duplaFinalBId, NomeFaseFinalReset);
                var duplasReset = await ResolverDuplasPorIdsAsync(
                    [duplaFinalAId, duplaFinalBId],
                    cancellationToken);
                var duplasResetPorId = duplasReset.ToDictionary(x => x.Id);

                novasPartidas.Add(CriarPartidaAgendada(
                    categoria,
                    duplasResetPorId[duplaFinalAId],
                    duplasResetPorId[duplaFinalBId],
                    NomeFaseFinalReset,
                    new MetadadosChave(SecaoChaveReset, 1, 1, [duplaInvictaId])));
                duplasComPartidaPendente.Add(duplaFinalAId);
                duplasComPartidaPendente.Add(duplaFinalBId);
            }
        }

        if (novasPartidas.Count == 0)
        {
            return;
        }

        foreach (var novaPartida in novasPartidas)
        {
            await partidaRepositorio.AdicionarAsync(novaPartida, cancellationToken);
        }

        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
    }

    private async Task SincronizarChaveDuplaEliminacaoCompletaAsync(
        CategoriaCompeticao categoria,
        CancellationToken cancellationToken)
    {
        var partidasCategoria = await partidaRepositorio.ListarPorCategoriaAsync(categoria.Id, cancellationToken);
        var partidasAutomaticas = partidasCategoria
            .Where(x => x.LadoDaChave.HasValue)
            .OrderBy(x => x.EhFinalissima ? 4 : x.EhFinal ? 3 : x.LadoDaChave == LadoDaChave.Perdedores ? 2 : 1)
            .ThenBy(x => x.Rodada ?? 0)
            .ThenBy(x => x.PosicaoNaChave ?? 0)
            .ToList();

        if (partidasAutomaticas.Count == 0)
        {
            await ProcessarAvancoChaveDuplaEliminacaoAsync(categoria, cancellationToken);
            return;
        }

        SincronizarEstadoPartidasGeradas(partidasAutomaticas, categoria.Competicao.PossuiFinalReset);

        foreach (var partida in partidasAutomaticas)
        {
            partida.DuplaA = partida.DuplaAId.HasValue
                ? await duplaRepositorio.ObterPorIdAsync(partida.DuplaAId.Value, cancellationToken)
                : null;
            partida.DuplaB = partida.DuplaBId.HasValue
                ? await duplaRepositorio.ObterPorIdAsync(partida.DuplaBId.Value, cancellationToken)
                : null;
            partida.DuplaVencedora = partida.DuplaVencedoraId.HasValue
                ? await duplaRepositorio.ObterPorIdAsync(partida.DuplaVencedoraId.Value, cancellationToken)
                : null;
            partida.AtualizarDataModificacao();
            partidaRepositorio.Atualizar(partida);
        }

        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<Partida>> CriarPartidasPendentesRodadaChaveAsync(
        CategoriaCompeticao categoria,
        EstadoRodadaChave estado,
        IReadOnlyDictionary<Guid, EstadoDuplaChaveDuplaEliminacao> controleDuplas,
        ISet<Guid> duplasComPartidaPendente,
        CancellationToken cancellationToken)
    {
        if (estado.Participantes.Count <= 1 || estado.Planejamento.Confrontos.Count == 0)
        {
            return [];
        }

        var ordensExistentes = estado.Partidas
            .Select(x => x.Metadados.Ordem)
            .ToHashSet();
        var confrontosPendentes = estado.Planejamento.Confrontos
            .Where(x => !ordensExistentes.Contains(x.Ordem))
            .ToList();

        if (confrontosPendentes.Count == 0)
        {
            return [];
        }

        var idsNecessarios = confrontosPendentes
            .SelectMany(x => new[] { x.DuplaAId, x.DuplaBId })
            .Distinct()
            .ToList();
        var duplas = await ResolverDuplasPorIdsAsync(idsNecessarios, cancellationToken);
        var duplasPorId = duplas.ToDictionary(x => x.Id);
        var nomeFase = MontarNomeFaseChaveDuplaEliminacao(estado.Secao, estado.Rodada);
        var partidasNovas = new List<Partida>();

        foreach (var confronto in confrontosPendentes)
        {
            ValidarDuplaDisponivelParaNovaPartida(controleDuplas, duplasComPartidaPendente, confronto.DuplaAId, nomeFase);
            ValidarDuplaDisponivelParaNovaPartida(controleDuplas, duplasComPartidaPendente, confronto.DuplaBId, nomeFase);

            partidasNovas.Add(CriarPartidaAgendada(
                categoria,
                duplasPorId[confronto.DuplaAId],
                duplasPorId[confronto.DuplaBId],
                nomeFase,
                new MetadadosChave(estado.Secao, estado.Rodada, confronto.Ordem, estado.Planejamento.DuplasEmEspera)));

            duplasComPartidaPendente.Add(confronto.DuplaAId);
            duplasComPartidaPendente.Add(confronto.DuplaBId);
        }

        return partidasNovas;
    }

    private static IReadOnlyList<Guid> ObterParticipantesIniciaisChaveDuplaEliminacao(
        IReadOnlyList<PartidaChave> partidasChave)
    {
        var partidasIniciais = ObterPartidasChavePorSecaoERodada(partidasChave, SecaoChaveVencedores, 1);
        if (partidasIniciais.Count == 0)
        {
            return [];
        }

        var participantes = new List<Guid>();

        foreach (var idDupla in partidasIniciais.SelectMany(x => x.Metadados.DuplasEmEspera))
        {
            if (!participantes.Contains(idDupla))
            {
                participantes.Add(idDupla);
            }
        }

        foreach (var partida in partidasIniciais)
        {
            if (partida.Partida.DuplaAId.HasValue && !participantes.Contains(partida.Partida.DuplaAId.Value))
            {
                participantes.Add(partida.Partida.DuplaAId.Value);
            }

            if (partida.Partida.DuplaBId.HasValue && !participantes.Contains(partida.Partida.DuplaBId.Value))
            {
                participantes.Add(partida.Partida.DuplaBId.Value);
            }
        }

        return participantes;
    }

    private async Task ValidarEdicaoPartidaGerenciadaPorChaveDuplaEliminacaoAsync(
        Partida partida,
        CategoriaCompeticao categoriaAtualizada,
        Guid categoriaOriginalId,
        Guid? duplaOriginalAId,
        Guid? duplaOriginalBId,
        string? faseOriginal,
        StatusPartida statusOriginal,
        int placarOriginalA,
        int placarOriginalB,
        Guid? vencedorOriginalId,
        string? faseAtualizada,
        StatusPartida statusAtualizado,
        int? placarAtualizadoA,
        int? placarAtualizadoB,
        Dupla duplaAtualizadaA,
        Dupla duplaAtualizadaB,
        MetadadosChave? metadadosChave,
        CancellationToken cancellationToken)
    {
        if (!(EhCategoriaComChaveDuplaEliminacao(partida.CategoriaCompeticao) ||
              EhCategoriaComChaveDuplaEliminacao(categoriaAtualizada)) ||
            metadadosChave is null ||
            !EhSecaoChaveDuplaEliminacao(metadadosChave.Lado))
        {
            return;
        }

        if (categoriaOriginalId != categoriaAtualizada.Id)
        {
            throw new RegraNegocioException("Partidas gerenciadas automaticamente na chave com dupla eliminação não podem mudar de categoria.");
        }

        if (duplaOriginalAId != duplaAtualizadaA.Id || duplaOriginalBId != duplaAtualizadaB.Id)
        {
            throw new RegraNegocioException("Partidas gerenciadas automaticamente na chave com dupla eliminação não permitem troca manual de duplas.");
        }

        if (!string.Equals(faseOriginal, faseAtualizada, StringComparison.OrdinalIgnoreCase))
        {
            throw new RegraNegocioException("Partidas gerenciadas automaticamente na chave com dupla eliminação não permitem troca manual de fase.");
        }

        var mudouResultado = statusOriginal != statusAtualizado ||
                            placarOriginalA != (placarAtualizadoA ?? 0) ||
                            placarOriginalB != (placarAtualizadoB ?? 0) ||
                            vencedorOriginalId != CalcularVencedorPorPlacar(partida, placarAtualizadoA, placarAtualizadoB, statusAtualizado);

        if (statusOriginal == StatusPartida.Encerrada && statusAtualizado != StatusPartida.Encerrada)
        {
            throw new RegraNegocioException("Partidas encerradas da chave com dupla eliminação não podem voltar para agendada.");
        }

        if (!mudouResultado || statusOriginal != StatusPartida.Encerrada)
        {
            return;
        }

        var partidasCategoria = await partidaRepositorio.ListarPorCategoriaAsync(categoriaAtualizada.Id, cancellationToken);
        var possuiDependenciaPosterior = partidasCategoria
            .Where(x => x.Id != partida.Id)
            .Select(x => new { Partida = x, Metadados = ExtrairMetadadosChave(x.Observacoes) })
            .Where(x => x.Metadados is not null && EhSecaoChaveDuplaEliminacao(x.Metadados!.Lado))
            .Any(x => EhPartidaPosteriorNaChaveDuplaEliminacao(metadadosChave, x.Metadados!));

        if (possuiDependenciaPosterior)
        {
            throw new RegraNegocioException("Não é permitido alterar manualmente o resultado de uma partida da chave com dupla eliminação após a geração de fases dependentes.");
        }
    }

    private static Guid? CalcularVencedorPorPlacar(
        Partida partida,
        int? placarDuplaA,
        int? placarDuplaB,
        StatusPartida status)
    {
        if (status != StatusPartida.Encerrada || !placarDuplaA.HasValue || !placarDuplaB.HasValue)
        {
            return null;
        }

        if (placarDuplaA.Value == placarDuplaB.Value)
        {
            return null;
        }

        return placarDuplaA.Value > placarDuplaB.Value ? partida.DuplaAId : partida.DuplaBId;
    }

    private static bool EhCategoriaComChaveDuplaEliminacao(CategoriaCompeticao categoria)
    {
        var formato = ObterFormatoCampeonatoEfetivo(categoria);
        return categoria.Competicao.Tipo != TipoCompeticao.Grupo &&
               formato?.TipoFormato == TipoFormatoCampeonato.Chave &&
               formato.QuantidadeDerrotasParaEliminacao == 2;
    }

    private static FormatoCampeonato? ObterFormatoCampeonatoEfetivo(CategoriaCompeticao categoria)
    {
        return categoria.ObterFormatoCampeonatoEfetivo();
    }

    private static bool EhSecaoChaveDuplaEliminacao(string secao)
    {
        return string.Equals(secao, SecaoChaveVencedores, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(secao, SecaoChavePerdedores, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(secao, SecaoChaveFinal, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(secao, SecaoChaveReset, StringComparison.OrdinalIgnoreCase);
    }

    private static bool EhPartidaPosteriorNaChaveDuplaEliminacao(MetadadosChave atual, MetadadosChave comparada)
    {
        if (string.Equals(comparada.Lado, SecaoChaveReset, StringComparison.OrdinalIgnoreCase))
        {
            return !string.Equals(atual.Lado, SecaoChaveReset, StringComparison.OrdinalIgnoreCase);
        }

        if (string.Equals(comparada.Lado, SecaoChaveFinal, StringComparison.OrdinalIgnoreCase))
        {
            return !string.Equals(atual.Lado, SecaoChaveFinal, StringComparison.OrdinalIgnoreCase) &&
                   !string.Equals(atual.Lado, SecaoChaveReset, StringComparison.OrdinalIgnoreCase);
        }

        if (string.Equals(atual.Lado, SecaoChaveVencedores, StringComparison.OrdinalIgnoreCase))
        {
            return !string.Equals(comparada.Lado, SecaoChaveVencedores, StringComparison.OrdinalIgnoreCase) ||
                   comparada.Rodada > atual.Rodada;
        }

        if (string.Equals(atual.Lado, SecaoChavePerdedores, StringComparison.OrdinalIgnoreCase))
        {
            return (string.Equals(comparada.Lado, SecaoChavePerdedores, StringComparison.OrdinalIgnoreCase) &&
                    comparada.Rodada > atual.Rodada) ||
                   string.Equals(comparada.Lado, SecaoChaveFinal, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(comparada.Lado, SecaoChaveReset, StringComparison.OrdinalIgnoreCase);
        }

        if (string.Equals(atual.Lado, SecaoChaveFinal, StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(comparada.Lado, SecaoChaveReset, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static IReadOnlyDictionary<Guid, EstadoDuplaChaveDuplaEliminacao> ConstruirControleDuplasChaveDuplaEliminacao(
        IReadOnlyList<Guid> participantesIniciais,
        IReadOnlyList<PartidaChave> partidasChave)
    {
        var derrotasPorDupla = participantesIniciais.ToDictionary(id => id, _ => 0);
        var partidasPendentesPorDupla = participantesIniciais.ToDictionary(id => id, _ => new List<Guid>());
        var posicaoAtualPorDupla = participantesIniciais.ToDictionary(id => id, _ => NomeFaseChaveVencedores);

        foreach (var partidaChave in partidasChave.Where(x => EhSecaoChaveDuplaEliminacao(x.Metadados.Lado)))
        {
            var duplaAId = partidaChave.Partida.DuplaAId
                ?? throw new RegraNegocioException("A chave com dupla eliminação ficou inconsistente: existe partida sem dupla A definida.");
            var duplaBId = partidaChave.Partida.DuplaBId
                ?? throw new RegraNegocioException("A chave com dupla eliminação ficou inconsistente: existe partida sem dupla B definida.");

            GarantirDuplaConhecidaNaChave(derrotasPorDupla, duplaAId);
            GarantirDuplaConhecidaNaChave(derrotasPorDupla, duplaBId);

            if (partidaChave.Partida.Status == StatusPartida.Encerrada && partidaChave.Partida.DuplaVencedoraId.HasValue)
            {
                var duplaPerdedoraId = ObterDuplaPerdedoraId(partidaChave.Partida);
                derrotasPorDupla[duplaPerdedoraId]++;
            }
            else
            {
                partidasPendentesPorDupla[duplaAId].Add(partidaChave.Partida.Id);
                partidasPendentesPorDupla[duplaBId].Add(partidaChave.Partida.Id);

                var posicao = ObterNomePosicaoAtualChaveDuplaEliminacao(partidaChave.Metadados.Lado);
                posicaoAtualPorDupla[duplaAId] = posicao;
                posicaoAtualPorDupla[duplaBId] = posicao;
            }
        }

        var estados = new Dictionary<Guid, EstadoDuplaChaveDuplaEliminacao>();
        foreach (var duplaId in participantesIniciais)
        {
            var derrotas = derrotasPorDupla[duplaId];
            var eliminada = derrotas >= 2;
            var partidasPendentesIds = partidasPendentesPorDupla[duplaId];
            var posicaoAtual = eliminada
                ? "Eliminada"
                : partidasPendentesIds.Count > 0
                    ? posicaoAtualPorDupla[duplaId]
                    : derrotas == 0
                        ? NomeFaseChaveVencedores
                        : NomeFaseChavePerdedores;

            estados[duplaId] = new EstadoDuplaChaveDuplaEliminacao(
                duplaId,
                derrotas,
                eliminada ? StatusDuplaChaveDuplaEliminacao.Eliminada : StatusDuplaChaveDuplaEliminacao.Ativa,
                posicaoAtual,
                partidasPendentesIds);
        }

        return estados;
    }

    private static void ValidarIntegridadeControleDuplasChaveDuplaEliminacao(
        IReadOnlyDictionary<Guid, EstadoDuplaChaveDuplaEliminacao> controleDuplas)
    {
        foreach (var estado in controleDuplas.Values)
        {
            if (estado.QuantidadeDerrotas > 2)
            {
                throw new RegraNegocioException("A chave com dupla eliminação ficou inconsistente: uma dupla acumulou derrotas acima do permitido.");
            }

            if (estado.Status == StatusDuplaChaveDuplaEliminacao.Eliminada && estado.PartidasPendentesIds.Count > 0)
            {
                throw new RegraNegocioException("A chave com dupla eliminação ficou inconsistente: existe dupla eliminada vinculada a partida futura.");
            }

            if (estado.PartidasPendentesIds.Count > 1)
            {
                throw new RegraNegocioException("A chave com dupla eliminação ficou inconsistente: uma dupla foi vinculada a mais de uma partida futura ao mesmo tempo.");
            }
        }
    }

    private static void ValidarDuplaDisponivelParaNovaPartida(
        IReadOnlyDictionary<Guid, EstadoDuplaChaveDuplaEliminacao> controleDuplas,
        ISet<Guid> duplasComPartidaPendente,
        Guid duplaId,
        string faseDestino)
    {
        if (!controleDuplas.TryGetValue(duplaId, out var estado))
        {
            throw new RegraNegocioException("A chave com dupla eliminação ficou inconsistente: dupla não encontrada no controle interno da competição.");
        }

        if (estado.Status == StatusDuplaChaveDuplaEliminacao.Eliminada)
        {
            throw new RegraNegocioException($"A dupla eliminada não pode ser vinculada à fase {faseDestino}.");
        }

        if (duplasComPartidaPendente.Contains(duplaId))
        {
            throw new RegraNegocioException($"A dupla já possui partida pendente e não pode ser vinculada novamente à fase {faseDestino}.");
        }
    }

    private static void GarantirDuplaConhecidaNaChave(
        IDictionary<Guid, int> derrotasPorDupla,
        Guid duplaId)
    {
        if (!derrotasPorDupla.ContainsKey(duplaId))
        {
            throw new RegraNegocioException("A chave com dupla eliminação ficou inconsistente: existe partida com dupla fora da estrutura inicial do campeonato.");
        }
    }

    private static string ObterNomePosicaoAtualChaveDuplaEliminacao(string secao)
    {
        if (string.Equals(secao, SecaoChaveVencedores, StringComparison.OrdinalIgnoreCase))
        {
            return NomeFaseChaveVencedores;
        }

        if (string.Equals(secao, SecaoChavePerdedores, StringComparison.OrdinalIgnoreCase))
        {
            return NomeFaseChavePerdedores;
        }

        if (string.Equals(secao, SecaoChaveFinal, StringComparison.OrdinalIgnoreCase))
        {
            return NomeFaseFinal;
        }

        if (string.Equals(secao, SecaoChaveReset, StringComparison.OrdinalIgnoreCase))
        {
            return NomeFaseFinalReset;
        }

        return "Chave";
    }

    private static IReadOnlyList<PartidaChave> ObterPartidasChavePorSecaoERodada(
        IReadOnlyList<PartidaChave> partidasChave,
        string secao,
        int rodada)
    {
        return partidasChave
            .Where(x => string.Equals(x.Metadados.Lado, secao, StringComparison.OrdinalIgnoreCase) &&
                        x.Metadados.Rodada == rodada)
            .OrderBy(x => x.Metadados.Ordem)
            .ToList();
    }

    private static PartidaChave? ObterPartidaUnicaPorSecao(
        IReadOnlyList<PartidaChave> partidasChave,
        string secao)
    {
        return partidasChave
            .Where(x => string.Equals(x.Metadados.Lado, secao, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Metadados.Rodada)
            .ThenBy(x => x.Metadados.Ordem)
            .FirstOrDefault();
    }

    private static IReadOnlyList<RodadaEstruturaCompeticaoDto> MontarEstruturaRodadasCategoria(
        CategoriaCompeticao categoria,
        IReadOnlyList<Partida> partidas)
    {
        if (partidas.Count == 0)
        {
            return [];
        }

        if (EhCategoriaComChaveDuplaEliminacao(categoria))
        {
            return MontarEstruturaRodadasChaveDuplaEliminacao(partidas);
        }

        return MontarEstruturaRodadasPadrao(partidas);
    }

    private static IReadOnlyList<RodadaEstruturaCompeticaoDto> MontarEstruturaRodadasChaveDuplaEliminacao(IReadOnlyList<Partida> partidas)
    {
        var partidasComMetadados = partidas
            .Select(partida => new { Partida = partida, Metadados = ExtrairMetadadosChave(partida.Observacoes) })
            .Where(x => x.Metadados is not null && EhSecaoChaveDuplaEliminacao(x.Metadados.Lado))
            .Select(x => new PartidaChave(x.Partida, x.Metadados!))
            .ToList();

        if (partidasComMetadados.Count == 0)
        {
            return MontarEstruturaRodadasPadrao(partidas);
        }

        var maiorRodadaBase = partidasComMetadados
            .Where(x => !string.Equals(x.Metadados.Lado, SecaoChaveFinal, StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(x.Metadados.Lado, SecaoChaveReset, StringComparison.OrdinalIgnoreCase))
            .Select(x => ObterNumeroRodadaExibicaoChave(x.Metadados))
            .DefaultIfEmpty(1)
            .Max();

        return partidasComMetadados
            .Select(x => new
            {
                x.Partida,
                NumeroRodada = ObterNumeroRodadaExibicaoChave(x.Metadados, maiorRodadaBase),
                OrdemJogo = x.Metadados.Ordem,
                OrdemTipo = ObterOrdemTipoJogoChave(x.Metadados),
                TipoJogo = ObterTipoJogoChave(x.Metadados),
                NomeRodada = $"Rodada {ObterNumeroRodadaExibicaoChave(x.Metadados, maiorRodadaBase):00}"
            })
            .OrderBy(x => x.NumeroRodada)
            .ThenBy(x => x.OrdemTipo)
            .ThenBy(x => x.OrdemJogo)
            .GroupBy(x => new { x.NumeroRodada, x.NomeRodada })
            .Select(grupo => new RodadaEstruturaCompeticaoDto(
                grupo.Key.NumeroRodada,
                grupo.Key.NomeRodada,
                grupo.Select(x => CriarJogoRodadaCompeticaoDto(x.Partida, x.OrdemJogo, x.TipoJogo)).ToList()))
            .ToList();
    }

    private static IReadOnlyList<RodadaEstruturaCompeticaoDto> MontarEstruturaRodadasPadrao(IReadOnlyList<Partida> partidas)
    {
        return partidas
            .Select((partida, indice) => new
            {
                Partida = partida,
                NumeroRodada = ExtrairNumeroRodadaDaFase(partida.FaseCampeonato),
                NomeRodada = $"Rodada {ExtrairNumeroRodadaDaFase(partida.FaseCampeonato):00}",
                OrdemJogo = indice + 1,
                TipoJogo = ObterTipoJogoPadrao(partida.FaseCampeonato)
            })
            .OrderBy(x => x.NumeroRodada)
            .ThenBy(x => x.OrdemJogo)
            .GroupBy(x => new { x.NumeroRodada, x.NomeRodada })
            .Select(grupo => new RodadaEstruturaCompeticaoDto(
                grupo.Key.NumeroRodada,
                grupo.Key.NomeRodada,
                grupo.Select(x => CriarJogoRodadaCompeticaoDto(x.Partida, x.OrdemJogo, x.TipoJogo)).ToList()))
            .ToList();
    }

    private async Task<IReadOnlyList<SituacaoDuplaCompeticaoDto>> MontarSituacaoDuplasChaveDuplaEliminacaoAsync(
        CategoriaCompeticao categoria,
        IReadOnlyList<Dupla> duplasInscritas,
        IReadOnlyList<Partida> partidasCategoria,
        CancellationToken cancellationToken)
    {
        var partidasChave = partidasCategoria
            .Select(partida => new { Partida = partida, Metadados = ExtrairMetadadosChave(partida.Observacoes) })
            .Where(x => x.Metadados is not null && EhSecaoChaveDuplaEliminacao(x.Metadados.Lado))
            .Select(x => new PartidaChave(x.Partida, x.Metadados!))
            .ToList();

        var participantesIniciais = partidasChave.Count > 0
            ? ObterParticipantesIniciaisChaveDuplaEliminacao(partidasChave)
            : duplasInscritas.Select(x => x.Id).Distinct().ToList();

        if (participantesIniciais.Count == 0)
        {
            return [];
        }

        var controleDuplas = ConstruirControleDuplasChaveDuplaEliminacao(participantesIniciais, partidasChave);
        ValidarIntegridadeControleDuplasChaveDuplaEliminacao(controleDuplas);

        var duplasPorId = duplasInscritas
            .Where(x => participantesIniciais.Contains(x.Id))
            .ToDictionary(x => x.Id);

        var idsDuplasFaltantes = participantesIniciais
            .Where(x => !duplasPorId.ContainsKey(x))
            .ToList();

        if (idsDuplasFaltantes.Count > 0)
        {
            var duplasFaltantes = await ResolverDuplasPorIdsAsync(idsDuplasFaltantes, cancellationToken);
            foreach (var dupla in duplasFaltantes)
            {
                duplasPorId[dupla.Id] = dupla;
            }
        }

        var campeaId = ObterCampeaChaveDuplaEliminacao(partidasChave, categoria.Competicao.PossuiFinalReset);
        var finalistasIds = ObterFinalistasAtuaisChaveDuplaEliminacao(partidasChave, campeaId);
        var partidasPorId = partidasCategoria.ToDictionary(x => x.Id);

        return participantesIniciais
            .Where(duplasPorId.ContainsKey)
            .Select(duplaId =>
            {
                var estado = controleDuplas[duplaId];
                var partidaPendenteId = estado.PartidasPendentesIds.FirstOrDefault();
                Partida? partidaPendente = null;
                var possuiPartidaPendente = partidaPendenteId != Guid.Empty &&
                                            partidasPorId.TryGetValue(partidaPendenteId, out partidaPendente);
                var status = campeaId == duplaId
                    ? "Campeã"
                    : finalistasIds.Contains(duplaId)
                        ? "Finalista"
                        : estado.Status == StatusDuplaChaveDuplaEliminacao.Eliminada
                            ? "Eliminada"
                            : "Ativa";

                return new SituacaoDuplaCompeticaoDto(
                    duplaId,
                    duplasPorId[duplaId].Nome,
                    estado.QuantidadeDerrotas,
                    status,
                    campeaId == duplaId ? "Campeã" : estado.PosicaoAtual,
                    possuiPartidaPendente ? partidaPendenteId : null,
                    possuiPartidaPendente ? partidaPendente?.FaseCampeonato : null);
            })
            .OrderBy(x => x.Status)
            .ThenBy(x => x.QuantidadeDerrotas)
            .ThenBy(x => x.NomeDupla)
            .ToList();
    }

    private static Guid? ObterCampeaChaveDuplaEliminacao(
        IReadOnlyList<PartidaChave> partidasChave,
        bool possuiFinalReset)
    {
        var reset = ObterPartidaUnicaPorSecao(partidasChave, SecaoChaveReset);
        if (reset?.Partida.Status == StatusPartida.Encerrada && reset.Partida.DuplaVencedoraId.HasValue)
        {
            return reset.Partida.DuplaVencedoraId.Value;
        }

        var final = ObterPartidaUnicaPorSecao(partidasChave, SecaoChaveFinal);
        if (final?.Partida.Status != StatusPartida.Encerrada || !final.Partida.DuplaVencedoraId.HasValue)
        {
            return null;
        }

        if (!possuiFinalReset)
        {
            return final.Partida.DuplaVencedoraId.Value;
        }

        var duplaInvictaId = ObterDuplaInvictaDaFinal(final);
        return final.Partida.DuplaVencedoraId.Value == duplaInvictaId
            ? duplaInvictaId
            : null;
    }

    private static ISet<Guid> ObterFinalistasAtuaisChaveDuplaEliminacao(
        IReadOnlyList<PartidaChave> partidasChave,
        Guid? campeaId)
    {
        if (campeaId.HasValue)
        {
            return new HashSet<Guid>();
        }

        var reset = ObterPartidaUnicaPorSecao(partidasChave, SecaoChaveReset);
        if (reset is not null)
        {
            return new[]
            {
                reset.Partida.DuplaAId,
                reset.Partida.DuplaBId
            }
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .ToHashSet();
        }

        var final = ObterPartidaUnicaPorSecao(partidasChave, SecaoChaveFinal);
        if (final is not null)
        {
            return new[]
            {
                final.Partida.DuplaAId,
                final.Partida.DuplaBId
            }
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .ToHashSet();
        }

        return new HashSet<Guid>();
    }

    private static Guid ObterDuplaInvictaDaFinal(PartidaChave final)
    {
        var duplaInvictaId = final.Metadados.DuplasEmEspera.FirstOrDefault();
        return duplaInvictaId == Guid.Empty
            ? final.Partida.DuplaAId ?? throw new RegraNegocioException("A final precisa ter a dupla invicta definida.")
            : duplaInvictaId;
    }

    private static JogoRodadaCompeticaoDto CriarJogoRodadaCompeticaoDto(Partida partida, int ordemJogo, string tipoJogo)
    {
        return new JogoRodadaCompeticaoDto(
            partida.Id,
            ordemJogo,
            tipoJogo,
            partida.FaseCampeonato,
            partida.Status,
            partida.DuplaAId,
            partida.DuplaA?.Nome ?? string.Empty,
            partida.DuplaBId,
            partida.DuplaB?.Nome ?? string.Empty,
            partida.PlacarDuplaA,
            partida.PlacarDuplaB,
            partida.DuplaVencedoraId,
            partida.DuplaVencedora?.Nome,
            partida.DataPartida);
    }

    private static int ObterNumeroRodadaExibicaoChave(MetadadosChave metadados, int maiorRodadaBase = 1)
    {
        if (string.Equals(metadados.Lado, SecaoChaveVencedores, StringComparison.OrdinalIgnoreCase))
        {
            return (metadados.Rodada * 2) - 1;
        }

        if (string.Equals(metadados.Lado, SecaoChavePerdedores, StringComparison.OrdinalIgnoreCase))
        {
            return metadados.Rodada + 1;
        }

        if (string.Equals(metadados.Lado, SecaoChaveFinal, StringComparison.OrdinalIgnoreCase))
        {
            return maiorRodadaBase + 1;
        }

        if (string.Equals(metadados.Lado, SecaoChaveReset, StringComparison.OrdinalIgnoreCase))
        {
            return maiorRodadaBase + 2;
        }

        return metadados.Rodada;
    }

    private static int ObterOrdemTipoJogoChave(MetadadosChave metadados)
    {
        if (string.Equals(metadados.Lado, SecaoChaveVencedores, StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (string.Equals(metadados.Lado, SecaoChavePerdedores, StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        if (string.Equals(metadados.Lado, SecaoChaveFinal, StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        if (string.Equals(metadados.Lado, SecaoChaveReset, StringComparison.OrdinalIgnoreCase))
        {
            return 4;
        }

        return 9;
    }

    private static string ObterTipoJogoChave(MetadadosChave metadados)
    {
        if (string.Equals(metadados.Lado, SecaoChaveVencedores, StringComparison.OrdinalIgnoreCase))
        {
            return "Chave vencedora";
        }

        if (string.Equals(metadados.Lado, SecaoChavePerdedores, StringComparison.OrdinalIgnoreCase))
        {
            return "Chave perdedora";
        }

        if (string.Equals(metadados.Lado, SecaoChaveFinal, StringComparison.OrdinalIgnoreCase))
        {
            return "Final";
        }

        if (string.Equals(metadados.Lado, SecaoChaveReset, StringComparison.OrdinalIgnoreCase))
        {
            return "Final reset";
        }

        return "Jogo";
    }

    private static string ObterTipoJogoPadrao(string? faseCampeonato)
    {
        if (string.IsNullOrWhiteSpace(faseCampeonato))
        {
            return "Jogo";
        }

        if (faseCampeonato.Contains("final de reset", StringComparison.OrdinalIgnoreCase))
        {
            return "Final reset";
        }

        if (string.Equals(faseCampeonato, NomeFaseFinal, StringComparison.OrdinalIgnoreCase))
        {
            return "Final";
        }

        if (faseCampeonato.Contains("chave dos vencedores", StringComparison.OrdinalIgnoreCase))
        {
            return "Chave vencedora";
        }

        if (faseCampeonato.Contains("chave dos perdedores", StringComparison.OrdinalIgnoreCase))
        {
            return "Chave perdedora";
        }

        if (faseCampeonato.Contains("grupo", StringComparison.OrdinalIgnoreCase))
        {
            return "Fase de grupos";
        }

        if (faseCampeonato.Contains("classificatória", StringComparison.OrdinalIgnoreCase))
        {
            return "Fase classificatória";
        }

        if (faseCampeonato.Contains("eliminatória", StringComparison.OrdinalIgnoreCase))
        {
            return "Fase eliminatória";
        }

        return faseCampeonato;
    }

    private static int ExtrairNumeroRodadaDaFase(string? faseCampeonato)
    {
        if (string.IsNullOrWhiteSpace(faseCampeonato))
        {
            return 1;
        }

        var indiceRodada = faseCampeonato.IndexOf("rodada", StringComparison.OrdinalIgnoreCase);
        if (indiceRodada < 0)
        {
            return 1;
        }

        var trecho = faseCampeonato[(indiceRodada + "rodada".Length)..].Trim();
        var numeros = new string(trecho.TakeWhile(char.IsDigit).ToArray());
        return int.TryParse(numeros, out var rodada) && rodada > 0 ? rodada : 1;
    }

    private static EstadoRodadaChave AvaliarRodadaChave(
        string secao,
        int rodada,
        IReadOnlyList<Guid> participantes,
        IReadOnlyList<PartidaChave> partidas)
    {
        var planejamento = PlanejarRodadaChave(participantes);
        if (participantes.Count <= 1 || planejamento.Confrontos.Count == 0)
        {
            return new EstadoRodadaChave(
                secao,
                rodada,
                participantes,
                planejamento,
                partidas,
                true,
                participantes,
                []);
        }

        var partidasPorOrdem = partidas
            .GroupBy(x => x.Metadados.Ordem)
            .ToDictionary(x => x.Key, x => x.First());
        var partidasPlanejadas = planejamento.Confrontos
            .Where(x => partidasPorOrdem.ContainsKey(x.Ordem))
            .Select(x => partidasPorOrdem[x.Ordem])
            .ToList();
        var concluiuTodosConfrontos = partidasPlanejadas.Count == planejamento.Confrontos.Count &&
                                      partidasPlanejadas.All(x => x.Partida.Status == StatusPartida.Encerrada &&
                                                                  x.Partida.DuplaVencedoraId.HasValue);

        if (!concluiuTodosConfrontos)
        {
            return new EstadoRodadaChave(
                secao,
                rodada,
                participantes,
                planejamento,
                partidas,
                false,
                [],
                []);
        }

        var classificados = planejamento.DuplasEmEspera
            .Concat(partidasPlanejadas.Select(x => x.Partida.DuplaVencedoraId!.Value))
            .ToList();
        var derrotados = partidasPlanejadas
            .Select(x => ObterDuplaPerdedoraId(x.Partida))
            .ToList();

        return new EstadoRodadaChave(
            secao,
            rodada,
            participantes,
            planejamento,
            partidasPlanejadas,
            true,
            classificados,
            derrotados);
    }

    private static PlanejamentoRodadaChave PlanejarRodadaChave(IReadOnlyList<Guid> participantes)
    {
        if (participantes.Count == 0)
        {
            return new PlanejamentoRodadaChave([], []);
        }

        var tamanhoChave = 1;
        while (tamanhoChave < participantes.Count)
        {
            tamanhoChave *= 2;
        }

        var quantidadeByes = tamanhoChave - participantes.Count;
        var duplasEmEspera = participantes.Take(quantidadeByes).ToList();
        var duplasComJogo = participantes.Skip(quantidadeByes).ToList();
        var confrontos = new List<ConfrontoPlanejadoChave>();

        for (var indice = 0; indice + 1 < duplasComJogo.Count; indice += 2)
        {
            confrontos.Add(new ConfrontoPlanejadoChave(
                duplasComJogo[indice],
                duplasComJogo[indice + 1],
                (indice / 2) + 1));
        }

        return new PlanejamentoRodadaChave(duplasEmEspera, confrontos);
    }

    private static Guid? ObterCampeaoSecao(
        IReadOnlyDictionary<int, EstadoRodadaChave> estados,
        int ultimaRodada)
    {
        if (!estados.TryGetValue(ultimaRodada, out var estadoFinal) ||
            !estadoFinal.Concluida ||
            estadoFinal.Classificados.Count != 1)
        {
            return null;
        }

        return estadoFinal.Classificados[0];
    }

    private static Guid ObterDuplaPerdedoraId(Partida partida)
    {
        if (!partida.DuplaVencedoraId.HasValue)
        {
            throw new RegraNegocioException("A partida precisa estar encerrada para identificar a dupla derrotada.");
        }

        if (!partida.DuplaAId.HasValue || !partida.DuplaBId.HasValue)
        {
            throw new RegraNegocioException("A partida precisa ter as duas duplas definidas para identificar a dupla derrotada.");
        }

        return partida.DuplaVencedoraId.Value == partida.DuplaAId
            ? partida.DuplaBId.Value
            : partida.DuplaAId.Value;
    }

    private static int CalcularQuantidadeRodadasChave(int quantidadeParticipantes)
    {
        var tamanhoChave = 1;
        var quantidadeRodadas = 0;

        while (tamanhoChave < quantidadeParticipantes)
        {
            tamanhoChave *= 2;
        }

        while (tamanhoChave > 1)
        {
            quantidadeRodadas++;
            tamanhoChave /= 2;
        }

        return quantidadeRodadas;
    }

    private static bool EhNomeFaseGrupo(string? nomeFaseBase)
    {
        return !string.IsNullOrWhiteSpace(nomeFaseBase) &&
               nomeFaseBase.StartsWith("Grupo ", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<IReadOnlyList<ClassificacaoGrupoFaseDeGrupos>> MontarClassificacaoFaseDeGruposAsync(
        CategoriaCompeticao categoria,
        IReadOnlyList<PartidaRodada> partidasGrupo,
        int classificadosPorGrupo,
        CancellationToken cancellationToken)
    {
        var grupos = partidasGrupo
            .GroupBy(x => x.Metadados.NomeFaseBase!, StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var classificacoes = new List<ClassificacaoGrupoFaseDeGrupos>();

        foreach (var grupo in grupos)
        {
            if (grupo.Any(x => x.Partida.Status != StatusPartida.Encerrada))
            {
                return [];
            }

            var referencia = grupo.First().Metadados;
            var duplas = await ResolverDuplasPorIdsAsync(referencia.OrdemDuplas, cancellationToken);
            var desempenhoPorDupla = duplas.ToDictionary(
                dupla => dupla.Id,
                dupla => new DesempenhoGrupoFaseDeGrupos(
                    dupla.Id,
                    dupla.Nome,
                    grupo.Key,
                    0,
                    0,
                    0m,
                    0,
                    0));

            foreach (var partida in grupo.OrderBy(x => x.Metadados.NumeroRodada).ThenBy(x => x.Metadados.OrdemConfronto))
            {
                var duplaAId = partida.Partida.DuplaAId
                    ?? throw new RegraNegocioException("A fase de grupos contém partida sem dupla A definida.");
                var duplaBId = partida.Partida.DuplaBId
                    ?? throw new RegraNegocioException("A fase de grupos contém partida sem dupla B definida.");

                var desempenhoA = desempenhoPorDupla[duplaAId];
                var desempenhoB = desempenhoPorDupla[duplaBId];

                desempenhoA = desempenhoA with
                {
                    Jogos = desempenhoA.Jogos + 1,
                    PontosMarcados = desempenhoA.PontosMarcados + partida.Partida.PlacarDuplaA,
                    PontosSofridos = desempenhoA.PontosSofridos + partida.Partida.PlacarDuplaB
                };
                desempenhoB = desempenhoB with
                {
                    Jogos = desempenhoB.Jogos + 1,
                    PontosMarcados = desempenhoB.PontosMarcados + partida.Partida.PlacarDuplaB,
                    PontosSofridos = desempenhoB.PontosSofridos + partida.Partida.PlacarDuplaA
                };

                if (partida.Partida.DuplaVencedoraId == partida.Partida.DuplaAId)
                {
                    desempenhoA = desempenhoA with
                    {
                        Vitorias = desempenhoA.Vitorias + 1,
                        PontosClassificacao = desempenhoA.PontosClassificacao + categoria.Competicao.ObterPontosVitoria()
                    };
                    desempenhoB = desempenhoB with
                    {
                        PontosClassificacao = desempenhoB.PontosClassificacao + categoria.Competicao.ObterPontosDerrota()
                    };
                }
                else if (partida.Partida.DuplaVencedoraId == partida.Partida.DuplaBId)
                {
                    desempenhoB = desempenhoB with
                    {
                        Vitorias = desempenhoB.Vitorias + 1,
                        PontosClassificacao = desempenhoB.PontosClassificacao + categoria.Competicao.ObterPontosVitoria()
                    };
                    desempenhoA = desempenhoA with
                    {
                        PontosClassificacao = desempenhoA.PontosClassificacao + categoria.Competicao.ObterPontosDerrota()
                    };
                }

                desempenhoPorDupla[duplaAId] = desempenhoA;
                desempenhoPorDupla[duplaBId] = desempenhoB;
            }

            var classificacaoGrupo = desempenhoPorDupla.Values
                .OrderByDescending(x => x.PontosClassificacao)
                .ThenByDescending(x => x.Vitorias)
                .ThenByDescending(x => x.SaldoPontos)
                .ThenByDescending(x => x.PontosMarcados)
                .ThenBy(x => x.NomeDupla, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (classificacaoGrupo.Count < classificadosPorGrupo)
            {
                throw new RegraNegocioException($"O {grupo.Key} não possui duplas suficientes para classificar {classificadosPorGrupo} dupla(s) ao mata-mata.");
            }

            classificacoes.Add(new ClassificacaoGrupoFaseDeGrupos(grupo.Key, classificacaoGrupo));
        }

        return classificacoes;
    }

    private static IReadOnlyList<ClassificadoGrupoEliminatoria> MontarClassificadosOrdenadosParaEliminatoria(
        IReadOnlyList<ClassificacaoGrupoFaseDeGrupos> classificacoes,
        int classificadosPorGrupo)
    {
        var classificados = new List<ClassificadoGrupoEliminatoria>();

        for (var posicao = 1; posicao <= classificadosPorGrupo; posicao++)
        {
            foreach (var grupo in classificacoes)
            {
                var dupla = grupo.Classificacao.Skip(posicao - 1).FirstOrDefault();
                if (dupla is null)
                {
                    continue;
                }

                classificados.Add(new ClassificadoGrupoEliminatoria(
                    dupla.DuplaId,
                    dupla.NomeDupla,
                    grupo.NomeGrupo,
                    posicao,
                    dupla.PontosClassificacao,
                    dupla.Vitorias,
                    dupla.SaldoPontos,
                    dupla.PontosMarcados));
            }
        }

        return classificados;
    }

    private async Task<IReadOnlyList<Partida>> CriarPrimeiraRodadaEliminatoriaFaseDeGruposAsync(
        CategoriaCompeticao categoria,
        IReadOnlyList<ClassificadoGrupoEliminatoria> classificadosOrdenados,
        CancellationToken cancellationToken)
    {
        var planejamento = PlanejarPrimeiraRodadaEliminatoriaFaseDeGrupos(classificadosOrdenados);
        if (planejamento.Confrontos.Count == 0)
        {
            return [];
        }

        var idsDuplas = planejamento.Confrontos
            .SelectMany(x => new[] { x.DuplaAId, x.DuplaBId })
            .Distinct()
            .ToList();
        var duplas = await ResolverDuplasPorIdsAsync(idsDuplas, cancellationToken);
        var duplasPorId = duplas.ToDictionary(x => x.Id);
        var fase = MontarNomeFaseEliminatoriaGrupos(1);

        return planejamento.Confrontos
            .Select(confronto => CriarPartidaAgendada(
                categoria,
                duplasPorId[confronto.DuplaAId],
                duplasPorId[confronto.DuplaBId],
                fase,
                new MetadadosChave(SecaoEliminatoriaGrupos, 1, confronto.Ordem, planejamento.DuplasEmEspera)))
            .ToList();
    }

    private async Task<IReadOnlyList<Partida>> CriarPartidasRodadaEliminatoriaFaseDeGruposAsync(
        CategoriaCompeticao categoria,
        int rodada,
        IReadOnlyList<Guid> participantes,
        CancellationToken cancellationToken)
    {
        var planejamento = PlanejarRodadaChave(participantes);
        if (planejamento.Confrontos.Count == 0)
        {
            return [];
        }

        var idsDuplas = planejamento.Confrontos
            .SelectMany(x => new[] { x.DuplaAId, x.DuplaBId })
            .Distinct()
            .ToList();
        var duplas = await ResolverDuplasPorIdsAsync(idsDuplas, cancellationToken);
        var duplasPorId = duplas.ToDictionary(x => x.Id);
        var fase = MontarNomeFaseEliminatoriaGrupos(rodada);

        return planejamento.Confrontos
            .Select(confronto => CriarPartidaAgendada(
                categoria,
                duplasPorId[confronto.DuplaAId],
                duplasPorId[confronto.DuplaBId],
                fase,
                new MetadadosChave(SecaoEliminatoriaGrupos, rodada, confronto.Ordem, planejamento.DuplasEmEspera)))
            .ToList();
    }

    private static PlanejamentoRodadaChave PlanejarPrimeiraRodadaEliminatoriaFaseDeGrupos(
        IReadOnlyList<ClassificadoGrupoEliminatoria> classificadosOrdenados)
    {
        if (classificadosOrdenados.Count == 0)
        {
            return new PlanejamentoRodadaChave([], []);
        }

        var tamanhoChave = 1;
        while (tamanhoChave < classificadosOrdenados.Count)
        {
            tamanhoChave *= 2;
        }

        var quantidadeByes = tamanhoChave - classificadosOrdenados.Count;
        var duplasEmEspera = classificadosOrdenados
            .Take(quantidadeByes)
            .Select(x => x.DuplaId)
            .ToList();
        var sementesRestantes = classificadosOrdenados.Skip(quantidadeByes).ToList();
        var confrontos = new List<ConfrontoPlanejadoChave>();

        while (sementesRestantes.Count > 1)
        {
            var mandante = sementesRestantes[0];
            sementesRestantes.RemoveAt(0);

            var indiceAdversario = -1;
            for (var indice = sementesRestantes.Count - 1; indice >= 0; indice--)
            {
                if (!string.Equals(sementesRestantes[indice].NomeGrupo, mandante.NomeGrupo, StringComparison.OrdinalIgnoreCase))
                {
                    indiceAdversario = indice;
                    break;
                }
            }

            if (indiceAdversario < 0)
            {
                indiceAdversario = sementesRestantes.Count - 1;
            }

            var adversario = sementesRestantes[indiceAdversario];
            sementesRestantes.RemoveAt(indiceAdversario);

            confrontos.Add(new ConfrontoPlanejadoChave(
                mandante.DuplaId,
                adversario.DuplaId,
                confrontos.Count + 1));
        }

        return new PlanejamentoRodadaChave(duplasEmEspera, confrontos);
    }

    private static EstadoRodadaChave AvaliarPrimeiraRodadaEliminatoriaFaseDeGrupos(
        IReadOnlyList<ClassificadoGrupoEliminatoria> classificadosOrdenados,
        IReadOnlyList<PartidaChave> partidas)
    {
        var planejamento = PlanejarPrimeiraRodadaEliminatoriaFaseDeGrupos(classificadosOrdenados);
        var participantes = classificadosOrdenados.Select(x => x.DuplaId).ToList();

        if (participantes.Count <= 1 || planejamento.Confrontos.Count == 0)
        {
            return new EstadoRodadaChave(
                SecaoEliminatoriaGrupos,
                1,
                participantes,
                planejamento,
                partidas,
                true,
                participantes,
                []);
        }

        var partidasPorOrdem = partidas
            .GroupBy(x => x.Metadados.Ordem)
            .ToDictionary(x => x.Key, x => x.First());
        var partidasPlanejadas = planejamento.Confrontos
            .Where(x => partidasPorOrdem.ContainsKey(x.Ordem))
            .Select(x => partidasPorOrdem[x.Ordem])
            .ToList();
        var concluiuTodosConfrontos = partidasPlanejadas.Count == planejamento.Confrontos.Count &&
                                      partidasPlanejadas.All(x => x.Partida.Status == StatusPartida.Encerrada &&
                                                                  x.Partida.DuplaVencedoraId.HasValue);

        if (!concluiuTodosConfrontos)
        {
            return new EstadoRodadaChave(
                SecaoEliminatoriaGrupos,
                1,
                participantes,
                planejamento,
                partidas,
                false,
                [],
                []);
        }

        var classificados = planejamento.DuplasEmEspera
            .Concat(partidasPlanejadas.Select(x => x.Partida.DuplaVencedoraId!.Value))
            .ToList();
        var derrotados = partidasPlanejadas
            .Select(x => ObterDuplaPerdedoraId(x.Partida))
            .ToList();

        return new EstadoRodadaChave(
            SecaoEliminatoriaGrupos,
            1,
            participantes,
            planejamento,
            partidasPlanejadas,
            true,
            classificados,
            derrotados);
    }

    private static string MontarNomeFaseEliminatoriaGrupos(int rodada)
    {
        return $"{NomeFaseEliminatoriaGrupos} - Rodada {rodada:00}";
    }

    private static IReadOnlyList<PartidaChave>? ObterRodadaMaisAltaConcluida(
        IReadOnlyList<PartidaChave> partidasChave,
        string lado)
    {
        var partidasLado = partidasChave
            .Where(x => string.Equals(x.Metadados.Lado, lado, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (partidasLado.Count == 0)
        {
            return null;
        }

        var rodadaMaisAlta = partidasLado.Max(x => x.Metadados.Rodada);
        var partidasRodada = partidasLado
            .Where(x => x.Metadados.Rodada == rodadaMaisAlta)
            .OrderBy(x => x.Metadados.Ordem)
            .ToList();

        if (partidasRodada.Any(x => x.Partida.Status != StatusPartida.Encerrada || !x.Partida.DuplaVencedoraId.HasValue))
        {
            return null;
        }

        return partidasRodada;
    }

    private async Task<IReadOnlyList<Dupla>> ResolverDuplasPorIdsAsync(
        IReadOnlyList<Guid> idsDuplas,
        CancellationToken cancellationToken)
    {
        var duplas = new List<Dupla>();

        foreach (var idDupla in idsDuplas)
        {
            var dupla = await duplaRepositorio.ObterPorIdAsync(idDupla, cancellationToken);
            if (dupla is null)
            {
                throw new RegraNegocioException("Uma das duplas da chave não foi encontrada para avançar a tabela.");
            }

            duplas.Add(dupla);
        }

        return duplas;
    }

    private static List<Partida> GerarPartidasRodadaChave(
        CategoriaCompeticao categoria,
        IReadOnlyList<Dupla> duplas,
        string lado,
        int rodada)
    {
        return GerarPartidasRodadaEliminatoria(
            categoria,
            duplas,
            lado.ToUpperInvariant(),
            rodada,
            $"Chave {lado.ToUpperInvariant()} - Rodada {rodada:00}");
    }

    private static List<Partida> GerarPartidasRodadaEliminatoria(
        CategoriaCompeticao categoria,
        IReadOnlyList<Dupla> duplas,
        string secao,
        int rodada,
        string nomeFase)
    {
        var tamanhoChave = 1;
        while (tamanhoChave < duplas.Count)
        {
            tamanhoChave *= 2;
        }

        var quantidadeByes = tamanhoChave - duplas.Count;
        var duplasEmEspera = duplas.Take(quantidadeByes).ToList();
        var duplasComJogo = duplas.Skip(quantidadeByes).ToList();
        if (duplasComJogo.Count < 2)
        {
            throw new RegraNegocioException("Não há confrontos suficientes para gerar a rodada da chave.");
        }

        var partidas = new List<Partida>();
        var idsDuplasEmEspera = duplasEmEspera.Select(x => x.Id).ToList();

        for (var indice = 0; indice + 1 < duplasComJogo.Count; indice += 2)
        {
            partidas.Add(CriarPartidaAgendada(
                categoria,
                duplasComJogo[indice],
                duplasComJogo[indice + 1],
                nomeFase,
                new MetadadosChave(secao, rodada, (indice / 2) + 1, idsDuplasEmEspera)));
        }

        return partidas;
    }

    private static string MontarNomeFaseChaveDuplaEliminacao(string secao, int rodada)
    {
        return secao switch
        {
            SecaoChaveVencedores => $"{NomeFaseChaveVencedores} - Rodada {rodada:00}",
            SecaoChavePerdedores => $"{NomeFaseChavePerdedores} - Rodada {rodada:00}",
            SecaoChaveFinal => NomeFaseFinal,
            SecaoChaveReset => NomeFaseFinalReset,
            _ => $"Chave - Rodada {rodada:00}"
        };
    }

    private static List<Partida> GerarChaveDuplaEliminacaoCompleta(
        CategoriaCompeticao categoria,
        IReadOnlyList<Dupla> duplas)
    {
        var participantesIniciais = duplas
            .Select(dupla => ParticipanteChave.Direto(dupla.Id))
            .ToList();
        var quantidadeRodadasVencedores = CalcularQuantidadeRodadasChave(duplas.Count);
        var quantidadeRodadasPerdedores = Math.Max(0, (quantidadeRodadasVencedores * 2) - 2);
        var possuiPreliminar = !EhPotenciaDeDois(duplas.Count);
        var partidas = new List<Partida>();
        var resultadosVencedores = new Dictionary<int, ResultadoRodadaChaveCompleta>();
        var participantesRodadaVencedores = (IReadOnlyList<ParticipanteChave>)participantesIniciais;

        for (var rodada = 1; rodada <= quantidadeRodadasVencedores; rodada++)
        {
            var resultado = CriarRodadaChaveAutomatica(
                categoria,
                participantesRodadaVencedores,
                LadoDaChave.Vencedores,
                rodada,
                rodada == 1 && possuiPreliminar);
            resultadosVencedores[rodada] = resultado;
            partidas.AddRange(resultado.Partidas);
            participantesRodadaVencedores = resultado.Classificados;
        }

        var resultadosPerdedores = new Dictionary<int, ResultadoRodadaChaveCompleta>();

        for (var rodada = 1; rodada <= quantidadeRodadasPerdedores; rodada++)
        {
            IReadOnlyList<ParticipanteChave>? participantesRodadaPerdedores = rodada switch
            {
                1 => resultadosVencedores.GetValueOrDefault(1)?.Derrotados,
                _ when rodada % 2 != 0 => resultadosPerdedores.GetValueOrDefault(rodada - 1)?.Classificados,
                _ => resultadosPerdedores.GetValueOrDefault(rodada - 1)?.Classificados is not null &&
                     resultadosVencedores.GetValueOrDefault((rodada / 2) + 1)?.Derrotados is not null
                    ? resultadosPerdedores[rodada - 1].Classificados
                        .Concat(resultadosVencedores[(rodada / 2) + 1].Derrotados)
                        .ToList()
                    : null
            };

            if (participantesRodadaPerdedores is null)
            {
                break;
            }

            var resultado = CriarRodadaChaveAutomatica(
                categoria,
                participantesRodadaPerdedores,
                LadoDaChave.Perdedores,
                rodada,
                false);
            resultadosPerdedores[rodada] = resultado;
            partidas.AddRange(resultado.Partidas);
        }

        var campeaoVencedores = resultadosVencedores.GetValueOrDefault(quantidadeRodadasVencedores)?.Classificados.SingleOrDefault();
        var campeaoPerdedores = resultadosPerdedores.GetValueOrDefault(quantidadeRodadasPerdedores)?.Classificados.SingleOrDefault();

        if (campeaoVencedores is not null && campeaoPerdedores is not null)
        {
            partidas.Add(CriarPartidaChaveAutomatica(
                categoria,
                campeaoVencedores,
                campeaoPerdedores,
                LadoDaChave.Final,
                1,
                1,
                false,
                true,
                false,
                NomeFaseFinal));
        }

        if (categoria.Competicao.PossuiFinalReset)
        {
            partidas.Add(new Partida
            {
                CategoriaCompeticaoId = categoria.Id,
                CategoriaCompeticao = categoria,
                FaseCampeonato = NomeFaseFinalReset,
                LadoDaChave = LadoDaChave.Finalissima,
                Rodada = 1,
                PosicaoNaChave = 1,
                Ativa = false,
                EhPreliminar = false,
                EhFinal = false,
                EhFinalissima = true,
                Status = StatusPartida.Agendada,
                StatusAprovacao = StatusAprovacaoPartida.Aprovada,
                PlacarDuplaA = 0,
                PlacarDuplaB = 0,
                Observacoes = "Tabela gerada automaticamente."
            });
        }

        VincularFluxoChaveGerada(partidas);
        SincronizarEstadoPartidasGeradas(partidas, categoria.Competicao.PossuiFinalReset);
        return partidas;
    }

    private static ResultadoRodadaChaveCompleta CriarRodadaChaveAutomatica(
        CategoriaCompeticao categoria,
        IReadOnlyList<ParticipanteChave> participantes,
        LadoDaChave ladoDaChave,
        int rodada,
        bool ehPreliminar)
    {
        var planejamento = PlanejarRodadaChaveParticipantes(participantes);
        if (participantes.Count <= 1 || planejamento.Confrontos.Count == 0)
        {
            return new ResultadoRodadaChaveCompleta([], participantes, []);
        }

        var partidas = planejamento.Confrontos
            .Select(confronto => CriarPartidaChaveAutomatica(
                categoria,
                confronto.ParticipanteA,
                confronto.ParticipanteB,
                ladoDaChave,
                rodada,
                confronto.Ordem,
                ehPreliminar,
                false,
                false,
                MontarNomeFaseChaveDuplaEliminacao(ObterSecaoChave(ladoDaChave), rodada)))
            .ToList();

        var classificados = planejamento.ParticipantesEmEspera
            .Concat(partidas.Select(partida => ParticipanteChave.Vencedor(partida.Id)))
            .ToList();
        var derrotados = partidas
            .Select(partida => ParticipanteChave.Perdedor(partida.Id))
            .ToList();

        return new ResultadoRodadaChaveCompleta(partidas, classificados, derrotados);
    }

    private static PlanejamentoRodadaChaveParticipantes PlanejarRodadaChaveParticipantes(
        IReadOnlyList<ParticipanteChave> participantes)
    {
        if (participantes.Count == 0)
        {
            return new PlanejamentoRodadaChaveParticipantes([], []);
        }

        var tamanhoChave = 1;
        while (tamanhoChave < participantes.Count)
        {
            tamanhoChave *= 2;
        }

        var quantidadeByes = tamanhoChave - participantes.Count;
        var participantesEmEspera = participantes.Take(quantidadeByes).ToList();
        var participantesComJogo = participantes.Skip(quantidadeByes).ToList();
        var confrontos = new List<ConfrontoPlanejadoChaveParticipantes>();

        for (var indice = 0; indice + 1 < participantesComJogo.Count; indice += 2)
        {
            confrontos.Add(new ConfrontoPlanejadoChaveParticipantes(
                participantesComJogo[indice],
                participantesComJogo[indice + 1],
                (indice / 2) + 1));
        }

        return new PlanejamentoRodadaChaveParticipantes(participantesEmEspera, confrontos);
    }

    private static Partida CriarPartidaChaveAutomatica(
        CategoriaCompeticao categoria,
        ParticipanteChave participanteA,
        ParticipanteChave participanteB,
        LadoDaChave ladoDaChave,
        int rodada,
        int posicaoNaChave,
        bool ehPreliminar,
        bool ehFinal,
        bool ehFinalissima,
        string faseCampeonato)
    {
        var partida = new Partida
        {
            CategoriaCompeticaoId = categoria.Id,
            CategoriaCompeticao = categoria,
            DuplaAId = participanteA.DuplaId,
            DuplaBId = participanteB.DuplaId,
            FaseCampeonato = faseCampeonato,
            LadoDaChave = ladoDaChave,
            Rodada = rodada,
            PosicaoNaChave = posicaoNaChave,
            PartidaOrigemParticipanteAId = participanteA.PartidaOrigemId,
            OrigemParticipanteATipo = participanteA.OrigemTipo,
            PartidaOrigemParticipanteBId = participanteB.PartidaOrigemId,
            OrigemParticipanteBTipo = participanteB.OrigemTipo,
            Ativa = participanteA.DuplaId.HasValue && participanteB.DuplaId.HasValue && !ehFinalissima,
            EhPreliminar = ehPreliminar,
            EhFinal = ehFinal,
            EhFinalissima = ehFinalissima,
            Status = StatusPartida.Agendada,
            StatusAprovacao = StatusAprovacaoPartida.Aprovada,
            PlacarDuplaA = 0,
            PlacarDuplaB = 0,
            Observacoes = "Tabela gerada automaticamente."
        };

        return partida;
    }

    private static void VincularFluxoChaveGerada(IReadOnlyList<Partida> partidas)
    {
        var partidasPorId = partidas.ToDictionary(x => x.Id);

        foreach (var partida in partidas)
        {
            VincularOrigemComDestino(
                partidasPorId,
                partida.PartidaOrigemParticipanteAId,
                partida.OrigemParticipanteATipo,
                partida.Id,
                SlotDestinoPartida.ParticipanteA);
            VincularOrigemComDestino(
                partidasPorId,
                partida.PartidaOrigemParticipanteBId,
                partida.OrigemParticipanteBTipo,
                partida.Id,
                SlotDestinoPartida.ParticipanteB);
        }
    }

    private static void VincularOrigemComDestino(
        IReadOnlyDictionary<Guid, Partida> partidasPorId,
        Guid? partidaOrigemId,
        OrigemClassificacaoPartida? origemTipo,
        Guid partidaDestinoId,
        SlotDestinoPartida slotDestino)
    {
        if (!partidaOrigemId.HasValue ||
            !origemTipo.HasValue ||
            !partidasPorId.TryGetValue(partidaOrigemId.Value, out var partidaOrigem))
        {
            return;
        }

        if (origemTipo == OrigemClassificacaoPartida.Vencedor)
        {
            partidaOrigem.ProximaPartidaVencedorId = partidaDestinoId;
            partidaOrigem.SlotDestinoVencedor = slotDestino;
            return;
        }

        partidaOrigem.ProximaPartidaPerdedorId = partidaDestinoId;
        partidaOrigem.SlotDestinoPerdedor = slotDestino;
    }

    private static void SincronizarEstadoPartidasGeradas(IReadOnlyList<Partida> partidas, bool possuiFinalReset)
    {
        var partidasPorId = partidas.ToDictionary(x => x.Id);

        foreach (var partida in partidas)
        {
            if (partida.PartidaOrigemParticipanteAId.HasValue)
            {
                partida.DuplaAId = ResolverParticipanteChave(partidasPorId, partida.PartidaOrigemParticipanteAId, partida.OrigemParticipanteATipo);
            }

            if (partida.PartidaOrigemParticipanteBId.HasValue)
            {
                partida.DuplaBId = ResolverParticipanteChave(partidasPorId, partida.PartidaOrigemParticipanteBId, partida.OrigemParticipanteBTipo);
            }

            if (partida.EhFinalissima)
            {
                var final = partidas.FirstOrDefault(x => x.EhFinal && !x.EhFinalissima);
                if (final is not null && final.Status == StatusPartida.Encerrada && final.DuplaVencedoraId == final.DuplaBId)
                {
                    partida.DuplaAId = final.DuplaAId;
                    partida.DuplaBId = final.DuplaBId;
                    partida.Ativa = possuiFinalReset && partida.DuplaAId.HasValue && partida.DuplaBId.HasValue;
                }
                else
                {
                    partida.DuplaAId = null;
                    partida.DuplaBId = null;
                    partida.Ativa = false;
                }
            }
            else
            {
                partida.Ativa = partida.DuplaAId.HasValue && partida.DuplaBId.HasValue;
            }

            if (!partida.Ativa && partida.Status != StatusPartida.Encerrada)
            {
                partida.PlacarDuplaA = 0;
                partida.PlacarDuplaB = 0;
                partida.DuplaVencedoraId = null;
            }
        }
    }

    private static Guid? ResolverParticipanteChave(
        IReadOnlyDictionary<Guid, Partida> partidasPorId,
        Guid? partidaOrigemId,
        OrigemClassificacaoPartida? origemTipo)
    {
        if (!partidaOrigemId.HasValue ||
            !origemTipo.HasValue ||
            !partidasPorId.TryGetValue(partidaOrigemId.Value, out var partidaOrigem) ||
            partidaOrigem.Status != StatusPartida.Encerrada)
        {
            return null;
        }

        if (origemTipo == OrigemClassificacaoPartida.Vencedor)
        {
            return partidaOrigem.DuplaVencedoraId;
        }

        return partidaOrigem.DuplaVencedoraId == partidaOrigem.DuplaAId
            ? partidaOrigem.DuplaBId
            : partidaOrigem.DuplaAId;
    }

    private static bool EhPotenciaDeDois(int valor)
    {
        return valor > 0 && (valor & (valor - 1)) == 0;
    }

    private static string ObterSecaoChave(LadoDaChave ladoDaChave)
    {
        return ladoDaChave switch
        {
            LadoDaChave.Vencedores => SecaoChaveVencedores,
            LadoDaChave.Perdedores => SecaoChavePerdedores,
            LadoDaChave.Final => SecaoChaveFinal,
            LadoDaChave.Finalissima => SecaoChaveReset,
            _ => SecaoChaveVencedores
        };
    }

    private static string MontarObservacoesPartida(
        string? observacaoUsuario,
        MetadadosChave? metadadosChave,
        MetadadosRodada? metadadosRodada = null,
        MetadadosLados? metadadosLados = null)
    {
        var observacaoNormalizada = string.IsNullOrWhiteSpace(observacaoUsuario)
            ? null
            : observacaoUsuario.Trim();

        var linhasMetadados = new List<string>();

        if (metadadosChave is not null)
        {
            linhasMetadados.Add($"{MarcadorMetadadosChave}{metadadosChave.Lado};{metadadosChave.Rodada};{metadadosChave.Ordem};{string.Join(',', metadadosChave.DuplasEmEspera)}]]");
        }

        if (metadadosRodada is not null)
        {
            linhasMetadados.Add($"{MarcadorMetadadosRodada}{metadadosRodada.NomeFaseBase};{metadadosRodada.NumeroRodada};{metadadosRodada.OrdemConfronto};{(metadadosRodada.TurnoEVolta ? 1 : 0)};{string.Join(',', metadadosRodada.OrdemDuplas)}]]");
        }

        if (metadadosLados is not null)
        {
            linhasMetadados.Add($"{MarcadorMetadadosLados}{metadadosLados.DuplaADireitaId};{metadadosLados.DuplaAEsquerdaId};{metadadosLados.DuplaBDireitaId};{metadadosLados.DuplaBEsquerdaId}]]");
        }

        if (linhasMetadados.Count == 0)
        {
            return observacaoNormalizada ?? string.Empty;
        }

        return string.IsNullOrWhiteSpace(observacaoNormalizada)
            ? string.Join('\n', linhasMetadados)
            : $"{observacaoNormalizada}\n{string.Join('\n', linhasMetadados)}";
    }

    private static MetadadosChave? ExtrairMetadadosChave(string? observacoes)
    {
        if (string.IsNullOrWhiteSpace(observacoes))
        {
            return null;
        }

        var linhas = observacoes
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var linhaMetadados = linhas.LastOrDefault(x => x.StartsWith(MarcadorMetadadosChave, StringComparison.Ordinal));
        if (string.IsNullOrWhiteSpace(linhaMetadados))
        {
            return null;
        }

        var conteudo = linhaMetadados
            .Replace(MarcadorMetadadosChave, string.Empty, StringComparison.Ordinal)
            .Replace("]]", string.Empty, StringComparison.Ordinal);
        var partes = conteudo.Split(';');
        if (partes.Length < 4 ||
            !int.TryParse(partes[1], out var rodada) ||
            !int.TryParse(partes[2], out var ordem))
        {
            return null;
        }

        var idsDuplasEmEspera = partes[3]
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(valor => Guid.TryParse(valor, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToList();

        return new MetadadosChave(partes[0], rodada, ordem, idsDuplasEmEspera);
    }

    private static MetadadosRodada? ExtrairMetadadosRodada(string? observacoes)
    {
        if (string.IsNullOrWhiteSpace(observacoes))
        {
            return null;
        }

        var linhas = observacoes
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var linhaMetadados = linhas.LastOrDefault(x => x.StartsWith(MarcadorMetadadosRodada, StringComparison.Ordinal));
        if (string.IsNullOrWhiteSpace(linhaMetadados))
        {
            return null;
        }

        var conteudo = linhaMetadados
            .Replace(MarcadorMetadadosRodada, string.Empty, StringComparison.Ordinal)
            .Replace("]]", string.Empty, StringComparison.Ordinal);
        var partes = conteudo.Split(';');
        if (partes.Length < 5 ||
            !int.TryParse(partes[1], out var numeroRodada) ||
            !int.TryParse(partes[2], out var ordemConfronto) ||
            !int.TryParse(partes[3], out var turnoEVoltaInt))
        {
            return null;
        }

        var ordemDuplas = partes[4]
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(valor => Guid.TryParse(valor, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToList();

        if (ordemDuplas.Count == 0)
        {
            return null;
        }

        var nomeFaseBase = string.IsNullOrWhiteSpace(partes[0]) ? null : partes[0];
        return new MetadadosRodada(nomeFaseBase, numeroRodada, ordemConfronto, turnoEVoltaInt == 1, ordemDuplas);
    }

    private static MetadadosLados? ExtrairMetadadosLados(string? observacoes)
    {
        if (string.IsNullOrWhiteSpace(observacoes))
        {
            return null;
        }

        var linhas = observacoes
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var linhaMetadados = linhas.LastOrDefault(x => x.StartsWith(MarcadorMetadadosLados, StringComparison.Ordinal));
        if (string.IsNullOrWhiteSpace(linhaMetadados))
        {
            return null;
        }

        var conteudo = linhaMetadados
            .Replace(MarcadorMetadadosLados, string.Empty, StringComparison.Ordinal)
            .Replace("]]", string.Empty, StringComparison.Ordinal);
        var partes = conteudo.Split(';');
        if (partes.Length < 4 ||
            !Guid.TryParse(partes[0], out var duplaADireitaId) ||
            !Guid.TryParse(partes[1], out var duplaAEsquerdaId) ||
            !Guid.TryParse(partes[2], out var duplaBDireitaId) ||
            !Guid.TryParse(partes[3], out var duplaBEsquerdaId))
        {
            return null;
        }

        return new MetadadosLados(duplaADireitaId, duplaAEsquerdaId, duplaBDireitaId, duplaBEsquerdaId);
    }

    private sealed record MetadadosChave(string Lado, int Rodada, int Ordem, IReadOnlyList<Guid> DuplasEmEspera);
    private sealed record MetadadosLados(Guid DuplaADireitaId, Guid DuplaAEsquerdaId, Guid DuplaBDireitaId, Guid DuplaBEsquerdaId);
    private sealed record DesempenhoGrupoFaseDeGrupos(
        Guid DuplaId,
        string NomeDupla,
        string NomeGrupo,
        int Jogos,
        int Vitorias,
        decimal PontosClassificacao,
        int PontosMarcados,
        int PontosSofridos)
    {
        public int SaldoPontos => PontosMarcados - PontosSofridos;
    }
    private sealed record ClassificacaoGrupoFaseDeGrupos(string NomeGrupo, IReadOnlyList<DesempenhoGrupoFaseDeGrupos> Classificacao);
    private sealed record ClassificadoGrupoEliminatoria(
        Guid DuplaId,
        string NomeDupla,
        string NomeGrupo,
        int PosicaoGrupo,
        decimal PontosClassificacao,
        int Vitorias,
        int SaldoPontos,
        int PontosMarcados);
    private sealed record ParticipanteChave(Guid? DuplaId, Guid? PartidaOrigemId, OrigemClassificacaoPartida? OrigemTipo)
    {
        public static ParticipanteChave Direto(Guid duplaId) => new(duplaId, null, null);
        public static ParticipanteChave Vencedor(Guid partidaOrigemId) => new(null, partidaOrigemId, OrigemClassificacaoPartida.Vencedor);
        public static ParticipanteChave Perdedor(Guid partidaOrigemId) => new(null, partidaOrigemId, OrigemClassificacaoPartida.Perdedor);
    }
    private sealed record ConfrontoPlanejadoChaveParticipantes(
        ParticipanteChave ParticipanteA,
        ParticipanteChave ParticipanteB,
        int Ordem);
    private sealed record PlanejamentoRodadaChaveParticipantes(
        IReadOnlyList<ParticipanteChave> ParticipantesEmEspera,
        IReadOnlyList<ConfrontoPlanejadoChaveParticipantes> Confrontos);
    private sealed record ResultadoRodadaChaveCompleta(
        IReadOnlyList<Partida> Partidas,
        IReadOnlyList<ParticipanteChave> Classificados,
        IReadOnlyList<ParticipanteChave> Derrotados);
    private sealed record ConfrontoPlanejadoChave(Guid DuplaAId, Guid DuplaBId, int Ordem);
    private sealed record PlanejamentoRodadaChave(IReadOnlyList<Guid> DuplasEmEspera, IReadOnlyList<ConfrontoPlanejadoChave> Confrontos);
    private sealed record EstadoRodadaChave(
        string Secao,
        int Rodada,
        IReadOnlyList<Guid> Participantes,
        PlanejamentoRodadaChave Planejamento,
        IReadOnlyList<PartidaChave> Partidas,
        bool Concluida,
        IReadOnlyList<Guid> Classificados,
        IReadOnlyList<Guid> Derrotados);
    private enum StatusDuplaChaveDuplaEliminacao
    {
        Ativa = 1,
        Eliminada = 2
    }
    private sealed record EstadoDuplaChaveDuplaEliminacao(
        Guid DuplaId,
        int QuantidadeDerrotas,
        StatusDuplaChaveDuplaEliminacao Status,
        string PosicaoAtual,
        IReadOnlyList<Guid> PartidasPendentesIds);
    private sealed record ConfrontoRoundRobin(Dupla DuplaA, Dupla DuplaB);
    private sealed record PartidaChave(Partida Partida, MetadadosChave Metadados);
    private sealed record MetadadosRodada(string? NomeFaseBase, int NumeroRodada, int OrdemConfronto, bool TurnoEVolta, IReadOnlyList<Guid> OrdemDuplas);
    private sealed record PartidaRodada(Partida Partida, MetadadosRodada Metadados);
    private sealed record RodadaRoundRobin(int Numero, IReadOnlyList<ConfrontoRoundRobin> Confrontos);
}
