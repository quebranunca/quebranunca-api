using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Servicos;
using PlataformaFutevolei.Dominio.Entidades;
using Xunit;

namespace PlataformaFutevolei.Aplicacao.Tests;

public class ConsolidacaoAtletaServicoTests
{
    [Fact]
    public async Task ConsolidarCandidatosAsync_MesmoEmail_EscolheAtletaComMaisPartidas()
    {
        var atletaSemJogos = CriarAtleta("Atleta Sem Jogos", "atleta@teste.com");
        var atletaComJogos = CriarAtleta("Atleta Com Jogos", "atleta@teste.com");
        var cenario = Cenario.Criar([atletaSemJogos, atletaComJogos]);
        cenario.DefinirMetricas(atletaSemJogos, partidas: 0, historico: 1);
        cenario.DefinirMetricas(atletaComJogos, partidas: 3, historico: 1);

        var vencedor = await cenario.Servico.ConsolidarCandidatosAsync(
            [atletaSemJogos, atletaComJogos],
            emailNormalizado: " atleta@teste.com ");

        Assert.Equal(atletaComJogos.Id, vencedor.Id);
        Assert.Contains(cenario.Consolidacao.Transferencias, x =>
            x.VencedorId == atletaComJogos.Id && x.PerdedorId == atletaSemJogos.Id);
    }

    [Fact]
    public async Task ConsolidarCandidatosAsync_AtletaVinculadoConfiavel_TemPrioridade()
    {
        var atletaVinculado = CriarAtleta("Atleta Vinculado", "atleta@teste.com");
        var atletaComJogos = CriarAtleta("Atleta Com Jogos", "atleta@teste.com");
        var cenario = Cenario.Criar([atletaVinculado, atletaComJogos]);
        cenario.DefinirMetricas(atletaVinculado, partidas: 0, historico: 0, possuiUsuario: true);
        cenario.DefinirMetricas(atletaComJogos, partidas: 10, historico: 10);

        var vencedor = await cenario.Servico.ConsolidarCandidatosAsync(
            [atletaVinculado, atletaComJogos],
            atletaVinculado.Id,
            "atleta@teste.com");

        Assert.Equal(atletaVinculado.Id, vencedor.Id);
    }

    [Fact]
    public async Task ConsolidarCandidatosAsync_SemVinculoConfiavel_NaoPriorizaUsuarioApenasPorEstarVinculado()
    {
        var atletaVinculadoNaoConfiavel = CriarAtleta("Atleta Vinculado", "atleta@teste.com");
        var atletaComHistorico = CriarAtleta("Atleta Com Historico", "atleta@teste.com");
        var cenario = Cenario.Criar([atletaVinculadoNaoConfiavel, atletaComHistorico]);
        cenario.DefinirMetricas(atletaVinculadoNaoConfiavel, partidas: 0, historico: 0, possuiUsuario: true);
        cenario.DefinirMetricas(atletaComHistorico, partidas: 8, historico: 8);

        var vencedor = await cenario.Servico.ConsolidarCandidatosAsync(
            [atletaVinculadoNaoConfiavel, atletaComHistorico],
            atletaVinculadoConfiavelId: null,
            emailNormalizado: "atleta@teste.com");

        Assert.Equal(atletaComHistorico.Id, vencedor.Id);
    }

    [Fact]
    public async Task ConsolidarCandidatosAsync_PartidasEmpatadas_EscolheMaiorHistoricoGeral()
    {
        var atletaMenosHistorico = CriarAtleta("Menos Historico", "atleta@teste.com");
        var atletaMaisHistorico = CriarAtleta("Mais Historico", "atleta@teste.com");
        var cenario = Cenario.Criar([atletaMenosHistorico, atletaMaisHistorico]);
        cenario.DefinirMetricas(atletaMenosHistorico, partidas: 2, historico: 3);
        cenario.DefinirMetricas(atletaMaisHistorico, partidas: 2, historico: 9);

        var vencedor = await cenario.Servico.ConsolidarCandidatosAsync(
            [atletaMenosHistorico, atletaMaisHistorico],
            emailNormalizado: "atleta@teste.com");

        Assert.Equal(atletaMaisHistorico.Id, vencedor.Id);
    }

