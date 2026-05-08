using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Interfaces.Seguranca;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Aplicacao.Utilitarios;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Aplicacao.Servicos;

public class GrupoPadraoServico(
    IGrupoRepositorio grupoRepositorio,
    IGrupoAtletaRepositorio grupoAtletaRepositorio,
    IUnidadeTrabalho unidadeTrabalho,
    IAutorizacaoUsuarioServico autorizacaoUsuarioServico
) : IGrupoPadraoServico
{
    public string NomeGrupoGeral => NomeGrupoGeralConstante;

    private const string NomeGrupoGeralConstante = GruposPadrao.NomeGeral;

    public async Task<Grupo> ObterOuCriarGrupoGeralAsync(CancellationToken cancellationToken = default)
    {
        var grupoExistente = await grupoRepositorio.ObterPorNomeNormalizadoAsync(NomeGrupoGeralConstante, cancellationToken);
        if (grupoExistente is not null)
        {
            if (grupoExistente.UsuarioOrganizadorId is not null ||
                !string.Equals(grupoExistente.Nome, NomeGrupoGeralConstante, StringComparison.Ordinal))
            {
                grupoExistente.Nome = NomeGrupoGeralConstante;
                grupoExistente.UsuarioOrganizadorId = null;
                grupoExistente.AtualizarDataModificacao();
                grupoRepositorio.Atualizar(grupoExistente);
                await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
            }

            return grupoExistente;
        }

        var grupo = new Grupo
        {
            Nome = NomeGrupoGeralConstante,
            Descricao = "Grupo global criado automaticamente para partidas sem grupo informado.",
            DataInicio = DateTime.UtcNow,
            UsuarioOrganizadorId = null
        };

        await grupoRepositorio.AdicionarAsync(grupo, cancellationToken);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        return await grupoRepositorio.ObterPorIdAsync(grupo.Id, cancellationToken) ?? grupo;
    }

    public async Task<Grupo> ResolverGrupoRegistroPartidaAsync(
        Guid? grupoId,
        string? nomeNovoGrupo,
        CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        if (grupoId.HasValue && grupoId.Value != Guid.Empty)
        {
            var grupoSelecionado = await grupoRepositorio.ObterPorIdAsync(grupoId.Value, cancellationToken)
                ?? throw new RegraNegocioException("Grupo informado não foi encontrado.");
            if (!await UsuarioPodeUsarGrupoExistenteAsync(grupoSelecionado, usuario, cancellationToken))
            {
                throw new RegraNegocioException("Você só pode registrar partidas em grupos dos quais participa.");
            }

            return grupoSelecionado;
        }

        if (string.IsNullOrWhiteSpace(nomeNovoGrupo))
        {
            return await ObterOuCriarGrupoGeralAsync(cancellationToken);
        }

        var nomeNormalizado = NormalizarNome(nomeNovoGrupo);
        if (string.Equals(nomeNormalizado, NomeGrupoGeralConstante, StringComparison.OrdinalIgnoreCase))
        {
            return await ObterOuCriarGrupoGeralAsync(cancellationToken);
        }

        var grupoExistente = await grupoRepositorio.ObterPorNomeNormalizadoAsync(nomeNormalizado, cancellationToken);
        if (grupoExistente is not null)
        {
            if (await UsuarioPodeUsarGrupoExistenteAsync(grupoExistente, usuario, cancellationToken))
            {
                return grupoExistente;
            }

            throw new RegraNegocioException("Já existe um grupo com esse nome. Use outro nome ou selecione o grupo existente se você participar dele.");
        }

        var grupo = new Grupo
        {
            Nome = nomeNormalizado,
            Descricao = "Criada automaticamente a partir do registro rápido de partidas.",
            DataInicio = DateTime.UtcNow,
            UsuarioOrganizadorId = usuario.Perfil is PerfilUsuario.Organizador or PerfilUsuario.Atleta ? usuario.Id : null
        };

        await grupoRepositorio.AdicionarAsync(grupo, cancellationToken);
        if (usuario.AtletaId.HasValue)
        {
            await grupoAtletaRepositorio.AdicionarAsync(new GrupoAtleta
            {
                GrupoId = grupo.Id,
                AtletaId = usuario.AtletaId.Value
            }, cancellationToken);
        }

        return grupo;
    }

    public async Task ValidarNomeDisponivelOuAcessivelAsync(
        string nome,
        Guid? grupoIgnoradoId = null,
        CancellationToken cancellationToken = default)
    {
        var nomeNormalizado = NormalizarNome(nome);
        var grupoExistente = await grupoRepositorio.ObterPorNomeNormalizadoAsync(nomeNormalizado, cancellationToken);
        if (grupoExistente is null || grupoExistente.Id == grupoIgnoradoId)
        {
            return;
        }

        throw new RegraNegocioException("Já existe um grupo com esse nome.");
    }

    private async Task<bool> UsuarioPodeUsarGrupoExistenteAsync(
        Grupo grupo,
        Usuario usuario,
        CancellationToken cancellationToken)
    {
        if (EhGrupoGeral(grupo) || usuario.Perfil == PerfilUsuario.Administrador || grupo.UsuarioOrganizadorId == usuario.Id)
        {
            return true;
        }

        return await grupoRepositorio.AtletaPossuiAcessoAsync(grupo.Id, usuario.Id, usuario.AtletaId, cancellationToken);
    }

    private static bool EhGrupoGeral(Grupo grupo)
        => string.Equals(NormalizarNome(grupo.Nome), NomeGrupoGeralConstante, StringComparison.OrdinalIgnoreCase);

    private static string NormalizarNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new RegraNegocioException("Nome do grupo é obrigatório.");
        }

        return nome.Trim();
    }
}
