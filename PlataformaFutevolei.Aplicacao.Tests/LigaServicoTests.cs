using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Servicos;
using PlataformaFutevolei.Dominio.Entidades;
using Xunit;

namespace PlataformaFutevolei.Aplicacao.Tests;

public class LigaServicoTests
{
    [Fact]
    public async Task ListarAsync_ComLigas_RetornaDtosOrdenadosDoRepositorio()
    {
        var cenario = new Cenario();
        cenario.Ligas.Itens.Add(new Liga { Nome = "Liga B", Descricao = "B" });
        cenario.Ligas.Itens.Add(new Liga { Nome = "Liga A", Descricao = "A" });

        var resultado = await cenario.Servico.ListarAsync();

        Assert.Equal(2, resultado.Count);
        Assert.Equal("Liga B", resultado[0].Nome);
        Assert.Equal("Liga A", resultado[1].Nome);
    }

    [Fact]
    public async Task ObterPorIdAsync_LigaExistente_RetornaDto()
    {
        var cenario = new Cenario();
        var liga = new Liga { Nome = "Liga Praia", Descricao = "Temporada" };
        cenario.Ligas.Itens.Add(liga);

        var resultado = await cenario.Servico.ObterPorIdAsync(liga.Id);

        Assert.Equal(liga.Id, resultado.Id);
        Assert.Equal("Liga Praia", resultado.Nome);
        Assert.Equal("Temporada", resultado.Descricao);
    }

    [Fact]
    public async Task ObterPorIdAsync_LigaInexistente_Bloqueia()
    {
        var cenario = new Cenario();

        var excecao = await Assert.ThrowsAsync<EntidadeNaoEncontradaException>(() =>
            cenario.Servico.ObterPorIdAsync(Guid.NewGuid()));

        Assert.Equal("Liga não encontrada.", excecao.Message);
    }

    [Fact]
    public async Task CriarAsync_DadosValidos_NormalizaESalva()
    {
        var cenario = new Cenario();

        var resultado = await cenario.Servico.CriarAsync(new CriarLigaDto("  Liga Verão  ", "  Santos  "));

        var liga = Assert.Single(cenario.Ligas.Itens);
        Assert.Equal(liga.Id, resultado.Id);
        Assert.Equal("Liga Verão", liga.Nome);
        Assert.Equal("Santos", liga.Descricao);
        Assert.Equal(1, cenario.UnidadeTrabalho.Salvamentos);
    }

    [Fact]
    public async Task CriarAsync_NomeVazio_Bloqueia()
    {
        var cenario = new Cenario();

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.CriarAsync(new CriarLigaDto("   ", "Descricao")));

        Assert.Equal("Nome da liga é obrigatório.", excecao.Message);
        Assert.Empty(cenario.Ligas.Itens);
        Assert.Equal(0, cenario.UnidadeTrabalho.Salvamentos);
    }

    [Fact]
    public async Task CriarAsync_NomeDuplicado_Bloqueia()
    {
        var cenario = new Cenario();
        cenario.Ligas.Itens.Add(new Liga { Nome = "Liga Verão" });

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.CriarAsync(new CriarLigaDto("Liga Verão", null)));

        Assert.Equal("Já existe uma liga cadastrada com este nome.", excecao.Message);
        Assert.Single(cenario.Ligas.Itens);
        Assert.Equal(0, cenario.UnidadeTrabalho.Salvamentos);
    }

    [Fact]
    public async Task AtualizarAsync_DadosValidos_AtualizaESalva()
    {
        var cenario = new Cenario();
        var liga = new Liga { Nome = "Liga Antiga", Descricao = "Antiga" };
        cenario.Ligas.Itens.Add(liga);

        var resultado = await cenario.Servico.AtualizarAsync(liga.Id, new AtualizarLigaDto(" Liga Nova ", " Nova "));

        Assert.Equal(liga.Id, resultado.Id);
        Assert.Equal("Liga Nova", liga.Nome);
        Assert.Equal("Nova", liga.Descricao);
        Assert.Equal(1, cenario.UnidadeTrabalho.Salvamentos);
    }

    [Fact]
    public async Task AtualizarAsync_LigaInexistente_Bloqueia()
    {
        var cenario = new Cenario();

        var excecao = await Assert.ThrowsAsync<EntidadeNaoEncontradaException>(() =>
            cenario.Servico.AtualizarAsync(Guid.NewGuid(), new AtualizarLigaDto("Liga", null)));

        Assert.Equal("Liga não encontrada.", excecao.Message);
    }

    [Fact]
    public async Task AtualizarAsync_NomeDuplicadoDeOutraLiga_Bloqueia()
    {
        var cenario = new Cenario();
        var liga = new Liga { Nome = "Liga Antiga" };
        cenario.Ligas.Itens.Add(liga);
        cenario.Ligas.Itens.Add(new Liga { Nome = "Liga Existente" });

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.AtualizarAsync(liga.Id, new AtualizarLigaDto("Liga Existente", null)));

        Assert.Equal("Já existe uma liga cadastrada com este nome.", excecao.Message);
        Assert.Equal("Liga Antiga", liga.Nome);
        Assert.Equal(0, cenario.UnidadeTrabalho.Salvamentos);
    }

    [Fact]
    public async Task RemoverAsync_LigaExistente_RemoveESalva()
    {
        var cenario = new Cenario();
        var liga = new Liga { Nome = "Liga Removida" };
        cenario.Ligas.Itens.Add(liga);

        await cenario.Servico.RemoverAsync(liga.Id);

        Assert.Empty(cenario.Ligas.Itens);
        Assert.Equal(1, cenario.UnidadeTrabalho.Salvamentos);
    }

    [Fact]
    public async Task RemoverAsync_LigaInexistente_Bloqueia()
    {
        var cenario = new Cenario();

        var excecao = await Assert.ThrowsAsync<EntidadeNaoEncontradaException>(() =>
            cenario.Servico.RemoverAsync(Guid.NewGuid()));

        Assert.Equal("Liga não encontrada.", excecao.Message);
        Assert.Equal(0, cenario.UnidadeTrabalho.Salvamentos);
    }

    private sealed class Cenario
    {
        public Cenario()
        {
            Servico = new LigaServico(Ligas, UnidadeTrabalho);
        }

        public LigaServico Servico { get; }
        public LigaRepositorioMemoria Ligas { get; } = new();
        public UnidadeTrabalhoStub UnidadeTrabalho { get; } = new();
    }

    private sealed class LigaRepositorioMemoria : ILigaRepositorio
    {
        public List<Liga> Itens { get; } = [];

        public Task<IReadOnlyList<Liga>> ListarAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Liga>>(Itens.ToList());

        public Task<Liga?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Itens.FirstOrDefault(x => x.Id == id));

        public Task<Liga?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default)
            => Task.FromResult(Itens.FirstOrDefault(x => x.Nome == nome));

        public Task AdicionarAsync(Liga liga, CancellationToken cancellationToken = default)
        {
            Itens.Add(liga);
            return Task.CompletedTask;
        }

        public void Atualizar(Liga liga)
        {
            if (!Itens.Contains(liga))
            {
                Itens.Add(liga);
            }
        }

        public void Remover(Liga liga) => Itens.Remove(liga);
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
}
