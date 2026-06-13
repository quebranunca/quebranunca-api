using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Interfaces.Seguranca;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Aplicacao.Servicos;
using PlataformaFutevolei.Dominio.Entidades;
using Xunit;

namespace PlataformaFutevolei.Aplicacao.Tests;

public class PrivacidadeServicoTests
{
    [Fact]
    public async Task ObterPoliticaAtualAsync_RetornaVersaoVigente()
    {
        var cenario = new Cenario(new Usuario
        {
            Nome = "João",
            Email = "joao@example.com"
        });

        var politica = await cenario.Servico.ObterPoliticaAtualAsync();

        Assert.Equal(PrivacidadeServico.VersaoPoliticaPrivacidadeAtual, politica.Versao);
        Assert.True(politica.ExigeAceitePoliticaPrivacidade);
        Assert.True(politica.ExigeAceiteTermosUso);
        Assert.Equal(new DateTime(2026, 5, 18, 0, 0, 0, DateTimeKind.Utc), politica.VigenteDesdeUtc);
    }

    [Fact]
    public async Task ObterMinhasPreferenciasAsync_RetornaPreferenciasDoUsuarioAtual()
    {
        var cenario = new Cenario(new Usuario
        {
            Nome = "João",
            Email = "joao@example.com",
            PerfilPublico = true,
            ExibirEmail = false,
            PermitirUsoLocalizacao = true,
            PermitirUsoImagem = false
        });
        cenario.Usuario.AtualizarFotoPerfil("https://cdn.example/foto.jpg", "public-id");

        var preferencias = await cenario.Servico.ObterMinhasPreferenciasAsync();

        Assert.True(preferencias.PerfilPublico);
        Assert.False(preferencias.ExibirEmail);
        Assert.True(preferencias.PermitirUsoLocalizacao);
        Assert.False(preferencias.PermitirUsoImagem);
        Assert.True(preferencias.PossuiFotoPerfil);
        Assert.False(preferencias.ExclusaoSolicitada);
    }

    [Fact]
    public async Task AtualizarMinhasPreferenciasAsync_AtualizaPreferenciasDoUsuario()
    {
        var cenario = new Cenario(new Usuario
        {
            Nome = "João",
            Email = "joao@example.com",
            PerfilPublico = false,
            ExibirEmail = true,
            PermitirUsoLocalizacao = false,
            PermitirUsoImagem = false
        });

        var preferencias = await cenario.Servico.AtualizarMinhasPreferenciasAsync(
            new AtualizarPreferenciasPrivacidadeDto(
                PerfilPublico: true,
                ExibirEmail: false,
                PermitirUsoLocalizacao: true,
                PermitirUsoImagem: true));

        var usuario = cenario.Usuario;
        Assert.True(usuario.PerfilPublico);
        Assert.False(usuario.ExibirEmail);
        Assert.True(usuario.PermitirUsoLocalizacao);
        Assert.True(usuario.PermitirUsoImagem);

        Assert.True(preferencias.PerfilPublico);
        Assert.False(preferencias.ExibirEmail);
        Assert.True(preferencias.PermitirUsoLocalizacao);
        Assert.True(preferencias.PermitirUsoImagem);
    }

    [Fact]
    public async Task AtualizarMinhasPreferenciasAsync_DesativaLocalizacaoEImagem()
    {
        var cenario = new Cenario(new Usuario
        {
            Nome = "João",
            Email = "joao@example.com",
            PerfilPublico = true,
            ExibirEmail = true,
            PermitirUsoLocalizacao = true,
            PermitirUsoImagem = true
        });

        var preferencias = await cenario.Servico.AtualizarMinhasPreferenciasAsync(
            new AtualizarPreferenciasPrivacidadeDto(
                PerfilPublico: true,
                ExibirEmail: true,
                PermitirUsoLocalizacao: false,
                PermitirUsoImagem: false));

        Assert.False(cenario.Usuario.PermitirUsoLocalizacao);
        Assert.False(cenario.Usuario.PermitirUsoImagem);
        Assert.False(preferencias.PermitirUsoLocalizacao);
        Assert.False(preferencias.PermitirUsoImagem);
    }

