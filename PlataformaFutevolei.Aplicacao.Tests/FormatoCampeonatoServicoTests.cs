using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Aplicacao.Servicos;
using PlataformaFutevolei.Aplicacao.Utilitarios;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;
using Xunit;

namespace PlataformaFutevolei.Aplicacao.Tests;

public class FormatoCampeonatoServicoTests
{
    [Fact]
    public async Task ListarAsync_SemFormatos_CriaPadroesEOrdenaPrimeiro()
    {
        var cenario = new Cenario();

        var formatos = await cenario.Servico.ListarAsync();

        Assert.Equal(FormatosCampeonatoPadrao.Lista.Count, formatos.Count);
        Assert.All(formatos, formato => Assert.True(formato.EhPadrao));
        Assert.Equal(1, cenario.UnidadeTrabalho.QuantidadeSalvamentos);
    }

    [Fact]
    public async Task ListarAsync_ComFormatoPersonalizado_RetornaPadroesAntesDoPersonalizado()
    {
        var cenario = new Cenario();
        cenario.Formatos.Itens.Add(CriarFormato("Formato Z", TipoFormatoCampeonato.Chave));

        var formatos = await cenario.Servico.ListarAsync();

        Assert.True(formatos.Take(3).All(x => x.EhPadrao));
        Assert.Equal("Formato Z", formatos.Last().Nome);
    }

    [Fact]
    public async Task CriarAsync_PontosCorridos_NormalizaCamposNaoAplicaveis()
    {
        var cenario = new Cenario();

        var formato = await cenario.Servico.CriarAsync(new CriarFormatoCampeonatoDto(
            "  Liga Corrida  ",
            "  Todos contra todos  ",
            TipoFormatoCampeonato.PontosCorridos,
            true,
            QuantidadeGrupos: 4,
            ClassificadosPorGrupo: 2,
            GeraMataMataAposGrupos: true,
            TurnoEVolta: true,
            TipoChave: "Simples",
            QuantidadeDerrotasParaEliminacao: 1,
            PermiteCabecaDeChave: true,
            DisputaTerceiroLugar: true));

        Assert.Equal("Liga Corrida", formato.Nome);
        Assert.Equal("Todos contra todos", formato.Descricao);
        Assert.Equal(TipoFormatoCampeonato.PontosCorridos, formato.TipoFormato);
        Assert.True(formato.TurnoEVolta);
        Assert.Null(formato.QuantidadeGrupos);
        Assert.Null(formato.ClassificadosPorGrupo);
        Assert.False(formato.GeraMataMataAposGrupos);
        Assert.Null(formato.TipoChave);
        Assert.Null(formato.QuantidadeDerrotasParaEliminacao);
        Assert.False(formato.PermiteCabecaDeChave);
        Assert.False(formato.DisputaTerceiroLugar);
    }

    [Fact]
    public async Task CriarAsync_FaseDeGruposComMataMata_ValidaECriaCampos()
    {
        var cenario = new Cenario();

        var formato = await cenario.Servico.CriarAsync(new CriarFormatoCampeonatoDto(
            "Fase grupos",
            null,
            TipoFormatoCampeonato.FaseDeGrupos,
            true,
            QuantidadeGrupos: 3,
            ClassificadosPorGrupo: 2,
            GeraMataMataAposGrupos: true,
            TurnoEVolta: false,
            TipoChave: null,
            QuantidadeDerrotasParaEliminacao: null,
            PermiteCabecaDeChave: false,
            DisputaTerceiroLugar: false));

        Assert.Equal(3, formato.QuantidadeGrupos);
        Assert.Equal(2, formato.ClassificadosPorGrupo);
        Assert.True(formato.GeraMataMataAposGrupos);
    }

