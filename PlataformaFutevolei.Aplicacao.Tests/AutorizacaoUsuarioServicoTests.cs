using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Interfaces.Seguranca;
using PlataformaFutevolei.Aplicacao.Servicos;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;
using Xunit;

namespace PlataformaFutevolei.Aplicacao.Tests;

public class AutorizacaoUsuarioServicoTests
{
    [Fact]
    public async Task GarantirAdministradorAsync_UsuarioAtleta_Bloqueia()
    {
        var usuario = new Usuario { Nome = "Atleta", Perfil = PerfilUsuario.Atleta, Ativo = true };
        var servico = CriarServico(usuario);

        await Assert.ThrowsAsync<AcessoNegadoException>(() => servico.GarantirAdministradorAsync());
    }

    [Fact]
    public async Task GarantirAdministradorAsync_Administrador_Permite()
    {
        var usuario = new Usuario { Nome = "Admin", Perfil = PerfilUsuario.Administrador, Ativo = true };
        var servico = CriarServico(usuario);

        await servico.GarantirAdministradorAsync();
    }

    [Fact]
    public async Task GarantirAdministradorAsync_SemUsuarioAutenticado_Bloqueia()
    {
        var servico = CriarServico(null);

        await Assert.ThrowsAsync<AcessoNegadoException>(() => servico.GarantirAdministradorAsync());
    }

    [Fact]
    public async Task GarantirAdministradorAsync_Organizador_Bloqueia()
    {
        var usuario = new Usuario { Nome = "Organizador", Perfil = PerfilUsuario.Organizador, Ativo = true };
        var servico = CriarServico(usuario);

        await Assert.ThrowsAsync<AcessoNegadoException>(() => servico.GarantirAdministradorAsync());
    }

    [Fact]
    public async Task GarantirAdminOuOrganizadorAsync_Organizador_Permite()
    {
        var usuario = new Usuario { Nome = "Organizador", Perfil = PerfilUsuario.Organizador, Ativo = true };
        var servico = CriarServico(usuario);

        await servico.GarantirAdminOuOrganizadorAsync();
    }

    [Fact]
    public async Task GarantirAdminOuOrganizadorAsync_Atleta_Bloqueia()
    {
        var usuario = new Usuario { Nome = "Atleta", Perfil = PerfilUsuario.Atleta, Ativo = true };
        var servico = CriarServico(usuario);

        await Assert.ThrowsAsync<RegraNegocioException>(() => servico.GarantirAdminOuOrganizadorAsync());
    }

    [Fact]
    public async Task ObterUsuarioAtualAsync_UsuarioInativo_RetornaNulo()
    {
        var usuario = new Usuario { Nome = "Inativo", Perfil = PerfilUsuario.Administrador, Ativo = false };
        var servico = CriarServico(usuario);

        var atual = await servico.ObterUsuarioAtualAsync();

        Assert.Null(atual);
    }

    [Fact]
    public async Task ObterUsuarioAtualObrigatorioAsync_UsuarioInativo_Bloqueia()
    {
        var usuario = new Usuario { Nome = "Inativo", Perfil = PerfilUsuario.Administrador, Ativo = false };
        var servico = CriarServico(usuario);

        await Assert.ThrowsAsync<EntidadeNaoEncontradaException>(() => servico.ObterUsuarioAtualObrigatorioAsync());
    }

    [Fact]
    public async Task GarantirAcessoAtletaAsync_Administrador_PermiteAcessoAQualquerAtleta()
    {
        var usuario = new Usuario { Nome = "Admin", Perfil = PerfilUsuario.Administrador, Ativo = true };
        var servico = CriarServico(usuario);

        await servico.GarantirAcessoAtletaAsync(Guid.NewGuid());
    }

    [Fact]
    public async Task GarantirAcessoAtletaAsync_AtletaVinculado_Permite()
    {
        var atletaId = Guid.NewGuid();
        var usuario = new Usuario { Nome = "Atleta", Perfil = PerfilUsuario.Atleta, Ativo = true, AtletaId = atletaId };
        var servico = CriarServico(usuario);

        await servico.GarantirAcessoAtletaAsync(atletaId);
    }