    [Fact]
    public async Task AtualizarMinhasPreferenciasAsync_UsuarioNaoEncontrado_Bloqueia()
    {
        var cenario = new Cenario(new Usuario
        {
            Nome = "João",
            Email = "joao@example.com"
        });
        cenario.DefinirUsuarioAtual(new Usuario
        {
            Nome = "Outro",
            Email = "outro@example.com"
        });

        var excecao = await Assert.ThrowsAsync<EntidadeNaoEncontradaException>(() =>
            cenario.Servico.AtualizarMinhasPreferenciasAsync(new AtualizarPreferenciasPrivacidadeDto(true, true, true, true)));

        Assert.Equal("Usuário não encontrado.", excecao.Message);
    }

    [Fact]
    public async Task RegistrarConsentimentoAsync_ConsentimentoValido_RegistraPoliticaEBandeiras()
    {
        var cenario = new Cenario(new Usuario
        {
            Nome = "João",
            Email = "joao@example.com",
            PermitirUsoLocalizacao = false,
            PermitirUsoImagem = false
        });

        var preferencias = await cenario.Servico.RegistrarConsentimentoAsync(new RegistrarConsentimentoLgpdDto(
            AceitouPoliticaPrivacidade: true,
            AceitouTermosUso: true,
            AceitouUsoLocalizacao: true,
            AceitouUsoImagem: false,
            IpAddress: new string('I', 80),
            UserAgent: "Mozilla/5.0"));

        Assert.NotNull(cenario.UltimoConsentimento);
        var consentimento = cenario.UltimoConsentimento!;
        Assert.Equal(PrivacidadeServico.VersaoPoliticaPrivacidadeAtual, consentimento.VersaoPoliticaPrivacidade);
        Assert.True(consentimento.AceitouPoliticaPrivacidade);
        Assert.True(consentimento.AceitouTermosUso);
        Assert.True(consentimento.AceitouUsoLocalizacao);
        Assert.False(consentimento.AceitouUsoImagem);
        Assert.Equal(cenario.Usuario.Id, consentimento.UsuarioId);
        Assert.Equal(new string('I', 64), consentimento.IpAddress);
        Assert.Equal("Mozilla/5.0", consentimento.UserAgent);

        Assert.True(cenario.Usuario.PermitirUsoLocalizacao);
        Assert.False(cenario.Usuario.PermitirUsoImagem);
        Assert.True(preferencias.PermitirUsoLocalizacao);
        Assert.False(preferencias.PermitirUsoImagem);
    }

    [Fact]
    public async Task RegistrarConsentimentoAsync_NaoAceitaPoliticaOuTermos_Bloqueia()
    {
        var cenario = new Cenario(new Usuario
        {
            Nome = "João",
            Email = "joao@example.com"
        });

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() => cenario.Servico.RegistrarConsentimentoAsync(
            new RegistrarConsentimentoLgpdDto(
                AceitouPoliticaPrivacidade: false,
                AceitouTermosUso: true,
                AceitouUsoLocalizacao: false,
                AceitouUsoImagem: false)));

