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

public class GrupoAtletaServico(
    IGrupoAtletaRepositorio grupoAtletaRepositorio,
    ICompeticaoRepositorio competicaoRepositorio,
    IAtletaRepositorio atletaRepositorio,
    IUsuarioRepositorio usuarioRepositorio,
    IUnidadeTrabalho unidadeTrabalho,
    IAutorizacaoUsuarioServico autorizacaoUsuarioServico,
    IResolvedorAtletaDuplaServico resolvedorAtletaDuplaServico,
    IPendenciaServico pendenciaServico
) : IGrupoAtletaServico
{
    public async Task<IReadOnlyList<GrupoAtletaDto>> ListarPorCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        var competicao = await ObterGrupoValidoAsync(competicaoId, cancellationToken);

        if (usuario.Perfil is PerfilUsuario.Administrador or PerfilUsuario.Organizador)
        {
            await autorizacaoUsuarioServico.GarantirGestaoCompeticaoAsync(competicaoId, cancellationToken);
        }

        var atletas = await grupoAtletaRepositorio.ListarPorCompeticaoAsync(competicao.Id, cancellationToken);
        return atletas.Select(x => x.ParaDto()).ToList();
    }

    public async Task<GrupoAtletaDto> CriarAsync(
        Guid competicaoId,
        CriarGrupoAtletaDto dto,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.NomeAtleta))
        {
            throw new RegraNegocioException("Nome do atleta é obrigatório.");
        }

        await autorizacaoUsuarioServico.GarantirGestaoCompeticaoAsync(competicaoId, cancellationToken);
        await ObterGrupoValidoAsync(competicaoId, cancellationToken);

        var (nome, apelido) = NormalizadorNomeAtleta.NormalizarNomeEApelido(dto.NomeAtleta, dto.ApelidoAtleta);
        var atleta = await resolvedorAtletaDuplaServico.ObterOuCriarAtletaAsync(nome, apelido, true, cancellationToken);

        var grupoAtletaExistente = await grupoAtletaRepositorio.ObterPorCompeticaoEAtletaAsync(competicaoId, atleta.Id, cancellationToken);
        if (grupoAtletaExistente is not null)
        {
            throw new RegraNegocioException("Este atleta já faz parte do grupo.");
        }

        var grupoAtleta = new GrupoAtleta
        {
            CompeticaoId = competicaoId,
            AtletaId = atleta.Id,
            Competicao = await competicaoRepositorio.ObterPorIdAsync(competicaoId, cancellationToken) ?? throw new EntidadeNaoEncontradaException("Grupo não encontrado."),
            Atleta = atleta
        };

        await grupoAtletaRepositorio.AdicionarAsync(grupoAtleta, cancellationToken);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        var grupoAtletaCriado = await grupoAtletaRepositorio.ObterPorIdAsync(grupoAtleta.Id, cancellationToken);
        return grupoAtletaCriado!.ParaDto();
    }

    public async Task RemoverAsync(Guid competicaoId, Guid id, CancellationToken cancellationToken = default)
    {
        await autorizacaoUsuarioServico.GarantirGestaoCompeticaoAsync(competicaoId, cancellationToken);
        var usuarioAtual = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        await ObterGrupoValidoAsync(competicaoId, cancellationToken);

        var grupoAtleta = await grupoAtletaRepositorio.ObterPorIdAsync(id, cancellationToken);
        if (grupoAtleta is null || grupoAtleta.CompeticaoId != competicaoId)
        {
            throw new EntidadeNaoEncontradaException("Atleta do grupo não encontrado.");
        }

        if (usuarioAtual.AtletaId.HasValue && grupoAtleta.AtletaId == usuarioAtual.AtletaId.Value)
        {
            throw new RegraNegocioException("Você não pode remover o próprio atleta do grupo.");
        }

        grupoAtletaRepositorio.Remover(grupoAtleta);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
    }

    public async Task<UsuarioLogadoDto> AssumirMeuNomeNoGrupoAsync(
        Guid competicaoId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var usuarioAtual = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        if (usuarioAtual.Perfil != PerfilUsuario.Atleta)
        {
            throw new RegraNegocioException("Apenas usuários com perfil atleta podem assumir um nome no grupo.");
        }

        await ObterGrupoValidoAsync(competicaoId, cancellationToken);

        var grupoAtleta = await grupoAtletaRepositorio.ObterPorIdAsync(id, cancellationToken);
        if (grupoAtleta is null || grupoAtleta.CompeticaoId != competicaoId)
        {
            throw new EntidadeNaoEncontradaException("Atleta do grupo não encontrado.");
        }

        var usuario = await usuarioRepositorio.ObterPorIdParaAtualizacaoAsync(usuarioAtual.Id, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Usuário não encontrado.");

        var usuarioComMesmoAtleta = await usuarioRepositorio.ObterPorAtletaIdAsync(grupoAtleta.AtletaId, cancellationToken);
        if (usuarioComMesmoAtleta is not null && usuarioComMesmoAtleta.Id != usuario.Id)
        {
            throw new RegraNegocioException("Este nome do grupo já está vinculado a outro usuário.");
        }

        usuario.AtletaId = grupoAtleta.AtletaId;
        if (grupoAtleta.Atleta.Email is null)
        {
            grupoAtleta.Atleta.Email = usuario.Email;
            grupoAtleta.Atleta.AtualizarDataModificacao();
            atletaRepositorio.Atualizar(grupoAtleta.Atleta);
        }

        usuario.AtualizarDataModificacao();
        usuarioRepositorio.Atualizar(usuario);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
        await pendenciaServico.SincronizarAposVinculoAtletaAsync(grupoAtleta.AtletaId, cancellationToken);

        var atualizado = await usuarioRepositorio.ObterPorIdAsync(usuario.Id, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Usuário não encontrado.");
        return atualizado.ParaDto();
    }

    private async Task<Competicao> ObterGrupoValidoAsync(Guid competicaoId, CancellationToken cancellationToken)
    {
        var competicao = await competicaoRepositorio.ObterPorIdAsync(competicaoId, cancellationToken);
        if (competicao is null)
        {
            throw new EntidadeNaoEncontradaException("Grupo não encontrado.");
        }

        if (competicao.Tipo != TipoCompeticao.Grupo)
        {
            throw new RegraNegocioException("A operação solicitada está disponível apenas para grupos.");
        }

        return competicao;
    }
}
