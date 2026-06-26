using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Interfaces.Seguranca;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Aplicacao.Mapeadores;
using PlataformaFutevolei.Aplicacao.Utilitarios;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;

namespace PlataformaFutevolei.Aplicacao.Servicos;

public class AutenticacaoServico(
    IUsuarioRepositorio usuarioRepositorio,
    IConviteCadastroRepositorio conviteCadastroRepositorio,
    ICodigoAcessoEmailRepositorio codigoAcessoEmailRepositorio,
    IUnidadeTrabalho unidadeTrabalho,
    ISenhaServico senhaServico,
    ITokenJwtServico tokenJwtServico,
    IUsuarioContexto usuarioContexto,
    IResolvedorAtletaDuplaServico resolvedorAtletaDuplaServico,
    IPendenciaServico pendenciaServico,
    IEnvioEmailCodigoLoginServico envioEmailCodigoLoginServico,
    IPrivacidadeServico privacidadeServico
) : IAutenticacaoServico
{
    private const string StatusCodigoEnviado = "CodigoEnviado";
    private const string StatusAutenticado = "Autenticado";
    private const string StatusCadastroIncompleto = "CadastroIncompleto";
    private const int MaxTentativasCodigoAcesso = 5;
    private static readonly TimeSpan ValidadeCodigoLogin = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan ValidadeCadastroToken = TimeSpan.FromMinutes(30);

    public async Task<RespostaAutenticacaoDto> RegistrarAsync(RegistrarUsuarioRequisicaoDto dto, CancellationToken cancellationToken = default)
    {
        ValidarRegistro(dto);
        var emailNormalizado = NormalizarEmailObrigatorio(dto.Email);
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
        ValidarAceiteLgpd(dto);

        var agora = DateTime.UtcNow;
        var usuario = new Usuario
        {
            Nome = dto.Nome.Trim(),
            Email = emailNormalizado,
            SenhaHash = senhaServico.GerarHash(GerarSenhaInicialInterna()),
            Perfil = PerfilUsuario.Atleta,
            Ativo = true,
            EmailConfirmadoEmUtc = agora,
            CadastroCompletoEmUtc = agora
        };

        await VincularAtletaPendenteSeNecessarioAsync(usuario, dto.Nome, null, cancellationToken);
        conviteCadastro.MarcarComoUtilizado(agora);

        await usuarioRepositorio.AdicionarAsync(usuario, cancellationToken);
        await privacidadeServico.RegistrarConsentimentoUsuarioAsync(usuario, new RegistrarConsentimentoLgpdDto(
            dto.AceitouPoliticaPrivacidade,
            dto.AceitouTermosUso,
            dto.AceitouUsoLocalizacao,
            dto.AceitouUsoImagem,
            VersaoPoliticaPrivacidade: PrivacidadeServico.VersaoPoliticaPrivacidadeAtual,
            VersaoTermosUso: PrivacidadeServico.VersaoTermosUsoAtual,
            DeclarouMaiorDe18: false,
            AceitouMarketing: false,
            Origem: "ConviteCadastro",
            IpAddress: dto.IpAddress,
            UserAgent: dto.UserAgent), cancellationToken);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
        if (usuario.AtletaId.HasValue)
        {
            await pendenciaServico.SincronizarAposVinculoAtletaAsync(usuario.AtletaId.Value, cancellationToken);
        }

        return await CriarRespostaAutenticacaoAsync(usuario, cancellationToken, reutilizarExpiracaoRefreshTokenAtual: false);
    }

    public async Task<IniciarAcessoRespostaDto> IniciarAcessoAsync(
        IniciarAcessoRequisicaoDto dto,
        CancellationToken cancellationToken = default)
    {
        var emailNormalizado = NormalizarEmailObrigatorio(dto.Email);
        var usuario = await usuarioRepositorio.ObterPorEmailAsync(emailNormalizado, cancellationToken);
        var usuarioAtivo = usuario is not null && usuario.Ativo;
        var cadastroNovo = usuario is null;
        var podeEntrarComSenha = usuarioAtivo && UsuarioPossuiSenha(usuario!);
        var mensagemPadrao = "Se o e-mail estiver correto, enviaremos as instruções de acesso.";
        if (usuarioAtivo || cadastroNovo)
        {
            var finalidade = usuarioAtivo
                ? FinalidadeCodigoAcessoEmail.Login
                : FinalidadeCodigoAcessoEmail.CadastroPublico;
            await CriarEEnviarCodigoAcessoAsync(
                emailNormalizado,
                finalidade,
                usuarioAtivo ? usuario : null,
                cancellationToken);
        }

        return new IniciarAcessoRespostaDto(
            StatusCodigoEnviado,
            MascararEmail(emailNormalizado),
            podeEntrarComSenha,
            cadastroNovo,
            mensagemPadrao);
    }

    public async Task<ConfirmarCodigoAcessoRespostaDto> ConfirmarCodigoAcessoAsync(
        ConfirmarCodigoAcessoRequisicaoDto dto,
        CancellationToken cancellationToken = default)
    {
        var emailNormalizado = NormalizarEmailObrigatorio(dto.Email);
        var codigoInformado = NormalizarCodigoAcesso(dto.Codigo);
        var usuario = await usuarioRepositorio.ObterPorEmailParaAtualizacaoAsync(emailNormalizado, cancellationToken);
        var finalidade = usuario is not null && usuario.Ativo
            ? FinalidadeCodigoAcessoEmail.Login
            : FinalidadeCodigoAcessoEmail.CadastroPublico;

        var codigoAcesso = await ValidarCodigoAcessoAsync(
            emailNormalizado,
            finalidade,
            codigoInformado,
            cancellationToken);

        var agora = DateTime.UtcNow;
        if (finalidade == FinalidadeCodigoAcessoEmail.Login)
        {
            if (usuario is null || !usuario.Ativo)
            {
                throw new RegraNegocioException("Código de acesso inválido ou expirado.");
            }

            codigoAcesso.ConsumidoEmUtc = agora;
            codigoAcesso.AtualizarDataModificacao();
            codigoAcessoEmailRepositorio.Atualizar(codigoAcesso);

            usuario.EmailConfirmadoEmUtc ??= agora;
            usuario.AtualizarDataModificacao();
            usuarioRepositorio.Atualizar(usuario);
            await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);

            var autenticacao = await CriarRespostaAutenticacaoAsync(
                usuario,
                cancellationToken,
                reutilizarExpiracaoRefreshTokenAtual: false);
            return RespostaCodigoAutenticado(autenticacao);
        }

        if (usuario is not null)
        {
            throw new RegraNegocioException("Já existe um usuário cadastrado com este e-mail.");
        }

        var cadastroToken = GerarCadastroToken();
        codigoAcesso.ConsumidoEmUtc = agora;
        codigoAcesso.CadastroTokenHash = GerarHashCadastroToken(cadastroToken);
        codigoAcesso.CadastroTokenExpiraEmUtc = agora.Add(ValidadeCadastroToken);
        codigoAcesso.AtualizarDataModificacao();
        codigoAcessoEmailRepositorio.Atualizar(codigoAcesso);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        return new ConfirmarCodigoAcessoRespostaDto(
            StatusCadastroIncompleto,
            CadastroToken: cadastroToken,
            EmailConfirmado: true);
    }

    public async Task<RespostaAutenticacaoDto> CompletarCadastroPublicoAsync(
        CompletarCadastroPublicoRequisicaoDto dto,
        CancellationToken cancellationToken = default)
    {
        ValidarCadastroPublico(dto);

        var cadastroTokenHash = GerarHashCadastroToken(dto.CadastroToken.Trim());
        var agora = DateTime.UtcNow;
        var codigoAcesso = await codigoAcessoEmailRepositorio.ObterPorCadastroTokenHashParaAtualizacaoAsync(
            cadastroTokenHash,
            agora,
            cancellationToken);
        if (codigoAcesso is null)
        {
            throw new RegraNegocioException("Cadastro expirado. Solicite um novo código para continuar.");
        }

        var usuarioExistente = await usuarioRepositorio.ObterPorEmailAsync(codigoAcesso.EmailNormalizado, cancellationToken);
        if (usuarioExistente is not null)
        {
            throw new RegraNegocioException("Já existe um usuário cadastrado com este e-mail.");
        }

        var nomeExibicao = NormalizadorNomeAtleta.NormalizarTexto(dto.NomeExibicao);
        var apelido = NormalizadorNomeAtleta.NormalizarTexto(dto.Apelido);
        var usuario = new Usuario
        {
            Nome = nomeExibicao,
            Email = codigoAcesso.EmailNormalizado,
            SenhaHash = senhaServico.GerarHash(GerarSenhaInicialInterna()),
            Perfil = PerfilUsuario.Atleta,
            Ativo = true,
            EmailConfirmadoEmUtc = agora,
            CadastroCompletoEmUtc = agora,
            ConsentimentoMarketingEmUtc = dto.AceitouMarketing ? agora : null
        };

        await VincularAtletaPendenteSeNecessarioAsync(usuario, nomeExibicao, apelido, cancellationToken);
        await usuarioRepositorio.AdicionarAsync(usuario, cancellationToken);

        await privacidadeServico.RegistrarConsentimentoUsuarioAsync(usuario, new RegistrarConsentimentoLgpdDto(
            AceitouPoliticaPrivacidade: dto.AceitouPoliticaPrivacidade,
            AceitouTermosUso: dto.AceitouTermos,
            AceitouUsoLocalizacao: false,
            AceitouUsoImagem: false,
            VersaoPoliticaPrivacidade: dto.VersaoPoliticaPrivacidade,
            VersaoTermosUso: dto.VersaoTermos,
            DeclarouMaiorDe18: dto.DeclarouMaiorDe18,
            AceitouMarketing: dto.AceitouMarketing,
            Origem: "CadastroPublico",
            IpAddress: dto.IpAddress,
            UserAgent: dto.UserAgent), cancellationToken);

        codigoAcesso.CadastroTokenHash = null;
        codigoAcesso.CadastroTokenExpiraEmUtc = null;
        codigoAcesso.AtualizarDataModificacao();
        codigoAcessoEmailRepositorio.Atualizar(codigoAcesso);

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
        if (usuario is null || !usuario.Ativo)
        {
            throw new RegraNegocioException("Credenciais inválidas.");
        }

        if (!UsuarioPossuiSenha(usuario))
        {
            throw new RegraNegocioException("Este usuário ainda não cadastrou senha. Entre com código por e-mail e cadastre uma senha no seu perfil.");
        }

        if (!senhaServico.Verificar(dto.Senha, usuario.SenhaHash))
        {
            throw new RegraNegocioException("Credenciais inválidas.");
        }

        return await CriarRespostaAutenticacaoAsync(usuario, cancellationToken, reutilizarExpiracaoRefreshTokenAtual: false);
    }

    public async Task<SolicitarCodigoLoginRespostaDto> SolicitarCodigoLoginAsync(
        SolicitarCodigoLoginRequisicaoDto dto,
        CancellationToken cancellationToken = default)
    {
        var emailNormalizado = NormalizarEmailCodigoLogin(dto);
        var usuario = await usuarioRepositorio.ObterPorEmailParaAtualizacaoAsync(emailNormalizado, cancellationToken);
        var mensagemPadrao = "Se o e-mail estiver cadastrado, um código de acesso foi enviado.";

        if (usuario is null || !usuario.Ativo)
        {
            return new SolicitarCodigoLoginRespostaDto(mensagemPadrao);
        }

        var codigoDesenvolvimento = await CriarEEnviarCodigoAcessoAsync(
            emailNormalizado,
            FinalidadeCodigoAcessoEmail.Login,
            usuario,
            cancellationToken);

        return new SolicitarCodigoLoginRespostaDto(mensagemPadrao, codigoDesenvolvimento);
    }

    private static string NormalizarEmailCodigoLogin(SolicitarCodigoLoginRequisicaoDto? dto)
    {
        return NormalizarEmailObrigatorio(dto?.Email);
    }

    private static string NormalizarEmailObrigatorio(string? emailInformado)
    {
        if (string.IsNullOrWhiteSpace(emailInformado))
        {
            throw new RegraNegocioException("E-mail é obrigatório.");
        }

        var emailNormalizado = emailInformado.Trim().ToLowerInvariant();
        try
        {
            var email = new MailAddress(emailNormalizado);
            if (!string.Equals(email.Address, emailNormalizado, StringComparison.OrdinalIgnoreCase))
            {
                throw new FormatException();
            }
        }
        catch
        {
            throw new RegraNegocioException("E-mail inválido.");
        }

        return emailNormalizado;
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

        var emailNormalizado = NormalizarEmailObrigatorio(dto.Email);
        var usuario = await usuarioRepositorio.ObterPorEmailParaAtualizacaoAsync(emailNormalizado, cancellationToken);
        if (usuario is null || !usuario.Ativo)
        {
            throw new RegraNegocioException("Código de acesso inválido ou expirado.");
        }

        var codigoAcesso = await ValidarCodigoAcessoAsync(
            emailNormalizado,
            FinalidadeCodigoAcessoEmail.Login,
            NormalizarCodigoAcesso(dto.Codigo),
            cancellationToken);

        codigoAcesso.ConsumidoEmUtc = DateTime.UtcNow;
        codigoAcesso.AtualizarDataModificacao();
        codigoAcessoEmailRepositorio.Atualizar(codigoAcesso);
        usuario.EmailConfirmadoEmUtc ??= DateTime.UtcNow;
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

        var agora = DateTime.UtcNow;
        usuario!.SenhaHash = senhaServico.GerarHash(dto.NovaSenha);
        usuario.SenhaDefinidaEmUtc ??= agora;
        usuario.SenhaAtualizadaEmUtc = agora;
        usuario.CodigoRedefinicaoSenhaHash = null;
        usuario.CodigoRedefinicaoSenhaExpiraEmUtc = null;
        usuario.AtualizarDataModificacao();
        usuarioRepositorio.Atualizar(usuario);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
    }

    public async Task<SegurancaUsuarioDto> ObterSegurancaUsuarioAtualAsync(CancellationToken cancellationToken = default)
    {
        var usuario = await ObterUsuarioAtualParaAlteracaoSenhaAsync(cancellationToken);
        return new SegurancaUsuarioDto(UsuarioPossuiSenha(usuario));
    }

    public async Task<SegurancaUsuarioDto> DefinirSenhaAsync(
        DefinirSenhaRequisicaoDto dto,
        CancellationToken cancellationToken = default)
    {
        var usuario = await ObterUsuarioAtualParaAlteracaoSenhaAsync(cancellationToken);
        if (UsuarioPossuiSenha(usuario))
        {
            throw new RegraNegocioException("Senha já cadastrada. Use a alteração de senha.");
        }

        ValidarNovaSenha(dto.Senha, dto.ConfirmacaoSenha);

        var agora = DateTime.UtcNow;
        usuario.SenhaHash = senhaServico.GerarHash(dto.Senha);
        usuario.SenhaDefinidaEmUtc = agora;
        usuario.SenhaAtualizadaEmUtc = agora;
        usuario.AtualizarDataModificacao();
        usuarioRepositorio.Atualizar(usuario);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        return new SegurancaUsuarioDto(true);
    }

    public async Task<SegurancaUsuarioDto> AlterarSenhaAsync(
        AlterarSenhaRequisicaoDto dto,
        CancellationToken cancellationToken = default)
    {
        var usuario = await ObterUsuarioAtualParaAlteracaoSenhaAsync(cancellationToken);
        if (!UsuarioPossuiSenha(usuario))
        {
            throw new RegraNegocioException("Cadastre uma senha antes de alterá-la.");
        }

        if (string.IsNullOrWhiteSpace(dto.SenhaAtual))
        {
            throw new RegraNegocioException("Senha atual é obrigatória.");
        }

        if (!senhaServico.Verificar(dto.SenhaAtual, usuario.SenhaHash))
        {
            throw new RegraNegocioException("Senha atual incorreta.");
        }

        ValidarNovaSenha(dto.NovaSenha, dto.ConfirmacaoNovaSenha);

        var agora = DateTime.UtcNow;
        usuario.SenhaHash = senhaServico.GerarHash(dto.NovaSenha);
        usuario.SenhaAtualizadaEmUtc = agora;
        usuario.AtualizarDataModificacao();
        usuarioRepositorio.Atualizar(usuario);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        return new SegurancaUsuarioDto(true);
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

        return usuario.ParaDto() with
        {
            PoliticaPrivacidadePendente = await privacidadeServico.UsuarioPrecisaAceitarPoliticaAsync(
                usuario.Id,
                cancellationToken)
        };
    }

    private async Task<Usuario> ObterUsuarioAtualParaAlteracaoSenhaAsync(CancellationToken cancellationToken)
    {
        if (usuarioContexto.UsuarioId is null)
        {
            throw new RegraNegocioException("Usuário não autenticado.");
        }

        var usuario = await usuarioRepositorio.ObterPorIdParaAtualizacaoAsync(usuarioContexto.UsuarioId.Value, cancellationToken);
        if (usuario is null || !usuario.Ativo)
        {
            throw new EntidadeNaoEncontradaException("Usuário não encontrado.");
        }

        return usuario;
    }

    private static void ValidarNovaSenha(string? novaSenha, string? confirmacaoSenha)
    {
        if (string.IsNullOrWhiteSpace(novaSenha))
        {
            throw new RegraNegocioException("Senha é obrigatória.");
        }

        if (string.IsNullOrWhiteSpace(confirmacaoSenha))
        {
            throw new RegraNegocioException("Confirmação de senha é obrigatória.");
        }

        if (novaSenha.Length < 6)
        {
            throw new RegraNegocioException("A senha deve ter no mínimo 6 caracteres.");
        }

        if (!string.Equals(novaSenha, confirmacaoSenha, StringComparison.Ordinal))
        {
            throw new RegraNegocioException("Senha e confirmação devem ser iguais.");
        }
    }

    private static void ValidarCadastroPublico(CompletarCadastroPublicoRequisicaoDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CadastroToken))
        {
            throw new RegraNegocioException("Cadastro expirado. Solicite um novo código para continuar.");
        }

        if (string.IsNullOrWhiteSpace(dto.NomeExibicao))
        {
            throw new RegraNegocioException("Nome de exibição é obrigatório.");
        }

        if (!dto.AceitouTermos)
        {
            throw new RegraNegocioException("É necessário aceitar os Termos de Uso para continuar.");
        }

        if (!dto.AceitouPoliticaPrivacidade)
        {
            throw new RegraNegocioException("É necessário aceitar a Política de Privacidade para continuar.");
        }

        if (!dto.DeclarouMaiorDe18)
        {
            throw new RegraNegocioException("É necessário declarar que você tem 18 anos ou mais para continuar.");
        }
    }

    private static bool UsuarioPossuiSenha(Usuario usuario)
        => usuario.SenhaDefinidaEmUtc.HasValue;

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

    private static void ValidarAceiteLgpd(RegistrarUsuarioRequisicaoDto dto)
    {
        if (!dto.AceitouPoliticaPrivacidade || !dto.AceitouTermosUso)
        {
            throw new RegraNegocioException("É necessário aceitar a Política de Privacidade e os Termos de Uso para continuar.");
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
        string? apelido,
        CancellationToken cancellationToken)
    {
        if (usuario.Perfil != PerfilUsuario.Atleta)
        {
            return;
        }

        var atleta = await resolvedorAtletaDuplaServico.ObterOuCriarAtletaParaUsuarioAsync(
            nomeUsuario,
            usuario.Email,
            apelido,
            cancellationToken);
        usuario.AtletaId = atleta.Id;
        usuario.Atleta = atleta;
    }

    private async Task<string?> CriarEEnviarCodigoAcessoAsync(
        string emailNormalizado,
        FinalidadeCodigoAcessoEmail finalidade,
        Usuario? usuario,
        CancellationToken cancellationToken)
    {
        var agora = DateTime.UtcNow;
        var codigosPendentes = await codigoAcessoEmailRepositorio.ListarPendentesPorEmailFinalidadeParaAtualizacaoAsync(
            emailNormalizado,
            finalidade,
            cancellationToken);
        foreach (var pendente in codigosPendentes)
        {
            pendente.ConsumidoEmUtc = agora;
            pendente.AtualizarDataModificacao();
            codigoAcessoEmailRepositorio.Atualizar(pendente);
        }

        var codigo = GerarCodigoLogin();
        var codigoAcesso = new CodigoAcessoEmail
        {
            EmailNormalizado = emailNormalizado,
            CodigoHash = senhaServico.GerarHash(codigo),
            Finalidade = finalidade,
            ExpiraEmUtc = agora.Add(ValidadeCodigoLogin),
            Tentativas = 0,
            UltimoEnvioEmUtc = agora
        };

        await codigoAcessoEmailRepositorio.AdicionarAsync(codigoAcesso, cancellationToken);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        var destinatario = usuario ?? new Usuario
        {
            Nome = "QuebraNunca",
            Email = emailNormalizado,
            Ativo = true
        };

        var resultado = await envioEmailCodigoLoginServico.EnviarAsync(destinatario, codigo, cancellationToken);
        if (!resultado.TentativaRealizada || !resultado.Enviado)
        {
            codigoAcesso.ConsumidoEmUtc = DateTime.UtcNow;
            codigoAcesso.AtualizarDataModificacao();
            codigoAcessoEmailRepositorio.Atualizar(codigoAcesso);
            await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);

            throw new RegraNegocioException(
                resultado.Erro ?? "Não foi possível enviar o código de acesso por e-mail.");
        }

        return resultado.CodigoDesenvolvimento;
    }

    private async Task<CodigoAcessoEmail> ValidarCodigoAcessoAsync(
        string emailNormalizado,
        FinalidadeCodigoAcessoEmail finalidade,
        string codigoInformado,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(codigoInformado))
        {
            throw new RegraNegocioException("Código de acesso é obrigatório.");
        }

        var agora = DateTime.UtcNow;
        var codigoAcesso = await codigoAcessoEmailRepositorio.ObterAtivoPorEmailFinalidadeParaAtualizacaoAsync(
            emailNormalizado,
            finalidade,
            agora,
            cancellationToken);
        if (codigoAcesso is null)
        {
            throw new RegraNegocioException("Código de acesso inválido ou expirado.");
        }

        if (codigoAcesso.Tentativas >= MaxTentativasCodigoAcesso)
        {
            throw new RegraNegocioException("Muitas tentativas inválidas. Solicite um novo código.");
        }

        if (!senhaServico.Verificar(codigoInformado, codigoAcesso.CodigoHash))
        {
            codigoAcesso.Tentativas++;
            if (codigoAcesso.Tentativas >= MaxTentativasCodigoAcesso)
            {
                codigoAcesso.ConsumidoEmUtc = agora;
            }
            codigoAcesso.AtualizarDataModificacao();
            codigoAcessoEmailRepositorio.Atualizar(codigoAcesso);
            await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);

            throw new RegraNegocioException(codigoAcesso.Tentativas >= MaxTentativasCodigoAcesso
                ? "Muitas tentativas inválidas. Solicite um novo código."
                : "Código de acesso inválido ou expirado.");
        }

        return codigoAcesso;
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
        var usuarioDto = usuario.ParaDto() with
        {
            PoliticaPrivacidadePendente = await privacidadeServico.UsuarioPrecisaAceitarPoliticaAsync(
                usuario.Id,
                cancellationToken)
        };

        return new RespostaAutenticacaoDto(
            token,
            refreshToken,
            expiracaoToken,
            expiracaoRefreshToken,
            usuarioDto);
    }

    private static string NormalizarCodigoAcesso(string? codigo)
    {
        return string.IsNullOrWhiteSpace(codigo)
            ? string.Empty
            : codigo.Trim();
    }

    private static string MascararEmail(string email)
    {
        var partes = email.Split('@', 2);
        if (partes.Length != 2)
        {
            return "***";
        }

        var inicio = partes[0].Length == 0 ? "*" : partes[0][0].ToString();
        return $"{inicio}***@{partes[1]}";
    }

    private static ConfirmarCodigoAcessoRespostaDto RespostaCodigoAutenticado(RespostaAutenticacaoDto autenticacao)
        => new(
            StatusAutenticado,
            autenticacao.Token,
            autenticacao.RefreshToken,
            autenticacao.TokenExpiraEmUtc,
            autenticacao.RefreshTokenExpiraEmUtc,
            autenticacao.Usuario,
            EmailConfirmado: true);

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

    private static string GerarCadastroToken()
    {
        Span<byte> bytes = stackalloc byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-", StringComparison.Ordinal)
            .Replace("/", "_", StringComparison.Ordinal)
            .TrimEnd('=');
    }

    private static string GerarHashCadastroToken(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
