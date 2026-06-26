using PlataformaFutevolei.Aplicacao.DTOs;

namespace PlataformaFutevolei.Aplicacao.Interfaces.Servicos;

public interface IAutenticacaoServico
{
    Task<RespostaAutenticacaoDto> RegistrarAsync(RegistrarUsuarioRequisicaoDto dto, CancellationToken cancellationToken = default);
    Task<IniciarAcessoRespostaDto> IniciarAcessoAsync(IniciarAcessoRequisicaoDto dto, CancellationToken cancellationToken = default);
    Task<ConfirmarCodigoAcessoRespostaDto> ConfirmarCodigoAcessoAsync(ConfirmarCodigoAcessoRequisicaoDto dto, CancellationToken cancellationToken = default);
    Task<RespostaAutenticacaoDto> CompletarCadastroPublicoAsync(CompletarCadastroPublicoRequisicaoDto dto, CancellationToken cancellationToken = default);
    Task<RespostaAutenticacaoDto> LoginAsync(LoginRequisicaoDto dto, CancellationToken cancellationToken = default);
    Task<SolicitarCodigoLoginRespostaDto> SolicitarCodigoLoginAsync(
        SolicitarCodigoLoginRequisicaoDto dto,
        CancellationToken cancellationToken = default);
    Task<RespostaAutenticacaoDto> LoginComCodigoAsync(LoginCodigoRequisicaoDto dto, CancellationToken cancellationToken = default);
    Task<RespostaAutenticacaoDto> RenovarTokenAsync(
        RenovarTokenRequisicaoDto dto,
        CancellationToken cancellationToken = default);
    Task<SolicitarRedefinicaoSenhaRespostaDto> SolicitarRedefinicaoSenhaAsync(
        EsqueciSenhaRequisicaoDto dto,
        CancellationToken cancellationToken = default);
    Task RedefinirSenhaAsync(RedefinirSenhaRequisicaoDto dto, CancellationToken cancellationToken = default);
    Task<SegurancaUsuarioDto> ObterSegurancaUsuarioAtualAsync(CancellationToken cancellationToken = default);
    Task<SegurancaUsuarioDto> DefinirSenhaAsync(DefinirSenhaRequisicaoDto dto, CancellationToken cancellationToken = default);
    Task<SegurancaUsuarioDto> AlterarSenhaAsync(AlterarSenhaRequisicaoDto dto, CancellationToken cancellationToken = default);
    Task<UsuarioLogadoDto> ObterUsuarioAtualAsync(CancellationToken cancellationToken = default);
}
