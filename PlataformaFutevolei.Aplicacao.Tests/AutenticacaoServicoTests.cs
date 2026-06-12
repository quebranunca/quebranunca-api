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
    public async Task LoginAsync_CredenciaisValidas_RetornaTokens()
    {
        var cenario = new Cenario();
        cenario.Usuarios.Itens.Add(new Usuario
        {
            Nome = "João",
            Email = "joao@example.com",
            SenhaHash = "hash:123456",
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
            Perfil = PerfilUsuario.Atleta,
            Ativo = true
        });

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() => cenario.Servico.LoginAsync(new LoginRequisicaoDto("joao@example.com", "outra")));

        Assert.Equal("Credenciais inválidas.", excecao.Message);
    }

    [Fact]
    public async Task SolicitarCodigoLoginAsync_EmailValido_GeraHashEExpiraESalvaUsuario()
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
        Assert.NotNull(usuario.CodigoLoginHash);
        Assert.NotNull(usuario.CodigoLoginExpiraEmUtc);
        Assert.Equal("123456", cenario.EnvioEmailCodigo.Resultado?.CodigoDesenvolvimento);
        Assert.NotNull(cenario.EnvioEmailCodigo.UltimoCodigo);
        Assert.Equal(HashSenha(cenario.EnvioEmailCodigo.UltimoCodigo), usuario.CodigoLoginHash);
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
    public async Task LoginComCodigoAsync_CodigoValido_RetornaAuthELimpaCodigo()
    {
        var cenario = new Cenario();
        var hashCodigo = cenario.SenhaServico.GerarHash("123456");
        var usuario = new Usuario
        {
            Nome = "João",
            Email = "joao@example.com",
            SenhaHash = "hash:senha",
            Perfil = PerfilUsuario.Atleta,
            Ativo = true,
            CodigoLoginHash = hashCodigo,
            CodigoLoginExpiraEmUtc = DateTime.UtcNow.AddMinutes(10)
        };
        cenario.Usuarios.Itens.Add(usuario);

        var resposta = await cenario.Servico.LoginComCodigoAsync(new LoginCodigoRequisicaoDto("joao@example.com", "123456"));

        Assert.Equal("João", resposta.Usuario.Nome);
        Assert.Null(usuario.CodigoLoginHash);
        Assert.Null(usuario.CodigoLoginExpiraEmUtc);
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
            Ativo = true,
            CodigoLoginHash = cenario.SenhaServico.GerarHash("123456"),
            CodigoLoginExpiraEmUtc = DateTime.UtcNow.AddMinutes(10)
        });

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.LoginComCodigoAsync(new LoginCodigoRequisicaoDto("joao@example.com", "654321")));

        Assert.Equal("Código de acesso inválido ou expirado.", excecao.Message);
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
            Ativo = true,
            CodigoLoginHash = cenario.SenhaServico.GerarHash("123456"),
            CodigoLoginExpiraEmUtc = DateTime.UtcNow.AddMinutes(-1)
        });

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() => cenario.Servico.LoginComCodigoAsync(new LoginCodigoRequisicaoDto("joao@example.com", "123456")));

        Assert.Equal("Código de acesso inválido ou expirado.", excecao.Message);
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
        Assert.NotNull(usuario.CodigoRedefinicaoSenhaHash);
        Assert.NotNull(usuario.CodigoRedefinicaoSenhaExpiraEmUtc);
    }

    [Fact]
    public async Task SolicitarRedefinicaoSenhaAsync_EmailInexistente_NaoRevelaExistencia()
    {
        var cenario = new Cenario();
        var resposta = await cenario.Servico.SolicitarRedefinicaoSenhaAsync(new EsqueciSenhaRequisicaoDto("desconhecido@example.com"));

        Assert.Equal("Se o e-mail estiver cadastrado, um código de redefinição foi gerado.", resposta.Mensagem);
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

        await cenario.Servico.RedefinirSenhaAsync(new RedefinirSenhaRequisicaoDto(" joao@example.com ", "123456", "nova123"));

        Assert.Equal("hash:nova123", usuario.SenhaHash);
        Assert.Null(usuario.CodigoRedefinicaoSenhaHash);
        Assert.Null(usuario.CodigoRedefinicaoSenhaExpiraEmUtc);
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
            Ativo = true,
            CodigoRedefinicaoSenhaHash = cenario.SenhaServico.GerarHash("123456"),
            CodigoRedefinicaoSenhaExpiraEmUtc = DateTime.UtcNow.AddMinutes(10)
        });

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.RedefinirSenhaAsync(new RedefinirSenhaRequisicaoDto("joao@example.com", "654321", "nova123")));

        Assert.Equal("Código de redefinição inválido ou expirado.", excecao.Message);
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
            Ativo = true,
            CodigoRedefinicaoSenhaHash = cenario.SenhaServico.GerarHash("123456"),
            CodigoRedefinicaoSenhaExpiraEmUtc = DateTime.UtcNow.AddMinutes(-1)
        });

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.RedefinirSenhaAsync(new RedefinirSenhaRequisicaoDto("joao@example.com", "123456", "nova123")));

        Assert.Equal("Código de redefinição inválido ou expirado.", excecao.Message);
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

    private static string HashSenha(string senha) => $"hash:{senha}";

    private sealed class Cenario
    {
        public Cenario()
        {
            ConviteUsuario = Guid.NewGuid();
            Servico = new AutenticacaoServico(
                Usuarios,
                Convites,
                UnidadeTrabalho,
                SenhaServico,
                TokenJwt,
                new UsuarioContextoStub(null),
                ResolvedorAtleta,
                PendenciaServico,
                EnvioEmailCodigo,
                new PrivacidadeServicoStub());
        }

        public AutenticacaoServico Servico { get; }
        public UsuarioRepositorioMemoria Usuarios { get; } = new();
        public ConviteRepositorioMemoria Convites { get; } = new();
        public UnidadeTrabalhoStub UnidadeTrabalho { get; } = new();
        public SenhaServicoStub SenhaServico { get; } = new();
        public TokenJwtServicoStub TokenJwt { get; } = new();
        public ResolvedorAtletaDuplaServicoStub ResolvedorAtleta { get; } = new();
        public PendenciaServicoStub PendenciaServico { get; } = new();
        public EnvioEmailCodigoLoginStub EnvioEmailCodigo { get; } = new();
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

        public Task<Atleta> ObterOuCriarAtletaParaUsuarioAsync(string nomeInformado, string emailInformado, CancellationToken cancellationToken = default)
            => Task.FromResult(AtletaPadrao ?? new Atleta
            {
                Nome = nomeInformado,
                Email = emailInformado,
            });

        public Task<Dupla> ObterOuCriarDuplaAsync(Atleta atleta1, Atleta atleta2, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<GrupoAtleta> GarantirAtletaNoGrupoAsync(
            Guid grupoId,
            Atleta atleta,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class PendenciaServicoStub : IPendenciaServico
    {
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
            => Task.CompletedTask;
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
        public Task<PoliticaPrivacidadeAtualDto> ObterPoliticaAtualAsync(CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

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
            => Task.CompletedTask;

        public Task<bool> UsuarioPrecisaAceitarPoliticaAsync(Guid usuarioId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }
}