    [Fact]
    public async Task CriarAsync_Chave_ValidaECriaCampos()
    {
        var cenario = new Cenario();

        var formato = await cenario.Servico.CriarAsync(new CriarFormatoCampeonatoDto(
            "Chave dupla",
            null,
            TipoFormatoCampeonato.Chave,
            true,
            null,
            null,
            false,
            false,
            "  Dupla eliminação  ",
            2,
            true,
            true));

        Assert.Equal("Dupla eliminação", formato.TipoChave);
        Assert.Equal(2, formato.QuantidadeDerrotasParaEliminacao);
        Assert.True(formato.PermiteCabecaDeChave);
        Assert.True(formato.DisputaTerceiroLugar);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CriarAsync_NomeObrigatorio_Bloqueia(string nome)
    {
        var cenario = new Cenario();

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.CriarAsync(new CriarFormatoCampeonatoDto(
                nome,
                null,
                TipoFormatoCampeonato.PontosCorridos,
                true,
                null,
                null,
                false,
                false,
                null,
                null,
                false,
                false)));

        Assert.Equal("Nome do formato é obrigatório.", excecao.Message);
    }

    [Fact]
    public async Task CriarAsync_NomeDuplicado_Bloqueia()
    {
        var cenario = new Cenario();
        cenario.Formatos.Itens.Add(CriarFormato("Formato repetido", TipoFormatoCampeonato.PontosCorridos));

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.CriarAsync(new CriarFormatoCampeonatoDto(
                "Formato repetido",
                null,
                TipoFormatoCampeonato.PontosCorridos,
                true,
                null,
                null,
                false,
                false,
                null,
                null,
                false,
                false)));