    [Fact]
    public async Task ConsolidarCandidatosAsync_SemPartidasOuHistorico_EscolheMaisAntigo()
    {
        var atletaMaisAntigo = CriarAtleta("Mais Antigo", "atleta@teste.com");
        var atletaMaisNovo = CriarAtleta("Mais Novo", "atleta@teste.com");
        var dataAntiga = DateTime.UtcNow.AddDays(-30);
        var dataNova = DateTime.UtcNow.AddDays(-1);
        var cenario = Cenario.Criar([atletaMaisNovo, atletaMaisAntigo]);
        cenario.DefinirMetricas(atletaMaisAntigo, partidas: 0, historico: 0, dataCriacao: dataAntiga);
        cenario.DefinirMetricas(atletaMaisNovo, partidas: 0, historico: 0, dataCriacao: dataNova);

        var vencedor = await cenario.Servico.ConsolidarCandidatosAsync(
            [atletaMaisNovo, atletaMaisAntigo],
            emailNormalizado: "atleta@teste.com");

        Assert.Equal(atletaMaisAntigo.Id, vencedor.Id);
    }

    [Fact]
    public async Task ConsolidarCandidatosAsync_MesmosCandidatosRepetidos_EhIdempotente()
    {
        var atleta = CriarAtleta("Atleta", "atleta@teste.com");
        var cenario = Cenario.Criar([atleta]);
        cenario.DefinirMetricas(atleta, partidas: 1, historico: 1);

        var vencedor = await cenario.Servico.ConsolidarCandidatosAsync(
            [atleta, atleta],
            emailNormalizado: "atleta@teste.com");

        Assert.Equal(atleta.Id, vencedor.Id);
        Assert.Empty(cenario.Consolidacao.Transferencias);
    }

    [Fact]
    public async Task ConsolidarCandidatosAsync_CandidatoUnicoChamadoDuasVezes_NaoGeraEfeitoColateral()
    {
        var atleta = CriarAtleta("Atleta", "atleta@teste.com");
        var cenario = Cenario.Criar([atleta]);
        cenario.DefinirMetricas(atleta, partidas: 1, historico: 1);

        var primeiro = await cenario.Servico.ConsolidarCandidatosAsync([atleta], emailNormalizado: "atleta@teste.com");
        var segundo = await cenario.Servico.ConsolidarCandidatosAsync([atleta], emailNormalizado: "atleta@teste.com");

        Assert.Equal(atleta.Id, primeiro.Id);
        Assert.Equal(atleta.Id, segundo.Id);
        Assert.Empty(cenario.Consolidacao.Transferencias);
    }

    [Fact]
    public async Task ConsolidarCandidatosAsync_ExecutaTransferenciaEmTransacao()
    {
        var vencedor = CriarAtleta("Vencedor", "atleta@teste.com");
        var perdedor = CriarAtleta("Perdedor", "atleta@teste.com");
        var cenario = Cenario.Criar([vencedor, perdedor]);
        cenario.DefinirMetricas(vencedor, partidas: 1, historico: 1);
        cenario.DefinirMetricas(perdedor, partidas: 0, historico: 0);

        await cenario.Servico.ConsolidarCandidatosAsync([vencedor, perdedor], emailNormalizado: "atleta@teste.com");

        Assert.Equal(1, cenario.UnidadeTrabalho.TransacoesExecutadas);
        Assert.Equal(1, cenario.UnidadeTrabalho.Salvamentos);
    }

    [Fact]
    public async Task ConsolidarCandidatosAsync_ErroNaTransferencia_AbortaSemSalvar()
    {
        var vencedor = CriarAtleta("Vencedor", "atleta@teste.com");
        var perdedor = CriarAtleta("Perdedor", "atleta@teste.com");
        var cenario = Cenario.Criar([vencedor, perdedor]);
        cenario.DefinirMetricas(vencedor, partidas: 1, historico: 1);
        cenario.DefinirMetricas(perdedor, partidas: 0, historico: 0);
        cenario.Consolidacao.ExcecaoAoTransferir = new RegraNegocioException("dupla ficaria com o mesmo atleta duas vezes");

        var erro = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.ConsolidarCandidatosAsync([vencedor, perdedor], emailNormalizado: "atleta@teste.com"));

