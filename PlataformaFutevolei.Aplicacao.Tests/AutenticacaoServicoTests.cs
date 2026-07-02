using System.Security.Cryptography;
using System.Text;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Interfaces.Seguranca;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Aplicacao.Servicos;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;
using Xunit;

namespace PlataformaFutevolei.Aplicacao.Tests;

public class AutenticacaoServicoTests
{
    [Fact]
    public async Task RegistrarAsync_RegistroValido_CriaUsuarioAtivoNormalizadoEVinculaAtleta()
    {
        var cenario = new Cenario();
        cenario.AtletaRetornado = new Atleta { Email = "atleta@example.com" };
        cenario.ResolvedorAtleta.AtletaPadrao = cenario.AtletaRetornado;
        var convite = CriarConvite(" JOAO@EXAMPLE.COM ", "c0de-123", cenario.ConviteUsuario.ToString("N"));
        cenario.Convites.Itens.Add(convite);

        var resposta = await cenario.Servico.RegistrarAsync(new RegistrarUsuarioRequisicaoDto(
            ConviteIdPublico: cenario.ConviteUsuario.ToString("N"),
            CodigoConvite: "c0de-123",
            Nome: "  João  ",
            Email: "  JOAO@EXAMPLE.COM  ",
            Senha: "123456",
            AceitouPoliticaPrivacidade: true,
            AceitouTermosUso: true,
            AceitouUsoLocalizacao: true,
            AceitouUsoImagem: true));

        var usuario = Assert.Single(cenario.Usuarios.Itens);

        Assert.Equal("joao@example.com", usuario.Email);
        Assert.Equal("João", usuario.Nome);
        Assert.True(usuario.Ativo);
        Assert.Equal(PerfilUsuario.Atleta, usuario.Perfil);
        Assert.NotNull(usuario.Atleta);
        Assert.Equal(cenario.AtletaRetornado.Id, usuario.Atleta!.Id);
        Assert.False(string.IsNullOrWhiteSpace(resposta.Token));
        Assert.False(string.IsNullOrWhiteSpace(resposta.RefreshToken));
        Assert.Equal(resposta.Usuario.Id, usuario.Id);
        Assert.True(convite.FoiUtilizado());
        Assert.NotNull(convite.UsadoEmUtc);
    }

    [Fact]
    public async Task RegistrarAsync_EmailDuplicado_Bloqueia()
    {
        var cenario = new Cenario();
        cenario.Usuarios.Itens.Add(new Usuario
        {
            Email = "joao@example.com",
            Nome = "Usuário",
            Perfil = PerfilUsuario.Atleta,
            Ativo = true
        });
        cenario.Convites.Itens.Add(CriarConvite("joao@example.com", "code", cenario.ConviteUsuario.ToString("N")));

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() => cenario.Servico.RegistrarAsync(new RegistrarUsuarioRequisicaoDto(
            cenario.ConviteUsuario.ToString("N"),
            "code",
            "João",
            "JOAO@EXAMPLE.COM",
            "123456",
            true,
            true)));

