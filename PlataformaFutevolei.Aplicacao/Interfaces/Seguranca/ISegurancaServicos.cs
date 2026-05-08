using PlataformaFutevolei.Dominio.Entidades;

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
    Task<Usuario?> ObterUsuarioAtualAsync(CancellationToken cancellationToken = default);
    Task<Usuario> ObterUsuarioAtualObrigatorioAsync(CancellationToken cancellationToken = default);
    Task GarantirAdministradorAsync(CancellationToken cancellationToken = default);
    Task GarantirAdminOuOrganizadorAsync(CancellationToken cancellationToken = default);
    Task GarantirAcessoAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default);
    Task GarantirGestaoCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default);
    Task GarantirGestaoGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default);
}
