using System.Net.Mime;
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

public class UsuarioServicoTests
{
    [Fact]
    public async Task AtualizarMeuUsuarioAsync_NomeValido_AtualizaNomeEAtleta()
    {
        var usuario = new Usuario
        {
            Nome = "Joao",
            Email = "joao@example.com",
            Perfil = PerfilUsuario.Atleta,
            Ativo = true,
            Atleta = new Atleta { Nome = "Joao", Apelido = "J" }
        };
        var cenario = new Cenario(usuario);

        var resposta = await cenario.Servico.AtualizarMeuUsuarioAsync(new AtualizarMeuUsuarioDto("  João da Silva  "));

        Assert.Equal("João da Silva", resposta.Nome);
        Assert.Equal("João da Silva", usuario.Atleta?.Nome);
        Assert.Equal("João Silva", usuario.Atleta?.Apelido);
    }

    [Fact]
    public async Task AtualizarMeuUsuarioAsync_NomeVazio_Bloqueia()
    {
        var cenario = new Cenario(new Usuario
        {
            Nome = "Joao",
            Email = "joao@example.com",
            Perfil = PerfilUsuario.Atleta,
            Ativo = true
        });

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.AtualizarMeuUsuarioAsync(new AtualizarMeuUsuarioDto("   ")));