        Assert.Equal("Já existe um formato cadastrado com este nome.", excecao.Message);
    }

    [Fact]
    public async Task CriarAsync_FaseDeGruposSemQuantidade_Bloqueia()
    {
        var cenario = new Cenario();

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.CriarAsync(new CriarFormatoCampeonatoDto(
                "Fase inválida",
                null,
                TipoFormatoCampeonato.FaseDeGrupos,
                true,
                null,
                2,
                true,
                false,
                null,
                null,
                false,
                false)));

        Assert.Equal("Quantidade de grupos deve ser maior que zero para fase de grupos.", excecao.Message);
    }

    [Fact]
    public async Task CriarAsync_ChaveSemTipo_Bloqueia()
    {
        var cenario = new Cenario();

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.CriarAsync(new CriarFormatoCampeonatoDto(
                "Chave inválida",
                null,
                TipoFormatoCampeonato.Chave,
                true,
                null,
                null,
                false,
                false,
                " ",
                1,
                false,
                false)));

        Assert.Equal("Tipo da chave é obrigatório para formato em chave.", excecao.Message);
    }

    [Fact]
    public async Task CriarAsync_ChaveComDerrotasInvalidas_Bloqueia()
    {
        var cenario = new Cenario();

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.CriarAsync(new CriarFormatoCampeonatoDto(
                "Chave inválida",
                null,
                TipoFormatoCampeonato.Chave,
                true,
                null,
                null,
                false,
                false,
                "Simples",
                3,
                false,
                false)));

        Assert.Equal("Quantidade de derrotas para eliminação deve ser 1 ou 2.", excecao.Message);
    }

    [Fact]
    public async Task AtualizarAsync_FormatoPersonalizado_AtualizaCampos()
    {
        var cenario = new Cenario();
        var formato = CriarFormato("Original", TipoFormatoCampeonato.PontosCorridos);
        cenario.Formatos.Itens.Add(formato);

        var atualizado = await cenario.Servico.AtualizarAsync(formato.Id, new AtualizarFormatoCampeonatoDto(
            "Atualizado",
            "Descrição",
            TipoFormatoCampeonato.Chave,
            false,
            null,
            null,
            false,
            false,
            "Simples",
            1,
            false,
            true));

        Assert.Equal("Atualizado", atualizado.Nome);
        Assert.False(atualizado.Ativo);
        Assert.Equal(TipoFormatoCampeonato.Chave, atualizado.TipoFormato);
        Assert.Equal("Simples", atualizado.TipoChave);
        Assert.Equal(1, atualizado.QuantidadeDerrotasParaEliminacao);
        Assert.True(atualizado.DisputaTerceiroLugar);
    }

    [Fact]
    public async Task AtualizarAsync_FormatoPadrao_Bloqueia()
    {
        var cenario = new Cenario();
        await cenario.Servico.ListarAsync();
        var formatoPadrao = cenario.Formatos.Itens.First(x => FormatosCampeonatoPadrao.EhPadrao(x.Nome));

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.AtualizarAsync(formatoPadrao.Id, new AtualizarFormatoCampeonatoDto(
                "Novo nome",
                null,
                TipoFormatoCampeonato.PontosCorridos,
                true,
                null,
                null,
                false,
                false,
                null,
                null,
                false,
                false)));

        Assert.Equal("Formatos padrão não podem ser alterados.", excecao.Message);
    }

    [Fact]
    public async Task RemoverAsync_FormatoPersonalizado_Remove()
    {
        var cenario = new Cenario();
        var formato = CriarFormato("Remover", TipoFormatoCampeonato.PontosCorridos);
        cenario.Formatos.Itens.Add(formato);

        await cenario.Servico.RemoverAsync(formato.Id);

        Assert.DoesNotContain(cenario.Formatos.Itens, x => x.Id == formato.Id);
    }

    [Fact]
    public async Task RemoverAsync_FormatoPadrao_Bloqueia()
    {
        var cenario = new Cenario();
        await cenario.Servico.ListarAsync();
        var formatoPadrao = cenario.Formatos.Itens.First(x => FormatosCampeonatoPadrao.EhPadrao(x.Nome));

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.RemoverAsync(formatoPadrao.Id));

        Assert.Equal("Formatos padrão não podem ser excluídos.", excecao.Message);
    }

    [Fact]
    public async Task ObterPorIdAsync_FormatoInexistente_Bloqueia()
    {
        var cenario = new Cenario();

        await Assert.ThrowsAsync<EntidadeNaoEncontradaException>(() =>
            cenario.Servico.ObterPorIdAsync(Guid.NewGuid()));
    }

    private static FormatoCampeonato CriarFormato(string nome, TipoFormatoCampeonato tipoFormato)
        => new()
        {
            Nome = nome,
            TipoFormato = tipoFormato,
            Ativo = true
        };

    private sealed class Cenario
    {
        public Cenario()
        {
            Formatos = new FormatoCampeonatoRepositorioStub();
            UnidadeTrabalho = new UnidadeTrabalhoStub();
            Servico = new FormatoCampeonatoServico(Formatos, UnidadeTrabalho);
        }

        public FormatoCampeonatoRepositorioStub Formatos { get; }
        public UnidadeTrabalhoStub UnidadeTrabalho { get; }
        public FormatoCampeonatoServico Servico { get; }
    }

    private sealed class FormatoCampeonatoRepositorioStub : IFormatoCampeonatoRepositorio
    {
        public List<FormatoCampeonato> Itens { get; } = [];

        public Task<IReadOnlyList<FormatoCampeonato>> ListarAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<FormatoCampeonato>>(Itens);

        public Task<FormatoCampeonato?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Itens.FirstOrDefault(x => x.Id == id));

        public Task<FormatoCampeonato?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default)
            => Task.FromResult(Itens.FirstOrDefault(x => string.Equals(x.Nome, nome, StringComparison.OrdinalIgnoreCase)));

        public Task AdicionarAsync(FormatoCampeonato formato, CancellationToken cancellationToken = default)
        {
            Itens.Add(formato);
            return Task.CompletedTask;
        }

        public void Atualizar(FormatoCampeonato formato)
        {
        }

        public void Remover(FormatoCampeonato formato)
        {
            Itens.Remove(formato);
        }
    }

    private sealed class UnidadeTrabalhoStub : IUnidadeTrabalho
    {
        public int QuantidadeSalvamentos { get; private set; }

        public Task<int> SalvarAlteracoesAsync(CancellationToken cancellationToken = default)
        {
            QuantidadeSalvamentos++;
            return Task.FromResult(1);
        }

        public Task ExecutarEmTransacaoAsync(Func<CancellationToken, Task> operacao, CancellationToken cancellationToken = default)
            => operacao(cancellationToken);
    }
}