    [Fact]
    public async Task GarantirAcessoAtletaAsync_AtletaDiferente_Bloqueia()
    {
        var usuario = new Usuario { Nome = "Atleta", Perfil = PerfilUsuario.Atleta, Ativo = true, AtletaId = Guid.NewGuid() };
        var servico = CriarServico(usuario);

        await Assert.ThrowsAsync<RegraNegocioException>(() => servico.GarantirAcessoAtletaAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GarantirGestaoGrupoAsync_Administrador_Permite()
    {
        var usuario = new Usuario { Nome = "Admin", Perfil = PerfilUsuario.Administrador, Ativo = true };
        var grupo = new Grupo { Nome = "Grupo", DataInicio = DateTime.UtcNow, UsuarioOrganizadorId = Guid.NewGuid() };
        var servico = CriarServico(usuario, grupos: [grupo]);

        await servico.GarantirGestaoGrupoAsync(grupo.Id);
    }

    [Fact]
    public async Task GarantirGestaoGrupoAsync_OrganizadorDono_Permite()
    {
        var usuario = new Usuario { Nome = "Organizador", Perfil = PerfilUsuario.Organizador, Ativo = true };
        var grupo = new Grupo { Nome = "Grupo", DataInicio = DateTime.UtcNow, UsuarioOrganizadorId = usuario.Id };
        var servico = CriarServico(usuario, grupos: [grupo]);

        await servico.GarantirGestaoGrupoAsync(grupo.Id);
    }

    [Fact]
    public async Task GarantirGestaoGrupoAsync_MembroComumNaoDono_Bloqueia()
    {
        var usuario = new Usuario { Nome = "Atleta", Perfil = PerfilUsuario.Atleta, Ativo = true, AtletaId = Guid.NewGuid() };
        var grupo = new Grupo { Nome = "Grupo", DataInicio = DateTime.UtcNow, UsuarioOrganizadorId = Guid.NewGuid() };
        var servico = CriarServico(usuario, grupos: [grupo]);

        await Assert.ThrowsAsync<AcessoNegadoException>(() => servico.GarantirGestaoGrupoAsync(grupo.Id));
    }

    [Fact]
    public async Task GarantirGestaoGrupoAsync_GrupoInexistente_Bloqueia()
    {
        var usuario = new Usuario { Nome = "Admin", Perfil = PerfilUsuario.Administrador, Ativo = true };
        var servico = CriarServico(usuario);

        await Assert.ThrowsAsync<EntidadeNaoEncontradaException>(() => servico.GarantirGestaoGrupoAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GarantirGestaoCompeticaoAsync_OrganizadorDono_Permite()
    {
        var usuario = new Usuario { Nome = "Organizador", Perfil = PerfilUsuario.Organizador, Ativo = true };
        var competicao = new Competicao { Nome = "Campeonato", Tipo = TipoCompeticao.Campeonato, UsuarioOrganizadorId = usuario.Id };
        var servico = CriarServico(usuario, competicoes: [competicao]);

        await servico.GarantirGestaoCompeticaoAsync(competicao.Id);
    }

    [Fact]
    public async Task GarantirGestaoCompeticaoAsync_OrganizadorNaoDono_Bloqueia()
    {
        var usuario = new Usuario { Nome = "Organizador", Perfil = PerfilUsuario.Organizador, Ativo = true };
        var competicao = new Competicao { Nome = "Campeonato", Tipo = TipoCompeticao.Campeonato, UsuarioOrganizadorId = Guid.NewGuid() };
        var servico = CriarServico(usuario, competicoes: [competicao]);

        await Assert.ThrowsAsync<RegraNegocioException>(() => servico.GarantirGestaoCompeticaoAsync(competicao.Id));
    }

    [Fact]
    public async Task GarantirGestaoCompeticaoAsync_AtletaDonoDeGrupo_Permite()
    {
        var usuario = new Usuario { Nome = "Atleta", Perfil = PerfilUsuario.Atleta, Ativo = true };
        var competicao = new Competicao { Nome = "Grupo", Tipo = TipoCompeticao.Grupo, UsuarioOrganizadorId = usuario.Id };
        var servico = CriarServico(usuario, competicoes: [competicao]);

        await servico.GarantirGestaoCompeticaoAsync(competicao.Id);
    }

    [Fact]
    public async Task GarantirGestaoCompeticaoAsync_AtletaEmCampeonato_Bloqueia()
    {
        var usuario = new Usuario { Nome = "Atleta", Perfil = PerfilUsuario.Atleta, Ativo = true };
        var competicao = new Competicao { Nome = "Campeonato", Tipo = TipoCompeticao.Campeonato, UsuarioOrganizadorId = usuario.Id };
        var servico = CriarServico(usuario, competicoes: [competicao]);

        await Assert.ThrowsAsync<RegraNegocioException>(() => servico.GarantirGestaoCompeticaoAsync(competicao.Id));
    }

    [Fact]
    public async Task GarantirGestaoCompeticaoAsync_CompeticaoInexistente_Bloqueia()
    {
        var usuario = new Usuario { Nome = "Admin", Perfil = PerfilUsuario.Administrador, Ativo = true };
        var servico = CriarServico(usuario);

        await Assert.ThrowsAsync<EntidadeNaoEncontradaException>(() => servico.GarantirGestaoCompeticaoAsync(Guid.NewGuid()));
    }

    private static AutorizacaoUsuarioServico CriarServico(
        Usuario? usuario,
        IReadOnlyList<Competicao>? competicoes = null,
        IReadOnlyList<Grupo>? grupos = null)
    {
        return new AutorizacaoUsuarioServico(
            new UsuarioRepositorioStub(usuario is null ? [] : [usuario]),
            new CompeticaoRepositorioStub(competicoes ?? []),
            new GrupoRepositorioStub(grupos ?? []),
            new UsuarioContextoStub(usuario?.Id));
    }

    private sealed class UsuarioContextoStub(Guid? usuarioId) : IUsuarioContexto
    {
        public Guid? UsuarioId { get; } = usuarioId;
    }

    private sealed class UsuarioRepositorioStub(IReadOnlyList<Usuario> usuarios) : IUsuarioRepositorio
    {
        private readonly List<Usuario> usuarios = usuarios.ToList();

        public Task<IReadOnlyList<Usuario>> ListarAsync(string? nome, string? email, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Usuario>>(usuarios);
        public Task<int> ContarAdministradoresAtivosAsync(CancellationToken cancellationToken = default) => Task.FromResult(usuarios.Count(x => x.Perfil == PerfilUsuario.Administrador && x.Ativo));
        public Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default) => Task.FromResult(usuarios.FirstOrDefault(x => x.Email == email));
        public Task<Usuario?> ObterPorEmailParaAtualizacaoAsync(string email, CancellationToken cancellationToken = default) => Task.FromResult(usuarios.FirstOrDefault(x => x.Email == email));
        public Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(usuarios.FirstOrDefault(x => x.Id == id));
        public Task<Usuario?> ObterPorIdParaAtualizacaoAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(usuarios.FirstOrDefault(x => x.Id == id));
        public Task<Usuario?> ObterPorAtletaIdAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult(usuarios.FirstOrDefault(x => x.AtletaId == atletaId));
        public Task<Usuario?> ObterPorAtletaIdParaAtualizacaoAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult(usuarios.FirstOrDefault(x => x.AtletaId == atletaId));
        public Task AdicionarAsync(Usuario usuario, CancellationToken cancellationToken = default) { usuarios.Add(usuario); return Task.CompletedTask; }
        public void Atualizar(Usuario usuario) { }
    }

    private sealed class CompeticaoRepositorioStub(IReadOnlyList<Competicao> competicoes) : ICompeticaoRepositorio
    {
        private readonly List<Competicao> competicoes = competicoes.ToList();

        public Task<IReadOnlyList<Competicao>> ListarAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Competicao>>(competicoes);
        public Task<Competicao?> ObterGrupoResumoUsuarioAsync(Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult(competicoes.FirstOrDefault(x => x.UsuarioOrganizadorId == usuarioId));
        public Task<IReadOnlyList<Guid>> ListarIdsComAcessoAtletaAsync(Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Guid>>(competicoes.Select(x => x.Id).ToList());
        public Task<bool> AtletaPossuiAcessoAsync(Guid competicaoId, Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult(competicoes.Any(x => x.Id == competicaoId));
        public Task<Competicao?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default) => Task.FromResult(competicoes.FirstOrDefault(x => x.Nome == nome));
        public Task<Competicao?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(competicoes.FirstOrDefault(x => x.Id == id));
        public Task<Competicao?> ObterPorIdComCategoriasAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(competicoes.FirstOrDefault(x => x.Id == id));
        public Task AdicionarAsync(Competicao competicao, CancellationToken cancellationToken = default) { competicoes.Add(competicao); return Task.CompletedTask; }
        public void Atualizar(Competicao competicao) { }
        public void Remover(Competicao competicao) => competicoes.Remove(competicao);
    }

    private sealed class GrupoRepositorioStub(IReadOnlyList<Grupo> grupos) : IGrupoRepositorio
    {
        private readonly List<Grupo> grupos = grupos.ToList();

        public Task<IReadOnlyList<Grupo>> ListarAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Grupo>>(grupos);
        public Task<IReadOnlyList<Grupo>> ListarParaSelecaoAsync(Guid usuarioId, Guid? atletaId, bool incluirPrivadosDeTerceiros, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Grupo>>(grupos);
        public Task<int> ContarPublicosAsync(CancellationToken cancellationToken = default) => Task.FromResult(grupos.Count(x => x.Publico));
        public Task<Grupo?> ObterResumoUsuarioAsync(Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult(grupos.FirstOrDefault(x => x.UsuarioOrganizadorId == usuarioId));
        public Task<IReadOnlyList<Grupo>> ListarResumosUsuarioAsync(Guid usuarioId, Guid? atletaId, int limite, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Grupo>>(grupos.Take(limite).ToList());
        public Task<IReadOnlyList<Grupo>> ListarDashboardUsuarioAsync(Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Grupo>>(grupos);
        public Task<IReadOnlyList<Guid>> ListarIdsComAcessoAtletaAsync(Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Guid>>(grupos.Select(x => x.Id).ToList());
        public Task<bool> AtletaPossuiAcessoAsync(Guid grupoId, Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult(grupos.Any(x => x.Id == grupoId));
        public Task<Grupo?> ObterPorNomeEOrganizadorAsync(string nome, Guid? usuarioOrganizadorId, CancellationToken cancellationToken = default) => Task.FromResult(grupos.FirstOrDefault(x => x.Nome == nome && x.UsuarioOrganizadorId == usuarioOrganizadorId));
        public Task<Grupo?> ObterPorNomeNormalizadoAsync(string nome, CancellationToken cancellationToken = default) => Task.FromResult(grupos.FirstOrDefault(x => x.Nome == nome));
        public Task<IReadOnlyList<Grupo>> ListarPorUsuarioOrganizadorParaAtualizacaoAsync(Guid usuarioOrganizadorId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Grupo>>(grupos.Where(x => x.UsuarioOrganizadorId == usuarioOrganizadorId).ToList());
        public Task<Grupo?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(grupos.FirstOrDefault(x => x.Id == id));
        public Task AdicionarAsync(Grupo grupo, CancellationToken cancellationToken = default) { grupos.Add(grupo); return Task.CompletedTask; }
        public void Atualizar(Grupo grupo) { }
        public void Remover(Grupo grupo) => grupos.Remove(grupo);
    }
}
