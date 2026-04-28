using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Interfaces.Seguranca;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Aplicacao.Mapeadores;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Aplicacao.Servicos;

public class CategoriaCompeticaoServico(
    ICategoriaCompeticaoRepositorio categoriaRepositorio,
    ICompeticaoRepositorio competicaoRepositorio,
    IFormatoCampeonatoRepositorio formatoRepositorio,
    IInscricaoCampeonatoRepositorio inscricaoRepositorio,
    IPartidaRepositorio partidaRepositorio,
    IUnidadeTrabalho unidadeTrabalho,
    IAutorizacaoUsuarioServico autorizacaoUsuarioServico
) : ICategoriaCompeticaoServico
{
    public async Task<IReadOnlyList<CategoriaCompeticaoDto>> ListarPorCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualAsync(cancellationToken);
        if (usuario is null)
        {
            var competicao = await competicaoRepositorio.ObterPorIdAsync(competicaoId, cancellationToken);
            if (competicao is null)
            {
                throw new EntidadeNaoEncontradaException("Competição não encontrada.");
            }

            if (!AceitaInscricoes(competicao.Tipo))
            {
                throw new RegraNegocioException("Visitantes só podem visualizar categorias de campeonatos e eventos.");
            }
        }
        else if (usuario.Perfil == PerfilUsuario.Atleta)
        {
            var competicao = await competicaoRepositorio.ObterPorIdAsync(competicaoId, cancellationToken);
            if (competicao is null)
            {
                throw new EntidadeNaoEncontradaException("Competição não encontrada.");
            }

            var possuiAcesso = await competicaoRepositorio.AtletaPossuiAcessoAsync(
                competicaoId,
                usuario.Id,
                usuario.AtletaId,
                cancellationToken);
            if (!possuiAcesso && !PodeVisualizarCategoriasPublicas(competicao))
            {
                throw new RegraNegocioException("Atletas só podem visualizar categorias de competições das quais fazem parte.");
            }
        }
        else
        {
            await autorizacaoUsuarioServico.GarantirGestaoCompeticaoAsync(competicaoId, cancellationToken);
        }

        var categorias = await categoriaRepositorio.ListarPorCompeticaoAsync(competicaoId, cancellationToken);
        return categorias.Select(x => x.ParaDto()).ToList();
    }

    public async Task<CategoriaCompeticaoDto> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        var categoria = await categoriaRepositorio.ObterPorIdAsync(id, cancellationToken);
        if (categoria is null)
        {
            throw new EntidadeNaoEncontradaException("Categoria não encontrada.");
        }

        if (usuario.Perfil == PerfilUsuario.Atleta)
        {
            var possuiAcesso = await competicaoRepositorio.AtletaPossuiAcessoAsync(
                categoria.CompeticaoId,
                usuario.Id,
                usuario.AtletaId,
                cancellationToken);
            if (!possuiAcesso && !PodeVisualizarCategoriasPublicas(categoria.Competicao))
            {
                throw new RegraNegocioException("Atletas só podem visualizar categorias de competições das quais fazem parte.");
            }
        }
        else
        {
            await autorizacaoUsuarioServico.GarantirGestaoCompeticaoAsync(categoria.CompeticaoId, cancellationToken);
        }

        return categoria.ParaDto();
    }

    public async Task<CategoriaCompeticaoDto> CriarAsync(CriarCategoriaCompeticaoDto dto, CancellationToken cancellationToken = default)
    {
        await autorizacaoUsuarioServico.GarantirGestaoCompeticaoAsync(dto.CompeticaoId, cancellationToken);
        Validar(dto.PesoRanking, dto.QuantidadeMaximaDuplas);

        var competicao = await competicaoRepositorio.ObterPorIdAsync(dto.CompeticaoId, cancellationToken);
        if (competicao is null)
        {
            throw new RegraNegocioException("Toda categoria deve pertencer a uma competição existente.");
        }

        var formatoCampeonatoId = await ValidarFormatoCampeonatoAsync(competicao, dto.FormatoCampeonatoId, cancellationToken);
        var nomeCategoria = await ResolverNomeAsync(
            dto.CompeticaoId,
            null,
            dto.Nome,
            dto.Genero,
            dto.Nivel,
            cancellationToken);

        var categoria = new CategoriaCompeticao
        {
            CompeticaoId = dto.CompeticaoId,
            FormatoCampeonatoId = formatoCampeonatoId,
            Nome = nomeCategoria,
            Genero = dto.Genero,
            Nivel = dto.Nivel,
            PesoRanking = dto.PesoRanking ?? 1m,
            QuantidadeMaximaDuplas = dto.QuantidadeMaximaDuplas,
            InscricoesEncerradas = dto.InscricoesEncerradas
        };

        await categoriaRepositorio.AdicionarAsync(categoria, cancellationToken);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
        var categoriaCriada = await categoriaRepositorio.ObterPorIdAsync(categoria.Id, cancellationToken);
        return categoriaCriada!.ParaDto();
    }

    public async Task<CategoriaCompeticaoDto> AtualizarAsync(Guid id, AtualizarCategoriaCompeticaoDto dto, CancellationToken cancellationToken = default)
    {
        var categoria = await categoriaRepositorio.ObterPorIdAsync(id, cancellationToken);
        if (categoria is null)
        {
            throw new EntidadeNaoEncontradaException("Categoria não encontrada.");
        }

        await autorizacaoUsuarioServico.GarantirGestaoCompeticaoAsync(categoria.CompeticaoId, cancellationToken);
        Validar(dto.PesoRanking, dto.QuantidadeMaximaDuplas);

        var formatoCampeonatoId = await ValidarFormatoCampeonatoAsync(categoria.Competicao, dto.FormatoCampeonatoId, cancellationToken);
        await ValidarLimiteDuplasAsync(categoria.Id, dto.QuantidadeMaximaDuplas, cancellationToken);
        var nomeCategoria = await ResolverNomeAsync(
            categoria.CompeticaoId,
            categoria.Id,
            dto.Nome,
            dto.Genero,
            dto.Nivel,
            cancellationToken);

        categoria.FormatoCampeonatoId = formatoCampeonatoId;
        categoria.Nome = nomeCategoria;
        categoria.Genero = dto.Genero;
        categoria.Nivel = dto.Nivel;
        categoria.PesoRanking = dto.PesoRanking ?? 1m;
        categoria.QuantidadeMaximaDuplas = dto.QuantidadeMaximaDuplas;

        if (dto.InscricoesEncerradas)
        {
            categoria.EncerrarInscricoes();
        }
        else
        {
            categoria.ReabrirInscricoes();
        }

        categoriaRepositorio.Atualizar(categoria);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
        var categoriaAtualizada = await categoriaRepositorio.ObterPorIdAsync(id, cancellationToken);
        return categoriaAtualizada!.ParaDto();
    }

    public async Task<CategoriaCompeticaoDto> AprovarTabelaJogosAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var categoria = await categoriaRepositorio.ObterPorIdAsync(id, cancellationToken);
        if (categoria is null)
        {
            throw new EntidadeNaoEncontradaException("Categoria não encontrada.");
        }

        await autorizacaoUsuarioServico.GarantirGestaoCompeticaoAsync(categoria.CompeticaoId, cancellationToken);

        if (categoria.Competicao.Tipo == TipoCompeticao.Grupo)
        {
            throw new RegraNegocioException("A aprovação do sorteio está disponível apenas para campeonatos e eventos.");
        }

        if (categoria.TabelaJogosAprovada)
        {
            throw new RegraNegocioException("O sorteio desta categoria já foi aprovado.");
        }

        var partidas = await partidaRepositorio.ListarPorCategoriaAsync(categoria.Id, cancellationToken);
        if (partidas.Count == 0)
        {
            throw new RegraNegocioException("Gere a tabela de jogos da categoria antes de aprovar o sorteio.");
        }

        var usuarioAtual = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        categoria.AprovarTabelaJogos(usuarioAtual.Id, DateTime.UtcNow);
        categoriaRepositorio.Atualizar(categoria);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        var categoriaAtualizada = await categoriaRepositorio.ObterPorIdAsync(id, cancellationToken);
        return categoriaAtualizada!.ParaDto();
    }

    public async Task RemoverAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var categoria = await categoriaRepositorio.ObterPorIdAsync(id, cancellationToken);
        if (categoria is null)
        {
            throw new EntidadeNaoEncontradaException("Categoria não encontrada.");
        }

        await autorizacaoUsuarioServico.GarantirGestaoCompeticaoAsync(categoria.CompeticaoId, cancellationToken);
        categoriaRepositorio.Remover(categoria);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
    }

    private static void Validar(decimal? pesoRanking, int? quantidadeMaximaDuplas)
    {
        if (pesoRanking.HasValue && pesoRanking.Value <= 0)
        {
            throw new RegraNegocioException("Peso de ranking da categoria deve ser maior que zero.");
        }

        if (quantidadeMaximaDuplas.HasValue && quantidadeMaximaDuplas.Value <= 0)
        {
            throw new RegraNegocioException("Quantidade máxima de duplas deve ser maior que zero.");
        }
    }

    private async Task ValidarLimiteDuplasAsync(
        Guid categoriaId,
        int? quantidadeMaximaDuplas,
        CancellationToken cancellationToken)
    {
        if (!quantidadeMaximaDuplas.HasValue)
        {
            return;
        }

        var quantidadeInscricoes = await inscricaoRepositorio.ContarPorCategoriaAsync(categoriaId, cancellationToken: cancellationToken);
        if (quantidadeInscricoes > quantidadeMaximaDuplas.Value)
        {
            throw new RegraNegocioException("A categoria já possui mais duplas inscritas do que o novo limite informado.");
        }
    }

    private async Task<string> ResolverNomeAsync(
        Guid competicaoId,
        Guid? categoriaIgnoradaId,
        string? nomeInformado,
        GeneroCategoria genero,
        NivelCategoria nivel,
        CancellationToken cancellationToken)
    {
        var nome = nomeInformado?.Trim();
        if (!string.IsNullOrWhiteSpace(nome))
        {
            return nome;
        }

        var categoriasCompeticao = await categoriaRepositorio.ListarPorCompeticaoAsync(competicaoId, cancellationToken);
        var possuiCategoriaComMesmoGeneroENivel = categoriasCompeticao.Any(categoria =>
            categoria.Id != categoriaIgnoradaId &&
            categoria.Genero == genero &&
            categoria.Nivel == nivel);

        if (possuiCategoriaComMesmoGeneroENivel)
        {
            throw new RegraNegocioException("Informe o nome da categoria quando já existir outra com o mesmo gênero e nível técnico nesta competição.");
        }

        return $"{ObterNomeNivel(nivel)} {ObterNomeGenero(genero)}";
    }

    private static string ObterNomeGenero(GeneroCategoria genero)
    {
        return genero switch
        {
            GeneroCategoria.Masculino => "Masculino",
            GeneroCategoria.Feminino => "Feminino",
            GeneroCategoria.Misto => "Misto",
            _ => "Categoria"
        };
    }

    private static string ObterNomeNivel(NivelCategoria nivel)
    {
        return nivel switch
        {
            NivelCategoria.Estreante => "Estreante",
            NivelCategoria.Iniciante => "Iniciante",
            NivelCategoria.Intermediario => "Intermediário",
            NivelCategoria.Amador => "Amador",
            NivelCategoria.Profissional => "Profissional",
            NivelCategoria.Livre => "Livre",
            _ => "Categoria"
        };
    }

    private async Task<Guid?> ValidarFormatoCampeonatoAsync(
        Competicao competicao,
        Guid? formatoCampeonatoId,
        CancellationToken cancellationToken)
    {
        if (!formatoCampeonatoId.HasValue)
        {
            return null;
        }

        var formato = await formatoRepositorio.ObterPorIdAsync(formatoCampeonatoId.Value, cancellationToken);
        if (formato is null)
        {
            throw new RegraNegocioException("O formato de campeonato informado não foi encontrado.");
        }

        if (!formato.Ativo)
        {
            throw new RegraNegocioException("O formato de campeonato informado está inativo.");
        }

        if (competicao.Tipo == TipoCompeticao.Grupo && formato.TipoFormato != TipoFormatoCampeonato.PontosCorridos)
        {
            throw new RegraNegocioException("Categorias de grupos só podem usar formato padrão de pontos corridos.");
        }

        return formato.Id;
    }

    private static bool AceitaInscricoes(TipoCompeticao tipo)
    {
        return tipo is TipoCompeticao.Campeonato or TipoCompeticao.Evento;
    }

    private static bool PodeVisualizarCategoriasPublicas(Competicao competicao)
    {
        return AceitaInscricoes(competicao.Tipo);
    }
}
