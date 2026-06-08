using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Aplicacao.Interfaces.Seguranca;

public interface ISenhaServico
{
    string GerarHash(string senha);
    bool Verificar(string senha, string hash);
}

public interface ITokenJwtServico
{
    string GerarToken(Usuario usuario, DateTime expiraEmUtc);
    Guid? ObterUsuarioIdTokenExpirado(string token);
    DateTime ObterExpiracaoTokenAcessoUtc(DateTime? limiteMaximoUtc = null);
    DateTime ObterExpiracaoRefreshTokenUtc();
}

public interface IUsuarioContexto
{
    Guid? UsuarioId { get; }
}

public interface IAutorizacaoUsuarioServico
{
    bool EhAdministrador(Usuario? usuario)
        => usuario?.Perfil == PerfilUsuario.Administrador;

    bool EhAdminOuOrganizador(Usuario? usuario)
        => usuario?.Perfil is PerfilUsuario.Administrador or PerfilUsuario.Organizador;

    Task<Usuario?> ObterUsuarioAtualAsync(CancellationToken cancellationToken = default);
    Task<Usuario> ObterUsuarioAtualObrigatorioAsync(CancellationToken cancellationToken = default);

    async Task<Usuario> ObterAdministradorAtualObrigatorioAsync(CancellationToken cancellationToken = default)
    {
        var usuario = await ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        if (!EhAdministrador(usuario))
        {
            throw new AcessoNegadoException("Apenas administradores podem executar esta operação.");
        }

        return usuario;
    }

    async Task<Usuario> ObterAdminOuOrganizadorAtualObrigatorioAsync(CancellationToken cancellationToken = default)
    {
        var usuario = await ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        if (!EhAdminOuOrganizador(usuario))
        {
            throw new RegraNegocioException("Apenas administradores ou organizadores podem executar esta operação.");
        }

        return usuario;
    }

    Task GarantirAdministradorAsync(CancellationToken cancellationToken = default);
    Task GarantirAdminOuOrganizadorAsync(CancellationToken cancellationToken = default);
    Task GarantirAcessoAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default);
    Task GarantirGestaoCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default);
    Task GarantirGestaoGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default);
}