        Assert.Equal("Nome é obrigatório.", excecao.Message);
    }

    [Fact]
    public async Task AtualizarMinhaFotoPerfilAsync_UsuarioComFotoAnterior_RemoveArquivoAnterior()
    {
        var usuario = new Usuario
        {
            Nome = "Joao",
            Email = "joao@example.com",
            Perfil = PerfilUsuario.Atleta,
            Ativo = true
        };
        usuario.AtualizarFotoPerfil("https://cdn.example/antiga.jpg", "publico-antigo");
        var cenario = new Cenario(usuario);

        var resposta = await cenario.Servico.AtualizarMinhaFotoPerfilAsync(CriarArquivoFoto());

        Assert.Equal("https://cdn.example/foto1.jpg", resposta.FotoPerfilUrl);
        Assert.Equal("foto-perfil", usuario.FotoPerfilPublicId);
        Assert.Contains("publico-antigo", cenario.FotoService.PublicIdsRemovidos);
    }

    [Fact]
    public async Task VincularMeuAtletaAsync_AtletaInexistente_Bloqueia()
    {
        var cenario = new Cenario(new Usuario
        {
            Nome = "Admin",
            Email = "admin@example.com",
            Perfil = PerfilUsuario.Administrador,
            Ativo = true
        });

        var excecao = await Assert.ThrowsAsync<EntidadeNaoEncontradaException>(() =>
            cenario.Servico.VincularMeuAtletaAsync(new VincularAtletaUsuarioDto(Guid.NewGuid())));

        Assert.Equal("Atleta não encontrado.", excecao.Message);
    }

    [Fact]
    public async Task VincularMeuAtletaAsync_ComAtletaValido_AssociaAtletaAoUsuario()
    {
        var atleta = new Atleta { Nome = "Atleta 1", Email = "atleta@example.com" };
        var usuario = new Usuario
        {
            Nome = "Admin",
            Email = "admin@example.com",
            Perfil = PerfilUsuario.Administrador,
            Ativo = true
        };
        var cenario = new Cenario(usuario);
        cenario.Atletas.Itens.Add(atleta);

        var resposta = await cenario.Servico.VincularMeuAtletaAsync(new VincularAtletaUsuarioDto(atleta.Id));

        Assert.Equal(atleta.Id, resposta.AtletaId);
        Assert.Equal(atleta.Id, usuario.AtletaId);
        Assert.Equal(atleta, usuario.Atleta);
        Assert.False(atleta.CadastroPendente);
    }

    [Fact]
    public async Task ExcluirMeuPerfilAsync_UsuarioAtivo_AnonimizaEDesativa()
    {
        var atleta = new Atleta
        {
            Nome = "Atleta",
            Email = "atleta@example.com",
            Apelido = "At"
        };
        var usuario = new Usuario
        {
            Nome = "Usuario Ativo",
            Email = "usuario@example.com",
            Perfil = PerfilUsuario.Organizador,
            Ativo = true,
            Atleta = atleta
        };
        var cenario = new Cenario(usuario);

        await cenario.Servico.ExcluirMeuPerfilAsync();

        Assert.False(usuario.Ativo);
        Assert.True(usuario.DadosAnonimizados);
        Assert.False(string.IsNullOrWhiteSpace(usuario.ExcluidoPorUsuarioId.ToString()));
        Assert.Equal(usuario.Id, usuario.ExcluidoPorUsuarioId);
        Assert.Equal("Usuário excluído", usuario.Nome);
        Assert.Equal("usuario-excluido-" + usuario.Id.ToString("N") + "@excluido.local", usuario.Email);
        Assert.Null(usuario.AtletaId);
    }

    [Fact]
    public async Task ListarAsync_NaoAdministrador_Bloqueia()
    {
        var cenario = new Cenario(new Usuario
        {
            Nome = "Comum",
            Email = "comum@example.com",
            Perfil = PerfilUsuario.Organizador,
            Ativo = true
        }, autorizarComoAdmin: false);

        var excecao = await Assert.ThrowsAsync<AcessoNegadoException>(() =>
            cenario.Servico.ListarAsync(null, null));

        Assert.Equal("Apenas administradores podem executar esta operação.", excecao.Message);
    }

    [Fact]
    public async Task AtualizarAsync_NomeVazio_Bloqueia()
    {
        var usuario = new Usuario
        {
            Nome = "Admin",
            Email = "admin@example.com",
            Perfil = PerfilUsuario.Administrador,
            Ativo = true
        };
        var cenario = new Cenario(usuario);

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.AtualizarAsync(usuario.Id, new AtualizarUsuarioDto("   ", "admin@example.com", PerfilUsuario.Administrador, true, null)));

        Assert.Equal("Nome é obrigatório.", excecao.Message);
    }

    [Fact]
    public async Task AtualizarAsync_EmailVazio_Bloqueia()
    {
        var usuario = new Usuario
        {
            Nome = "Admin",
            Email = "admin@example.com",
            Perfil = PerfilUsuario.Administrador,
            Ativo = true
        };
        var cenario = new Cenario(usuario);

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.AtualizarAsync(usuario.Id, new AtualizarUsuarioDto("Admin", "   ", PerfilUsuario.Administrador, true, null)));

        Assert.Equal("E-mail é obrigatório.", excecao.Message);
    }

    [Fact]
    public async Task AtualizarAsync_PerfilInvalido_Bloqueia()
    {
        var usuario = new Usuario
        {
            Nome = "Admin",
            Email = "admin@example.com",
            Perfil = PerfilUsuario.Administrador,
            Ativo = true
        };
        var cenario = new Cenario(usuario);

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.AtualizarAsync(usuario.Id, new AtualizarUsuarioDto("Admin", "admin@example.com", (PerfilUsuario)999, true, null)));

        Assert.Equal("Perfil inválido.", excecao.Message);
    }

    [Fact]
    public async Task AtualizarAsync_EmailDuplicado_Bloqueia()
    {
        var usuario = new Usuario
        {
            Nome = "Admin",
            Email = "admin@example.com",
            Perfil = PerfilUsuario.Administrador,
            Ativo = true
        };
        var outro = new Usuario
        {
            Nome = "Outro",
            Email = "outro@example.com",
            Perfil = PerfilUsuario.Organizador,
            Ativo = true
        };
        var cenario = new Cenario(usuario);
        cenario.Usuarios.Itens.Add(outro);

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.AtualizarAsync(usuario.Id, new AtualizarUsuarioDto("Admin", " OUTRO@EXAMPLE.COM ", PerfilUsuario.Administrador, true, null)));

        Assert.Equal("Já existe um usuário cadastrado com este e-mail.", excecao.Message);
        Assert.Equal("admin@example.com", usuario.Email);
    }

    [Fact]
    public async Task AtualizarAsync_AtletaJaVinculadoAOutroUsuario_Bloqueia()
    {
        var atleta = new Atleta { Nome = "Atleta", Email = "atleta@example.com" };
        var usuario = new Usuario
        {
            Nome = "Admin",
            Email = "admin@example.com",
            Perfil = PerfilUsuario.Administrador,
            Ativo = true
        };
        var outro = new Usuario
        {
            Nome = "Outro",
            Email = "outro@example.com",
            Perfil = PerfilUsuario.Atleta,
            Ativo = true,
            AtletaId = atleta.Id,
            Atleta = atleta
        };
        var cenario = new Cenario(usuario);
        cenario.Usuarios.Itens.Add(outro);
        cenario.Atletas.Itens.Add(atleta);

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.AtualizarAsync(usuario.Id, new AtualizarUsuarioDto("Admin", "admin@example.com", PerfilUsuario.Administrador, true, atleta.Id)));

        Assert.Equal("Este atleta já está vinculado a outro usuário.", excecao.Message);
        Assert.Null(usuario.AtletaId);
    }

    [Fact]
    public async Task ExcluirMeuPerfilAsync_UsuarioInativo_Bloqueia()
    {
        var cenario = new Cenario(new Usuario
        {
            Nome = "Usuario Inativo",
            Email = "inativo@example.com",
            Perfil = PerfilUsuario.Organizador,
            Ativo = false
        });

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.ExcluirMeuPerfilAsync());

        Assert.Equal("Usuário já está inativo.", excecao.Message);
    }

    [Fact]
    public async Task ExcluirMeuPerfilAsync_UsuarioComDadosSensiveis_LimpaDadosSensiveis()
    {
        var atleta = new Atleta
        {
            Nome = "Atleta",
            Email = "atleta@example.com",
            Apelido = "At",
            Telefone = "11999999999",
            Instagram = "@atleta",
            Cpf = "12345678900",
            Bairro = "Centro",
            Cidade = "Santos",
            Estado = "SP",
            Nivel = NivelAtleta.Intermediario,
            DataNascimento = new DateTime(1990, 1, 1)
        };
        var usuario = new Usuario
        {
            Nome = "Usuario Ativo",
            Email = "usuario@example.com",
            SenhaHash = "hash",
            CodigoLoginHash = "codigo-login",
            CodigoLoginExpiraEmUtc = DateTime.UtcNow.AddMinutes(10),
            CodigoRedefinicaoSenhaHash = "codigo-senha",
            CodigoRedefinicaoSenhaExpiraEmUtc = DateTime.UtcNow.AddMinutes(10),
            RefreshTokenHash = "refresh",
            RefreshTokenExpiraEmUtc = DateTime.UtcNow.AddDays(1),
            Perfil = PerfilUsuario.Organizador,
            Ativo = true,
            PerfilPublico = true,
            ExibirEmail = true,
            PermitirUsoLocalizacao = true,
            PermitirUsoImagem = true,
            Atleta = atleta
        };
        usuario.AtualizarFotoPerfil("https://cdn.example/foto.jpg", "public-id");
        var cenario = new Cenario(usuario);

        await cenario.Servico.ExcluirMeuPerfilAsync();

        Assert.Equal(string.Empty, usuario.SenhaHash);
        Assert.Null(usuario.CodigoLoginHash);
        Assert.Null(usuario.CodigoLoginExpiraEmUtc);
        Assert.Null(usuario.CodigoRedefinicaoSenhaHash);
        Assert.Null(usuario.CodigoRedefinicaoSenhaExpiraEmUtc);
        Assert.Null(usuario.RefreshTokenHash);
        Assert.Null(usuario.RefreshTokenExpiraEmUtc);
        Assert.False(usuario.PerfilPublico);
        Assert.False(usuario.ExibirEmail);
        Assert.False(usuario.PermitirUsoLocalizacao);
        Assert.False(usuario.PermitirUsoImagem);
        Assert.Null(usuario.FotoPerfilUrl);
        Assert.Null(usuario.FotoPerfilPublicId);
        Assert.Equal("Usuário excluído", atleta.Nome);
        Assert.Equal("Usuário excluído", atleta.Apelido);
        Assert.Null(atleta.Email);
        Assert.Null(atleta.Telefone);
        Assert.Null(atleta.Instagram);
        Assert.Null(atleta.Cpf);
        Assert.Null(atleta.Bairro);
        Assert.Null(atleta.Cidade);
        Assert.Null(atleta.Estado);
        Assert.Null(atleta.Nivel);
        Assert.Null(atleta.DataNascimento);
    }

    private static ArquivoFotoPerfilDto CriarArquivoFoto()
    {
        return new ArquivoFotoPerfilDto(new MemoryStream([1, 2, 3]), "foto.jpg", MediaTypeNames.Image.Jpeg, 3);
    }

    private sealed class Cenario
    {
        public Cenario(Usuario usuario, bool autorizarComoAdmin = true)
        {
            Usuarios.Itens.Add(usuario);
            Autorizacao.UsuarioAtual = usuario;
            Autorizacao.PermiteAdmin = autorizarComoAdmin;

            Servico = new UsuarioServico(
                Usuarios,
                Atletas,
                Convites,
                GrupoAtletas,
                Pendencias,
                Partidas,
                UnidadeTrabalho,
                Autorizacao,
                PendenciaServico,
                ConsolidacaoAtleta,
                FotoService);
        }

        public UsuarioServico Servico { get; }
        public UsuarioRepositorioMemoria Usuarios { get; } = new();
        public AtletaRepositorioMemoria Atletas { get; } = new();
        public ConviteCadastroRepositorioMemoria Convites { get; } = new();
        public GrupoAtletaRepositorioMemoria GrupoAtletas { get; } = new();
        public PendenciaUsuarioRepositorioMemoria Pendencias { get; } = new();
        public PartidaRepositorioMemoria Partidas { get; } = new();
        public UnidadeTrabalhoStub UnidadeTrabalho { get; } = new();
        public AutorizacaoUsuarioServicoStub Autorizacao { get; } = new();
        public PendenciaServicoStub PendenciaServico { get; } = new();
        public ConsolidacaoAtletaServicoStub ConsolidacaoAtleta { get; } = new();
        public FotoPerfilServiceStub FotoService { get; } = new();

        public sealed class UsuarioRepositorioMemoria : IUsuarioRepositorio
        {
            public List<Usuario> Itens { get; } = [];

            public Task<IReadOnlyList<Usuario>> ListarAsync(string? nome, string? email, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Usuario>>(Itens.ToList());

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

        public sealed class AtletaRepositorioMemoria : IAtletaRepositorio
        {
            public List<Atleta> Itens { get; } = [];

            public Task<IReadOnlyList<Atleta>> ListarAsync(CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Atleta>>(Itens.ToList());

            public Task<int> ContarAsync(CancellationToken cancellationToken = default)
                => Task.FromResult(Itens.Count);

            public Task<IReadOnlyList<Atleta>> ListarComEmailEmPartidasSemUsuarioAsync(CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Atleta>>(Array.Empty<Atleta>());

            public Task<IReadOnlyList<Atleta>> ListarInscritosPorOrganizadorAsync(Guid usuarioOrganizadorId, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Atleta>>(Array.Empty<Atleta>());

            public Task<bool> PertenceAoOrganizadorAsync(Guid atletaId, Guid usuarioOrganizadorId, CancellationToken cancellationToken = default)
                => Task.FromResult(false);

            public Task<IReadOnlyList<Atleta>> BuscarAsync(string? termo, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Atleta>>(Array.Empty<Atleta>());

            public Task<IDictionary<Guid, int>> ContarPartidasPorAtletasAsync(IEnumerable<Guid> atletaIds, CancellationToken cancellationToken = default)
                => Task.FromResult<IDictionary<Guid, int>>(new Dictionary<Guid, int>());

            public Task<IReadOnlyList<Atleta>> BuscarSugestoesPorCompeticaoAsync(Guid competicaoId, string termo, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Atleta>>(Array.Empty<Atleta>());

            public Task<Atleta?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
                => Task.FromResult(Itens.FirstOrDefault(x => x.Id == id));

            public Task<Atleta?> ObterPorIdParaAtualizacaoAsync(Guid id, CancellationToken cancellationToken = default)
                => Task.FromResult(Itens.FirstOrDefault(x => x.Id == id));

            public Task<Atleta?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default)
                => Task.FromResult<Atleta?>(null);

            public Task<IReadOnlyList<Atleta>> ListarPorNomeAsync(string nome, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Atleta>>(Array.Empty<Atleta>());

            public Task<IReadOnlyList<Atleta>> ListarPorEmailAsync(string email, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Atleta>>(Itens.Where(x => x.Email == email).ToList());

            public Task AdicionarAsync(Atleta atleta, CancellationToken cancellationToken = default)
                => throw new NotSupportedException();

            public Task AdicionarMedidasAsync(AtletaMedidas medidas, CancellationToken cancellationToken = default)
                => throw new NotSupportedException();

            public void Atualizar(Atleta atleta) { }

            public void AtualizarMedidas(AtletaMedidas medidas) { }

            public void Remover(Atleta atleta) { }

            
        }

        public sealed class ConviteCadastroRepositorioMemoria : IConviteCadastroRepositorio
        {
            public List<ConviteCadastro> Itens { get; } = [];

            public Task<IReadOnlyList<ConviteCadastro>> ListarAsync(CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<ConviteCadastro>>(Itens.ToList());

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

        public sealed class GrupoAtletaRepositorioMemoria : IGrupoAtletaRepositorio
        {
            public List<GrupoAtleta> Itens { get; } = [];

            public Task<IReadOnlyList<GrupoAtleta>> ListarPorGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<GrupoAtleta>>(Itens.Where(x => x.GrupoId == grupoId).ToList());

            public Task<IReadOnlyList<GrupoAtleta>> ListarPorGrupoParaAtualizacaoAsync(Guid grupoId, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<GrupoAtleta>>(Itens.Where(x => x.GrupoId == grupoId).ToList());

            public Task<IReadOnlyList<GrupoAtleta>> BuscarPorGrupoAsync(Guid grupoId, string termo, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<GrupoAtleta>>(Itens.Where(x => x.GrupoId == grupoId).ToList());

            public Task<IReadOnlyList<GrupoAtleta>> ListarPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<GrupoAtleta>>(Itens.Where(x => x.AtletaId == atletaId).ToList());

            public Task<IReadOnlyList<GrupoAtleta>> ListarPorAtletaParaAtualizacaoAsync(Guid atletaId, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<GrupoAtleta>>(Itens.Where(x => x.AtletaId == atletaId).ToList());

            public Task<GrupoAtleta?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
                => Task.FromResult<GrupoAtleta?>(Itens.FirstOrDefault(x => x.Id == id));

            public Task<GrupoAtleta?> ObterPorGrupoEAtletaAsync(Guid grupoId, Guid atletaId, CancellationToken cancellationToken = default)
                => Task.FromResult<GrupoAtleta?>(Itens.FirstOrDefault(x => x.GrupoId == grupoId && x.AtletaId == atletaId));

            public Task AdicionarAsync(GrupoAtleta grupoAtleta, CancellationToken cancellationToken = default)
            {
                Itens.Add(grupoAtleta);
                return Task.CompletedTask;
            }

            public void Remover(GrupoAtleta grupoAtleta) => Itens.Remove(grupoAtleta);
        }

        public sealed class PendenciaUsuarioRepositorioMemoria : IPendenciaUsuarioRepositorio
        {
            public List<PendenciaUsuario> Itens { get; } = [];

            public Task<IReadOnlyList<PendenciaUsuario>> ListarPendentesPorUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<PendenciaUsuario>>(Array.Empty<PendenciaUsuario>());

            public Task<IReadOnlyList<PendenciaUsuario>> ListarPendentesPorPartidaAsync(Guid partidaId, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<PendenciaUsuario>>(Array.Empty<PendenciaUsuario>());

            public Task<IReadOnlyList<PendenciaUsuario>> ListarPendentesPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<PendenciaUsuario>>(Array.Empty<PendenciaUsuario>());

            public Task<IReadOnlyList<PendenciaUsuario>> ListarPendentesPorUsuarioParaAtualizacaoAsync(Guid usuarioId, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<PendenciaUsuario>>(Itens.Where(x => x.UsuarioId == usuarioId).ToList());

            public Task<PendenciaUsuario?> ObterPendenteAsync(TipoPendenciaUsuario tipo, Guid usuarioId, Guid? partidaId, Guid? atletaId, CancellationToken cancellationToken = default)
                => Task.FromResult<PendenciaUsuario?>(null);

            public Task<bool> ExistePendentePorUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken = default)
                => Task.FromResult(false);

            public Task<PendenciaUsuario?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
                => Task.FromResult(Itens.FirstOrDefault(x => x.Id == id));

            public Task AdicionarAsync(PendenciaUsuario pendencia, CancellationToken cancellationToken = default)
            {
                Itens.Add(pendencia);
                return Task.CompletedTask;
            }

            public void Atualizar(PendenciaUsuario pendencia) { }

        }

        public sealed class PartidaRepositorioMemoria : IPartidaRepositorio
        {
            public Task<IReadOnlyList<Partida>> ListarPorCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Partida>>(Array.Empty<Partida>());

            public Task<int> ContarRegistradasAsync(CancellationToken cancellationToken = default)
                => Task.FromResult(0);

            public Task<IReadOnlyList<Partida>> ListarPorGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Partida>>(Array.Empty<Partida>());

            public Task<IReadOnlyList<Partida>> ListarPorCategoriaAsync(Guid categoriaId, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Partida>>(Array.Empty<Partida>());

            public Task<IReadOnlyList<Partida>> ListarPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Partida>>(Array.Empty<Partida>());

            public Task<IReadOnlyList<Partida>> ListarPorUsuarioCriadorAsync(Guid usuarioId, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Partida>>(Array.Empty<Partida>());

            public Task<IReadOnlyList<Partida>> ListarAdministracaoAsync(CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Partida>>(Array.Empty<Partida>());

            public Task<IReadOnlyList<Partida>> ListarFeedAsync(int skip, int take, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Partida>>(Array.Empty<Partida>());

            public Task<IReadOnlyList<Partida>> ListarPorDiaAsync(DateTime inicioUtc, DateTime fimUtc, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Partida>>(Array.Empty<Partida>());

            public Task<IReadOnlyList<Partida>> ListarPorAtletaParaRemocaoAsync(Guid atletaId, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Partida>>(Array.Empty<Partida>());

            public Task<IReadOnlyList<Partida>> ListarReferenciandoPartidasAsync(IReadOnlyCollection<Guid> partidaIds, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Partida>>(Array.Empty<Partida>());

            public Task<Partida?> ObterUltimaDoGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default)
                => Task.FromResult<Partida?>(null);

            public Task<Partida?> ObterUltimaDoAtletaNoGrupoAsync(Guid grupoId, Guid atletaId, CancellationToken cancellationToken = default)
                => Task.FromResult<Partida?>(null);

            public Task<IReadOnlyList<Partida>> ListarComAtletasPendentesPorUsuarioCriadorAsync(Guid usuarioId, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Partida>>(Array.Empty<Partida>());

            public Task<IReadOnlyList<Partida>> ListarComPendenteDeVinculoPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Partida>>(Array.Empty<Partida>());

            public Task<bool> ExisteAtletaPendenteEmPartidaCriadaPorUsuarioAsync(Guid usuarioId, Guid atletaId, CancellationToken cancellationToken = default)
                => Task.FromResult(false);

            public Task<IReadOnlyList<Partida>> ListarParaRankingGeralAsync(Guid? usuarioOrganizadorId, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Partida>>(Array.Empty<Partida>());

            public Task<IReadOnlyList<Partida>> ListarParaRankingPorLigaAsync(Guid ligaId, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Partida>>(Array.Empty<Partida>());

            public Task<IReadOnlyList<Partida>> ListarParaRankingSemCompeticaoOuCategoriaAsync(Guid? usuarioOrganizadorId, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Partida>>(Array.Empty<Partida>());

            public Task<IReadOnlyList<Partida>> ListarParaRankingPorCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Partida>>(Array.Empty<Partida>());

            public Task<IReadOnlyList<Partida>> ListarParaRankingPorGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Partida>>(Array.Empty<Partida>());

            public Task<Guid?> ObterUltimaCompeticaoComPartidaEncerradaAsync(Guid? usuarioOrganizadorId, Guid? atletaId, CancellationToken cancellationToken = default)
                => Task.FromResult<Guid?>(null);

            public Task<AtletasSugestoesPartidaDto> ObterSugestoesPartidaAsync(
                Guid atletaId,
                Guid? grupoId,
                int limitePorSecao,
                CancellationToken cancellationToken = default)
                => Task.FromResult(new AtletasSugestoesPartidaDto([], []));

            public Task<UsuarioResumoDto> ObterResumoUsuarioPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default)
                => Task.FromResult(new UsuarioResumoDto(string.Empty, 0, 0, 0, 0, 0, 0, 0));

            public Task<Partida?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
                => Task.FromResult<Partida?>(null);

            public Task AdicionarAsync(Partida partida, CancellationToken cancellationToken = default)
                => Task.CompletedTask;

            public void Atualizar(Partida partida) { }

            public void Remover(Partida partida) { }
        }

        public sealed class UnidadeTrabalhoStub : IUnidadeTrabalho
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

        public sealed class AutorizacaoUsuarioServicoStub : IAutorizacaoUsuarioServico
        {
            public Usuario? UsuarioAtual { get; set; }
            public bool PermiteAdmin { get; set; } = true;

            public Task<Usuario?> ObterUsuarioAtualAsync(CancellationToken cancellationToken = default)
                => Task.FromResult(UsuarioAtual);

            public Task<Usuario> ObterUsuarioAtualObrigatorioAsync(CancellationToken cancellationToken = default)
                => Task.FromResult(UsuarioAtual ?? throw new EntidadeNaoEncontradaException("Usuário não encontrado."));

            public Task GarantirAdministradorAsync(CancellationToken cancellationToken = default)
            {
                if (!PermiteAdmin)
                {
                    throw new AcessoNegadoException("Apenas administradores podem executar esta operação.");
                }

                return Task.CompletedTask;
            }

            public Task GarantirAdminOuOrganizadorAsync(CancellationToken cancellationToken = default)
                => Task.CompletedTask;

            public Task GarantirAcessoAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default)
                => Task.CompletedTask;

            public Task GarantirGestaoCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default)
                => Task.CompletedTask;

            public Task GarantirGestaoGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default)
                => Task.CompletedTask;
        }

        public sealed class PendenciaServicoStub : IPendenciaServico
        {
            public Task<IReadOnlyList<PendenciaUsuarioDto>> ListarMinhasAsync(CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<PendenciaUsuarioDto>>(Array.Empty<PendenciaUsuarioDto>());

            public Task<PendenciasResumoDto> ObterResumoAsync(CancellationToken cancellationToken = default)
                => Task.FromResult(new PendenciasResumoDto(0, 0, 0, 0));

            public Task<bool> ExistePendenciaAsync(CancellationToken cancellationToken = default)
                => Task.FromResult(false);

            public Task<PendenciaUsuarioDto> AprovarPartidaAsync(Guid pendenciaId, ResponderPendenciaPartidaDto dto, CancellationToken cancellationToken = default)
                => throw new NotSupportedException();

            public Task<PendenciaUsuarioDto> ContestarPartidaAsync(Guid pendenciaId, ResponderPendenciaPartidaDto dto, CancellationToken cancellationToken = default)
                => throw new NotSupportedException();

            public Task<AtualizarContatoPendenciaResultadoDto> CompletarContatoAsync(
                Guid pendenciaId,
                AtualizarContatoPendenciaDto dto,
                CancellationToken cancellationToken = default)
                => throw new NotSupportedException();

            public Task<PendenciaUsuarioDto> ConfirmarVinculoAtletaCadastradoAsync(
                Guid pendenciaId,
                ConfirmarVinculoAtletaPendenciaDto dto,
                CancellationToken cancellationToken = default)
                => throw new NotSupportedException();

            public Task InicializarFluxoPartidaAsync(Partida partida, Guid usuarioRegistradorId, CancellationToken cancellationToken = default)
                => Task.CompletedTask;

            public Task SincronizarAposVinculoAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default)
                => Task.CompletedTask;
        }

        public sealed class ConsolidacaoAtletaServicoStub : IConsolidacaoAtletaServico
        {
            public Task<Atleta> ConsolidarCandidatosAsync(
                IEnumerable<Atleta?> candidatos,
                Guid? atletaVinculadoConfiavelId = null,
                string? emailNormalizado = null,
                CancellationToken cancellationToken = default)
                => Task.FromResult(candidatos.OfType<Atleta>().First());

            public Task<SaneamentoAtletasEmailResumoDto> ConsolidarDuplicadosPorEmailAsync(CancellationToken cancellationToken = default)
                => throw new NotSupportedException();
        }

        public sealed class FotoPerfilServiceStub : IFotoPerfilService
        {
            public List<string> PublicIdsRemovidos { get; } = new();

            public Task<FotoPerfilUploadDto> EnviarAsync(ArquivoFotoPerfilDto arquivo, CancellationToken cancellationToken = default)
                => Task.FromResult(new FotoPerfilUploadDto("https://cdn.example/foto1.jpg", "foto-perfil"));

            public Task<FotoPerfilUploadDto> EnviarGrupoAsync(ArquivoFotoPerfilDto arquivo, CancellationToken cancellationToken = default)
                => throw new NotSupportedException();

            public Task RemoverAsync(string publicId, CancellationToken cancellationToken = default)
            {
                PublicIdsRemovidos.Add(publicId);
                return Task.CompletedTask;
            }
        }
    }
}
