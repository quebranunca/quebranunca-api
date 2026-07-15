using Microsoft.Extensions.Logging.Abstractions;
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

public class ConviteCadastroServicoTests
{
    [Fact]
    public async Task CriarAsync_EmailValido_CriaConviteNormalizadoComCodigo()
    {
        var cenario = new Cenario();

        var convite = await cenario.Servico.CriarAsync(new CriarConviteCadastroDto(
            " ATLETA@EXAMPLE.COM ",
            "(13) 99999-0000",
            PerfilUsuario.Administrador,
            DateTime.UtcNow.AddDays(2),
            null));

        var criado = Assert.Single(cenario.Convites.Itens);
        Assert.Equal("atleta@example.com", convite.Email);
        Assert.Equal("atleta@example.com", criado.Email);
        Assert.Equal("13 999990000", criado.Telefone);
        Assert.Equal(PerfilUsuario.Atleta, criado.PerfilDestino);
        Assert.Equal(cenario.UsuarioAdmin.Id, criado.CriadoPorUsuarioId);
        Assert.False(string.IsNullOrWhiteSpace(criado.IdentificadorPublico));
        Assert.False(string.IsNullOrWhiteSpace(criado.CodigoConvite));
        Assert.False(string.IsNullOrWhiteSpace(criado.CodigoConviteHash));
    }

    [Fact]
    public async Task CriarAsync_EmailNormalizado_ConsultaUsuarioAntesDeCriar()
    {
        var cenario = new Cenario();

        await cenario.Servico.CriarAsync(new CriarConviteCadastroDto(
            " NOVO@EXAMPLE.COM ",
            null,
            null,
            DateTime.UtcNow.AddDays(1),
            "manual"));

        Assert.Equal("novo@example.com", cenario.Usuarios.UltimoEmailConsultado);
    }

    [Fact]
    public async Task CriarAsync_EmailJaCadastrado_Bloqueia()
    {
        var cenario = new Cenario();
        cenario.Usuarios.Itens.Add(new Usuario
        {
            Nome = "Atleta",
            Email = "atleta@example.com",
            Perfil = PerfilUsuario.Atleta,
            Ativo = true
        });

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.CriarAsync(new CriarConviteCadastroDto(
                " ATLETA@EXAMPLE.COM ",
                null,
                null,
                DateTime.UtcNow.AddDays(1),
                null)));