        Assert.Equal("É necessário aceitar a Política de Privacidade e os Termos de Uso para continuar.", excecao.Message);
        Assert.Null(cenario.UltimoConsentimento);
    }

    [Fact]
    public async Task RegistrarConsentimentoAsync_VersaoVazia_UsaVersaoAtual()
    {
        var cenario = new Cenario(new Usuario
        {
            Nome = "João",
            Email = "joao@example.com"
        });

        await cenario.Servico.RegistrarConsentimentoAsync(new RegistrarConsentimentoLgpdDto(
            AceitouPoliticaPrivacidade: true,
            AceitouTermosUso: true,
            AceitouUsoLocalizacao: false,
            AceitouUsoImagem: false,
            VersaoPoliticaPrivacidade: "   "));

        Assert.NotNull(cenario.UltimoConsentimento);
        Assert.Equal(PrivacidadeServico.VersaoPoliticaPrivacidadeAtual, cenario.UltimoConsentimento!.VersaoPoliticaPrivacidade);
    }

    [Fact]
    public async Task UsuarioPrecisaAceitarPoliticaAsync_SemConsentimento_RetornaTrue()
    {
        var cenario = new Cenario(new Usuario
        {
            Nome = "João",
            Email = "joao@example.com"
        });

        var precisaAceitar = await cenario.Servico.UsuarioPrecisaAceitarPoliticaAsync(cenario.Usuario.Id);

        Assert.True(precisaAceitar);
    }

    [Fact]
    public async Task UsuarioPrecisaAceitarPoliticaAsync_VersaoAtualETermosAceitos_RetornaFalse()
    {
        var cenario = new Cenario(new Usuario
        {
            Nome = "João",
            Email = "joao@example.com"
        });
        await cenario.AdicionarConsentimentoAsync(new UsuarioConsentimentoLgpd
        {
            UsuarioId = cenario.Usuario.Id,
            VersaoPoliticaPrivacidade = PrivacidadeServico.VersaoPoliticaPrivacidadeAtual,
            AceitouPoliticaPrivacidade = true,
            AceitouTermosUso = true,
            AceitouUsoLocalizacao = false,
            AceitouUsoImagem = false,
            AceitoEm = DateTime.UtcNow
        });

        var precisaAceitar = await cenario.Servico.UsuarioPrecisaAceitarPoliticaAsync(cenario.Usuario.Id);

        Assert.False(precisaAceitar);
    }

    [Fact]
    public async Task UsuarioPrecisaAceitarPoliticaAsync_VersaoAntigaOuTermosNaoAceitos_RetornaTrue()
    {
        var cenario = new Cenario(new Usuario
        {
            Nome = "João",
            Email = "joao@example.com"
        });
        await cenario.AdicionarConsentimentoAsync(new UsuarioConsentimentoLgpd
        {
            UsuarioId = cenario.Usuario.Id,
            VersaoPoliticaPrivacidade = "2025-01-01",
            AceitouPoliticaPrivacidade = true,
            AceitouTermosUso = false,
            AceitouUsoLocalizacao = true,
            AceitouUsoImagem = true,
            AceitoEm = DateTime.UtcNow
        });

        var precisaAceitar = await cenario.Servico.UsuarioPrecisaAceitarPoliticaAsync(cenario.Usuario.Id);

        Assert.True(precisaAceitar);
    }

    [Fact]
    public async Task SolicitarExclusaoContaAsync_MarcaExclusaoESinalizaUsuarioServico()
    {
        var cenario = new Cenario(new Usuario
        {
            Nome = "João",
            Email = "joao@example.com"
        });

        await cenario.Servico.SolicitarExclusaoContaAsync();

        Assert.NotNull(cenario.Usuario.ExclusaoSolicitadaEmUtc);
        Assert.True(cenario.UsuarioSolicitouExclusao);
    }

    [Fact]
    public async Task SolicitarExclusaoContaAsync_UsuarioNaoEncontrado_Bloqueia()
    {
        var cenario = new Cenario(new Usuario
        {
            Nome = "João",
            Email = "joao@example.com"
        });
        cenario.DefinirUsuarioAtual(new Usuario
        {
            Nome = "Outro",
            Email = "outro@example.com"
        });

        var excecao = await Assert.ThrowsAsync<EntidadeNaoEncontradaException>(() =>
            cenario.Servico.SolicitarExclusaoContaAsync());

        Assert.Equal("Usuário não encontrado.", excecao.Message);
        Assert.False(cenario.UsuarioSolicitouExclusao);
    }

    private sealed class Cenario
    {
        public Cenario(Usuario usuario)
        {
            Usuario = usuario;
            Usuarios.Itens.Add(usuario);
            Autorizacao.UsuarioAtual = usuario;
            Servico = new PrivacidadeServico(Usuarios, Consentimentos, UnidadeTrabalho, Autorizacao, UsuarioServico);
        }

        public Usuario Usuario { get; }
        public PrivacidadeServico Servico { get; }
        public UsuarioConsentimentoLgpd? UltimoConsentimento => Consentimentos.ObterUltimoPorUsuarioAsync(Usuario.Id).GetAwaiter().GetResult();
        public bool UsuarioSolicitouExclusao => UsuarioServico.ExclusaoSolicitada;

        public Task AdicionarConsentimentoAsync(UsuarioConsentimentoLgpd consentimento)
            => Consentimentos.AdicionarAsync(consentimento);
        public void DefinirUsuarioAtual(Usuario usuario)
            => Autorizacao.UsuarioAtual = usuario;

        private readonly UsuarioRepositorioMemoria Usuarios = new();
        private readonly UsuarioConsentimentoLgpdRepositorioMemoria Consentimentos = new();
        private readonly UnidadeTrabalhoStub UnidadeTrabalho = new();
        private readonly AutorizacaoUsuarioServicoStub Autorizacao = new();
        private readonly UsuarioServicoStub UsuarioServico = new();

        private sealed class UsuarioRepositorioMemoria : IUsuarioRepositorio
        {
            public readonly List<Usuario> Itens = new();

            public Task<IReadOnlyList<Usuario>> ListarAsync(string? nome, string? email, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Usuario>>(Itens);

            public Task<int> ContarAdministradoresAtivosAsync(CancellationToken cancellationToken = default)
                => Task.FromResult(Itens.Count(x => x.Perfil == Dominio.Enums.PerfilUsuario.Administrador && x.Ativo));

            public Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default)
                => Task.FromResult(Itens.FirstOrDefault(x => x.Email == email));

            public Task<Usuario?> ObterPorEmailParaAtualizacaoAsync(string email, CancellationToken cancellationToken = default)
                => Task.FromResult(Itens.FirstOrDefault(x => x.Email == email));

            public Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
                => Task.FromResult(Itens.FirstOrDefault(x => x.Id == id));

            public Task<Usuario?> ObterPorIdParaAtualizacaoAsync(Guid id, CancellationToken cancellationToken = default)
                => Task.FromResult(Itens.FirstOrDefault(x => x.Id == id));

            public Task<Usuario?> ObterPorAtletaIdAsync(Guid atletaId, CancellationToken cancellationToken = default)
                => Task.FromResult<Usuario?>(null);

            public Task<Usuario?> ObterPorAtletaIdParaAtualizacaoAsync(Guid atletaId, CancellationToken cancellationToken = default)
                => Task.FromResult<Usuario?>(null);

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

        private sealed class UsuarioConsentimentoLgpdRepositorioMemoria : IUsuarioConsentimentoLgpdRepositorio
        {
            public readonly List<UsuarioConsentimentoLgpd> Itens = new();

            public Task<UsuarioConsentimentoLgpd?> ObterUltimoPorUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken = default)
            {
                var ultimo = Itens
                    .Where(x => x.UsuarioId == usuarioId)
                    .OrderByDescending(x => x.AceitoEm)
                    .FirstOrDefault();

                return Task.FromResult<UsuarioConsentimentoLgpd?>(ultimo);
            }

            public Task AdicionarAsync(UsuarioConsentimentoLgpd consentimento, CancellationToken cancellationToken = default)
            {
                Itens.Add(consentimento);
                return Task.CompletedTask;
            }
        }

        private sealed class UnidadeTrabalhoStub : IUnidadeTrabalho
        {
            public int TotalSalvamentos { get; private set; }

            public Task<int> SalvarAlteracoesAsync(CancellationToken cancellationToken = default)
            {
                TotalSalvamentos++;
                return Task.FromResult(TotalSalvamentos);
            }

            public Task ExecutarEmTransacaoAsync(Func<CancellationToken, Task> operacao, CancellationToken cancellationToken = default)
                => operacao(cancellationToken);
        }

        private sealed class AutorizacaoUsuarioServicoStub : IAutorizacaoUsuarioServico
        {
            public Usuario? UsuarioAtual { get; set; }

            public Task<Usuario?> ObterUsuarioAtualAsync(CancellationToken cancellationToken = default)
                => Task.FromResult(UsuarioAtual);

            public Task<Usuario> ObterUsuarioAtualObrigatorioAsync(CancellationToken cancellationToken = default)
                => Task.FromResult(UsuarioAtual ?? throw new EntidadeNaoEncontradaException("Usuário não encontrado."));

            public Task GarantirAdministradorAsync(CancellationToken cancellationToken = default)
                => Task.CompletedTask;

            public Task GarantirAdminOuOrganizadorAsync(CancellationToken cancellationToken = default)
                => Task.CompletedTask;

            public Task GarantirAcessoAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default)
                => Task.CompletedTask;

            public Task GarantirGestaoCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default)
                => Task.CompletedTask;

            public Task GarantirGestaoGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default)
                => Task.CompletedTask;
        }

        private sealed class UsuarioServicoStub : IUsuarioServico
        {
            public bool ExclusaoSolicitada { get; private set; }

            public Task ExcluirMeuPerfilAsync(CancellationToken cancellationToken = default)
            {
                ExclusaoSolicitada = true;
                return Task.CompletedTask;
            }

            public Task<PreferenciasPrivacidadeDto> ObterMinhasPreferenciasAsync(CancellationToken cancellationToken = default)
                => throw new NotImplementedException();

            public Task<PreferenciasPrivacidadeDto> AtualizarMinhasPreferenciasAsync(
                AtualizarPreferenciasPrivacidadeDto dto,
                CancellationToken cancellationToken = default)
                => throw new NotImplementedException();

            public Task<FotoPerfilRespostaDto> AtualizarMinhaFotoPerfilAsync(
                ArquivoFotoPerfilDto arquivo,
                CancellationToken cancellationToken = default)
                => throw new NotImplementedException();

            public Task<UsuarioLogadoDto> AtualizarMeuUsuarioAsync(
                AtualizarMeuUsuarioDto dto,
                CancellationToken cancellationToken = default)
                => throw new NotImplementedException();

            public Task<UsuarioLogadoDto> VincularMeuAtletaAsync(
                VincularAtletaUsuarioDto dto,
                CancellationToken cancellationToken = default)
                => throw new NotImplementedException();

            public Task<UsuarioLogadoDto> ObterMeuUsuarioAsync(CancellationToken cancellationToken = default)
                => throw new NotImplementedException();

            public Task<UsuarioResumoDto> ObterMeuResumoAsync(CancellationToken cancellationToken = default)
                => throw new NotImplementedException();

            public Task<UsuarioDto> AtualizarAsync(
                Guid id,
                AtualizarUsuarioDto dto,
                CancellationToken cancellationToken = default)
                => throw new NotImplementedException();

            public Task<IReadOnlyList<UsuarioDto>> ListarAsync(
                string? nome,
                string? email,
                CancellationToken cancellationToken = default)
                => throw new NotImplementedException();

            public Task ExcluirPorAdministradorAsync(
                Guid id,
                CancellationToken cancellationToken = default)
                => throw new NotImplementedException();

            public Task SolicitarExclusaoContaAsync(CancellationToken cancellationToken = default)
                => throw new NotImplementedException();
        }
    }
}