        Assert.Equal("Já existe um usuário cadastrado com este e-mail.", excecao.Message);
        Assert.Single(cenario.Usuarios.Itens);
    }

    [Fact]
    public async Task RegistrarAsync_ConviteInvalido_Bloqueia()
    {
        var cenario = new Cenario();

        var excecao = await Assert.ThrowsAsync<EntidadeNaoEncontradaException>(() => cenario.Servico.RegistrarAsync(new RegistrarUsuarioRequisicaoDto(
            ConviteIdPublico: Guid.NewGuid().ToString("N"),
            CodigoConvite: "999-999",
            Nome: "João",
            Email: "joao@example.com",
            Senha: "123456",
            AceitouPoliticaPrivacidade: true,
            AceitouTermosUso: true)));

        Assert.Equal("Convite de cadastro não encontrado.", excecao.Message);
    }

    [Fact]
    public async Task RegistrarAsync_EmailVazio_Bloqueia()
    {
        var cenario = new Cenario();

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() => cenario.Servico.RegistrarAsync(new RegistrarUsuarioRequisicaoDto(
            ConviteIdPublico: cenario.ConviteUsuario.ToString("N"),
            CodigoConvite: "code",
            Nome: "João",
            Email: "   ",
            Senha: "123456",
            AceitouPoliticaPrivacidade: true,
            AceitouTermosUso: true)));

        Assert.Equal("E-mail é obrigatório.", excecao.Message);
        Assert.Empty(cenario.Usuarios.Itens);
    }

    [Fact]
    public async Task RegistrarAsync_EmailInvalido_Bloqueia()
    {
        var cenario = new Cenario();
        cenario.Convites.Itens.Add(CriarConvite("email-invalido", "code", cenario.ConviteUsuario.ToString("N")));

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() => cenario.Servico.RegistrarAsync(new RegistrarUsuarioRequisicaoDto(
            ConviteIdPublico: cenario.ConviteUsuario.ToString("N"),
            CodigoConvite: "code",
            Nome: "João",
            Email: "email-invalido",
            Senha: "123456",
            AceitouPoliticaPrivacidade: true,
            AceitouTermosUso: true)));

        Assert.Equal("E-mail inválido.", excecao.Message);
        Assert.Empty(cenario.Usuarios.Itens);
    }

    [Fact]
    public async Task RegistrarAsync_NomeVazio_Bloqueia()
    {
        var cenario = new Cenario();

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() => cenario.Servico.RegistrarAsync(new RegistrarUsuarioRequisicaoDto(
            ConviteIdPublico: cenario.ConviteUsuario.ToString("N"),
            CodigoConvite: "code",
            Nome: "   ",
            Email: "joao@example.com",
            Senha: "123456",
            AceitouPoliticaPrivacidade: true,
            AceitouTermosUso: true)));

        Assert.Equal("Nome é obrigatório.", excecao.Message);
        Assert.Empty(cenario.Usuarios.Itens);
    }

    [Fact]
    public async Task RegistrarAsync_ConviteExpirado_Bloqueia()
    {
        var cenario = new Cenario();
        var convite = CriarConvite("joao@example.com", "code", cenario.ConviteUsuario.ToString("N"));
        convite.ExpiraEmUtc = DateTime.UtcNow.AddMinutes(-1);
        cenario.Convites.Itens.Add(convite);

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() => cenario.Servico.RegistrarAsync(new RegistrarUsuarioRequisicaoDto(
            cenario.ConviteUsuario.ToString("N"),
            "code",
            "João",
            "joao@example.com",
            "123456",
            true,
            true)));

        Assert.Equal("Este convite está expirado.", excecao.Message);
        Assert.Empty(cenario.Usuarios.Itens);
    }

    [Fact]
    public async Task RegistrarAsync_ConviteJaUtilizado_Bloqueia()
    {
        var cenario = new Cenario();
        var convite = CriarConvite("joao@example.com", "code", cenario.ConviteUsuario.ToString("N"));
        convite.UsadoEmUtc = DateTime.UtcNow.AddMinutes(-5);
        cenario.Convites.Itens.Add(convite);

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() => cenario.Servico.RegistrarAsync(new RegistrarUsuarioRequisicaoDto(
            cenario.ConviteUsuario.ToString("N"),
            "code",
            "João",
            "joao@example.com",
            "123456",
            true,
            true)));

        Assert.Equal("Este convite já foi utilizado.", excecao.Message);
        Assert.Empty(cenario.Usuarios.Itens);
    }

    [Fact]
    public async Task RegistrarAsync_CodigoConviteInvalido_Bloqueia()
    {
        var cenario = new Cenario();
        cenario.Convites.Itens.Add(CriarConvite("joao@example.com", "code", cenario.ConviteUsuario.ToString("N")));

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() => cenario.Servico.RegistrarAsync(new RegistrarUsuarioRequisicaoDto(
            cenario.ConviteUsuario.ToString("N"),
            "outro",
            "João",
            "joao@example.com",
            "123456",
            true,
            true)));

        Assert.Equal("Código do convite inválido.", excecao.Message);
        Assert.Empty(cenario.Usuarios.Itens);
    }

    [Fact]
    public async Task LoginAsync_CredenciaisValidas_RetornaTokens()
    {
        var cenario = new Cenario();
        cenario.Usuarios.Itens.Add(new Usuario
        {
            Nome = "João",
            Email = "joao@example.com",
            SenhaHash = "hash:123456",
            SenhaDefinidaEmUtc = DateTime.UtcNow.AddDays(-1),
            SenhaAtualizadaEmUtc = DateTime.UtcNow.AddDays(-1),
            Perfil = PerfilUsuario.Atleta,
            Ativo = true,
        });

        var resposta = await cenario.Servico.LoginAsync(new LoginRequisicaoDto(" JOAO@EXAMPLE.COM ", "123456"));

        Assert.Equal("joao@example.com", resposta.Usuario.Email);
        Assert.False(string.IsNullOrWhiteSpace(resposta.Token));
        Assert.False(string.IsNullOrWhiteSpace(resposta.RefreshToken));
    }

    [Fact]
    public async Task LoginAsync_CredenciaisInvalidas_Bloqueia()
    {
        var cenario = new Cenario();

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() => cenario.Servico.LoginAsync(new LoginRequisicaoDto(
            "joao@example.com",
            "123456")));

        Assert.Equal("Credenciais inválidas.", excecao.Message);
    }

    [Fact]
    public async Task LoginAsync_UsuarioInativo_Bloqueia()
    {
        var cenario = new Cenario();
        cenario.Usuarios.Itens.Add(new Usuario
        {
            Nome = "João",
            Email = "joao@example.com",
            SenhaHash = "hash:123456",
            SenhaDefinidaEmUtc = DateTime.UtcNow.AddDays(-1),
            SenhaAtualizadaEmUtc = DateTime.UtcNow.AddDays(-1),
            Perfil = PerfilUsuario.Atleta,
            Ativo = false
        });

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() => cenario.Servico.LoginAsync(new LoginRequisicaoDto("joao@example.com", "123456")));

        Assert.Equal("Credenciais inválidas.", excecao.Message);
    }

    [Fact]
    public async Task LoginAsync_SenhaInvalida_Bloqueia()
    {
        var cenario = new Cenario();
        cenario.Usuarios.Itens.Add(new Usuario
        {
            Nome = "João",
            Email = "joao@example.com",
            SenhaHash = "hash:123456",
            SenhaDefinidaEmUtc = DateTime.UtcNow.AddDays(-1),
            SenhaAtualizadaEmUtc = DateTime.UtcNow.AddDays(-1),
            Perfil = PerfilUsuario.Atleta,
            Ativo = true
        });

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() => cenario.Servico.LoginAsync(new LoginRequisicaoDto("joao@example.com", "outra")));

        Assert.Equal("Credenciais inválidas.", excecao.Message);
    }

    [Fact]
    public async Task SolicitarCodigoLoginAsync_EmailValido_GeraHashEExpiraESalvaCodigoSeguro()
    {
        var cenario = new Cenario();
        var usuario = new Usuario
        {
            Nome = "João",
            Email = "joao@example.com",
            SenhaHash = "hash-antigo",
            Perfil = PerfilUsuario.Atleta,
            Ativo = true
        };
        cenario.Usuarios.Itens.Add(usuario);

        var resposta = await cenario.Servico.SolicitarCodigoLoginAsync(new SolicitarCodigoLoginRequisicaoDto(" joao@example.com "));

        Assert.Equal("Se o e-mail estiver cadastrado, um código de acesso foi enviado.", resposta.Mensagem);
        Assert.Equal("123456", cenario.EnvioEmailCodigo.Resultado?.CodigoDesenvolvimento);
        Assert.NotNull(cenario.EnvioEmailCodigo.UltimoCodigo);
        Assert.Null(usuario.CodigoLoginHash);
        Assert.Null(usuario.CodigoLoginExpiraEmUtc);

        var codigoAcesso = Assert.Single(cenario.CodigosAcesso.Itens);
        Assert.Equal("joao@example.com", codigoAcesso.EmailNormalizado);
        Assert.Equal(FinalidadeCodigoAcessoEmail.CriarSenhaPrimeiroAcesso, codigoAcesso.Finalidade);
        Assert.Null(codigoAcesso.ConsumidoEmUtc);
        Assert.True(codigoAcesso.ExpiraEmUtc > DateTime.UtcNow);
        Assert.Equal(HashSenha(cenario.EnvioEmailCodigo.UltimoCodigo), codigoAcesso.CodigoHash);
    }

    [Fact]
    public async Task SolicitarCodigoLoginAsync_UsuarioInexistente_NaoRevelaExistencia()
    {
        var cenario = new Cenario();

        var resposta = await cenario.Servico.SolicitarCodigoLoginAsync(new SolicitarCodigoLoginRequisicaoDto("desconhecido@example.com"));

        Assert.Equal("Se o e-mail estiver cadastrado, um código de acesso foi enviado.", resposta.Mensagem);
        Assert.Null(cenario.EnvioEmailCodigo.UltimoEmail);
    }

    [Fact]
    public async Task SolicitarCodigoLoginAsync_EmailVazio_Bloqueia()
    {
        var cenario = new Cenario();

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.SolicitarCodigoLoginAsync(new SolicitarCodigoLoginRequisicaoDto("   ")));

        Assert.Equal("E-mail é obrigatório.", excecao.Message);
        Assert.Null(cenario.EnvioEmailCodigo.UltimoEmail);
    }

    [Fact]
    public async Task SolicitarCodigoLoginAsync_EmailInvalido_Bloqueia()
    {
        var cenario = new Cenario();

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.SolicitarCodigoLoginAsync(new SolicitarCodigoLoginRequisicaoDto("email-invalido")));

        Assert.Equal("E-mail inválido.", excecao.Message);
        Assert.Null(cenario.EnvioEmailCodigo.UltimoEmail);
    }

    [Fact]
    public async Task SolicitarCodigoLoginAsync_UsuarioInativo_NaoGeraCodigo()
    {
        var cenario = new Cenario();
        var usuario = new Usuario
        {
            Nome = "João",
            Email = "joao@example.com",
            SenhaHash = "hash-antigo",
            Perfil = PerfilUsuario.Atleta,
            Ativo = false
        };
        cenario.Usuarios.Itens.Add(usuario);

        var resposta = await cenario.Servico.SolicitarCodigoLoginAsync(new SolicitarCodigoLoginRequisicaoDto("joao@example.com"));

        Assert.Equal("Se o e-mail estiver cadastrado, um código de acesso foi enviado.", resposta.Mensagem);
        Assert.Null(usuario.CodigoLoginHash);
        Assert.Null(usuario.CodigoLoginExpiraEmUtc);
        Assert.Null(cenario.EnvioEmailCodigo.UltimoEmail);
        Assert.Empty(cenario.CodigosAcesso.Itens);
    }

    [Fact]
    public async Task LoginComCodigoAsync_CodigoValido_NaoAutenticaComoLoginDefinitivo()
    {
        var cenario = new Cenario();
        var usuario = new Usuario
        {
            Nome = "João",
            Email = "joao@example.com",
            SenhaHash = "hash:senha",
            Perfil = PerfilUsuario.Atleta,
            Ativo = true
        };
        cenario.Usuarios.Itens.Add(usuario);
        var codigoAcesso = CriarCodigoAcesso(cenario, "joao@example.com", "123456");

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.LoginComCodigoAsync(new LoginCodigoRequisicaoDto("joao@example.com", "123456")));

        Assert.Equal("Confirme o código pelo fluxo de criação de senha para continuar.", excecao.Message);
        Assert.Null(codigoAcesso.ConsumidoEmUtc);
        Assert.Null(usuario.EmailConfirmadoEmUtc);
    }

    [Fact]
    public async Task LoginComCodigoAsync_EmailNormalizado_NaoAutenticaComoLoginDefinitivo()
    {
        var cenario = new Cenario();
        var usuario = new Usuario
        {
            Nome = "João",
            Email = "joao@example.com",
            SenhaHash = "hash:senha",
            Perfil = PerfilUsuario.Atleta,
            Ativo = true
        };
        cenario.Usuarios.Itens.Add(usuario);
        var codigoAcesso = CriarCodigoAcesso(cenario, "joao@example.com", "123456");

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.LoginComCodigoAsync(new LoginCodigoRequisicaoDto(" JOAO@EXAMPLE.COM ", "123456")));

        Assert.Equal("Confirme o código pelo fluxo de criação de senha para continuar.", excecao.Message);
        Assert.Null(codigoAcesso.ConsumidoEmUtc);
    }

    [Fact]
    public async Task LoginComCodigoAsync_CodigoIncorreto_Bloqueia()
    {
        var cenario = new Cenario();
        cenario.Usuarios.Itens.Add(new Usuario
        {
            Nome = "João",
            Email = "joao@example.com",
            SenhaHash = "hash:senha",
            Perfil = PerfilUsuario.Atleta,
            Ativo = true
        });
        var codigoAcesso = CriarCodigoAcesso(cenario, "joao@example.com", "123456");

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.LoginComCodigoAsync(new LoginCodigoRequisicaoDto("joao@example.com", "654321")));

        Assert.Equal("Confirme o código pelo fluxo de criação de senha para continuar.", excecao.Message);
        Assert.Equal(0, codigoAcesso.Tentativas);
        Assert.Null(codigoAcesso.ConsumidoEmUtc);
    }

    [Fact]
    public async Task LoginComCodigoAsync_CodigoExpirado_Bloqueia()
    {
        var cenario = new Cenario();
        cenario.Usuarios.Itens.Add(new Usuario
        {
            Nome = "João",
            Email = "joao@example.com",
            SenhaHash = "hash:senha",
            Perfil = PerfilUsuario.Atleta,
            Ativo = true
        });
        var codigoAcesso = CriarCodigoAcesso(
            cenario,
            "joao@example.com",
            "123456",
            expiraEmUtc: DateTime.UtcNow.AddMinutes(-1));

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() => cenario.Servico.LoginComCodigoAsync(new LoginCodigoRequisicaoDto("joao@example.com", "123456")));

        Assert.Equal("Confirme o código pelo fluxo de criação de senha para continuar.", excecao.Message);
        Assert.Null(codigoAcesso.ConsumidoEmUtc);
    }

    [Fact]
    public async Task IniciarAcessoAsync_EmailNovo_EnviaCodigoCadastroPublico()
    {
        var cenario = new Cenario();

        var resposta = await cenario.Servico.IniciarAcessoAsync(new IniciarAcessoRequisicaoDto(" NOVO@EXAMPLE.COM "));

        Assert.Equal("CadastroNovoCodigoEnviado", resposta.Status);
        Assert.True(resposta.CadastroNovo);
        Assert.False(resposta.PodeEntrarComSenha);
        Assert.Equal("n***@example.com", resposta.EmailMascarado);
        Assert.Equal("123456", resposta.CodigoDesenvolvimento);
        Assert.Equal("novo@example.com", cenario.EnvioEmailCodigo.UltimoEmail);
        Assert.Empty(cenario.Usuarios.Itens);
        var codigoAcesso = Assert.Single(cenario.CodigosAcesso.Itens);
        Assert.Equal("novo@example.com", codigoAcesso.EmailNormalizado);
        Assert.Equal(FinalidadeCodigoAcessoEmail.CadastroPublico, codigoAcesso.Finalidade);
        Assert.Null(codigoAcesso.ConsumidoEmUtc);
    }

    [Fact]
    public async Task IniciarAcessoAsync_UsuarioExistenteSemSenha_EnviaCodigoCriacaoSenha()
    {
        var cenario = new Cenario();
        cenario.Usuarios.Itens.Add(CriarUsuarioSemSenha());

        var resposta = await cenario.Servico.IniciarAcessoAsync(new IniciarAcessoRequisicaoDto("JOAO@EXAMPLE.COM"));

        Assert.Equal("CriarSenhaNecessarioCodigoEnviado", resposta.Status);
        Assert.False(resposta.CadastroNovo);
        Assert.False(resposta.PodeEntrarComSenha);
        Assert.Single(cenario.Usuarios.Itens);
        var codigoAcesso = Assert.Single(cenario.CodigosAcesso.Itens);
        Assert.Equal(FinalidadeCodigoAcessoEmail.CriarSenhaPrimeiroAcesso, codigoAcesso.Finalidade);
    }

    [Fact]
    public async Task IniciarAcessoAsync_UsuarioExistenteComSenha_PermiteSenhaSemEnviarCodigo()
    {
        var cenario = new Cenario();
        cenario.Usuarios.Itens.Add(CriarUsuarioComSenha("123456"));

        var resposta = await cenario.Servico.IniciarAcessoAsync(new IniciarAcessoRequisicaoDto("joao@example.com"));

        Assert.Equal("EntrarComSenha", resposta.Status);
        Assert.False(resposta.CadastroNovo);
        Assert.True(resposta.PodeEntrarComSenha);
        Assert.Single(cenario.Usuarios.Itens);
        Assert.Empty(cenario.CodigosAcesso.Itens);
    }

    [Fact]
    public async Task ConfirmarCodigoAcessoAsync_UsuarioExistenteSemSenha_RetornaTokenTemporarioSemJwt()
    {
        var cenario = new Cenario();
        var usuario = CriarUsuarioSemSenha();
        cenario.Usuarios.Itens.Add(usuario);
        var codigoAcesso = CriarCodigoAcesso(cenario, "joao@example.com", "123456");

        var resposta = await cenario.Servico.ConfirmarCodigoAcessoAsync(new ConfirmarCodigoAcessoRequisicaoDto(" JOAO@EXAMPLE.COM ", "123456"));

        Assert.Equal("CriarSenhaNecessario", resposta.Status);
        Assert.Null(resposta.Usuario);
        Assert.Null(resposta.Token);
        Assert.Null(resposta.RefreshToken);
        Assert.False(string.IsNullOrWhiteSpace(resposta.SenhaToken));
        Assert.NotNull(usuario.EmailConfirmadoEmUtc);
        Assert.NotNull(codigoAcesso.ConsumidoEmUtc);
        Assert.NotNull(codigoAcesso.CadastroTokenHash);
        Assert.NotNull(codigoAcesso.CadastroTokenExpiraEmUtc);
        Assert.Empty(cenario.Privacidade.Consentimentos);
        Assert.Null(cenario.ResolvedorAtleta.UltimoEmailInformado);
    }

    [Fact]
    public async Task CriarSenhaComTokenAsync_UsuarioExistenteSemSenha_TokenValido_DefineSenhaEAutentica()
    {
        var cenario = new Cenario();
        var usuario = CriarUsuarioSemSenha();
        cenario.Usuarios.Itens.Add(usuario);
        var codigoAcesso = CriarCodigoAcesso(cenario, "joao@example.com", "123456");
        var confirmacao = await cenario.Servico.ConfirmarCodigoAcessoAsync(
            new ConfirmarCodigoAcessoRequisicaoDto("joao@example.com", "123456"));

        var resposta = await cenario.Servico.CriarSenhaComTokenAsync(new CriarSenhaComTokenRequisicaoDto(
            confirmacao.SenhaToken!,
            "nova123",
            "nova123"));

        Assert.Equal(usuario.Id, resposta.Usuario.Id);
        Assert.True(resposta.Usuario.PossuiSenha);
        Assert.True(resposta.Usuario.SenhaCadastrada);
        Assert.Empty(resposta.Usuario.PendenciasConta);
        Assert.False(string.IsNullOrWhiteSpace(resposta.Token));
        Assert.False(string.IsNullOrWhiteSpace(resposta.RefreshToken));
        Assert.Equal("hash:nova123", usuario.SenhaHash);
        Assert.NotNull(usuario.SenhaDefinidaEmUtc);
        Assert.NotNull(usuario.SenhaAtualizadaEmUtc);
        Assert.NotNull(usuario.EmailConfirmadoEmUtc);
        Assert.Null(codigoAcesso.CadastroTokenHash);
        Assert.Null(codigoAcesso.CadastroTokenExpiraEmUtc);
    }

    [Fact]
    public async Task CriarSenhaComTokenAsync_TokenTemporarioExpirado_Bloqueia()
    {
        var cenario = new Cenario();
        var usuario = CriarUsuarioSemSenha();
        cenario.Usuarios.Itens.Add(usuario);
        var codigoAcesso = CriarCodigoAcesso(cenario, "joao@example.com", "123456");
        var confirmacao = await cenario.Servico.ConfirmarCodigoAcessoAsync(
            new ConfirmarCodigoAcessoRequisicaoDto("joao@example.com", "123456"));
        codigoAcesso.CadastroTokenExpiraEmUtc = DateTime.UtcNow.AddMinutes(-1);

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.CriarSenhaComTokenAsync(new CriarSenhaComTokenRequisicaoDto(
                confirmacao.SenhaToken!,
                "nova123",
                "nova123")));

        Assert.Equal("Token de criação de senha inválido ou expirado.", excecao.Message);
        Assert.Null(usuario.SenhaDefinidaEmUtc);
    }

    [Fact]
    public async Task ConfirmarCodigoAcessoAsync_EmailNovo_RetornaCadastroIncompletoComTokenTemporario()
    {
        var cenario = new Cenario();
        var codigoAcesso = CriarCodigoAcesso(
            cenario,
            "novo@example.com",
            "123456",
            FinalidadeCodigoAcessoEmail.CadastroPublico);

        var resposta = await cenario.Servico.ConfirmarCodigoAcessoAsync(new ConfirmarCodigoAcessoRequisicaoDto("novo@example.com", "123456"));

        Assert.Equal("CadastroIncompleto", resposta.Status);
        Assert.True(resposta.EmailConfirmado);
        Assert.False(string.IsNullOrWhiteSpace(resposta.CadastroToken));
        Assert.Null(resposta.Token);
        Assert.Null(resposta.RefreshToken);
        Assert.Null(resposta.Usuario);
        Assert.Empty(cenario.Usuarios.Itens);
        Assert.NotNull(codigoAcesso.ConsumidoEmUtc);
        Assert.NotNull(codigoAcesso.CadastroTokenHash);
        Assert.NotNull(codigoAcesso.CadastroTokenExpiraEmUtc);
    }

    [Fact]
    public async Task CompletarCadastroPublicoAsync_DadosValidos_CriaUsuarioAtletaSemConviteEAutentica()
    {
        var cenario = new Cenario();
        var atletaExistente = new Atleta
        {
            Nome = "Atleta pendente",
            Email = "novo@example.com"
        };
        cenario.ResolvedorAtleta.AtletaPadrao = atletaExistente;
        var cadastroToken = await ObterCadastroTokenAsync(cenario, "novo@example.com");

        var resposta = await cenario.Servico.CompletarCadastroPublicoAsync(CadastroPublicoValido(
            cadastroToken,
            apelido: "Gu QN",
            aceitouMarketing: false));

        var usuario = Assert.Single(cenario.Usuarios.Itens);
        Assert.Equal("novo@example.com", usuario.Email);
        Assert.Equal("Gustavo", usuario.Nome);
        Assert.Equal(PerfilUsuario.Atleta, usuario.Perfil);
        Assert.True(usuario.Ativo);
        Assert.NotNull(usuario.EmailConfirmadoEmUtc);
        Assert.NotNull(usuario.CadastroCompletoEmUtc);
        Assert.NotNull(usuario.SenhaDefinidaEmUtc);
        Assert.NotNull(usuario.SenhaAtualizadaEmUtc);
        Assert.Equal("hash:Senha123", usuario.SenhaHash);
        Assert.Equal(atletaExistente.Id, usuario.AtletaId);
        Assert.Equal("Gu QN", cenario.ResolvedorAtleta.UltimoApelidoInformado);
        Assert.Equal(atletaExistente.Id, cenario.PendenciaServico.UltimoAtletaSincronizado);
        Assert.Null(usuario.ConsentimentoMarketingEmUtc);
        Assert.True(resposta.Usuario.PossuiSenha);
        Assert.True(resposta.Usuario.SenhaCadastrada);
        Assert.Empty(resposta.Usuario.PendenciasConta);
        Assert.False(string.IsNullOrWhiteSpace(resposta.Token));
        Assert.False(string.IsNullOrWhiteSpace(resposta.RefreshToken));

        var consentimento = Assert.Single(cenario.Privacidade.Consentimentos);
        Assert.True(consentimento.AceitouTermosUso);
        Assert.True(consentimento.AceitouPoliticaPrivacidade);
        Assert.True(consentimento.DeclarouMaiorDe18);
        Assert.False(consentimento.AceitouMarketing);
        Assert.Equal(PrivacidadeServico.VersaoTermosUsoAtual, consentimento.VersaoTermosUso);
        Assert.Equal(PrivacidadeServico.VersaoPoliticaPrivacidadeAtual, consentimento.VersaoPoliticaPrivacidade);
        Assert.Equal("CadastroPublico", consentimento.Origem);
    }

    [Fact]
    public async Task CompletarCadastroPublicoAsync_SemSenha_Bloqueia()
    {
        var cenario = new Cenario();
        var cadastroToken = await ObterCadastroTokenAsync(cenario, "novo@example.com");

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.CompletarCadastroPublicoAsync(CadastroPublicoValido(cadastroToken, senha: "   ", confirmacaoSenha: "   ")));

        Assert.Equal("Senha é obrigatória.", excecao.Message);
        Assert.Empty(cenario.Usuarios.Itens);
        Assert.Empty(cenario.Privacidade.Consentimentos);
    }

    [Fact]
    public async Task CompletarCadastroPublicoAsync_ConfirmacaoSenhaDiferente_Bloqueia()
    {
        var cenario = new Cenario();
        var cadastroToken = await ObterCadastroTokenAsync(cenario, "novo@example.com");

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.CompletarCadastroPublicoAsync(CadastroPublicoValido(cadastroToken, senha: "Senha123", confirmacaoSenha: "Outra123")));

        Assert.Equal("Senha e confirmação devem ser iguais.", excecao.Message);
        Assert.Empty(cenario.Usuarios.Itens);
        Assert.Empty(cenario.Privacidade.Consentimentos);
    }

    [Fact]
    public async Task CompletarCadastroPublicoAsync_NomeVazio_Bloqueia()
    {
        var cenario = new Cenario();
        var cadastroToken = await ObterCadastroTokenAsync(cenario, "novo@example.com");

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.CompletarCadastroPublicoAsync(CadastroPublicoValido(cadastroToken, nome: "   ")));

        Assert.Equal("Nome de exibição é obrigatório.", excecao.Message);
        Assert.Empty(cenario.Usuarios.Itens);
        Assert.Empty(cenario.Privacidade.Consentimentos);
    }

    [Fact]
    public async Task CompletarCadastroPublicoAsync_SemTermos_Bloqueia()
    {
        var cenario = new Cenario();
        var cadastroToken = await ObterCadastroTokenAsync(cenario, "novo@example.com");

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.CompletarCadastroPublicoAsync(CadastroPublicoValido(cadastroToken, aceitouTermos: false)));

        Assert.Equal("É necessário aceitar os Termos de Uso para continuar.", excecao.Message);
        Assert.Empty(cenario.Usuarios.Itens);
        Assert.Empty(cenario.Privacidade.Consentimentos);
    }

    [Fact]
    public async Task CompletarCadastroPublicoAsync_SemPoliticaPrivacidade_Bloqueia()
    {
        var cenario = new Cenario();
        var cadastroToken = await ObterCadastroTokenAsync(cenario, "novo@example.com");

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.CompletarCadastroPublicoAsync(CadastroPublicoValido(cadastroToken, aceitouPoliticaPrivacidade: false)));

        Assert.Equal("É necessário aceitar a Política de Privacidade para continuar.", excecao.Message);
        Assert.Empty(cenario.Usuarios.Itens);
        Assert.Empty(cenario.Privacidade.Consentimentos);
    }

    [Fact]
    public async Task CompletarCadastroPublicoAsync_SemDeclaracaoMaioridade_Bloqueia()
    {
        var cenario = new Cenario();
        var cadastroToken = await ObterCadastroTokenAsync(cenario, "novo@example.com");

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.CompletarCadastroPublicoAsync(CadastroPublicoValido(cadastroToken, declarouMaiorDe18: false)));

        Assert.Equal("É necessário declarar que você tem 18 anos ou mais para continuar.", excecao.Message);
        Assert.Empty(cenario.Usuarios.Itens);
        Assert.Empty(cenario.Privacidade.Consentimentos);
    }

    [Fact]
    public async Task CompletarCadastroPublicoAsync_MarketingOpcionalNaoBloqueiaCadastro()
    {
        var cenario = new Cenario();
        var cadastroToken = await ObterCadastroTokenAsync(cenario, "novo@example.com");

        await cenario.Servico.CompletarCadastroPublicoAsync(CadastroPublicoValido(cadastroToken, aceitouMarketing: false));

        var usuario = Assert.Single(cenario.Usuarios.Itens);
        Assert.Null(usuario.ConsentimentoMarketingEmUtc);
        Assert.False(Assert.Single(cenario.Privacidade.Consentimentos).AceitouMarketing);
    }

    [Fact]
    public async Task CompletarCadastroPublicoAsync_MarketingAceito_RegistraConsentimentoSeparado()
    {
        var cenario = new Cenario();
        var cadastroToken = await ObterCadastroTokenAsync(cenario, "novo@example.com");

        var resposta = await cenario.Servico.CompletarCadastroPublicoAsync(CadastroPublicoValido(cadastroToken, aceitouMarketing: true));

        var usuario = Assert.Single(cenario.Usuarios.Itens);
        Assert.NotNull(usuario.ConsentimentoMarketingEmUtc);
        Assert.True(Assert.Single(cenario.Privacidade.Consentimentos).AceitouMarketing);
        Assert.False(string.IsNullOrWhiteSpace(resposta.Token));
        Assert.False(string.IsNullOrWhiteSpace(resposta.RefreshToken));
    }

    [Fact]
    public async Task ConfirmarCodigoAcessoAsync_CodigoConsumido_NaoPermiteReuso()
    {
        var cenario = new Cenario();
        cenario.Usuarios.Itens.Add(CriarUsuarioSemSenha());
        CriarCodigoAcesso(
            cenario,
            "joao@example.com",
            "123456",
            consumidoEmUtc: DateTime.UtcNow.AddMinutes(-1));

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.ConfirmarCodigoAcessoAsync(new ConfirmarCodigoAcessoRequisicaoDto("joao@example.com", "123456")));

        Assert.Equal("Código de acesso inválido ou expirado.", excecao.Message);
    }

    [Fact]
    public async Task ConfirmarCodigoAcessoAsync_CodigoInvalidoParaCadastroNovo_NaoCriaUsuario()
    {
        var cenario = new Cenario();
        var codigoAcesso = CriarCodigoAcesso(
            cenario,
            "novo@example.com",
            "123456",
            FinalidadeCodigoAcessoEmail.CadastroPublico);

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.ConfirmarCodigoAcessoAsync(new ConfirmarCodigoAcessoRequisicaoDto("novo@example.com", "000000")));

        Assert.Equal("Código de acesso inválido ou expirado.", excecao.Message);
        Assert.Equal(1, codigoAcesso.Tentativas);
        Assert.Null(codigoAcesso.ConsumidoEmUtc);
        Assert.Empty(cenario.Usuarios.Itens);
    }

    [Fact]
    public async Task ConfirmarCodigoAcessoAsync_CodigoExpirado_Bloqueia()
    {
        var cenario = new Cenario();
        cenario.Usuarios.Itens.Add(CriarUsuarioSemSenha());
        CriarCodigoAcesso(
            cenario,
            "joao@example.com",
            "123456",
            expiraEmUtc: DateTime.UtcNow.AddMinutes(-1));

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.ConfirmarCodigoAcessoAsync(new ConfirmarCodigoAcessoRequisicaoDto("joao@example.com", "123456")));

        Assert.Equal("Código de acesso inválido ou expirado.", excecao.Message);
        Assert.Null(cenario.Usuarios.Itens.Single().EmailConfirmadoEmUtc);
    }

    [Fact]
    public async Task ConfirmarCodigoAcessoAsync_ExcessoTentativas_BloqueiaEConsomeCodigo()
    {
        var cenario = new Cenario();
        cenario.Usuarios.Itens.Add(CriarUsuarioSemSenha());
        var codigoAcesso = CriarCodigoAcesso(cenario, "joao@example.com", "123456");

        for (var tentativa = 1; tentativa <= 4; tentativa++)
        {
            var excecaoTentativa = await Assert.ThrowsAsync<RegraNegocioException>(() =>
                cenario.Servico.ConfirmarCodigoAcessoAsync(new ConfirmarCodigoAcessoRequisicaoDto("joao@example.com", "000000")));
            Assert.Equal("Código de acesso inválido ou expirado.", excecaoTentativa.Message);
            Assert.Equal(tentativa, codigoAcesso.Tentativas);
            Assert.Null(codigoAcesso.ConsumidoEmUtc);
        }

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.ConfirmarCodigoAcessoAsync(new ConfirmarCodigoAcessoRequisicaoDto("joao@example.com", "000000")));

        Assert.Equal("Muitas tentativas inválidas. Solicite um novo código.", excecao.Message);
        Assert.Equal(5, codigoAcesso.Tentativas);
        Assert.NotNull(codigoAcesso.ConsumidoEmUtc);
        Assert.Null(cenario.Usuarios.Itens.Single().EmailConfirmadoEmUtc);
    }

    [Fact]
    public async Task CompletarCadastroPublicoAsync_TokenTemporarioInvalido_Bloqueia()
    {
        var cenario = new Cenario();

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.CompletarCadastroPublicoAsync(CadastroPublicoValido("token-invalido")));

        Assert.Equal("Cadastro expirado. Solicite um novo código para continuar.", excecao.Message);
        Assert.Empty(cenario.Usuarios.Itens);
        Assert.Empty(cenario.Privacidade.Consentimentos);
    }

    [Fact]
    public async Task CompletarCadastroPublicoAsync_TokenTemporarioExpirado_Bloqueia()
    {
        var cenario = new Cenario();
        var cadastroToken = await ObterCadastroTokenAsync(cenario, "novo@example.com");
        var codigoAcesso = Assert.Single(cenario.CodigosAcesso.Itens);
        codigoAcesso.CadastroTokenExpiraEmUtc = DateTime.UtcNow.AddMinutes(-1);

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.CompletarCadastroPublicoAsync(CadastroPublicoValido(cadastroToken)));

        Assert.Equal("Cadastro expirado. Solicite um novo código para continuar.", excecao.Message);
        Assert.Empty(cenario.Usuarios.Itens);
        Assert.Empty(cenario.Privacidade.Consentimentos);
    }

    [Fact]
    public async Task CompletarCadastroPublicoAsync_EmailNormalizadoExistente_BloqueiaDuplicidade()
    {
        var cenario = new Cenario();
        var cadastroToken = await ObterCadastroTokenAsync(cenario, " PESSOA@EXAMPLE.COM ");
        cenario.Usuarios.Itens.Add(new Usuario
        {
            Nome = "Pessoa existente",
            Email = "pessoa@example.com",
            Perfil = PerfilUsuario.Atleta,
            Ativo = true
        });

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.CompletarCadastroPublicoAsync(CadastroPublicoValido(cadastroToken)));

        Assert.Equal("Já existe um usuário cadastrado com este e-mail.", excecao.Message);
        Assert.Single(cenario.Usuarios.Itens);
    }

    [Fact]
    public async Task CompletarCadastroPublicoAsync_AtletaPendenteEncontrado_NaoCriaOutroAtleta()
    {
        var cenario = new Cenario();
        var atletaPendente = new Atleta
        {
            Nome = "Atleta pendente",
            Email = "novo@example.com"
        };
        cenario.ResolvedorAtleta.AtletaPadrao = atletaPendente;
        var cadastroToken = await ObterCadastroTokenAsync(cenario, "novo@example.com");

        await cenario.Servico.CompletarCadastroPublicoAsync(CadastroPublicoValido(cadastroToken));

        var usuario = Assert.Single(cenario.Usuarios.Itens);
        Assert.Equal(atletaPendente.Id, usuario.AtletaId);
        Assert.Same(atletaPendente, usuario.Atleta);
    }

    [Fact]
    public async Task SolicitarRedefinicaoSenhaAsync_EmailValido_GeraHashEExpira()
    {
        var cenario = new Cenario();
        var usuario = new Usuario
        {
            Nome = "João",
            Email = "joao@example.com",
            SenhaHash = "hash-antigo",
            Perfil = PerfilUsuario.Atleta,
            Ativo = true
        };
        cenario.Usuarios.Itens.Add(usuario);

        var resposta = await cenario.Servico.SolicitarRedefinicaoSenhaAsync(new EsqueciSenhaRequisicaoDto(" joao@example.com "));

        Assert.Equal("Se o e-mail estiver cadastrado, um código de redefinição foi gerado.", resposta.Mensagem);
        Assert.Null(usuario.CodigoRedefinicaoSenhaHash);
        Assert.Null(usuario.CodigoRedefinicaoSenhaExpiraEmUtc);
        var codigoAcesso = Assert.Single(cenario.CodigosAcesso.Itens);
        Assert.Equal(FinalidadeCodigoAcessoEmail.RedefinirSenha, codigoAcesso.Finalidade);
        Assert.Equal("joao@example.com", codigoAcesso.EmailNormalizado);
        Assert.Equal(HashSenha(cenario.EnvioEmailCodigo.UltimoCodigo!), codigoAcesso.CodigoHash);
        Assert.True(codigoAcesso.ExpiraEmUtc > DateTime.UtcNow);
    }

    [Fact]
    public async Task SolicitarRedefinicaoSenhaAsync_EmailInexistente_NaoRevelaExistencia()
    {
        var cenario = new Cenario();
        var resposta = await cenario.Servico.SolicitarRedefinicaoSenhaAsync(new EsqueciSenhaRequisicaoDto("desconhecido@example.com"));

        Assert.Equal("Se o e-mail estiver cadastrado, um código de redefinição foi gerado.", resposta.Mensagem);
    }

    [Fact]
    public async Task SolicitarRedefinicaoSenhaAsync_EmailVazio_Bloqueia()
    {
        var cenario = new Cenario();

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.SolicitarRedefinicaoSenhaAsync(new EsqueciSenhaRequisicaoDto("   ")));

        Assert.Equal("E-mail é obrigatório.", excecao.Message);
    }

    [Fact]
    public async Task SolicitarRedefinicaoSenhaAsync_UsuarioInativo_NaoGeraCodigo()
    {
        var cenario = new Cenario();
        var usuario = new Usuario
        {
            Nome = "João",
            Email = "joao@example.com",
            SenhaHash = "hash-antigo",
            Perfil = PerfilUsuario.Atleta,
            Ativo = false
        };
        cenario.Usuarios.Itens.Add(usuario);

        var resposta = await cenario.Servico.SolicitarRedefinicaoSenhaAsync(new EsqueciSenhaRequisicaoDto("joao@example.com"));

        Assert.Equal("Se o e-mail estiver cadastrado, um código de redefinição foi gerado.", resposta.Mensagem);
        Assert.Null(usuario.CodigoRedefinicaoSenhaHash);
        Assert.Null(usuario.CodigoRedefinicaoSenhaExpiraEmUtc);
    }

    [Fact]
    public async Task RedefinirSenhaAsync_CodigoValido_AtualizaSenhaELimpaCodigo()
    {
        var cenario = new Cenario();
        var hashCodigo = cenario.SenhaServico.GerarHash("123456");
        var usuario = new Usuario
        {
            Nome = "João",
            Email = "joao@example.com",
            SenhaHash = "hash-antigo",
            Perfil = PerfilUsuario.Atleta,
            Ativo = true,
            CodigoRedefinicaoSenhaHash = hashCodigo,
            CodigoRedefinicaoSenhaExpiraEmUtc = DateTime.UtcNow.AddMinutes(10)
        };
        cenario.Usuarios.Itens.Add(usuario);
        var codigoAcesso = CriarCodigoAcesso(
            cenario,
            "joao@example.com",
            "123456",
            FinalidadeCodigoAcessoEmail.RedefinirSenha);

        await cenario.Servico.RedefinirSenhaAsync(new RedefinirSenhaRequisicaoDto(" joao@example.com ", "123456", "nova123"));

        Assert.Equal("hash:nova123", usuario.SenhaHash);
        Assert.NotNull(usuario.SenhaDefinidaEmUtc);
        Assert.NotNull(usuario.SenhaAtualizadaEmUtc);
        Assert.Null(usuario.CodigoRedefinicaoSenhaHash);
        Assert.Null(usuario.CodigoRedefinicaoSenhaExpiraEmUtc);
        Assert.NotNull(codigoAcesso.ConsumidoEmUtc);
    }

    [Fact]
    public async Task RedefinirSenhaAsync_EmailNormalizado_CodigoValidoAtualizaSenha()
    {
        var cenario = new Cenario();
        var usuario = new Usuario
        {
            Nome = "João",
            Email = "joao@example.com",
            SenhaHash = "hash-antigo",
            Perfil = PerfilUsuario.Atleta,
            Ativo = true,
            CodigoRedefinicaoSenhaHash = cenario.SenhaServico.GerarHash("123456"),
            CodigoRedefinicaoSenhaExpiraEmUtc = DateTime.UtcNow.AddMinutes(10)
        };
        cenario.Usuarios.Itens.Add(usuario);
        CriarCodigoAcesso(
            cenario,
            "joao@example.com",
            "123456",
            FinalidadeCodigoAcessoEmail.RedefinirSenha);

        await cenario.Servico.RedefinirSenhaAsync(new RedefinirSenhaRequisicaoDto(" JOAO@EXAMPLE.COM ", "123456", "nova123"));

        Assert.Equal("hash:nova123", usuario.SenhaHash);
        Assert.NotNull(usuario.SenhaDefinidaEmUtc);
        Assert.NotNull(usuario.SenhaAtualizadaEmUtc);
        Assert.Null(usuario.CodigoRedefinicaoSenhaHash);
        Assert.Null(usuario.CodigoRedefinicaoSenhaExpiraEmUtc);
    }

    [Fact]
    public async Task RedefinirSenhaAsync_NovaSenhaFraca_Bloqueia()
    {
        var cenario = new Cenario();
        cenario.Usuarios.Itens.Add(new Usuario
        {
            Nome = "João",
            Email = "joao@example.com",
            SenhaHash = "hash-antigo",
            Perfil = PerfilUsuario.Atleta,
            Ativo = true,
            CodigoRedefinicaoSenhaHash = cenario.SenhaServico.GerarHash("123456"),
            CodigoRedefinicaoSenhaExpiraEmUtc = DateTime.UtcNow.AddMinutes(10)
        });

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.RedefinirSenhaAsync(new RedefinirSenhaRequisicaoDto("joao@example.com", "123456", "12345")));

        Assert.Equal("A senha deve ter no mínimo 6 caracteres.", excecao.Message);
    }

    [Fact]
    public async Task RedefinirSenhaAsync_CodigoInvalido_Bloqueia()
    {
        var cenario = new Cenario();
        cenario.Usuarios.Itens.Add(new Usuario
        {
            Nome = "João",
            Email = "joao@example.com",
            SenhaHash = "hash-antigo",
            Perfil = PerfilUsuario.Atleta,
            Ativo = true
        });
        var codigoAcesso = CriarCodigoAcesso(
            cenario,
            "joao@example.com",
            "123456",
            FinalidadeCodigoAcessoEmail.RedefinirSenha);

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.RedefinirSenhaAsync(new RedefinirSenhaRequisicaoDto("joao@example.com", "654321", "nova123")));

        Assert.Equal("Código de acesso inválido ou expirado.", excecao.Message);
        Assert.Equal(1, codigoAcesso.Tentativas);
        Assert.Null(codigoAcesso.ConsumidoEmUtc);
    }

    [Fact]
    public async Task RedefinirSenhaAsync_CodigoExpirado_Bloqueia()
    {
        var cenario = new Cenario();
        cenario.Usuarios.Itens.Add(new Usuario
        {
            Nome = "João",
            Email = "joao@example.com",
            SenhaHash = "hash-antigo",
            Perfil = PerfilUsuario.Atleta,
            Ativo = true
        });
        var codigoAcesso = CriarCodigoAcesso(
            cenario,
            "joao@example.com",
            "123456",
            FinalidadeCodigoAcessoEmail.RedefinirSenha,
            expiraEmUtc: DateTime.UtcNow.AddMinutes(-1));

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.RedefinirSenhaAsync(new RedefinirSenhaRequisicaoDto("joao@example.com", "123456", "nova123")));

        Assert.Equal("Código de acesso inválido ou expirado.", excecao.Message);
        Assert.Null(codigoAcesso.ConsumidoEmUtc);
    }

    [Fact]
    public async Task LoginAsync_UsuarioSemSenhaDefinida_OrientaLoginPorCodigo()
    {
        var cenario = new Cenario();
        cenario.Usuarios.Itens.Add(new Usuario
        {
            Nome = "João",
            Email = "joao@example.com",
            SenhaHash = "hash:123456",
            Perfil = PerfilUsuario.Atleta,
            Ativo = true
        });

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.LoginAsync(new LoginRequisicaoDto("joao@example.com", "123456")));

        Assert.Equal("Este usuário ainda não cadastrou senha. Entre com código por e-mail e cadastre uma senha no seu perfil.", excecao.Message);
    }

    [Fact]
    public async Task DefinirSenhaAsync_UsuarioAutenticadoSemSenha_DefineHashEDatas()
    {
        var usuario = CriarUsuarioSemSenha();
        var cenario = new Cenario(usuario.Id);
        cenario.Usuarios.Itens.Add(usuario);

        var resposta = await cenario.Servico.DefinirSenhaAsync(new DefinirSenhaRequisicaoDto("nova123", "nova123"));

        Assert.True(resposta.PossuiSenha);
        Assert.Equal("hash:nova123", usuario.SenhaHash);
        Assert.NotNull(usuario.SenhaDefinidaEmUtc);
        Assert.NotNull(usuario.SenhaAtualizadaEmUtc);
    }

    [Fact]
    public async Task ObterUsuarioAtualAsync_UsuarioSemSenha_RetornaPendenciaCriarSenhaSemBloquear()
    {
        var usuario = CriarUsuarioSemSenha();
        var cenario = new Cenario(usuario.Id);
        cenario.Usuarios.Itens.Add(usuario);

        var resposta = await cenario.Servico.ObterUsuarioAtualAsync();

        Assert.False(resposta.PossuiSenha);
        Assert.False(resposta.SenhaCadastrada);
        var pendencia = Assert.Single(resposta.PendenciasConta);
        Assert.Equal("CriarSenha", pendencia.Tipo);
        Assert.True(pendencia.Obrigatoria);
        Assert.False(pendencia.Bloqueante);
        Assert.Equal("Crie uma senha para continuar acessando sua conta com segurança.", pendencia.Mensagem);
    }

    [Fact]
    public async Task ObterUsuarioAtualAsync_AposDefinirSenha_RemovePendenciaCriarSenha()
    {
        var usuario = CriarUsuarioSemSenha();
        var cenario = new Cenario(usuario.Id);
        cenario.Usuarios.Itens.Add(usuario);

        await cenario.Servico.DefinirSenhaAsync(new DefinirSenhaRequisicaoDto("nova123", "nova123"));
        var resposta = await cenario.Servico.ObterUsuarioAtualAsync();

        Assert.True(resposta.PossuiSenha);
        Assert.True(resposta.SenhaCadastrada);
        Assert.Empty(resposta.PendenciasConta);
    }

    [Fact]
    public async Task DefinirSenhaAsync_ConfirmacaoDivergente_Bloqueia()
    {
        var usuario = CriarUsuarioSemSenha();
        var cenario = new Cenario(usuario.Id);
        cenario.Usuarios.Itens.Add(usuario);

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.DefinirSenhaAsync(new DefinirSenhaRequisicaoDto("nova123", "outra123")));

        Assert.Equal("Senha e confirmação devem ser iguais.", excecao.Message);
        Assert.Null(usuario.SenhaDefinidaEmUtc);
    }

    [Fact]
    public async Task DefinirSenhaAsync_SenhaMenorQueSeisCaracteres_Bloqueia()
    {
        var usuario = CriarUsuarioSemSenha();
        var cenario = new Cenario(usuario.Id);
        cenario.Usuarios.Itens.Add(usuario);

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.DefinirSenhaAsync(new DefinirSenhaRequisicaoDto("12345", "12345")));

        Assert.Equal("A senha deve ter no mínimo 6 caracteres.", excecao.Message);
        Assert.Null(usuario.SenhaDefinidaEmUtc);
    }

    [Fact]
    public async Task DefinirSenhaAsync_UsuarioJaPossuiSenha_Bloqueia()
    {
        var usuario = CriarUsuarioComSenha("atual123");
        var cenario = new Cenario(usuario.Id);
        cenario.Usuarios.Itens.Add(usuario);

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.DefinirSenhaAsync(new DefinirSenhaRequisicaoDto("nova123", "nova123")));

        Assert.Equal("Senha já cadastrada. Use a alteração de senha.", excecao.Message);
        Assert.Equal("hash:atual123", usuario.SenhaHash);
    }

    [Fact]
    public async Task AlterarSenhaAsync_SenhaAtualCorreta_AtualizaHashEPreservaDataDefinicao()
    {
        var usuario = CriarUsuarioComSenha("atual123");
        var definidaEm = usuario.SenhaDefinidaEmUtc;
        var cenario = new Cenario(usuario.Id);
        cenario.Usuarios.Itens.Add(usuario);

        var resposta = await cenario.Servico.AlterarSenhaAsync(new AlterarSenhaRequisicaoDto("atual123", "nova123", "nova123"));

        Assert.True(resposta.PossuiSenha);
        Assert.Equal("hash:nova123", usuario.SenhaHash);
        Assert.Equal(definidaEm, usuario.SenhaDefinidaEmUtc);
        Assert.NotNull(usuario.SenhaAtualizadaEmUtc);
        Assert.NotEqual(definidaEm, usuario.SenhaAtualizadaEmUtc);
    }

    [Fact]
    public async Task AlterarSenhaAsync_SenhaAtualIncorreta_Bloqueia()
    {
        var usuario = CriarUsuarioComSenha("atual123");
        var cenario = new Cenario(usuario.Id);
        cenario.Usuarios.Itens.Add(usuario);

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.AlterarSenhaAsync(new AlterarSenhaRequisicaoDto("errada", "nova123", "nova123")));

        Assert.Equal("Senha atual incorreta.", excecao.Message);
        Assert.Equal("hash:atual123", usuario.SenhaHash);
    }

    [Fact]
    public async Task AlterarSenhaAsync_ConfirmacaoDivergente_Bloqueia()
    {
        var usuario = CriarUsuarioComSenha("atual123");
        var cenario = new Cenario(usuario.Id);
        cenario.Usuarios.Itens.Add(usuario);

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.AlterarSenhaAsync(new AlterarSenhaRequisicaoDto("atual123", "nova123", "outra123")));

        Assert.Equal("Senha e confirmação devem ser iguais.", excecao.Message);
        Assert.Equal("hash:atual123", usuario.SenhaHash);
    }

    [Fact]
    public async Task AlterarSenhaAsync_SenhaMenorQueSeisCaracteres_Bloqueia()
    {
        var usuario = CriarUsuarioComSenha("atual123");
        var cenario = new Cenario(usuario.Id);
        cenario.Usuarios.Itens.Add(usuario);

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.AlterarSenhaAsync(new AlterarSenhaRequisicaoDto("atual123", "12345", "12345")));

        Assert.Equal("A senha deve ter no mínimo 6 caracteres.", excecao.Message);
        Assert.Equal("hash:atual123", usuario.SenhaHash);
    }

    [Fact]
    public async Task ObterSegurancaUsuarioAtualAsync_RetornaStatusSemHash()
    {
        var usuario = CriarUsuarioComSenha("atual123");
        var cenario = new Cenario(usuario.Id);
        cenario.Usuarios.Itens.Add(usuario);

        var resposta = await cenario.Servico.ObterSegurancaUsuarioAtualAsync();

        Assert.True(resposta.PossuiSenha);
        Assert.DoesNotContain(typeof(SegurancaUsuarioDto).GetProperties(), propriedade => propriedade.Name == nameof(Usuario.SenhaHash));
        Assert.DoesNotContain(typeof(UsuarioLogadoDto).GetProperties(), propriedade => propriedade.Name == nameof(Usuario.SenhaHash));
    }

    [Fact]
    public async Task RenovarTokenAsync_RefreshValido_RenovaMantendoExpiracaoExistente()
    {
        var usuario = new Usuario
        {
            Nome = "João",
            Email = "joao@example.com",
            SenhaHash = "hash:senha",
            Perfil = PerfilUsuario.Atleta,
            Ativo = true,
            RefreshTokenHash = "hash:refresh-anterior",
            RefreshTokenExpiraEmUtc = DateTime.UtcNow.AddHours(1)
        };
        var cenario = new Cenario();
        cenario.UsuarioContexto = usuario.Id;
        cenario.Usuarios.Itens.Add(usuario);
        cenario.TokenJwt.ResultadoUsuarioId = usuario.Id;

        var resposta = await cenario.Servico.RenovarTokenAsync(new RenovarTokenRequisicaoDto("expired-token", "refresh-anterior"));

        Assert.Equal(usuario.RefreshTokenExpiraEmUtc, resposta.RefreshTokenExpiraEmUtc);
        Assert.Equal(usuario.Id, resposta.Usuario.Id);
        Assert.Equal("token", resposta.Token);
        Assert.NotEqual("hash:refresh-anterior", usuario.RefreshTokenHash);
    }

    [Fact]
    public async Task RenovarTokenAsync_UsuarioInexistente_Bloqueia()
    {
        var cenario = new Cenario();
        cenario.TokenJwt.ResultadoUsuarioId = Guid.NewGuid();

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.RenovarTokenAsync(new RenovarTokenRequisicaoDto("expired-token", "refresh-anterior")));

        Assert.Equal("Usuário não encontrado ou inativo.", excecao.Message);
    }

    [Fact]
    public async Task RenovarTokenAsync_UsuarioInativo_Bloqueia()
    {
        var cenario = new Cenario();
        var usuario = new Usuario
        {
            Nome = "João",
            Email = "joao@example.com",
            SenhaHash = "hash:senha",
            Perfil = PerfilUsuario.Atleta,
            Ativo = false,
            RefreshTokenHash = "hash:refresh-anterior",
            RefreshTokenExpiraEmUtc = DateTime.UtcNow.AddHours(1)
        };
        cenario.Usuarios.Itens.Add(usuario);
        cenario.TokenJwt.ResultadoUsuarioId = usuario.Id;

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.RenovarTokenAsync(new RenovarTokenRequisicaoDto("expired-token", "refresh-anterior")));

        Assert.Equal("Usuário não encontrado ou inativo.", excecao.Message);
    }

    [Fact]
    public async Task RenovarTokenAsync_RefreshInvalido_Bloqueia()
    {
        var cenario = new Cenario();
        cenario.Usuarios.Itens.Add(new Usuario
        {
            Nome = "João",
            Email = "joao@example.com",
            SenhaHash = "hash:senha",
            Perfil = PerfilUsuario.Atleta,
            Ativo = true,
            RefreshTokenHash = "hash:refresh-outro",
            RefreshTokenExpiraEmUtc = DateTime.UtcNow.AddHours(1)
        });
        cenario.TokenJwt.ResultadoUsuarioId = cenario.Usuarios.Itens.First().Id;

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.RenovarTokenAsync(new RenovarTokenRequisicaoDto("expired-token", "refresh-invalido")));

        Assert.Equal("Sessão expirada. Faça login novamente.", excecao.Message);
    }

    [Fact]
    public async Task RenovarTokenAsync_RefreshExpirado_Bloqueia()
    {
        var cenario = new Cenario();
        var usuario = new Usuario
        {
            Nome = "João",
            Email = "joao@example.com",
            SenhaHash = "hash:senha",
            Perfil = PerfilUsuario.Atleta,
            Ativo = true,
            RefreshTokenHash = "hash:refresh-anterior",
            RefreshTokenExpiraEmUtc = DateTime.UtcNow.AddMinutes(-1)
        };
        cenario.Usuarios.Itens.Add(usuario);
        cenario.TokenJwt.ResultadoUsuarioId = usuario.Id;

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.RenovarTokenAsync(new RenovarTokenRequisicaoDto("expired-token", "refresh-anterior")));

        Assert.Equal("Sessão expirada. Faça login novamente.", excecao.Message);
    }

    private static ConviteCadastro CriarConvite(string email, string codigo, string identificador)
    {
        return new ConviteCadastro
        {
            Email = email.Trim().ToLowerInvariant(),
            IdentificadorPublico = identificador,
            ExpiraEmUtc = DateTime.UtcNow.AddMinutes(15),
            Ativo = true,
            PerfilDestino = PerfilUsuario.Atleta,
            CodigoConvite = codigo,
            CodigoConviteHash = HashConvite(codigo)
        };
    }

    private static string HashConvite(string codigo)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(NormalizarConvite(codigo)));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string NormalizarConvite(string codigoConvite)
    {
        return new string((codigoConvite ?? string.Empty)
            .Where(char.IsLetterOrDigit)
            .ToArray())
            .Trim()
            .ToLowerInvariant();
    }

    private static Usuario CriarUsuarioSemSenha()
        => new()
        {
            Nome = "João",
            Email = "joao@example.com",
            SenhaHash = "hash-interno",
            Perfil = PerfilUsuario.Atleta,
            Ativo = true
        };

    private static Usuario CriarUsuarioComSenha(string senha)
        => new()
        {
            Nome = "João",
            Email = "joao@example.com",
            SenhaHash = HashSenha(senha),
            SenhaDefinidaEmUtc = DateTime.UtcNow.AddDays(-2),
            SenhaAtualizadaEmUtc = DateTime.UtcNow.AddDays(-2),
            Perfil = PerfilUsuario.Atleta,
            Ativo = true
        };

    private static string HashSenha(string senha) => $"hash:{senha}";

    private static CodigoAcessoEmail CriarCodigoAcesso(
        Cenario cenario,
        string email,
        string codigo,
        FinalidadeCodigoAcessoEmail finalidade = FinalidadeCodigoAcessoEmail.CriarSenhaPrimeiroAcesso,
        DateTime? expiraEmUtc = null,
        DateTime? consumidoEmUtc = null)
    {
        var codigoAcesso = new CodigoAcessoEmail
        {
            EmailNormalizado = email.Trim().ToLowerInvariant(),
            CodigoHash = cenario.SenhaServico.GerarHash(codigo),
            Finalidade = finalidade,
            ExpiraEmUtc = expiraEmUtc ?? DateTime.UtcNow.AddMinutes(10),
            ConsumidoEmUtc = consumidoEmUtc,
            UltimoEnvioEmUtc = DateTime.UtcNow
        };
        cenario.CodigosAcesso.Itens.Add(codigoAcesso);
        return codigoAcesso;
    }

    private static async Task<string> ObterCadastroTokenAsync(Cenario cenario, string email)
    {
        await cenario.Servico.IniciarAcessoAsync(new IniciarAcessoRequisicaoDto(email));
        var codigo = cenario.EnvioEmailCodigo.UltimoCodigo
            ?? throw new InvalidOperationException("Código de teste não foi enviado.");
        var resposta = await cenario.Servico.ConfirmarCodigoAcessoAsync(
            new ConfirmarCodigoAcessoRequisicaoDto(email, codigo));
        return resposta.CadastroToken
            ?? throw new InvalidOperationException("Token de cadastro de teste não foi gerado.");
    }

    private static CompletarCadastroPublicoRequisicaoDto CadastroPublicoValido(
        string cadastroToken,
        string nome = "Gustavo",
        string? apelido = null,
        bool aceitouTermos = true,
        bool aceitouPoliticaPrivacidade = true,
        bool declarouMaiorDe18 = true,
        bool aceitouMarketing = false,
        string? senha = "Senha123",
        string? confirmacaoSenha = "Senha123")
        => new(
            cadastroToken,
            nome,
            apelido,
            aceitouTermos,
            PrivacidadeServico.VersaoTermosUsoAtual,
            aceitouPoliticaPrivacidade,
            PrivacidadeServico.VersaoPoliticaPrivacidadeAtual,
            declarouMaiorDe18,
            aceitouMarketing,
            Senha: senha,
            ConfirmacaoSenha: confirmacaoSenha);

    private sealed class Cenario
    {
        public Cenario(Guid? usuarioContexto = null)
        {
            ConviteUsuario = Guid.NewGuid();
            UsuarioContexto = usuarioContexto;
            Servico = new AutenticacaoServico(
                Usuarios,
                Convites,
                CodigosAcesso,
                UnidadeTrabalho,
                SenhaServico,
                TokenJwt,
                new UsuarioContextoStub(usuarioContexto),
                ResolvedorAtleta,
                PendenciaServico,
                EnvioEmailCodigo,
                Privacidade);
        }

        public AutenticacaoServico Servico { get; }
        public UsuarioRepositorioMemoria Usuarios { get; } = new();
        public ConviteRepositorioMemoria Convites { get; } = new();
        public CodigoAcessoEmailRepositorioMemoria CodigosAcesso { get; } = new();
        public UnidadeTrabalhoStub UnidadeTrabalho { get; } = new();
        public SenhaServicoStub SenhaServico { get; } = new();
        public TokenJwtServicoStub TokenJwt { get; } = new();
        public ResolvedorAtletaDuplaServicoStub ResolvedorAtleta { get; } = new();
        public PendenciaServicoStub PendenciaServico { get; } = new();
        public EnvioEmailCodigoLoginStub EnvioEmailCodigo { get; } = new();
        public PrivacidadeServicoStub Privacidade { get; } = new();
        public Guid ConviteUsuario { get; }
        public Guid? UsuarioContexto { get; set; }

        public Atleta? AtletaRetornado { get; set; }

        private sealed class UsuarioContextoStub : IUsuarioContexto
        {
            public UsuarioContextoStub(Guid? usuarioId) => UsuarioId = usuarioId;

            public Guid? UsuarioId { get; }
        }
    }

    private sealed class UsuarioRepositorioMemoria : IUsuarioRepositorio
    {
        public readonly List<Usuario> Itens = new();

        public Task<IReadOnlyList<Usuario>> ListarAsync(string? nome, string? email, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Usuario>>(Itens);

        public Task<int> ContarAdministradoresAtivosAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Itens.Count(x => x.Perfil == PerfilUsuario.Administrador && x.Ativo));

        public Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default)
            => Task.FromResult(Itens.FirstOrDefault(x => x.Email == email));

        public Task<Usuario?> ObterPorEmailParaAtualizacaoAsync(string email, CancellationToken cancellationToken = default)
            => Task.FromResult(Itens.FirstOrDefault(x => x.Email == email));

        public Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Itens.FirstOrDefault(x => x.Id == id));

        public Task<Usuario?> ObterPorIdParaAtualizacaoAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Itens.FirstOrDefault(x => x.Id == id));

        public Task<Usuario?> ObterPorAtletaIdAsync(Guid atletaId, CancellationToken cancellationToken = default)
            => Task.FromResult(Itens.FirstOrDefault(x => x.AtletaId == atletaId));

        public Task<Usuario?> ObterPorAtletaIdParaAtualizacaoAsync(Guid atletaId, CancellationToken cancellationToken = default)
            => Task.FromResult(Itens.FirstOrDefault(x => x.AtletaId == atletaId));

        public Task AdicionarAsync(Usuario usuario, CancellationToken cancellationToken = default)
        {
            Itens.Add(usuario);
            return Task.CompletedTask;
        }

        public void Atualizar(Usuario usuario)
        {
            if (!Itens.Contains(usuario))
            {
                Itens.Add(usuario);
            }
        }
    }

    private sealed class ConviteRepositorioMemoria : IConviteCadastroRepositorio
    {
        public readonly List<ConviteCadastro> Itens = new();

        public Task<IReadOnlyList<ConviteCadastro>> ListarAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ConviteCadastro>>(Itens);

        public Task<IReadOnlyList<ConviteCadastro>> ListarAtivosPorUsuarioOuEmailAsync(
            Guid usuarioId,
            string email,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ConviteCadastro>>(Itens.Where(x => x.CriadoPorUsuarioId == usuarioId || x.Email == email).ToList());

        public Task<ConviteCadastro?> ObterAtivoPendentePorEmailAsync(string email, DateTime dataUtc, CancellationToken cancellationToken = default)
            => Task.FromResult<ConviteCadastro?>(null);

        public Task<ConviteCadastro?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Itens.FirstOrDefault(x => x.Id == id));

        public Task<ConviteCadastro?> ObterPorIdParaAtualizacaoAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Itens.FirstOrDefault(x => x.Id == id));

        public Task<ConviteCadastro?> ObterPorIdentificadorPublicoAsync(string identificadorPublico, CancellationToken cancellationToken = default)
            => Task.FromResult(Itens.FirstOrDefault(x => x.IdentificadorPublico == identificadorPublico));

        public Task<ConviteCadastro?> ObterPorIdentificadorPublicoParaAtualizacaoAsync(string identificadorPublico, CancellationToken cancellationToken = default)
            => Task.FromResult(Itens.FirstOrDefault(x => x.IdentificadorPublico == identificadorPublico));

        public Task AdicionarAsync(ConviteCadastro conviteCadastro, CancellationToken cancellationToken = default)
        {
            Itens.Add(conviteCadastro);
            return Task.CompletedTask;
        }

        public void Atualizar(ConviteCadastro conviteCadastro)
        {
            if (!Itens.Contains(conviteCadastro))
            {
                Itens.Add(conviteCadastro);
            }
        }
    }

    private sealed class CodigoAcessoEmailRepositorioMemoria : ICodigoAcessoEmailRepositorio
    {
        public readonly List<CodigoAcessoEmail> Itens = new();

        public Task<IReadOnlyList<CodigoAcessoEmail>> ListarPendentesPorEmailFinalidadeParaAtualizacaoAsync(
            string emailNormalizado,
            FinalidadeCodigoAcessoEmail finalidade,
            CancellationToken cancellationToken = default)
        {
            var codigos = Itens
                .Where(x =>
                    x.EmailNormalizado == emailNormalizado &&
                    x.Finalidade == finalidade &&
                    x.ConsumidoEmUtc == null)
                .OrderByDescending(x => x.DataCriacao)
                .ToList();
            return Task.FromResult<IReadOnlyList<CodigoAcessoEmail>>(codigos);
        }

        public Task<CodigoAcessoEmail?> ObterAtivoPorEmailFinalidadeParaAtualizacaoAsync(
            string emailNormalizado,
            FinalidadeCodigoAcessoEmail finalidade,
            DateTime dataUtc,
            CancellationToken cancellationToken = default)
        {
            var codigo = Itens
                .Where(x =>
                    x.EmailNormalizado == emailNormalizado &&
                    x.Finalidade == finalidade &&
                    x.ConsumidoEmUtc == null &&
                    x.ExpiraEmUtc >= dataUtc)
                .OrderByDescending(x => x.DataCriacao)
                .FirstOrDefault();
            return Task.FromResult(codigo);
        }

        public Task<CodigoAcessoEmail?> ObterPorCadastroTokenHashParaAtualizacaoAsync(
            string cadastroTokenHash,
            DateTime dataUtc,
            CancellationToken cancellationToken = default)
        {
            var codigo = Itens
                .Where(x =>
                    x.Finalidade == FinalidadeCodigoAcessoEmail.CadastroPublico &&
                    x.CadastroTokenHash == cadastroTokenHash &&
                    x.CadastroTokenExpiraEmUtc != null &&
                    x.CadastroTokenExpiraEmUtc >= dataUtc)
                .OrderByDescending(x => x.DataCriacao)
                .FirstOrDefault();
            return Task.FromResult(codigo);
        }

        public Task<CodigoAcessoEmail?> ObterPorTokenTemporarioHashParaAtualizacaoAsync(
            string tokenHash,
            DateTime dataUtc,
            CancellationToken cancellationToken = default)
        {
            var codigo = Itens
                .Where(x =>
                    x.CadastroTokenHash == tokenHash &&
                    x.CadastroTokenExpiraEmUtc != null &&
                    x.CadastroTokenExpiraEmUtc >= dataUtc)
                .OrderByDescending(x => x.DataCriacao)
                .FirstOrDefault();
            return Task.FromResult(codigo);
        }

        public Task AdicionarAsync(CodigoAcessoEmail codigoAcessoEmail, CancellationToken cancellationToken = default)
        {
            Itens.Add(codigoAcessoEmail);
            return Task.CompletedTask;
        }

        public void Atualizar(CodigoAcessoEmail codigoAcessoEmail)
        {
            if (!Itens.Contains(codigoAcessoEmail))
            {
                Itens.Add(codigoAcessoEmail);
            }
        }
    }

    private sealed class UnidadeTrabalhoStub : IUnidadeTrabalho
    {
        public int Salvamentos { get; private set; }

        public Task<int> SalvarAlteracoesAsync(CancellationToken cancellationToken = default)
        {
            Salvamentos++;
            return Task.FromResult(Salvamentos);
        }

        public Task ExecutarEmTransacaoAsync(Func<CancellationToken, Task> operacao, CancellationToken cancellationToken = default)
            => operacao(cancellationToken);
    }

    private sealed class SenhaServicoStub : ISenhaServico
    {
        public string GerarHash(string senha)
            => "hash:" + senha;

        public bool Verificar(string senha, string hash)
            => hash == GerarHash(senha);
    }

    private sealed class TokenJwtServicoStub : ITokenJwtServico
    {
        public Guid? ResultadoUsuarioId { get; set; }

        public string GerarToken(Usuario usuario, DateTime expiraEmUtc)
            => "token";

        public Guid? ObterUsuarioIdTokenExpirado(string token)
            => ResultadoUsuarioId;

        public DateTime ObterExpiracaoTokenAcessoUtc(DateTime? limiteMaximoUtc = null)
            => limiteMaximoUtc?.AddMinutes(-1) ?? DateTime.UtcNow.AddMinutes(15);

        public DateTime ObterExpiracaoRefreshTokenUtc()
            => DateTime.UtcNow.AddDays(1);
    }

    private sealed class ResolvedorAtletaDuplaServicoStub : IResolvedorAtletaDuplaServico
    {
        public Atleta? AtletaPadrao { get; set; }
        public string? UltimoNomeInformado { get; private set; }
        public string? UltimoEmailInformado { get; private set; }
        public string? UltimoApelidoInformado { get; private set; }

        public Task<Atleta> ObterAtletaExistenteAsync(
            Guid atletaId,
            string mensagemQuandoInvalido,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<Atleta> ResolverAtletaAsync(
            Guid? atletaId,
            string? nomeInformado,
            string? apelidoInformado,
            string mensagemQuandoInvalido,
            bool cadastroPendente,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<Atleta> ObterOuCriarAtletaAsync(
            string? nomeInformado,
            string? apelidoInformado,
            bool cadastroPendente,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<Atleta> ObterOuCriarAtletaParaUsuarioAsync(
            string nomeInformado,
            string emailInformado,
            string? apelidoInformado = null,
            CancellationToken cancellationToken = default)
        {
            UltimoNomeInformado = nomeInformado;
            UltimoEmailInformado = emailInformado;
            UltimoApelidoInformado = apelidoInformado;
            return Task.FromResult(AtletaPadrao ?? new Atleta
            {
                Nome = nomeInformado,
                Apelido = apelidoInformado,
                Email = emailInformado,
            });
        }

        public Task<Dupla> ObterOuCriarDuplaAsync(Atleta atleta1, Atleta atleta2, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<GrupoAtleta> GarantirAtletaNoGrupoAsync(
            Guid grupoId,
            Atleta atleta,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class PendenciaServicoStub : IPendenciaServico
    {
        public Guid? UltimoAtletaSincronizado { get; private set; }

        public Task<IReadOnlyList<PendenciaUsuarioDto>> ListarMinhasAsync(
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PendenciaUsuarioDto>>([]);
        public Task<PendenciasResumoDto> ObterResumoAsync(
            CancellationToken cancellationToken = default)
            => Task.FromResult(new PendenciasResumoDto(0, 0, 0, 0));
        public Task<bool> ExistePendenciaAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(false);
        public Task<PendenciaUsuarioDto> AprovarPartidaAsync(
            Guid pendenciaId,
            ResponderPendenciaPartidaDto dto,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
        public Task<PendenciaUsuarioDto> ContestarPartidaAsync(
            Guid pendenciaId,
            ResponderPendenciaPartidaDto dto,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
        public Task<AtualizarContatoPendenciaResultadoDto> CompletarContatoAsync(
            Guid pendenciaId,
            AtualizarContatoPendenciaDto dto,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
        public Task<PendenciaUsuarioDto> ConfirmarVinculoAtletaCadastradoAsync(
            Guid pendenciaId,
            ConfirmarVinculoAtletaPendenciaDto dto,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
        public Task InicializarFluxoPartidaAsync(
            Partida partida,
            Guid usuarioRegistradorId,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
        public Task SincronizarAposVinculoAtletaAsync(
            Guid atletaId,
            CancellationToken cancellationToken = default)
        {
            UltimoAtletaSincronizado = atletaId;
            return Task.CompletedTask;
        }
    }

    private sealed class EnvioEmailCodigoLoginStub : IEnvioEmailCodigoLoginServico
    {
        public string? UltimoEmail { get; private set; }
        public string? UltimoCodigo { get; private set; }
        public ResultadoEnvioEmailCodigoLoginDto? Resultado { get; set; } = new(true, true, null, "envio-id", "123456");

        public Task<ResultadoEnvioEmailCodigoLoginDto> EnviarAsync(Usuario usuario, string codigo, CancellationToken cancellationToken = default)
        {
            UltimoEmail = usuario.Email;
            UltimoCodigo = codigo;
            return Task.FromResult(Resultado!);
        }
    }

    private sealed class PrivacidadeServicoStub : IPrivacidadeServico
    {
        public List<RegistrarConsentimentoLgpdDto> Consentimentos { get; } = new();
        public Usuario? UltimoUsuarioConsentimento { get; private set; }

        public Task<PoliticaPrivacidadeAtualDto> ObterPoliticaAtualAsync(CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<TermosVersaoAtualDto> ObterTermosVersaoAtualAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new TermosVersaoAtualDto(
                PrivacidadeServico.VersaoTermosUsoAtual,
                "/privacidade",
                PrivacidadeServico.VersaoPoliticaPrivacidadeAtual,
                "/privacidade"));

        public Task<PreferenciasPrivacidadeDto> ObterMinhasPreferenciasAsync(CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<PreferenciasPrivacidadeDto> AtualizarMinhasPreferenciasAsync(
            AtualizarPreferenciasPrivacidadeDto dto,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<PreferenciasPrivacidadeDto> RegistrarConsentimentoAsync(
            RegistrarConsentimentoLgpdDto dto,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task SolicitarExclusaoContaAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task RegistrarConsentimentoUsuarioAsync(Usuario usuario, RegistrarConsentimentoLgpdDto dto, CancellationToken cancellationToken = default)
        {
            UltimoUsuarioConsentimento = usuario;
            Consentimentos.Add(dto);
            return Task.CompletedTask;
        }

        public Task<bool> UsuarioPrecisaAceitarPoliticaAsync(Guid usuarioId, CancellationToken cancellationToken = default)
            => Task.FromResult(false);
    }
}
