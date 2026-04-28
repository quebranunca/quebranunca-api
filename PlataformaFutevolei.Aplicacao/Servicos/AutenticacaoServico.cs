using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Interfaces.Seguranca;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Aplicacao.Mapeadores;
using PlataformaFutevolei.Aplicacao.Utilitarios;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;
using System.Security.Cryptography;

namespace PlataformaFutevolei.Aplicacao.Servicos;

public class AutenticacaoServico(
    IUsuarioRepositorio usuarioRepositorio,
    IConviteCadastroRepositorio conviteCadastroRepositorio,
    IUnidadeTrabalho unidadeTrabalho,
    ISenhaServico senhaServico,
    ITokenJwtServico tokenJwtServico,
    IUsuarioContexto usuarioContexto,
    IResolvedorAtletaDuplaServico resolvedorAtletaDuplaServico,
    IPendenciaServico pendenciaServico,
    IEnvioEmailCodigoLoginServico envioEmailCodigoLoginServico
) : IAutenticacaoServico
{
    private static readonly TimeSpan ValidadeCodigoLogin = TimeSpan.FromMinutes(15);

    public async Task<RespostaAutenticacaoDto> RegistrarAsync(RegistrarUsuarioRequisicaoDto dto, CancellationToken cancellationToken = default)
    {
        ValidarRegistro(dto);
        var emailNormalizado = dto.Email.Trim().ToLowerInvariant();
        var usuarioExistente = await usuarioRepositorio.ObterPorEmailAsync(emailNormalizado, cancellationToken);
        if (usuarioExistente is not null)
        {
            throw new RegraNegocioException("Já existe um usuário cadastrado com este e-mail.");
        }

        var conviteCadastro = await ObterConviteParaRegistroAsync(dto, cancellationToken);
        if (conviteCadastro is null)
        {
            throw new EntidadeNaoEncontradaException("Convite de cadastro não encontrado.");
        }

        ValidarConviteParaRegistro(conviteCadastro, emailNormalizado);

        var usuario = new Usuario
        {
            Nome = dto.Nome.Trim(),
            Email = emailNormalizado,
            SenhaHash = senhaServico.GerarHash(GerarSenhaInicialInterna()),
            Perfil = PerfilUsuario.Atleta,
            Ativo = true
        };

        await VincularAtletaPendenteSeNecessarioAsync(usuario, dto.Nome, cancellationToken);
        conviteCadastro.MarcarComoUtilizado(DateTime.UtcNow);

        await usuarioRepositorio.AdicionarAsync(usuario, cancellationToken);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
        if (usuario.AtletaId.HasValue)
        {
            await pendenciaServico.SincronizarAposVinculoAtletaAsync(usuario.AtletaId.Value, cancellationToken);
        }

        return await CriarRespostaAutenticacaoAsync(usuario, cancellationToken, reutilizarExpiracaoRefreshTokenAtual: false);
    }

    public async Task<RespostaAutenticacaoDto> LoginAsync(LoginRequisicaoDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Senha))
        {
            throw new RegraNegocioException("E-mail e senha são obrigatórios.");
        }

        var emailNormalizado = dto.Email.Trim().ToLowerInvariant();
        var usuario = await usuarioRepositorio.ObterPorEmailAsync(emailNormalizado, cancellationToken);
        if (usuario is null || !usuario.Ativo || !senhaServico.Verificar(dto.Senha, usuario.SenhaHash))
        {
            throw new RegraNegocioException("Credenciais inválidas.");
        }

        return await CriarRespostaAutenticacaoAsync(usuario, cancellationToken, reutilizarExpiracaoRefreshTokenAtual: false);
    }

    public async Task<SolicitarCodigoLoginRespostaDto> SolicitarCodigoLoginAsync(
        SolicitarCodigoLoginRequisicaoDto dto,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
        {
            throw new RegraNegocioException("E-mail é obrigatório.");
        }

        var emailNormalizado = dto.Email.Trim().ToLowerInvariant();
        var usuario = await usuarioRepositorio.ObterPorEmailParaAtualizacaoAsync(emailNormalizado, cancellationToken);
        var mensagemPadrao = "Se o e-mail estiver cadastrado, um código de acesso foi enviado.";

        if (usuario is null || !usuario.Ativo)
        {
            return new SolicitarCodigoLoginRespostaDto(mensagemPadrao);
        }

        var codigo = GerarCodigoLogin();
        usuario.CodigoLoginHash = senhaServico.GerarHash(codigo);
        usuario.CodigoLoginExpiraEmUtc = DateTime.UtcNow.Add(ValidadeCodigoLogin);
        usuario.AtualizarDataModificacao();
        usuarioRepositorio.Atualizar(usuario);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        var resultado = await envioEmailCodigoLoginServico.EnviarAsync(usuario, codigo, cancellationToken);
        if (!resultado.TentativaRealizada || !resultado.Enviado)
        {
            usuario.CodigoLoginHash = null;
            usuario.CodigoLoginExpiraEmUtc = null;
            usuario.AtualizarDataModificacao();
            usuarioRepositorio.Atualizar(usuario);
            await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);

            throw new RegraNegocioException(
                resultado.Erro ?? "Não foi possível enviar o código de acesso por e-mail.");
        }

        return new SolicitarCodigoLoginRespostaDto(mensagemPadrao, resultado.CodigoDesenvolvimento);
    }

    public async Task<RespostaAutenticacaoDto> LoginComCodigoAsync(
        LoginCodigoRequisicaoDto dto,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
        {
            throw new RegraNegocioException("E-mail é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(dto.Codigo))
        {
            throw new RegraNegocioException("Código de acesso é obrigatório.");
        }

        var emailNormalizado = dto.Email.Trim().ToLowerInvariant();
        var usuario = await usuarioRepositorio.ObterPorEmailParaAtualizacaoAsync(emailNormalizado, cancellationToken);
        var codigoValido = usuario is not null
            && usuario.Ativo
            && !string.IsNullOrWhiteSpace(usuario.CodigoLoginHash)
            && usuario.CodigoLoginExpiraEmUtc is not null
            && usuario.CodigoLoginExpiraEmUtc.Value >= DateTime.UtcNow
            && senhaServico.Verificar(dto.Codigo.Trim(), usuario.CodigoLoginHash);

        if (!codigoValido)
        {
            throw new RegraNegocioException("Código de acesso inválido ou expirado.");
        }

        usuario!.CodigoLoginHash = null;
        usuario.CodigoLoginExpiraEmUtc = null;
        usuario.AtualizarDataModificacao();
        usuarioRepositorio.Atualizar(usuario);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        return await CriarRespostaAutenticacaoAsync(usuario, cancellationToken, reutilizarExpiracaoRefreshTokenAtual: false);
    }

    public async Task<RespostaAutenticacaoDto> RenovarTokenAsync(
        RenovarTokenRequisicaoDto dto,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Token) || string.IsNullOrWhiteSpace(dto.RefreshToken))
        {
            throw new RegraNegocioException("Token de renovação inválido.");
        }

        var usuarioId = tokenJwtServico.ObterUsuarioIdTokenExpirado(dto.Token.Trim());
        if (!usuarioId.HasValue)
        {
            throw new RegraNegocioException("Token de renovação inválido.");
        }

        var usuario = await usuarioRepositorio.ObterPorIdParaAtualizacaoAsync(usuarioId.Value, cancellationToken);
        if (usuario is null || !usuario.Ativo)
        {
            throw new RegraNegocioException("Usuário não encontrado ou inativo.");
        }

        var agora = DateTime.UtcNow;
        var refreshTokenValido = !string.IsNullOrWhiteSpace(usuario.RefreshTokenHash)
            && usuario.RefreshTokenExpiraEmUtc.HasValue
            && usuario.RefreshTokenExpiraEmUtc.Value >= agora
            && senhaServico.Verificar(dto.RefreshToken.Trim(), usuario.RefreshTokenHash);

        if (!refreshTokenValido)
        {
            throw new RegraNegocioException("Sessão expirada. Faça login novamente.");
        }

        return await CriarRespostaAutenticacaoAsync(usuario, cancellationToken, reutilizarExpiracaoRefreshTokenAtual: true);
    }

    public async Task<SolicitarRedefinicaoSenhaRespostaDto> SolicitarRedefinicaoSenhaAsync(
        EsqueciSenhaRequisicaoDto dto,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
        {
            throw new RegraNegocioException("E-mail é obrigatório.");
        }

        var emailNormalizado = dto.Email.Trim().ToLowerInvariant();
        var usuario = await usuarioRepositorio.ObterPorEmailParaAtualizacaoAsync(emailNormalizado, cancellationToken);
        var mensagemPadrao = "Se o e-mail estiver cadastrado, um código de redefinição foi gerado.";

        if (usuario is null || !usuario.Ativo)
        {
            return new SolicitarRedefinicaoSenhaRespostaDto(mensagemPadrao);
        }

        var codigo = GerarCodigoRedefinicao();
        usuario.CodigoRedefinicaoSenhaHash = senhaServico.GerarHash(codigo);
        usuario.CodigoRedefinicaoSenhaExpiraEmUtc = DateTime.UtcNow.AddMinutes(15);
        usuario.AtualizarDataModificacao();
        usuarioRepositorio.Atualizar(usuario);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        return new SolicitarRedefinicaoSenhaRespostaDto(mensagemPadrao);
    }

    public async Task RedefinirSenhaAsync(RedefinirSenhaRequisicaoDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
        {
            throw new RegraNegocioException("E-mail é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(dto.Codigo))
        {
            throw new RegraNegocioException("Código de redefinição é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(dto.NovaSenha) || dto.NovaSenha.Length < 6)
        {
            throw new RegraNegocioException("A nova senha deve ter no mínimo 6 caracteres.");
        }

        var emailNormalizado = dto.Email.Trim().ToLowerInvariant();
        var usuario = await usuarioRepositorio.ObterPorEmailParaAtualizacaoAsync(emailNormalizado, cancellationToken);
        var codigoValido = usuario is not null
            && usuario.Ativo
            && !string.IsNullOrWhiteSpace(usuario.CodigoRedefinicaoSenhaHash)
            && usuario.CodigoRedefinicaoSenhaExpiraEmUtc is not null
            && usuario.CodigoRedefinicaoSenhaExpiraEmUtc.Value >= DateTime.UtcNow
            && senhaServico.Verificar(dto.Codigo.Trim(), usuario.CodigoRedefinicaoSenhaHash);

        if (!codigoValido)
        {
            throw new RegraNegocioException("Código de redefinição inválido ou expirado.");
        }

        usuario!.SenhaHash = senhaServico.GerarHash(dto.NovaSenha);
        usuario.CodigoRedefinicaoSenhaHash = null;
        usuario.CodigoRedefinicaoSenhaExpiraEmUtc = null;
        usuario.AtualizarDataModificacao();
        usuarioRepositorio.Atualizar(usuario);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
    }

    public async Task<UsuarioLogadoDto> ObterUsuarioAtualAsync(CancellationToken cancellationToken = default)
    {
        if (usuarioContexto.UsuarioId is null)
        {
            throw new RegraNegocioException("Usuário não autenticado.");
        }

        var usuario = await usuarioRepositorio.ObterPorIdAsync(usuarioContexto.UsuarioId.Value, cancellationToken);
        if (usuario is null || !usuario.Ativo)
        {
            throw new EntidadeNaoEncontradaException("Usuário não encontrado.");
        }

        return usuario.ParaDto();
    }

    private static void ValidarRegistro(RegistrarUsuarioRequisicaoDto dto)
    {
        var usandoFluxoConvite = !string.IsNullOrWhiteSpace(dto.ConviteIdPublico)
            && !string.IsNullOrWhiteSpace(dto.CodigoConvite);

        if (!usandoFluxoConvite)
        {
            throw new RegraNegocioException("Informe um convite válido para continuar o cadastro.");
        }

        if (string.IsNullOrWhiteSpace(dto.Nome))
        {
            throw new RegraNegocioException("Nome é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(dto.Email))
        {
            throw new RegraNegocioException("E-mail é obrigatório.");
        }

    }

    private static string GerarSenhaInicialInterna()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    }

    private async Task<ConviteCadastro?> ObterConviteParaRegistroAsync(
        RegistrarUsuarioRequisicaoDto dto,
        CancellationToken cancellationToken)
    {
        var conviteCadastro = await conviteCadastroRepositorio.ObterPorIdentificadorPublicoParaAtualizacaoAsync(
            dto.ConviteIdPublico!.Trim(),
            cancellationToken);

        if (conviteCadastro is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(conviteCadastro.CodigoConviteHash)
            || conviteCadastro.CodigoConviteHash != CodigoConviteUtilitario.GerarHash(dto.CodigoConvite!))
        {
            throw new RegraNegocioException("Código do convite inválido.");
        }

        return conviteCadastro;
    }

    private static void ValidarConviteParaRegistro(ConviteCadastro conviteCadastro, string emailNormalizado)
    {
        if (!conviteCadastro.Ativo)
        {
            throw new RegraNegocioException("Este convite está cancelado.");
        }

        if (conviteCadastro.FoiUtilizado())
        {
            throw new RegraNegocioException("Este convite já foi utilizado.");
        }

        if (conviteCadastro.EstaExpirado(DateTime.UtcNow))
        {
            throw new RegraNegocioException("Este convite está expirado.");
        }

        if (!string.Equals(conviteCadastro.Email, emailNormalizado, StringComparison.OrdinalIgnoreCase))
        {
            throw new RegraNegocioException("O e-mail informado deve ser o mesmo do convite.");
        }
    }

    private async Task VincularAtletaPendenteSeNecessarioAsync(
        Usuario usuario,
        string nomeUsuario,
        CancellationToken cancellationToken)
    {
        if (usuario.Perfil != PerfilUsuario.Atleta)
        {
            return;
        }

        var atleta = await resolvedorAtletaDuplaServico.ObterOuCriarAtletaParaUsuarioAsync(
            nomeUsuario,
            usuario.Email,
            cancellationToken);
        usuario.AtletaId = atleta.Id;
        usuario.Atleta = atleta;
    }

    private async Task<RespostaAutenticacaoDto> CriarRespostaAutenticacaoAsync(
        Usuario usuario,
        CancellationToken cancellationToken,
        bool reutilizarExpiracaoRefreshTokenAtual)
    {
        var agora = DateTime.UtcNow;
        var expiracaoRefreshToken = reutilizarExpiracaoRefreshTokenAtual
            && usuario.RefreshTokenExpiraEmUtc is { } expiracaoAtual
            && expiracaoAtual > agora
            ? expiracaoAtual
            : tokenJwtServico.ObterExpiracaoRefreshTokenUtc();
        var expiracaoToken = tokenJwtServico.ObterExpiracaoTokenAcessoUtc(expiracaoRefreshToken);
        var refreshToken = GerarRefreshToken();

        usuario.RefreshTokenHash = senhaServico.GerarHash(refreshToken);
        usuario.RefreshTokenExpiraEmUtc = expiracaoRefreshToken;
        usuario.AtualizarDataModificacao();
        usuarioRepositorio.Atualizar(usuario);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        var token = tokenJwtServico.GerarToken(usuario, expiracaoToken);
        return new RespostaAutenticacaoDto(
            token,
            refreshToken,
            expiracaoToken,
            expiracaoRefreshToken,
            usuario.ParaDto());
    }

    private static string GerarRefreshToken()
    {
        Span<byte> bytes = stackalloc byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes);
    }

    private static string GerarCodigoRedefinicao()
    {
        var numero = RandomNumberGenerator.GetInt32(100000, 1000000);
        return numero.ToString();
    }

    private static string GerarCodigoLogin()
    {
        var numero = RandomNumberGenerator.GetInt32(100000, 1000000);
        return numero.ToString();
    }
}
