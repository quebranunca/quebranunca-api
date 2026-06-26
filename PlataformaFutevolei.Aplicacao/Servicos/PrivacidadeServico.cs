using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Interfaces.Seguranca;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Dominio.Entidades;

namespace PlataformaFutevolei.Aplicacao.Servicos;

public class PrivacidadeServico(
    IUsuarioRepositorio usuarioRepositorio,
    IUsuarioConsentimentoLgpdRepositorio consentimentoRepositorio,
    IUnidadeTrabalho unidadeTrabalho,
    IAutorizacaoUsuarioServico autorizacaoUsuarioServico,
    IUsuarioServico usuarioServico
) : IPrivacidadeServico
{
    public const string VersaoPoliticaPrivacidadeAtual = "2026-05-18";
    public const string VersaoTermosUsoAtual = "2026-05-18";
    private static readonly DateTime PoliticaVigenteDesdeUtc = new(2026, 5, 18, 0, 0, 0, DateTimeKind.Utc);
    private const string UrlPoliticaPrivacidadeAtual = "/privacidade";
    private const string UrlTermosUsoAtual = "/privacidade";

    public Task<PoliticaPrivacidadeAtualDto> ObterPoliticaAtualAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new PoliticaPrivacidadeAtualDto(
            VersaoPoliticaPrivacidadeAtual,
            PoliticaVigenteDesdeUtc,
            ExigeAceitePoliticaPrivacidade: true,
            ExigeAceiteTermosUso: true));
    }

    public Task<TermosVersaoAtualDto> ObterTermosVersaoAtualAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new TermosVersaoAtualDto(
            VersaoTermosUsoAtual,
            UrlTermosUsoAtual,
            VersaoPoliticaPrivacidadeAtual,
            UrlPoliticaPrivacidadeAtual));
    }

    public async Task<PreferenciasPrivacidadeDto> ObterMinhasPreferenciasAsync(CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        return MontarPreferencias(usuario);
    }

    public async Task<PreferenciasPrivacidadeDto> AtualizarMinhasPreferenciasAsync(
        AtualizarPreferenciasPrivacidadeDto dto,
        CancellationToken cancellationToken = default)
    {
        var usuarioAtual = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        var usuario = await usuarioRepositorio.ObterPorIdParaAtualizacaoAsync(usuarioAtual.Id, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Usuário não encontrado.");

        usuario.PerfilPublico = dto.PerfilPublico;
        usuario.ExibirEmail = dto.ExibirEmail;
        usuario.PermitirUsoLocalizacao = dto.PermitirUsoLocalizacao;
        usuario.PermitirUsoImagem = dto.PermitirUsoImagem;
        usuario.AtualizarDataModificacao();

        usuarioRepositorio.Atualizar(usuario);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        return MontarPreferencias(usuario);
    }

    public async Task<PreferenciasPrivacidadeDto> RegistrarConsentimentoAsync(
        RegistrarConsentimentoLgpdDto dto,
        CancellationToken cancellationToken = default)
    {
        var usuarioAtual = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        var usuario = await usuarioRepositorio.ObterPorIdParaAtualizacaoAsync(usuarioAtual.Id, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Usuário não encontrado.");

        await RegistrarConsentimentoUsuarioAsync(usuario, dto, cancellationToken);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        return MontarPreferencias(usuario);
    }

    public async Task SolicitarExclusaoContaAsync(CancellationToken cancellationToken = default)
    {
        var usuarioAtual = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        var usuario = await usuarioRepositorio.ObterPorIdParaAtualizacaoAsync(usuarioAtual.Id, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Usuário não encontrado.");

        usuario.ExclusaoSolicitadaEmUtc = DateTime.UtcNow;
        usuario.AtualizarDataModificacao();
        usuarioRepositorio.Atualizar(usuario);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        await usuarioServico.ExcluirMeuPerfilAsync(cancellationToken);
    }

    public async Task RegistrarConsentimentoUsuarioAsync(
        Usuario usuario,
        RegistrarConsentimentoLgpdDto dto,
        CancellationToken cancellationToken = default)
    {
        ValidarConsentimentoObrigatorio(dto);

        usuario.PermitirUsoLocalizacao = dto.AceitouUsoLocalizacao;
        usuario.PermitirUsoImagem = dto.AceitouUsoImagem;
        if (dto.AceitouMarketing)
        {
            usuario.ConsentimentoMarketingEmUtc ??= DateTime.UtcNow;
            usuario.RevogouMarketingEmUtc = null;
        }
        usuario.AtualizarDataModificacao();

        var aceitoEm = DateTime.UtcNow;
        await consentimentoRepositorio.AdicionarAsync(new UsuarioConsentimentoLgpd
        {
            UsuarioId = usuario.Id,
            Usuario = usuario,
            VersaoTermosUso = string.IsNullOrWhiteSpace(dto.VersaoTermosUso)
                ? VersaoTermosUsoAtual
                : dto.VersaoTermosUso.Trim(),
            VersaoPoliticaPrivacidade = string.IsNullOrWhiteSpace(dto.VersaoPoliticaPrivacidade)
                ? VersaoPoliticaPrivacidadeAtual
                : dto.VersaoPoliticaPrivacidade.Trim(),
            AceitouPoliticaPrivacidade = dto.AceitouPoliticaPrivacidade,
            AceitouTermosUso = dto.AceitouTermosUso,
            AceitouUsoLocalizacao = dto.AceitouUsoLocalizacao,
            AceitouUsoImagem = dto.AceitouUsoImagem,
            AceitoEm = aceitoEm,
            DeclarouMaioridadeEmUtc = dto.DeclarouMaiorDe18 ? aceitoEm : null,
            AceitouMarketing = dto.AceitouMarketing,
            ConsentimentoMarketingEmUtc = dto.AceitouMarketing ? aceitoEm : null,
            Origem = Limitar(dto.Origem, 80),
            IpAddress = Limitar(dto.IpAddress, 64),
            UserAgent = Limitar(dto.UserAgent, 512)
        }, cancellationToken);
    }

    public async Task<bool> UsuarioPrecisaAceitarPoliticaAsync(Guid usuarioId, CancellationToken cancellationToken = default)
    {
        var ultimo = await consentimentoRepositorio.ObterUltimoPorUsuarioAsync(usuarioId, cancellationToken);
        return ultimo is null ||
            ultimo.VersaoPoliticaPrivacidade != VersaoPoliticaPrivacidadeAtual ||
            ultimo.VersaoTermosUso != VersaoTermosUsoAtual ||
            !ultimo.AceitouPoliticaPrivacidade ||
            !ultimo.AceitouTermosUso;
    }

    private static void ValidarConsentimentoObrigatorio(RegistrarConsentimentoLgpdDto dto)
    {
        if (!dto.AceitouPoliticaPrivacidade || !dto.AceitouTermosUso)
        {
            throw new RegraNegocioException("É necessário aceitar a Política de Privacidade e os Termos de Uso para continuar.");
        }
    }

    private static PreferenciasPrivacidadeDto MontarPreferencias(Usuario usuario)
        => new(
            usuario.PerfilPublico,
            usuario.ExibirEmail,
            usuario.PermitirUsoLocalizacao,
            usuario.PermitirUsoImagem,
            PossuiFotoPerfil: !string.IsNullOrWhiteSpace(usuario.FotoPerfilUrl),
            usuario.ExclusaoSolicitadaEmUtc.HasValue);

    private static string? Limitar(string? valor, int tamanhoMaximo)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        var texto = valor.Trim();
        return texto.Length <= tamanhoMaximo ? texto : texto[..tamanhoMaximo];
    }
}
