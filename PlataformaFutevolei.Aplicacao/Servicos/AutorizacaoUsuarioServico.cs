using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Interfaces.Seguranca;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Aplicacao.Servicos;

public class AutorizacaoUsuarioServico(
    IUsuarioRepositorio usuarioRepositorio,
    ICompeticaoRepositorio competicaoRepositorio,
    IGrupoRepositorio grupoRepositorio,
    IUsuarioContexto usuarioContexto
) : IAutorizacaoUsuarioServico
{
    public bool EhAdministrador(Usuario? usuario)
        => usuario?.Perfil == PerfilUsuario.Administrador;

    public bool EhAdminOuOrganizador(Usuario? usuario)
        => usuario?.Perfil is PerfilUsuario.Administrador or PerfilUsuario.Organizador;

    public async Task<Usuario?> ObterUsuarioAtualAsync(CancellationToken cancellationToken = default)
    {
        if (usuarioContexto.UsuarioId is null)
        {
            return null;
        }

        var usuario = await usuarioRepositorio.ObterPorIdAsync(usuarioContexto.UsuarioId.Value, cancellationToken);
        return usuario is not null && usuario.Ativo ? usuario : null;
    }

    public async Task<Usuario> ObterUsuarioAtualObrigatorioAsync(CancellationToken cancellationToken = default)
    {
        if (usuarioContexto.UsuarioId is null)
        {
            throw new RegraNegocioException("Usuário não autenticado.");
        }

        var usuario = await ObterUsuarioAtualAsync(cancellationToken);
        if (usuario is null)
        {
            throw new EntidadeNaoEncontradaException("Usuário não encontrado.");
        }

        return usuario;
    }

    public async Task<Usuario> ObterAdministradorAtualObrigatorioAsync(CancellationToken cancellationToken = default)
    {
        if (usuarioContexto.UsuarioId is null)
        {
            throw new AcessoNegadoException("Apenas administradores podem executar esta operação.");
        }

        var usuario = await ObterUsuarioAtualAsync(cancellationToken);
        if (!EhAdministrador(usuario))
        {
            throw new AcessoNegadoException("Apenas administradores podem executar esta operação.");
        }

        return usuario!;
    }

    public async Task<Usuario> ObterAdminOuOrganizadorAtualObrigatorioAsync(CancellationToken cancellationToken = default)
    {
        var usuario = await ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        if (!EhAdminOuOrganizador(usuario))
        {
            throw new RegraNegocioException("Apenas administradores ou organizadores podem executar esta operação.");
        }

        return usuario;
    }

    public async Task GarantirAdministradorAsync(CancellationToken cancellationToken = default)
    {
        await ObterAdministradorAtualObrigatorioAsync(cancellationToken);
    }

    public async Task GarantirAdminOuOrganizadorAsync(CancellationToken cancellationToken = default)
    {
        await ObterAdminOuOrganizadorAtualObrigatorioAsync(cancellationToken);
    }

    public async Task GarantirAcessoAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default)
    {
        var usuario = await ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        if (EhAdministrador(usuario))
        {
            return;
        }

        if (usuario.AtletaId != atletaId)
        {
            throw new RegraNegocioException("Você só pode acessar os dados do seu atleta vinculado.");
        }
    }

    public async Task GarantirGestaoCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default)
    {
        var usuario = await ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        var competicao = await competicaoRepositorio.ObterPorIdAsync(competicaoId, cancellationToken);
        if (competicao is null)
        {
            throw new EntidadeNaoEncontradaException("Competição não encontrada.");
        }

        if (EhAdministrador(usuario))
        {
            return;
        }

        if (usuario.Perfil == PerfilUsuario.Organizador)
        {
            if (competicao.UsuarioOrganizadorId != usuario.Id)
            {
                throw new RegraNegocioException("O organizador só pode alterar competições vinculadas ao próprio usuário.");
            }

            return;
        }

        if (usuario.Perfil == PerfilUsuario.Atleta)
        {
            if (competicao.Tipo != TipoCompeticao.Grupo)
            {
                throw new RegraNegocioException("Atletas só podem gerenciar grupos criados pelo próprio usuário.");
            }

            if (competicao.UsuarioOrganizadorId != usuario.Id)
            {
                throw new RegraNegocioException("Você só pode alterar grupos vinculados ao próprio usuário.");
            }

            return;
        }

        throw new RegraNegocioException("Apenas administradores, organizadores ou o atleta dono do grupo podem gerenciar competições.");
    }

    public async Task GarantirGestaoGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default)
    {
        var usuario = await ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        var grupo = await grupoRepositorio.ObterPorIdAsync(grupoId, cancellationToken);
        if (grupo is null)
        {
            throw new EntidadeNaoEncontradaException("Grupo não encontrado.");
        }

        if (!grupo.Ativo)
        {
            throw new RegraNegocioException("Grupo excluído não pode ser alterado.");
        }

        if (EhAdministrador(usuario))
        {
            return;
        }

        if (usuario.Perfil is PerfilUsuario.Organizador or PerfilUsuario.Atleta)
        {
            if (grupo.UsuarioOrganizadorId != usuario.Id)
            {
                throw new AcessoNegadoException("Você só pode alterar grupos vinculados ao próprio usuário.");
            }

            return;
        }

        throw new AcessoNegadoException("Apenas administradores, organizadores ou o atleta dono do grupo podem gerenciar grupos.");
    }
}
