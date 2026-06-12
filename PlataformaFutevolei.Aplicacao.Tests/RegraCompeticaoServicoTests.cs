using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Interfaces.Seguranca;
using PlataformaFutevolei.Aplicacao.Servicos;
using PlataformaFutevolei.Aplicacao.Utilitarios;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;
using Xunit;

namespace PlataformaFutevolei.Aplicacao.Tests;

public class RegraCompeticaoServicoTests
{
    [Fact]
    public async Task ListarAsync_SemRegras_CriaERetornaRegrasPadrao()
    {
        var cenario = Cenario.Criar();

        var regras = await cenario.Servico.ListarAsync();

        Assert.Equal(RegrasCompeticaoPadrao.Lista.Count, regras.Count(x => x.EhPadrao));
        foreach (var definicao in RegrasCompeticaoPadrao.Lista)
        {
            var regra = Assert.Single(regras, x => x.Nome == definicao.Nome);
            Assert.Equal(definicao.PontosMinimosPartida, regra.PontosMinimosPartida);
            Assert.Equal(definicao.DiferencaMinimaPartida, regra.DiferencaMinimaPartida);
            Assert.Equal(definicao.PermiteEmpate, regra.PermiteEmpate);
        }
        Assert.Equal(RegrasCompeticaoPadrao.Lista.Count, cenario.Repositorio.Regras.Count);
        Assert.Equal(1, cenario.UnidadeTrabalho.Salvamentos);
    }

    [Fact]
    public async Task CriarAsync_RegraValida_CadastraComUsuarioCriador()
    {
        var cenario = Cenario.Criar();

        var regra = await cenario.Servico.CriarAsync(DtoValido("  Regra 21 pontos  "));

        Assert.Equal("Regra 21 pontos", regra.Nome);
        Assert.Equal("Descrição", regra.Descricao);
        Assert.Equal(21, regra.PontosMinimosPartida);
        Assert.Equal(2, regra.DiferencaMinimaPartida);
        Assert.True(regra.PermiteEmpate);
        Assert.Equal(cenario.Usuario.Id, regra.UsuarioCriadorId);
        Assert.Contains(cenario.Repositorio.Regras, x => x.Id == regra.Id);
    }

    [Fact]
    public async Task AtualizarAsync_RegraExistente_AtualizaDados()
    {
        var cenario = Cenario.Criar();
        var regra = cenario.AdicionarRegra("Regra antiga", cenario.Usuario);

        var atualizada = await cenario.Servico.AtualizarAsync(regra.Id, AtualizarDtoValido("Regra nova"));

        Assert.Equal(regra.Id, atualizada.Id);
        Assert.Equal("Regra nova", atualizada.Nome);
        Assert.Equal("Descrição atualizada", atualizada.Descricao);
        Assert.Equal(19, atualizada.PontosMinimosPartida);
        Assert.Equal(3, atualizada.DiferencaMinimaPartida);
        Assert.False(atualizada.PermiteEmpate);
        Assert.Equal(4m, atualizada.PontosVitoria);
        Assert.Equal(1m, atualizada.PontosDerrota);
        Assert.Equal(0.5m, atualizada.PontosParticipacao);
        Assert.Equal(12m, atualizada.PontosPrimeiroLugar);
        Assert.Equal(8m, atualizada.PontosSegundoLugar);
        Assert.Equal(4m, atualizada.PontosTerceiroLugar);
    }

    [Fact]
    public async Task RemoverAsync_RegraExistente_Remove()
    {
        var cenario = Cenario.Criar();
        var regra = cenario.AdicionarRegra("Regra removível", cenario.Usuario);

        await cenario.Servico.RemoverAsync(regra.Id);

        Assert.DoesNotContain(cenario.Repositorio.Regras, x => x.Id == regra.Id);
    }

    [Fact]
    public async Task CriarAsync_NomeDuplicado_Bloqueia()
    {
        var cenario = Cenario.Criar();
        cenario.AdicionarRegra("Regra duplicada", cenario.Usuario);

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.CriarAsync(DtoValido("Regra duplicada")));

