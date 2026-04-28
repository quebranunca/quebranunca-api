using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Interfaces.Seguranca;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Aplicacao.Mapeadores;
using PlataformaFutevolei.Aplicacao.Utilitarios;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Aplicacao.Servicos;

public class CompeticaoServico(
    ICompeticaoRepositorio competicaoRepositorio,
    ICategoriaCompeticaoRepositorio categoriaRepositorio,
    IGrupoAtletaRepositorio grupoAtletaRepositorio,
    IFormatoCampeonatoRepositorio formatoRepositorio,
    ILigaRepositorio ligaRepositorio,
    ILocalRepositorio localRepositorio,
    IRegraCompeticaoRepositorio regraRepositorio,
    IUnidadeTrabalho unidadeTrabalho,
    IAutorizacaoUsuarioServico autorizacaoUsuarioServico
) : ICompeticaoServico
{
    private const string NomeCompeticaoPartidasAvulsas = "Partidas avulsas";

    public async Task<IReadOnlyList<CompeticaoDto>> ListarAsync(
        bool incluirPublicas = false,
        CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualAsync(cancellationToken);
        var competicoes = await competicaoRepositorio.ListarAsync(cancellationToken);
        if (usuario is null)
        {
            return OrdenarCompeticoes(competicoes.Where(x => AceitaInscricoes(x.Tipo)))
                .Select(x => x.ParaDto())
                .ToList();
        }

        if (incluirPublicas)
        {
            var idsComAcesso = (await competicaoRepositorio.ListarIdsComAcessoAtletaAsync(
                    usuario.Id,
                    usuario.AtletaId,
                    cancellationToken))
                .ToHashSet();

            return OrdenarCompeticoes(competicoes.Where(x =>
                    AceitaInscricoes(x.Tipo) ||
                    (x.Tipo == TipoCompeticao.Grupo && idsComAcesso.Contains(x.Id))))
                .Select(x => x.ParaDto())
                .ToList();
        }

        if (usuario.Perfil == PerfilUsuario.Atleta)
        {
            var competicoesComAcesso = await competicaoRepositorio.ListarIdsComAcessoAtletaAsync(
                usuario.Id,
                usuario.AtletaId,
                cancellationToken);
            var idsComAcesso = competicoesComAcesso.ToHashSet();

            competicoes = competicoes
                .Where(x => idsComAcesso.Contains(x.Id))
                .OrderBy(x => x.DataInicio)
                .ThenBy(x => x.Nome)
                .ToList();

            return competicoes.Select(x => x.ParaDto()).ToList();
        }

        if (usuario.Perfil == PerfilUsuario.Organizador)
        {
            competicoes = competicoes.Where(x => x.UsuarioOrganizadorId == usuario.Id).ToList();
        }

        return competicoes.Select(x => x.ParaDto()).ToList();
    }

    public async Task<ResumoCompeticoesPublicoDto> ObterResumoPublicoAsync(CancellationToken cancellationToken = default)
    {
        var competicoes = await competicaoRepositorio.ListarAsync(cancellationToken);
        var totalGrupos = competicoes.Count(EhGrupoVisivelNoResumo);

        return new ResumoCompeticoesPublicoDto(totalGrupos);
    }

    public async Task<CompeticaoDto> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        var competicao = await competicaoRepositorio.ObterPorIdAsync(id, cancellationToken);
        if (competicao is null)
        {
            throw new EntidadeNaoEncontradaException("Competição não encontrada.");
        }

        if (usuario.Perfil == PerfilUsuario.Atleta)
        {
            var possuiAcesso = await competicaoRepositorio.AtletaPossuiAcessoAsync(
                competicao.Id,
                usuario.Id,
                usuario.AtletaId,
                cancellationToken);
            if (!possuiAcesso)
            {
                throw new RegraNegocioException("Atletas só podem visualizar competições das quais fazem parte.");
            }

            return competicao.ParaDto();
        }

        if (usuario.Perfil == PerfilUsuario.Organizador && competicao.UsuarioOrganizadorId != usuario.Id)
        {
            throw new RegraNegocioException("O organizador só pode acessar competições vinculadas ao próprio usuário.");
        }

        return competicao.ParaDto();
    }

    public async Task<CompeticaoDto> CriarAsync(CriarCompeticaoDto dto, CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        if (usuario.Perfil == PerfilUsuario.Atleta)
        {
            if (dto.Tipo != TipoCompeticao.Grupo)
            {
                throw new RegraNegocioException("Usuário com perfil atleta só pode criar grupos.");
            }
        }
        else if (usuario.Perfil is not PerfilUsuario.Administrador and not PerfilUsuario.Organizador)
        {
            throw new RegraNegocioException("Apenas administradores, organizadores ou atletas para grupos podem criar competições.");
        }

        var dataInicioUtc = NormalizarParaUtc(dto.DataInicio);
        var dataFimUtc = dto.DataFim.HasValue ? (DateTime?)NormalizarParaUtc(dto.DataFim.Value) : null;

        var link = NormalizarLink(dto.Link);

        Validar(dto.Nome, dataInicioUtc, dataFimUtc, link);
        var nome = dto.Nome.Trim();
        await ValidarNomeUnicoAsync(nome, null, cancellationToken);
        await ValidarLigaAsync(dto.LigaId, cancellationToken);
        await ValidarLocalAsync(dto.LocalId, cancellationToken);
        var formatoCampeonatoId = await ResolverFormatoCampeonatoAsync(dto.Tipo, dto.FormatoCampeonatoId, cancellationToken);
        var possuiFinalReset = await ResolverPossuiFinalResetAsync(dto.Tipo, formatoCampeonatoId, dto.PossuiFinalReset, cancellationToken);
        await ValidarRegraAsync(dto.RegraCompeticaoId, cancellationToken);

        var competicao = new Competicao
        {
            Nome = nome,
            Tipo = dto.Tipo,
            Descricao = dto.Descricao?.Trim(),
            Link = link,
            DataInicio = dataInicioUtc,
            DataFim = dataFimUtc,
            LigaId = dto.LigaId,
            LocalId = dto.LocalId,
            FormatoCampeonatoId = formatoCampeonatoId,
            RegraCompeticaoId = dto.RegraCompeticaoId,
            UsuarioOrganizadorId = usuario.Perfil is PerfilUsuario.Organizador or PerfilUsuario.Atleta || dto.Tipo == TipoCompeticao.Grupo
                ? usuario.Id
                : null,
            ContaRankingLiga = dto.LigaId.HasValue,
            InscricoesAbertas = ObterInscricoesAbertasParaCriacao(dto.Tipo, dto.InscricoesAbertas),
            PossuiFinalReset = possuiFinalReset
        };

        await competicaoRepositorio.AdicionarAsync(competicao, cancellationToken);

        if (dto.Tipo == TipoCompeticao.Grupo)
        {
            if (usuario.AtletaId.HasValue && await grupoAtletaRepositorio.ObterPorCompeticaoEAtletaAsync(competicao.Id, usuario.AtletaId.Value, cancellationToken) is null)
            {
                await grupoAtletaRepositorio.AdicionarAsync(new GrupoAtleta
                {
                    CompeticaoId = competicao.Id,
                    AtletaId = usuario.AtletaId.Value
                }, cancellationToken);
            }
        }

        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
        var competicaoCriada = await competicaoRepositorio.ObterPorIdAsync(competicao.Id, cancellationToken);
        return competicaoCriada!.ParaDto();
    }

    public async Task<CompeticaoDto> AtualizarAsync(Guid id, AtualizarCompeticaoDto dto, CancellationToken cancellationToken = default)
    {
        await autorizacaoUsuarioServico.GarantirGestaoCompeticaoAsync(id, cancellationToken);
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        if (usuario.Perfil == PerfilUsuario.Atleta && dto.Tipo != TipoCompeticao.Grupo)
        {
            throw new RegraNegocioException("Usuário com perfil atleta só pode manter competições do tipo grupo.");
        }

        var dataInicioUtc = NormalizarParaUtc(dto.DataInicio);
        var dataFimUtc = dto.DataFim.HasValue ? (DateTime?)NormalizarParaUtc(dto.DataFim.Value) : null;

        var link = NormalizarLink(dto.Link);

        Validar(dto.Nome, dataInicioUtc, dataFimUtc, link);
        await ValidarLigaAsync(dto.LigaId, cancellationToken);
        await ValidarLocalAsync(dto.LocalId, cancellationToken);

        var competicao = await competicaoRepositorio.ObterPorIdAsync(id, cancellationToken);
        if (competicao is null)
        {
            throw new EntidadeNaoEncontradaException("Competição não encontrada.");
        }

        var nome = dto.Nome.Trim();
        await ValidarNomeUnicoAsync(nome, id, cancellationToken);
        var formatoCampeonatoId = await ResolverFormatoCampeonatoAsync(dto.Tipo, dto.FormatoCampeonatoId, cancellationToken);
        var possuiFinalReset = await ResolverPossuiFinalResetAsync(dto.Tipo, formatoCampeonatoId, dto.PossuiFinalReset, cancellationToken);
        await ValidarCategoriasExistentesAsync(id, dto.Tipo, formatoCampeonatoId, cancellationToken);
        await ValidarRegraAsync(dto.RegraCompeticaoId, cancellationToken);

        competicao.Nome = nome;
        competicao.Tipo = dto.Tipo;
        competicao.Descricao = dto.Descricao?.Trim();
        competicao.Link = link;
        competicao.DataInicio = dataInicioUtc;
        competicao.DataFim = dataFimUtc;
        competicao.LigaId = dto.LigaId;
        competicao.LocalId = dto.LocalId;
        competicao.FormatoCampeonatoId = formatoCampeonatoId;
        competicao.RegraCompeticaoId = dto.RegraCompeticaoId;
        competicao.ContaRankingLiga = dto.LigaId.HasValue;
        competicao.InscricoesAbertas = ObterInscricoesAbertasParaAtualizacao(
            dto.Tipo,
            dto.InscricoesAbertas,
            competicao.InscricoesAbertas);
        competicao.PossuiFinalReset = possuiFinalReset;
        competicao.AtualizarDataModificacao();

        competicaoRepositorio.Atualizar(competicao);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
        var competicaoAtualizada = await competicaoRepositorio.ObterPorIdAsync(id, cancellationToken);
        return competicaoAtualizada!.ParaDto();
    }

    public async Task RemoverAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await autorizacaoUsuarioServico.GarantirGestaoCompeticaoAsync(id, cancellationToken);
        var competicao = await competicaoRepositorio.ObterPorIdAsync(id, cancellationToken);
        if (competicao is null)
        {
            throw new EntidadeNaoEncontradaException("Competição não encontrada.");
        }

        competicaoRepositorio.Remover(competicao);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
    }

    private async Task ValidarLigaAsync(Guid? ligaId, CancellationToken cancellationToken)
    {
        if (!ligaId.HasValue)
        {
            return;
        }

        var liga = await ligaRepositorio.ObterPorIdAsync(ligaId.Value, cancellationToken);
        if (liga is null)
        {
            throw new RegraNegocioException("A liga informada para a competição não foi encontrada.");
        }
    }

    private async Task ValidarLocalAsync(Guid? localId, CancellationToken cancellationToken)
    {
        if (!localId.HasValue)
        {
            return;
        }

        var local = await localRepositorio.ObterPorIdAsync(localId.Value, cancellationToken);
        if (local is null)
        {
            throw new RegraNegocioException("O local informado para a competição não foi encontrado.");
        }
    }

    private async Task ValidarRegraAsync(Guid? regraCompeticaoId, CancellationToken cancellationToken)
    {
        if (!regraCompeticaoId.HasValue)
        {
            return;
        }

        var regra = await regraRepositorio.ObterPorIdAsync(regraCompeticaoId.Value, cancellationToken);
        if (regra is null)
        {
            throw new RegraNegocioException("A regra informada para a competição não foi encontrada.");
        }
    }

    private async Task ValidarNomeUnicoAsync(string nome, Guid? idAtual, CancellationToken cancellationToken)
    {
        var existente = await competicaoRepositorio.ObterPorNomeAsync(nome, cancellationToken);
        if (existente is not null && existente.Id != idAtual)
        {
            throw new RegraNegocioException("Já existe uma competição cadastrada com este nome. Escolha outro nome.");
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

    private static bool ObterInscricoesAbertasParaCriacao(TipoCompeticao tipo, bool? inscricoesAbertas)
    {
        if (!AceitaInscricoes(tipo))
        {
            if (inscricoesAbertas is true)
            {
                throw new RegraNegocioException("Apenas campeonatos e eventos podem ter inscrições abertas.");
            }

            return false;
        }

        return inscricoesAbertas ?? true;
    }

    private static bool ObterInscricoesAbertasParaAtualizacao(
        TipoCompeticao tipo,
        bool? inscricoesAbertas,
        bool valorAtual)
    {
        if (!AceitaInscricoes(tipo))
        {
            if (inscricoesAbertas is true)
            {
                throw new RegraNegocioException("Apenas campeonatos e eventos podem ter inscrições abertas.");
            }

            return false;
        }

        return inscricoesAbertas ?? valorAtual;
    }

    private static bool AceitaInscricoes(TipoCompeticao tipo)
    {
        return tipo is TipoCompeticao.Campeonato or TipoCompeticao.Evento;
    }

    private static string? NormalizarLink(string? link)
    {
        return string.IsNullOrWhiteSpace(link) ? null : link.Trim();
    }

    private static IEnumerable<Competicao> OrdenarCompeticoes(IEnumerable<Competicao> competicoes)
    {
        return competicoes
            .OrderBy(x => x.DataInicio)
            .ThenBy(x => x.Nome);
    }

    private static bool EhGrupoVisivelNoResumo(Competicao competicao)
    {
        return competicao.Tipo == TipoCompeticao.Grupo &&
            !string.Equals(
                competicao.Nome?.Trim(),
                NomeCompeticaoPartidasAvulsas,
                StringComparison.OrdinalIgnoreCase);
    }

    private async Task<Guid?> ResolverFormatoCampeonatoAsync(
        TipoCompeticao tipo,
        Guid? formatoCampeonatoId,
        CancellationToken cancellationToken)
    {
        if (formatoCampeonatoId.HasValue)
        {
            var formato = await formatoRepositorio.ObterPorIdAsync(formatoCampeonatoId.Value, cancellationToken);
            if (formato is null)
            {
                throw new RegraNegocioException("O formato de competição informado não foi encontrado.");
            }

            if (!formato.Ativo)
            {
                throw new RegraNegocioException("O formato de competição informado está inativo.");
            }

            ValidarCompatibilidadeFormato(tipo, formato);
            return formato.Id;
        }

        return await ObterFormatoPadraoAsync(tipo, cancellationToken);
    }

    private async Task<bool> ResolverPossuiFinalResetAsync(
        TipoCompeticao tipo,
        Guid? formatoCampeonatoId,
        bool? possuiFinalReset,
        CancellationToken cancellationToken)
    {
        if (!AceitaInscricoes(tipo))
        {
            if (possuiFinalReset is true)
            {
                throw new RegraNegocioException("Final reset só pode ser configurada em campeonatos e eventos.");
            }

            return false;
        }

        var formatoEhChaveDuplaEliminacao = await FormatoEhChaveDuplaEliminacaoAsync(formatoCampeonatoId, cancellationToken);
        if (possuiFinalReset is true && !formatoEhChaveDuplaEliminacao)
        {
            throw new RegraNegocioException("Final reset só pode ser habilitada quando a competição usa chave com dupla eliminação.");
        }

        if (possuiFinalReset.HasValue)
        {
            return possuiFinalReset.Value;
        }

        return formatoEhChaveDuplaEliminacao;
    }

    private async Task<Guid?> ObterFormatoPadraoAsync(TipoCompeticao tipo, CancellationToken cancellationToken)
    {
        await GarantirFormatosPadraoAsync(cancellationToken);
        var formatos = await formatoRepositorio.ListarAsync(cancellationToken);

        if (tipo == TipoCompeticao.Grupo)
        {
            var formatoPontosCorridos = formatos.FirstOrDefault(x =>
                x.Ativo &&
                x.TipoFormato == TipoFormatoCampeonato.PontosCorridos &&
                x.Nome == FormatosCampeonatoPadrao.NomePontosCorridos);

            if (formatoPontosCorridos is null)
            {
                throw new RegraNegocioException("Cadastre um formato ativo de pontos corridos para usar como padrão em grupos.");
            }

            return formatoPontosCorridos.Id;
        }

        if (AceitaInscricoes(tipo))
        {
            var formatoChaveDuplaEliminacao = formatos.FirstOrDefault(x =>
                x.Ativo &&
                x.TipoFormato == TipoFormatoCampeonato.Chave &&
                x.Nome == FormatosCampeonatoPadrao.NomeChave &&
                x.QuantidadeDerrotasParaEliminacao == 2);

            if (formatoChaveDuplaEliminacao is null)
            {
                throw new RegraNegocioException("Cadastre um formato ativo de chave com dupla eliminação para usar como padrão em campeonatos e eventos.");
            }

            return formatoChaveDuplaEliminacao.Id;
        }

        return null;
    }

    private async Task GarantirFormatosPadraoAsync(CancellationToken cancellationToken)
    {
        var adicionouFormato = false;

        foreach (var definicao in FormatosCampeonatoPadrao.Lista)
        {
            var formatoExistente = await formatoRepositorio.ObterPorNomeAsync(definicao.Nome, cancellationToken);
            if (formatoExistente is not null)
            {
                continue;
            }

            await formatoRepositorio.AdicionarAsync(new FormatoCampeonato
            {
                Nome = definicao.Nome,
                Descricao = definicao.Descricao,
                TipoFormato = definicao.TipoFormato,
                Ativo = definicao.Ativo,
                QuantidadeGrupos = definicao.QuantidadeGrupos,
                ClassificadosPorGrupo = definicao.ClassificadosPorGrupo,
                GeraMataMataAposGrupos = definicao.GeraMataMataAposGrupos,
                TurnoEVolta = definicao.TurnoEVolta,
                TipoChave = definicao.TipoChave,
                QuantidadeDerrotasParaEliminacao = definicao.QuantidadeDerrotasParaEliminacao,
                PermiteCabecaDeChave = definicao.PermiteCabecaDeChave,
                DisputaTerceiroLugar = definicao.DisputaTerceiroLugar
            }, cancellationToken);

            adicionouFormato = true;
        }

        if (adicionouFormato)
        {
            await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
        }
    }

    private async Task<bool> FormatoEhChaveDuplaEliminacaoAsync(Guid? formatoCampeonatoId, CancellationToken cancellationToken)
    {
        if (!formatoCampeonatoId.HasValue)
        {
            return false;
        }

        var formato = await formatoRepositorio.ObterPorIdAsync(formatoCampeonatoId.Value, cancellationToken);
        return formato is not null &&
               formato.TipoFormato == TipoFormatoCampeonato.Chave &&
               formato.QuantidadeDerrotasParaEliminacao == 2;
    }

    private async Task ValidarCategoriasExistentesAsync(
        Guid competicaoId,
        TipoCompeticao tipo,
        Guid? formatoCampeonatoId,
        CancellationToken cancellationToken)
    {
        var categorias = await categoriaRepositorio.ListarPorCompeticaoAsync(competicaoId, cancellationToken);
        if (categorias.Count == 0)
        {
            return;
        }

        FormatoCampeonato? formatoCompeticao = null;
        if (formatoCampeonatoId.HasValue)
        {
            formatoCompeticao = await formatoRepositorio.ObterPorIdAsync(formatoCampeonatoId.Value, cancellationToken);
        }

        foreach (var categoria in categorias)
        {
            var formatoCategoria = categoria.FormatoCampeonato;
            if (formatoCategoria is not null)
            {
                ValidarCompatibilidadeFormato(tipo, formatoCategoria);
                continue;
            }

            if (formatoCompeticao is not null)
            {
                ValidarCompatibilidadeFormato(tipo, formatoCompeticao);
            }
        }
    }

    private static void ValidarCompatibilidadeFormato(TipoCompeticao tipo, FormatoCampeonato formato)
    {
        if (tipo == TipoCompeticao.Grupo && formato.TipoFormato != TipoFormatoCampeonato.PontosCorridos)
        {
            throw new RegraNegocioException("Competições do tipo grupo só podem usar formato de pontos corridos.");
        }
    }

    private static void Validar(
        string nome,
        DateTime dataInicio,
        DateTime? dataFim,
        string? link)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new RegraNegocioException("Nome da competição é obrigatório.");
        }

        if (dataInicio == default)
        {
            throw new RegraNegocioException("Data de início da competição é obrigatória.");
        }

        if (dataFim.HasValue && dataFim.Value < dataInicio)
        {
            throw new RegraNegocioException("A data fim não pode ser menor que a data de início.");
        }

        if (link?.Length > 500)
        {
            throw new RegraNegocioException("O link da competição deve ter no máximo 500 caracteres.");
        }

        if (!string.IsNullOrWhiteSpace(link) &&
            (!Uri.TryCreate(link, UriKind.Absolute, out var uri) ||
             uri.Scheme is not ("http" or "https")))
        {
            throw new RegraNegocioException("O link da competição deve ser uma URL http ou https válida.");
        }
    }
}