        Assert.Equal("Já existe um usuário cadastrado com este e-mail.", excecao.Message);
        Assert.Empty(cenario.Convites.Itens);
    }

    [Fact]
    public async Task CriarAsync_CanalWhatsappSemTelefone_Bloqueia()
    {
        var cenario = new Cenario();

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.CriarAsync(new CriarConviteCadastroDto(
                "atleta@example.com",
                null,
                null,
                DateTime.UtcNow.AddDays(1),
                "WhatsApp")));

        Assert.Equal("Telefone é obrigatório quando o canal de envio inclui WhatsApp.", excecao.Message);
    }

    [Fact]
    public async Task CriarParaPendenciaAtletaAsync_EmailComConviteAtivo_NaoDuplicaConvite()
    {
        var cenario = new Cenario();
        var conviteExistente = CriarConvite("atleta@example.com", DateTime.UtcNow.AddDays(2));
        cenario.Convites.Itens.Add(conviteExistente);

        var resultado = await cenario.Servico.CriarParaPendenciaAtletaAsync(new CriarConvitePendenciaAtletaDto(
            " ATLETA@EXAMPLE.COM ",
            null,
            cenario.UsuarioAdmin.Id,
            Guid.NewGuid(),
            Guid.NewGuid()));

        Assert.False(resultado.ConviteCriado);
        Assert.True(resultado.IgnoradoPorConviteAtivo);
        Assert.False(resultado.IgnoradoPorUsuarioExistente);
        Assert.Equal(conviteExistente.Id, resultado.ConviteId);
        Assert.Single(cenario.Convites.Itens);
    }

    [Fact]
    public async Task CriarParaPendenciaAtletaAsync_EmailComConviteExpirado_CriaNovoConvite()
    {
        var cenario = new Cenario();
        cenario.Convites.Itens.Add(CriarConvite("atleta@example.com", DateTime.UtcNow.AddDays(-1)));
        var atletaId = Guid.NewGuid();
        var partidaId = Guid.NewGuid();

        var resultado = await cenario.Servico.CriarParaPendenciaAtletaAsync(new CriarConvitePendenciaAtletaDto(
            " ATLETA@EXAMPLE.COM ",
            "13 98888-7777",
            cenario.UsuarioAdmin.Id,
            atletaId,
            partidaId));

        Assert.True(resultado.ConviteCriado);
        Assert.False(resultado.IgnoradoPorConviteAtivo);
        Assert.Equal(2, cenario.Convites.Itens.Count);
        var criado = cenario.Convites.Itens.Single(x => x.Id == resultado.ConviteId);
        Assert.Equal("atleta@example.com", criado.Email);
        Assert.Equal(atletaId, criado.AtletaId);
        Assert.Equal(partidaId, criado.PartidaId);
        Assert.Equal("e-mail", criado.CanalEnvio);
    }

    [Fact]
    public async Task CriarParaPendenciaAtletaAsync_EmailComUsuarioExistente_NaoCriaConvite()
    {
        var cenario = new Cenario();
        cenario.Usuarios.Itens.Add(new Usuario
        {
            Nome = "Atleta",
            Email = "atleta@example.com",
            Perfil = PerfilUsuario.Atleta,
            Ativo = true
        });

        var resultado = await cenario.Servico.CriarParaPendenciaAtletaAsync(new CriarConvitePendenciaAtletaDto(
            " ATLETA@EXAMPLE.COM ",
            null,
            cenario.UsuarioAdmin.Id,
            Guid.NewGuid(),
            Guid.NewGuid()));

        Assert.False(resultado.ConviteCriado);
        Assert.False(resultado.IgnoradoPorConviteAtivo);
        Assert.True(resultado.IgnoradoPorUsuarioExistente);
        Assert.Null(resultado.ConviteId);
        Assert.Empty(cenario.Convites.Itens);
    }

    [Fact]
    public async Task ObterLinkAceiteAsync_ConviteValido_RetornaLinkECodigoExistente()
    {
        var cenario = new Cenario();
        var convite = CriarConvite("atleta@example.com", DateTime.UtcNow.AddDays(1));
        convite.DefinirCodigoConvite("123-456", "hash");
        cenario.Convites.Itens.Add(convite);

        var link = await cenario.Servico.ObterLinkAceiteAsync(convite.Id);

        Assert.Equal($"https://app.test/convites/{convite.IdentificadorPublico}", link.LinkAceite);
        Assert.Equal("123-456", link.CodigoConvite);
    }

    [Theory]
    [InlineData("expirado")]
    [InlineData("inativo")]
    [InlineData("utilizado")]
    public async Task ObterLinkAceiteAsync_ConviteInvalido_Bloqueia(string estado)
    {
        var cenario = new Cenario();
        var convite = estado switch
        {
            "expirado" => CriarConvite("atleta@example.com", DateTime.UtcNow.AddDays(-1)),
            "inativo" => CriarConvite("atleta@example.com", DateTime.UtcNow.AddDays(1), ativo: false),
            _ => CriarConvite("atleta@example.com", DateTime.UtcNow.AddDays(1), usadoEmUtc: DateTime.UtcNow)
        };
        cenario.Convites.Itens.Add(convite);

        await Assert.ThrowsAsync<RegraNegocioException>(() => cenario.Servico.ObterLinkAceiteAsync(convite.Id));
    }

    [Fact]
    public async Task EnviarEmailAsync_EnvioComSucesso_RegistraSucesso()
    {
        var cenario = new Cenario();
        var convite = CriarConvite("atleta@example.com", DateTime.UtcNow.AddDays(1));
        cenario.Convites.Itens.Add(convite);
        cenario.Email.Resultado = new ResultadoEnvioEmailConviteDto(true, true, null, "msg-1");

        var enviado = await cenario.Servico.EnviarEmailAsync(convite.Id);

        Assert.Equal("Enviado", enviado.SituacaoEnvioEmail);
        Assert.NotNull(convite.EmailEnviadoEmUtc);
        Assert.Null(convite.ErroEnvioEmail);
        Assert.Single(cenario.Email.Envios);
        Assert.False(string.IsNullOrWhiteSpace(cenario.Email.Envios[0].CodigoConvite));
    }

    [Fact]
    public async Task EnviarEmailAsync_EnvioComFalha_RegistraFalhaEPropagaErro()
    {
        var cenario = new Cenario();
        var convite = CriarConvite("atleta@example.com", DateTime.UtcNow.AddDays(1));
        cenario.Convites.Itens.Add(convite);
        cenario.Email.Resultado = new ResultadoEnvioEmailConviteDto(true, false, "Falha SMTP", null);

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.EnviarEmailAsync(convite.Id));

        Assert.Equal("Falha SMTP", excecao.Message);
        Assert.Equal("Falha SMTP", convite.ErroEnvioEmail);
        Assert.NotNull(convite.UltimaTentativaEnvioEmailEmUtc);
        Assert.Null(convite.EmailEnviadoEmUtc);
    }

    [Fact]
    public async Task EnviarWhatsappAsync_EnvioComSucesso_RegistraSucesso()
    {
        var cenario = new Cenario();
        var convite = CriarConvite("atleta@example.com", DateTime.UtcNow.AddDays(1), telefone: "13 99999 0000");
        cenario.Convites.Itens.Add(convite);
        cenario.Whatsapp.Resultado = new ResultadoEnvioWhatsappConviteDto(true, true, null, "wa-1");

        var enviado = await cenario.Servico.EnviarWhatsappAsync(convite.Id);

        Assert.Equal("Enviado", enviado.SituacaoEnvioWhatsapp);
        Assert.NotNull(convite.WhatsappEnviadoEmUtc);
        Assert.Null(convite.ErroEnvioWhatsapp);
        Assert.Single(cenario.Whatsapp.Envios);
    }

    [Fact]
    public async Task EnviarWhatsappAsync_EnvioComFalha_RegistraFalhaEPropagaErro()
    {
        var cenario = new Cenario();
        var convite = CriarConvite("atleta@example.com", DateTime.UtcNow.AddDays(1), telefone: "13 99999 0000");
        cenario.Convites.Itens.Add(convite);
        cenario.Whatsapp.Resultado = new ResultadoEnvioWhatsappConviteDto(true, false, "Falha WhatsApp", null);

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.EnviarWhatsappAsync(convite.Id));

        Assert.Equal("Falha WhatsApp", excecao.Message);
        Assert.Equal("Falha WhatsApp", convite.ErroEnvioWhatsapp);
        Assert.NotNull(convite.UltimaTentativaEnvioWhatsappEmUtc);
        Assert.Null(convite.WhatsappEnviadoEmUtc);
    }

    [Fact]
    public async Task EnviarEmailAsync_ConviteUtilizado_BloqueiaReenvio()
    {
        var cenario = new Cenario();
        var convite = CriarConvite("atleta@example.com", DateTime.UtcNow.AddDays(1), usadoEmUtc: DateTime.UtcNow);
        cenario.Convites.Itens.Add(convite);

        await Assert.ThrowsAsync<RegraNegocioException>(() => cenario.Servico.EnviarEmailAsync(convite.Id));

        Assert.Empty(cenario.Email.Envios);
    }

    private static ConviteCadastro CriarConvite(
        string email,
        DateTime expiraEmUtc,
        bool ativo = true,
        DateTime? usadoEmUtc = null,
        string? telefone = null)
        => new()
        {
            Email = email,
            Telefone = telefone,
            IdentificadorPublico = Guid.NewGuid().ToString("N"),
            PerfilDestino = PerfilUsuario.Atleta,
            ExpiraEmUtc = expiraEmUtc,
            Ativo = ativo,
            UsadoEmUtc = usadoEmUtc,
            CriadoPorUsuarioId = Guid.NewGuid()
        };

    private sealed class Cenario
    {
        public Cenario()
        {
            UsuarioAdmin = new Usuario
            {
                Nome = "Admin",
                Email = "admin@example.com",
                Perfil = PerfilUsuario.Administrador,
                Ativo = true
            };
            Usuarios = new UsuarioRepositorioStub();
            Convites = new ConviteCadastroRepositorioStub();
            Email = new EnvioEmailConviteCadastroServicoStub();
            Whatsapp = new EnvioWhatsappConviteCadastroServicoStub();
            Servico = new ConviteCadastroServico(
                Convites,
                new AtletaRepositorioStub(),
                Usuarios,
                new UnidadeTrabalhoStub(),
                new AutorizacaoUsuarioServicoStub(UsuarioAdmin),
                new GeracaoLinkConviteCadastroServicoStub(),
                Email,
                Whatsapp,
                NullLogger<ConviteCadastroServico>.Instance);
        }

        public Usuario UsuarioAdmin { get; }
        public UsuarioRepositorioStub Usuarios { get; }
        public ConviteCadastroRepositorioStub Convites { get; }
        public EnvioEmailConviteCadastroServicoStub Email { get; }
        public EnvioWhatsappConviteCadastroServicoStub Whatsapp { get; }
        public ConviteCadastroServico Servico { get; }
    }

    private sealed class ConviteCadastroRepositorioStub : IConviteCadastroRepositorio
    {
        public List<ConviteCadastro> Itens { get; } = [];

        public Task<IReadOnlyList<ConviteCadastro>> ListarAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ConviteCadastro>>(Itens);

        public Task<IReadOnlyList<ConviteCadastro>> ListarAtivosPorUsuarioOuEmailAsync(
            Guid usuarioId,
            string email,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ConviteCadastro>>(Itens
                .Where(x => x.CriadoPorUsuarioId == usuarioId || string.Equals(x.Email, email, StringComparison.OrdinalIgnoreCase))
                .Where(x => x.Ativo)
                .ToList());

        public Task<ConviteCadastro?> ObterAtivoPendentePorEmailAsync(
            string email,
            DateTime dataUtc,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Itens.FirstOrDefault(x =>
                string.Equals(x.Email, email, StringComparison.OrdinalIgnoreCase) &&
                x.PodeSerUsado(dataUtc)));

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
        }
    }

    private sealed class UsuarioRepositorioStub : IUsuarioRepositorio
    {
        public List<Usuario> Itens { get; } = [];
        public string? UltimoEmailConsultado { get; private set; }

        public Task<IReadOnlyList<Usuario>> ListarAsync(string? nome, string? email, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Usuario>>(Itens);

        public Task<int> ContarAdministradoresAtivosAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Itens.Count(x => x.Perfil == PerfilUsuario.Administrador && x.Ativo));

        public Task<IReadOnlyList<Usuario>> ListarAdministradoresAtivosAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Usuario>>(Itens.Where(x => x.Perfil == PerfilUsuario.Administrador && x.Ativo).ToList());

        public Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            UltimoEmailConsultado = email;
            return Task.FromResult(Itens.FirstOrDefault(x => string.Equals(x.Email, email, StringComparison.OrdinalIgnoreCase)));
        }

        public Task<Usuario?> ObterPorEmailParaAtualizacaoAsync(string email, CancellationToken cancellationToken = default)
            => ObterPorEmailAsync(email, cancellationToken);

        public Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Itens.FirstOrDefault(x => x.Id == id));

        public Task<Usuario?> ObterPorIdParaAtualizacaoAsync(Guid id, CancellationToken cancellationToken = default)
            => ObterPorIdAsync(id, cancellationToken);

        public Task<Usuario?> ObterPorAtletaIdAsync(Guid atletaId, CancellationToken cancellationToken = default)
            => Task.FromResult(Itens.FirstOrDefault(x => x.AtletaId == atletaId));

        public Task<Usuario?> ObterPorAtletaIdParaAtualizacaoAsync(Guid atletaId, CancellationToken cancellationToken = default)
            => ObterPorAtletaIdAsync(atletaId, cancellationToken);

        public Task AdicionarAsync(Usuario usuario, CancellationToken cancellationToken = default)
        {
            Itens.Add(usuario);
            return Task.CompletedTask;
        }

        public void Atualizar(Usuario usuario)
        {
        }
    }

    private sealed class AtletaRepositorioStub : IAtletaRepositorio
    {
        public Task<IReadOnlyList<Atleta>> ListarAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Atleta>>([]);
        public Task<int> ContarAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<IReadOnlyList<Atleta>> ListarComEmailEmPartidasSemUsuarioAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Atleta>>([]);
        public Task<IReadOnlyList<Atleta>> ListarInscritosPorOrganizadorAsync(Guid usuarioOrganizadorId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Atleta>>([]);
        public Task<bool> PertenceAoOrganizadorAsync(Guid atletaId, Guid usuarioOrganizadorId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<IReadOnlyList<Atleta>> BuscarAsync(string? termo, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Atleta>>([]);
        public Task<IDictionary<Guid, int>> ContarPartidasPorAtletasAsync(IEnumerable<Guid> atletaIds, CancellationToken cancellationToken = default) => Task.FromResult<IDictionary<Guid, int>>(new Dictionary<Guid, int>());
        public Task<IReadOnlyList<Atleta>> BuscarSugestoesPorCompeticaoAsync(Guid competicaoId, string termo, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Atleta>>([]);
        public Task<Atleta?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Atleta?>(null);
        public Task<Atleta?> ObterPorIdParaAtualizacaoAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Atleta?>(null);
        public Task<Atleta?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default) => Task.FromResult<Atleta?>(null);
        public Task<IReadOnlyList<Atleta>> ListarPorNomeAsync(string nome, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Atleta>>([]);
        public Task<IReadOnlyList<Atleta>> ListarPorEmailAsync(string email, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Atleta>>([]);
        public Task AdicionarAsync(Atleta atleta, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AdicionarMedidasAsync(AtletaMedidas medidas, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Atualizar(Atleta atleta) { }
        public void AtualizarMedidas(AtletaMedidas medidas) { }
        public void Remover(Atleta atleta) { }
    }

    private sealed class UnidadeTrabalhoStub : IUnidadeTrabalho
    {
        public Task<int> SalvarAlteracoesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
        public Task ExecutarEmTransacaoAsync(Func<CancellationToken, Task> operacao, CancellationToken cancellationToken = default)
            => operacao(cancellationToken);
    }

    private sealed class AutorizacaoUsuarioServicoStub(Usuario usuario) : IAutorizacaoUsuarioServico
    {
        public bool EhAdministrador(Usuario? usuario) => usuario?.Perfil == PerfilUsuario.Administrador;
        public Task<Usuario?> ObterUsuarioAtualAsync(CancellationToken cancellationToken = default) => Task.FromResult<Usuario?>(usuario);
        public Task<Usuario> ObterUsuarioAtualObrigatorioAsync(CancellationToken cancellationToken = default) => Task.FromResult(usuario);
        public Task GarantirAdministradorAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task GarantirAdminOuOrganizadorAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task GarantirAcessoAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task GarantirGestaoCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task GarantirGestaoGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class GeracaoLinkConviteCadastroServicoStub : IGeracaoLinkConviteCadastroServico
    {
        public string Gerar(ConviteCadastro conviteCadastro)
            => $"https://app.test/convites/{conviteCadastro.IdentificadorPublico}";
    }

    private sealed class EnvioEmailConviteCadastroServicoStub : IEnvioEmailConviteCadastroServico
    {
        public ResultadoEnvioEmailConviteDto Resultado { get; set; } = new(false, false, null, null);
        public List<(Guid ConviteId, string CodigoConvite)> Envios { get; } = [];

        public Task<ResultadoEnvioEmailConviteDto> EnviarAsync(
            ConviteCadastro conviteCadastro,
            string codigoConvite,
            CancellationToken cancellationToken = default)
        {
            Envios.Add((conviteCadastro.Id, codigoConvite));
            return Task.FromResult(Resultado);
        }
    }

    private sealed class EnvioWhatsappConviteCadastroServicoStub : IEnvioWhatsappConviteCadastroServico
    {
        public ResultadoEnvioWhatsappConviteDto Resultado { get; set; } = new(false, false, null, null);
        public List<(Guid ConviteId, string CodigoConvite)> Envios { get; } = [];

        public Task<ResultadoEnvioWhatsappConviteDto> EnviarAsync(
            ConviteCadastro conviteCadastro,
            string codigoConvite,
            CancellationToken cancellationToken = default)
        {
            Envios.Add((conviteCadastro.Id, codigoConvite));
            return Task.FromResult(Resultado);
        }
    }
}