        Assert.Equal("Já existe uma regra cadastrada com este nome.", excecao.Message);
        Assert.Single(cenario.Repositorio.Regras, x => x.Nome == "Regra duplicada");
    }

    [Theory]
    [InlineData(0, 2, "Pontos mínimos da partida devem ser maiores que zero.")]
    [InlineData(18, 0, "Diferença mínima da partida deve ser maior que zero.")]
    public async Task CriarAsync_ValoresInvalidosRegraPartida_Bloqueia(
        int pontosMinimos,
        int diferencaMinima,
        string mensagem)
    {
        var cenario = Cenario.Criar();
        var dto = DtoValido() with
        {
            PontosMinimosPartida = pontosMinimos,
            DiferencaMinimaPartida = diferencaMinima
        };

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.CriarAsync(dto));

        Assert.Equal(mensagem, excecao.Message);
    }

    [Theory]
    [MemberData(nameof(PontuacoesInvalidas))]
    public async Task CriarAsync_ValoresInvalidosPontuacao_Bloqueia(
        CriarRegraCompeticaoDto dto,
        string mensagem)
    {
        var cenario = Cenario.Criar();

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.CriarAsync(dto));

        Assert.Equal(mensagem, excecao.Message);
    }

    [Fact]
    public async Task CriarAsync_UsuarioAtleta_Bloqueia()
    {
        var cenario = Cenario.Criar(PerfilUsuario.Atleta);

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.CriarAsync(DtoValido()));

        Assert.Equal("Apenas administradores ou organizadores podem executar esta operação.", excecao.Message);
    }

    [Fact]
    public async Task AtualizarAsync_OrganizadorDono_Permite()
    {
        var cenario = Cenario.Criar(PerfilUsuario.Organizador);
        var regra = cenario.AdicionarRegra("Regra do organizador", cenario.Usuario);

        var atualizada = await cenario.Servico.AtualizarAsync(regra.Id, AtualizarDtoValido("Regra do dono"));

        Assert.Equal("Regra do dono", atualizada.Nome);
    }

    [Fact]
    public async Task AtualizarAsync_OrganizadorNaoDono_Bloqueia()
    {
        var cenario = Cenario.Criar(PerfilUsuario.Organizador);
        var regra = cenario.AdicionarRegra("Regra de outro", new Usuario
        {
            Nome = "Outro organizador",
            Email = "outro@qnf.test",
            Perfil = PerfilUsuario.Organizador
        });

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.AtualizarAsync(regra.Id, AtualizarDtoValido("Regra alterada")));

        Assert.Equal("O organizador só pode alterar regras criadas pelo próprio usuário.", excecao.Message);
    }

    [Fact]
    public async Task RemoverAsync_OrganizadorNaoDono_Bloqueia()
    {
        var cenario = Cenario.Criar(PerfilUsuario.Organizador);
        var regra = cenario.AdicionarRegra("Regra de outro", new Usuario
        {
            Nome = "Outro organizador",
            Email = "outro@qnf.test",
            Perfil = PerfilUsuario.Organizador
        });

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.RemoverAsync(regra.Id));

        Assert.Equal("O organizador só pode excluir regras criadas pelo próprio usuário.", excecao.Message);
        Assert.Contains(cenario.Repositorio.Regras, x => x.Id == regra.Id);
    }

    [Fact]
    public async Task AtualizarAsync_Administrador_AlteraRegraDeOutroUsuario()
    {
        var cenario = Cenario.Criar(PerfilUsuario.Administrador);
        var regra = cenario.AdicionarRegra("Regra de organizador", new Usuario
        {
            Nome = "Organizador",
            Email = "organizador@qnf.test",
            Perfil = PerfilUsuario.Organizador
        });

        var atualizada = await cenario.Servico.AtualizarAsync(regra.Id, AtualizarDtoValido("Regra admin"));

        Assert.Equal("Regra admin", atualizada.Nome);
    }

    public static IEnumerable<object[]> PontuacoesInvalidas()
    {
        yield return [DtoValido() with { PontosVitoria = -1m }, "Pontuação por vitória não pode ser negativa."];
        yield return [DtoValido() with { PontosDerrota = -1m }, "Pontuação por derrota não pode ser negativa."];
        yield return [DtoValido() with { PontosParticipacao = -1m }, "Pontuação por participação não pode ser negativa."];
        yield return [DtoValido() with { PontosPrimeiroLugar = -1m }, "Pontuação por colocação não pode ser negativa."];
        yield return [DtoValido() with { PontosPrimeiroLugar = 5m, PontosSegundoLugar = 6m }, "A pontuação de 1º lugar não pode ser menor que a de 2º lugar."];
        yield return [DtoValido() with { PontosSegundoLugar = 3m, PontosTerceiroLugar = 4m }, "A pontuação de 2º lugar não pode ser menor que a de 3º lugar."];
    }

    private static CriarRegraCompeticaoDto DtoValido(string nome = "Regra válida")
        => new(
            nome,
            "Descrição",
            21,
            2,
            true,
            3m,
            0m,
            1m,
            10m,
            6m,
            3m);

    private static AtualizarRegraCompeticaoDto AtualizarDtoValido(string nome)
        => new(
            nome,
            "Descrição atualizada",
            19,
            3,
            false,
            4m,
            1m,
            0.5m,
            12m,
            8m,
            4m);

    private sealed class Cenario
    {
        private Cenario(PerfilUsuario perfil)
        {
            Usuario = new Usuario
            {
                Nome = "Gestor",
                Email = "gestor@qnf.test",
                Perfil = perfil
            };
            Repositorio = new RegraCompeticaoRepositorioMemoria();
            UnidadeTrabalho = new UnidadeTrabalhoStub();
            Servico = new RegraCompeticaoServico(
                Repositorio,
                UnidadeTrabalho,
                new AutorizacaoUsuarioServicoStub(Usuario));
        }

        public Usuario Usuario { get; }
        public RegraCompeticaoRepositorioMemoria Repositorio { get; }
        public UnidadeTrabalhoStub UnidadeTrabalho { get; }
        public RegraCompeticaoServico Servico { get; }

        public static Cenario Criar(PerfilUsuario perfil = PerfilUsuario.Organizador) => new(perfil);

        public RegraCompeticao AdicionarRegra(string nome, Usuario? criador = null)
        {
            var regra = new RegraCompeticao
            {
                Nome = nome,
                Descricao = "Descrição original",
                PontosMinimosPartida = 18,
                DiferencaMinimaPartida = 2,
                PermiteEmpate = false,
                PontosVitoria = 3m,
                PontosDerrota = 0m,
                PontosParticipacao = 0m,
                PontosPrimeiroLugar = 10m,
                PontosSegundoLugar = 6m,
                PontosTerceiroLugar = 3m,
                UsuarioCriadorId = criador?.Id,
                UsuarioCriador = criador
            };
            Repositorio.Regras.Add(regra);
            return regra;
        }
    }

    private sealed class RegraCompeticaoRepositorioMemoria : IRegraCompeticaoRepositorio
    {
        public List<RegraCompeticao> Regras { get; } = [];

        public Task<IReadOnlyList<RegraCompeticao>> ListarAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<RegraCompeticao>>(Regras.ToList());

        public Task<RegraCompeticao?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Regras.FirstOrDefault(x => x.Id == id));

        public Task<RegraCompeticao?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default)
            => Task.FromResult(Regras.FirstOrDefault(x => x.Nome == nome));

        public Task AdicionarAsync(RegraCompeticao regra, CancellationToken cancellationToken = default)
        {
            Regras.Add(regra);
            return Task.CompletedTask;
        }

        public void Atualizar(RegraCompeticao regra)
        {
        }

        public void Remover(RegraCompeticao regra) => Regras.Remove(regra);
    }

    private sealed class UnidadeTrabalhoStub : IUnidadeTrabalho
    {
        public int Salvamentos { get; private set; }

        public Task<int> SalvarAlteracoesAsync(CancellationToken cancellationToken = default)
        {
            Salvamentos++;
            return Task.FromResult(1);
        }

        public Task ExecutarEmTransacaoAsync(Func<CancellationToken, Task> operacao, CancellationToken cancellationToken = default)
            => operacao(cancellationToken);
    }

    private sealed class AutorizacaoUsuarioServicoStub(Usuario usuario) : IAutorizacaoUsuarioServico
    {
        public Task<Usuario?> ObterUsuarioAtualAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<Usuario?>(usuario);

        public Task<Usuario> ObterUsuarioAtualObrigatorioAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(usuario);

        public Task GarantirAdministradorAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task GarantirAdminOuOrganizadorAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task GarantirAcessoAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task GarantirGestaoCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task GarantirGestaoGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
