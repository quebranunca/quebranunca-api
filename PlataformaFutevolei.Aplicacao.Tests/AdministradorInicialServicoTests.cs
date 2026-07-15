using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Servicos;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;
using Xunit;

namespace PlataformaFutevolei.Aplicacao.Tests;

public class AdministradorInicialServicoTests
{
    [Fact]
    public async Task PromoverAsync_UsuarioExistente_PromoveSemCriarUsuario()
    {
        var usuario = CriarUsuario("quebranunca@gmail.com", PerfilUsuario.Atleta);
        var repositorio = new UsuarioRepositorioMemoria(usuario);
        var unidadeTrabalho = new UnidadeTrabalhoMemoria();
        var servico = new AdministradorInicialServico(repositorio, unidadeTrabalho);

        var resultado = await servico.PromoverAsync("quebranunca@gmail.com");

        Assert.True(resultado.Promovido);
        Assert.False(resultado.JaEraAdministrador);
        Assert.Equal(PerfilUsuario.Administrador, usuario.Perfil);
        Assert.Single(repositorio.Itens);
        Assert.Equal(1, repositorio.Atualizacoes);
        Assert.Equal(1, unidadeTrabalho.Salvamentos);
    }

    [Fact]
    public async Task PromoverAsync_UsuarioJaAdministrador_EhIdempotente()
    {
        var usuario = CriarUsuario("quebranunca@gmail.com", PerfilUsuario.Administrador);
        var repositorio = new UsuarioRepositorioMemoria(usuario);
        var unidadeTrabalho = new UnidadeTrabalhoMemoria();
        var servico = new AdministradorInicialServico(repositorio, unidadeTrabalho);

        var resultado = await servico.PromoverAsync("quebranunca@gmail.com");

        Assert.False(resultado.Promovido);
        Assert.True(resultado.JaEraAdministrador);
        Assert.Equal(PerfilUsuario.Administrador, usuario.Perfil);
        Assert.Equal(0, repositorio.Atualizacoes);
        Assert.Equal(0, unidadeTrabalho.Salvamentos);
    }

    [Fact]
    public async Task PromoverAsync_EmailNormalizado_LocalizaCadastro()
    {
        var usuario = CriarUsuario("quebranunca@gmail.com", PerfilUsuario.Atleta);
        var repositorio = new UsuarioRepositorioMemoria(usuario);
        var unidadeTrabalho = new UnidadeTrabalhoMemoria();
        var servico = new AdministradorInicialServico(repositorio, unidadeTrabalho);

        await servico.PromoverAsync("  QUEBRANUNCA@GMAIL.COM  ");

        Assert.Equal(PerfilUsuario.Administrador, usuario.Perfil);
        Assert.Equal("quebranunca@gmail.com", repositorio.UltimoEmailParaAtualizacao);
    }

    [Fact]
    public async Task PromoverAsync_UsuarioInexistente_FalhaComErroClaro()
    {
        var repositorio = new UsuarioRepositorioMemoria();
        var servico = new AdministradorInicialServico(repositorio, new UnidadeTrabalhoMemoria());

        var excecao = await Assert.ThrowsAsync<EntidadeNaoEncontradaException>(() =>
            servico.PromoverAsync("quebranunca@gmail.com"));

        Assert.Equal("Usuário configurado como administrador inicial não encontrado.", excecao.Message);
    }

    [Fact]
    public async Task PromoverAsync_OutroAdministradorAtivo_BloqueiaPromocaoAutomatica()
    {
        var usuario = CriarUsuario("quebranunca@gmail.com", PerfilUsuario.Atleta);
        var outroAdministrador = CriarUsuario("admin@example.com", PerfilUsuario.Administrador);
        var repositorio = new UsuarioRepositorioMemoria(usuario, outroAdministrador);
        var unidadeTrabalho = new UnidadeTrabalhoMemoria();
        var servico = new AdministradorInicialServico(repositorio, unidadeTrabalho);

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            servico.PromoverAsync("quebranunca@gmail.com"));

        Assert.Contains("Existem outros administradores ativos", excecao.Message);
        Assert.Equal(PerfilUsuario.Atleta, usuario.Perfil);
        Assert.Equal(0, repositorio.Atualizacoes);
        Assert.Equal(0, unidadeTrabalho.Salvamentos);
    }

    private static Usuario CriarUsuario(string email, PerfilUsuario perfil)
        => new()
        {
            Nome = "Usuário",
            Email = email,
            Perfil = perfil,
            Ativo = true
        };

    private sealed class UsuarioRepositorioMemoria(params Usuario[] usuarios) : IUsuarioRepositorio
    {
        public List<Usuario> Itens { get; } = usuarios.ToList();
        public int Atualizacoes { get; private set; }
        public string? UltimoEmailParaAtualizacao { get; private set; }

        public Task<IReadOnlyList<Usuario>> ListarAsync(string? nome, string? email, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Usuario>>(Itens);

        public Task<int> ContarAdministradoresAtivosAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Itens.Count(x => x.Perfil == PerfilUsuario.Administrador && x.Ativo));

        public Task<IReadOnlyList<Usuario>> ListarAdministradoresAtivosAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Usuario>>(Itens.Where(x => x.Perfil == PerfilUsuario.Administrador && x.Ativo).ToList());

        public Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default)
            => Task.FromResult(Itens.FirstOrDefault(x => string.Equals(x.Email, email, StringComparison.OrdinalIgnoreCase)));

        public Task<Usuario?> ObterPorEmailParaAtualizacaoAsync(string email, CancellationToken cancellationToken = default)
        {
            UltimoEmailParaAtualizacao = email;
            return ObterPorEmailAsync(email, cancellationToken);
        }

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
            Atualizacoes++;
        }
    }

    private sealed class UnidadeTrabalhoMemoria : IUnidadeTrabalho
    {
        public int Salvamentos { get; private set; }

        public Task<int> SalvarAlteracoesAsync(CancellationToken cancellationToken = default)
        {
            Salvamentos++;
            return Task.FromResult(1);
        }

        public Task ExecutarEmTransacaoAsync(
            Func<CancellationToken, Task> operacao,
            CancellationToken cancellationToken = default)
            => operacao(cancellationToken);
    }
}
