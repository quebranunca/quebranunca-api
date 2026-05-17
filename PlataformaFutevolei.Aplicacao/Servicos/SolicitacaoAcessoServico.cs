using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Interfaces.Seguranca;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Aplicacao.Servicos;

public class SolicitacaoAcessoServico(
    ISolicitacaoAcessoRepositorio solicitacaoAcessoRepositorio,
    IConviteCadastroServico conviteCadastroServico,
    IAutorizacaoUsuarioServico autorizacaoUsuarioServico,
    IUnidadeTrabalho unidadeTrabalho
) : ISolicitacaoAcessoServico
{
    private const string MensagemSucesso = "Solicitação enviada com sucesso.";

    public async Task<SolicitacaoAcessoRespostaDto> CriarAsync(
        CriarSolicitacaoAcessoDto dto,
        CancellationToken cancellationToken = default)
    {
        var nome = (dto.Nome ?? string.Empty).Trim();
        var email = (dto.Email ?? string.Empty).Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new RegraNegocioException("Informe seu nome para solicitar acesso.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new RegraNegocioException("Informe seu e-mail para solicitar acesso.");
        }

        if (await solicitacaoAcessoRepositorio.ExistePendentePorEmailAsync(email, cancellationToken))
        {
            return new SolicitacaoAcessoRespostaDto(MensagemSucesso);
        }

        var solicitacao = new SolicitacaoAcesso
        {
            Nome = nome,
            Email = email
        };

        await solicitacaoAcessoRepositorio.AdicionarAsync(solicitacao, cancellationToken);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        return new SolicitacaoAcessoRespostaDto(MensagemSucesso);
    }

    public async Task<IReadOnlyList<SolicitacaoAcessoAdminDto>> ListarAdminAsync(
        CancellationToken cancellationToken = default)
    {
        await autorizacaoUsuarioServico.GarantirAdministradorAsync(cancellationToken);
        var solicitacoes = await solicitacaoAcessoRepositorio.ListarAsync(cancellationToken);
        return solicitacoes.Select(MapearAdmin).ToList();
    }

    public async Task<SolicitacaoAcessoAdminDto> AprovarAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var solicitacao = await ObterPendenteComoAdminAsync(id, cancellationToken);
        solicitacao.Status = StatusSolicitacaoAcesso.Aprovado;
        solicitacaoAcessoRepositorio.Atualizar(solicitacao);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
        return MapearAdmin(solicitacao);
    }

    public async Task<SolicitacaoAcessoAdminDto> RejeitarAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var solicitacao = await ObterPendenteComoAdminAsync(id, cancellationToken);
        solicitacao.Status = StatusSolicitacaoAcesso.Rejeitado;
        solicitacaoAcessoRepositorio.Atualizar(solicitacao);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
        return MapearAdmin(solicitacao);
    }

    public async Task<SolicitacaoAcessoAdminDto> EnviarConviteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var solicitacao = await ObterPendenteOuAprovadaComoAdminAsync(id, cancellationToken);

        await conviteCadastroServico.CriarAsync(
            new CriarConviteCadastroDto(
                solicitacao.Email,
                Telefone: null,
                PerfilDestino: PerfilUsuario.Atleta,
                ExpiraEmUtc: null,
                CanalEnvio: "E-mail"),
            cancellationToken);

        solicitacao.Status = StatusSolicitacaoAcesso.ConviteEnviado;
        solicitacaoAcessoRepositorio.Atualizar(solicitacao);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
        return MapearAdmin(solicitacao);
    }

    private async Task<SolicitacaoAcesso> ObterPendenteComoAdminAsync(Guid id, CancellationToken cancellationToken)
    {
        var solicitacao = await ObterComoAdminAsync(id, cancellationToken);
        if (solicitacao.Status != StatusSolicitacaoAcesso.Pendente)
        {
            throw new RegraNegocioException("Apenas solicitações pendentes podem ser alteradas.");
        }

        return solicitacao;
    }

    private async Task<SolicitacaoAcesso> ObterPendenteOuAprovadaComoAdminAsync(Guid id, CancellationToken cancellationToken)
    {
        var solicitacao = await ObterComoAdminAsync(id, cancellationToken);
        if (solicitacao.Status is not StatusSolicitacaoAcesso.Pendente and not StatusSolicitacaoAcesso.Aprovado)
        {
            throw new RegraNegocioException("Apenas solicitações pendentes ou aprovadas podem receber convite.");
        }

        return solicitacao;
    }

    private async Task<SolicitacaoAcesso> ObterComoAdminAsync(Guid id, CancellationToken cancellationToken)
    {
        await autorizacaoUsuarioServico.GarantirAdministradorAsync(cancellationToken);
        var solicitacao = await solicitacaoAcessoRepositorio.ObterPorIdParaAtualizacaoAsync(id, cancellationToken);
        if (solicitacao is null)
        {
            throw new EntidadeNaoEncontradaException("Solicitação de acesso não encontrada.");
        }

        return solicitacao;
    }

    private static SolicitacaoAcessoAdminDto MapearAdmin(SolicitacaoAcesso solicitacao)
    {
        return new SolicitacaoAcessoAdminDto(
            solicitacao.Id,
            solicitacao.Nome,
            solicitacao.Email,
            solicitacao.Status,
            solicitacao.DataCriacao,
            solicitacao.DataAtualizacao);
    }
}