        Assert.Contains("dupla", erro.Message);
        Assert.Equal(1, cenario.UnidadeTrabalho.TransacoesExecutadas);
        Assert.Equal(0, cenario.UnidadeTrabalho.Salvamentos);
    }

    [Fact]
    public async Task ConsolidarDuplicadosPorEmailAsync_NaoEscolheApenasMaisAntigo()
    {
        var maisAntigoSemJogos = CriarAtleta("Mais Antigo", "atleta@teste.com");
        var maisNovoComJogos = CriarAtleta("Mais Novo", "atleta@teste.com");
        var cenario = Cenario.Criar([maisAntigoSemJogos, maisNovoComJogos]);
        cenario.Consolidacao.GruposDuplicados.Add([maisAntigoSemJogos, maisNovoComJogos]);
        cenario.DefinirMetricas(maisAntigoSemJogos, partidas: 0, historico: 0, dataCriacao: DateTime.UtcNow.AddDays(-10));
        cenario.DefinirMetricas(maisNovoComJogos, partidas: 2, historico: 2, dataCriacao: DateTime.UtcNow);

        var resumo = await cenario.Servico.ConsolidarDuplicadosPorEmailAsync();
        var grupo = Assert.Single(resumo.Grupos);

        Assert.Equal(maisNovoComJogos.Id, grupo.AtletaPrincipalId);
        Assert.Contains(maisAntigoSemJogos.Id, grupo.AtletasDuplicadosIds);
    }

    [Fact]
    public async Task ConsolidarDuplicadosPorEmailAsync_ReportaTransferenciasDeVinculos()
    {
        var vencedor = CriarAtleta("Vencedor", "atleta@teste.com");
        var perdedor = CriarAtleta("Perdedor", "atleta@teste.com");
        var cenario = Cenario.Criar([vencedor, perdedor]);
        cenario.Consolidacao.GruposDuplicados.Add([vencedor, perdedor]);
        cenario.DefinirMetricas(vencedor, partidas: 2, historico: 2);
        cenario.DefinirMetricas(perdedor, partidas: 0, historico: 1);
        cenario.Consolidacao.ContadoresTransferencia = new SaneamentoAtletasEmailContadoresDto(
            DuplasAtualizadas: 1,
            DuplasConsolidadas: 1,
            PartidasAtualizadas: 2,
            InscricoesAtualizadas: 1,
            InscricoesConsolidadas: 1,
            GruposAtualizados: 1,
            GruposConsolidados: 1,
            AprovacoesAtualizadas: 1,
            AprovacoesConsolidadas: 1,
            PendenciasAtualizadas: 1,
            ConvitesAtualizados: 1,
            UsuariosAtualizados: 1,
            AtletasRemovidos: 1);

        var resumo = await cenario.Servico.ConsolidarDuplicadosPorEmailAsync();
        var contadores = Assert.Single(resumo.Grupos).RegistrosMigrados;

        Assert.Equal(1, contadores.DuplasAtualizadas);
        Assert.Equal(1, contadores.GruposConsolidados);
        Assert.Equal(1, contadores.PendenciasAtualizadas);
        Assert.Equal(1, contadores.ConvitesAtualizados);
        Assert.Equal(1, contadores.UsuariosAtualizados);
    }

    [Fact]
    public async Task ConsolidarDuplicadosPorEmailAsync_ReportaDuplasEquivalentesGruposPendenciasEConvitesConsolidados()
    {
        var vencedor = CriarAtleta("Vencedor", "atleta@teste.com");
        var perdedor = CriarAtleta("Perdedor", "atleta@teste.com");
        var cenario = Cenario.Criar([vencedor, perdedor]);
        cenario.Consolidacao.GruposDuplicados.Add([vencedor, perdedor]);
        cenario.DefinirMetricas(vencedor, partidas: 4, historico: 4);
        cenario.DefinirMetricas(perdedor, partidas: 1, historico: 3);
        cenario.Consolidacao.ContadoresTransferencia = new SaneamentoAtletasEmailContadoresDto(
            DuplasAtualizadas: 0,
            DuplasConsolidadas: 1,
            PartidasAtualizadas: 3,
            InscricoesAtualizadas: 0,
            InscricoesConsolidadas: 1,
            GruposAtualizados: 0,
            GruposConsolidados: 1,
            AprovacoesAtualizadas: 0,
            AprovacoesConsolidadas: 1,
            PendenciasAtualizadas: 1,
            ConvitesAtualizados: 1,
            UsuariosAtualizados: 0,
            AtletasRemovidos: 1);

        var resumo = await cenario.Servico.ConsolidarDuplicadosPorEmailAsync();
        var contadores = Assert.Single(resumo.Grupos).RegistrosMigrados;

        Assert.Equal(1, contadores.DuplasConsolidadas);
        Assert.Equal(3, contadores.PartidasAtualizadas);
        Assert.Equal(1, contadores.GruposConsolidados);
        Assert.Equal(1, contadores.PendenciasAtualizadas);
        Assert.Equal(1, contadores.ConvitesAtualizados);
    }

    [Fact]
    public async Task ConsolidarCandidatosAsync_DoisUsuariosEnvolvidos_BloqueiaConsolidacao()
    {
        var atletaComUsuario = CriarAtleta("Atleta 1", "atleta@teste.com");
        var outroAtletaComUsuario = CriarAtleta("Atleta 2", "atleta@teste.com");
        var cenario = Cenario.Criar([atletaComUsuario, outroAtletaComUsuario]);
        cenario.DefinirMetricas(atletaComUsuario, partidas: 1, historico: 1, possuiUsuario: true);
        cenario.DefinirMetricas(outroAtletaComUsuario, partidas: 0, historico: 0, possuiUsuario: true);
        cenario.Consolidacao.ExcecaoAoTransferir = new RegraNegocioException("conflito de usuários vinculados");

        var erro = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.ConsolidarCandidatosAsync(
                [atletaComUsuario, outroAtletaComUsuario],
                atletaVinculadoConfiavelId: atletaComUsuario.Id,
                emailNormalizado: "atleta@teste.com"));

        Assert.Contains("usuários", erro.Message);
        Assert.Equal(0, cenario.UnidadeTrabalho.Salvamentos);
    }

    private static Atleta CriarAtleta(string nome, string email)
    {
        return new Atleta
        {
            Nome = nome,
            Apelido = nome,
            Email = email
        };
    }

    private sealed class Cenario
    {
        private Cenario(IReadOnlyList<Atleta> atletas)
        {
            Atletas = new AtletaRepositorioMemoria(atletas);
            Consolidacao = new ConsolidacaoAtletaRepositorioMemoria();
            UnidadeTrabalho = new UnidadeTrabalhoMemoria();
            Servico = new ConsolidacaoAtletaServico(Atletas, Consolidacao, UnidadeTrabalho);
        }

        public AtletaRepositorioMemoria Atletas { get; }
        public ConsolidacaoAtletaRepositorioMemoria Consolidacao { get; }
        public UnidadeTrabalhoMemoria UnidadeTrabalho { get; }
        public ConsolidacaoAtletaServico Servico { get; }

        public static Cenario Criar(IReadOnlyList<Atleta> atletas) => new(atletas);

        public void DefinirMetricas(
            Atleta atleta,
            int partidas,
            int historico,
            bool possuiUsuario = false,
            DateTime? dataCriacao = null)
        {
            Consolidacao.Metricas[atleta.Id] = new ConsolidacaoAtletaMetricasDto(
                atleta.Id,
                possuiUsuario,
                partidas,
                historico,
                0,
                0,
                0,
                0,
                0,
                dataCriacao ?? atleta.DataCriacao);
        }
    }

    private sealed class ConsolidacaoAtletaRepositorioMemoria : IConsolidacaoAtletaRepositorio
    {
        public Dictionary<Guid, ConsolidacaoAtletaMetricasDto> Metricas { get; } = [];
        public List<(Guid VencedorId, Guid PerdedorId)> Transferencias { get; } = [];
        public List<IReadOnlyList<Atleta>> GruposDuplicados { get; } = [];
        public SaneamentoAtletasEmailContadoresDto ContadoresTransferencia { get; set; } =
            new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1);
        public Exception? ExcecaoAoTransferir { get; set; }

        public Task<IDictionary<Guid, ConsolidacaoAtletaMetricasDto>> ObterMetricasAsync(
            IEnumerable<Guid> atletaIds,
            CancellationToken cancellationToken = default)
        {
            IDictionary<Guid, ConsolidacaoAtletaMetricasDto> resultado = atletaIds
                .Distinct()
                .Where(Metricas.ContainsKey)
                .ToDictionary(x => x, x => Metricas[x]);
            return Task.FromResult(resultado);
        }

        public Task<IReadOnlyList<IReadOnlyList<Atleta>>> ListarDuplicadosPorEmailAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<IReadOnlyList<Atleta>>>(GruposDuplicados);
        }

        public Task<SaneamentoAtletasEmailContadoresDto> TransferirVinculosAsync(
            Guid atletaVencedorId,
            Guid atletaPerdedorId,
            CancellationToken cancellationToken = default)
        {
            if (ExcecaoAoTransferir is not null)
            {
                throw ExcecaoAoTransferir;
            }

            Transferencias.Add((atletaVencedorId, atletaPerdedorId));
            return Task.FromResult(ContadoresTransferencia);
        }
    }

    private sealed class AtletaRepositorioMemoria(IReadOnlyList<Atleta> atletas) : IAtletaRepositorio
    {
        public Task<IReadOnlyList<Atleta>> ListarAsync(CancellationToken cancellationToken = default) => Task.FromResult(atletas);
        public Task<int> ContarAsync(CancellationToken cancellationToken = default) => Task.FromResult(atletas.Count);
        public Task<IReadOnlyList<Atleta>> ListarComEmailEmPartidasSemUsuarioAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Atleta>>([]);
        public Task<IReadOnlyList<Atleta>> ListarInscritosPorOrganizadorAsync(Guid usuarioOrganizadorId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Atleta>>([]);
        public Task<bool> PertenceAoOrganizadorAsync(Guid atletaId, Guid usuarioOrganizadorId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<IReadOnlyList<Atleta>> BuscarAsync(string? termo, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Atleta>>([]);
        public Task<IDictionary<Guid, int>> ContarPartidasPorAtletasAsync(IEnumerable<Guid> atletaIds, CancellationToken cancellationToken = default) => Task.FromResult<IDictionary<Guid, int>>(new Dictionary<Guid, int>());
        public Task<IReadOnlyList<Atleta>> BuscarSugestoesPorCompeticaoAsync(Guid competicaoId, string termo, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Atleta>>([]);
        public Task<Atleta?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(atletas.FirstOrDefault(x => x.Id == id));
        public Task<Atleta?> ObterPorIdParaAtualizacaoAsync(Guid id, CancellationToken cancellationToken = default) => ObterPorIdAsync(id, cancellationToken);
        public Task<Atleta?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default) => Task.FromResult(atletas.FirstOrDefault(x => x.Nome == nome));
        public Task<IReadOnlyList<Atleta>> ListarPorNomeAsync(string nome, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Atleta>>(atletas.Where(x => x.Nome == nome).ToList());
        public Task<IReadOnlyList<Atleta>> ListarPorEmailAsync(string email, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Atleta>>(atletas.Where(x => string.Equals(x.Email, email.Trim(), StringComparison.OrdinalIgnoreCase)).ToList());
        public Task AdicionarAsync(Atleta atleta, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AdicionarMedidasAsync(AtletaMedidas medidas, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Atualizar(Atleta atleta) { }
        public void AtualizarMedidas(AtletaMedidas medidas) { }
        public void Remover(Atleta atleta) { }
    }

    private sealed class UnidadeTrabalhoMemoria : IUnidadeTrabalho
    {
        public int Salvamentos { get; private set; }
        public int TransacoesExecutadas { get; private set; }

        public Task<int> SalvarAlteracoesAsync(CancellationToken cancellationToken = default)
        {
            Salvamentos++;
            return Task.FromResult(1);
        }

        public async Task ExecutarEmTransacaoAsync(Func<CancellationToken, Task> operacao, CancellationToken cancellationToken = default)
        {
            TransacoesExecutadas++;
            await operacao(cancellationToken);
        }
    }
}
