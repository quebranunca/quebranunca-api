using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Seguranca;
using PlataformaFutevolei.Aplicacao.Servicos;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;
using PlataformaFutevolei.Infraestrutura.Persistencia;
using PlataformaFutevolei.Infraestrutura.Repositorios;
using Xunit;

namespace PlataformaFutevolei.Integracao.Tests;

[Collection(nameof(PostgresIntegracaoCollection))]
public class PontuacaoBeneficioIntegracaoTests(PostgresIntegracaoFixture fixture) : IAsyncLifetime
{
    private readonly string prefixo = $"teste-consolidacao-atleta-pontos-qn-{Guid.NewGuid():N}";

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await fixture.LimparDadosAsync(prefixo);
    }

    [Fact]
    public async Task MigrationPermiteSaldoNegativoEMantemConstraintDosTotais()
    {
        Guid atletaId;
        await using (var dbContext = fixture.CriarContexto())
        {
            var usuario = CriarUsuarioComSaldo(dbContext, "saldo-negativo", 0);
            atletaId = usuario.AtletaId!.Value;
            dbContext.PontuacoesBeneficiosAtletas.Local.Single(x => x.AtletaId == atletaId).SaldoAtual = -20;
            await dbContext.SaveChangesAsync();
        }

        await using (var verificacao = fixture.CriarContexto())
        {
            var saldo = await verificacao.PontuacoesBeneficiosAtletas.SingleAsync(x => x.AtletaId == atletaId);
            Assert.Equal(-20, saldo.SaldoAtual);
        }

        await using (var totaisInvalidos = fixture.CriarContexto())
        {
            var saldo = await totaisInvalidos.PontuacoesBeneficiosAtletas.SingleAsync(x => x.AtletaId == atletaId);
            saldo.TotalAcumulado = -1;
            await Assert.ThrowsAsync<DbUpdateException>(() => totaisInvalidos.SaveChangesAsync());
        }
    }

    [Fact]
    public async Task Reconciliacao_DryRunNaoAlteraEAplicacaoCorrigeProjecaoComIdempotencia()
    {
        Usuario usuario;
        await using (var dbContext = fixture.CriarContexto())
        {
            usuario = CriarUsuarioComSaldo(dbContext, "reconciliacao", 99);
            usuario.Perfil = PerfilUsuario.Administrador;
            dbContext.Add(new ExtratoPontuacaoBeneficio
            {
                AtletaId = usuario.AtletaId!.Value,
                TipoEvento = TipoEventoPontuacaoBeneficio.PerfilCompleto,
                Pontos = 10,
                Descricao = "Perfil completo",
                Origem = "Perfil",
                ChaveIdempotencia = $"PERFIL_COMPLETO:ATLETA:{usuario.AtletaId}"
            });
            await dbContext.SaveChangesAsync();
        }

        await using (var dryRunContexto = fixture.CriarContexto())
        {
            var dryRun = await CriarServico(dryRunContexto, usuario).ReconciliarAsync(true, usuario.AtletaId);
            Assert.Equal(1, dryRun.AtletasComDivergencia);
        }
        await using (var verificacaoDryRun = fixture.CriarContexto())
        {
            Assert.Equal(99, (await verificacaoDryRun.PontuacoesBeneficiosAtletas.SingleAsync(x => x.AtletaId == usuario.AtletaId)).SaldoAtual);
        }

        await using (var aplicacaoContexto = fixture.CriarContexto())
        {
            var servico = CriarServico(aplicacaoContexto, usuario);
            var primeira = await servico.ReconciliarAsync(false, usuario.AtletaId);
            var segunda = await servico.ReconciliarAsync(false, usuario.AtletaId);
            Assert.Equal(1, primeira.ProjecoesAtualizadas);
            Assert.Equal(0, segunda.AtletasCorrigidos);
        }
        await using var verificacao = fixture.CriarContexto();
        var saldo = await verificacao.PontuacoesBeneficiosAtletas.SingleAsync(x => x.AtletaId == usuario.AtletaId);
        Assert.Equal(10, saldo.SaldoAtual);
        Assert.Equal(10, saldo.TotalAcumulado);
        Assert.Equal(0, saldo.TotalResgatado);
        Assert.Single(await verificacao.ExtratosPontuacaoBeneficio.Where(x => x.AtletaId == usuario.AtletaId).ToListAsync());
    }

    [Fact]
    public async Task Reconciliacao_ApenasUmaAplicacaoAdquireAdvisoryLock()
    {
        await using var primeiroContexto = fixture.CriarContexto();
        await using var segundoContexto = fixture.CriarContexto();
        var primeiro = new PontuacaoBeneficioRepositorio(primeiroContexto);
        var segundo = new PontuacaoBeneficioRepositorio(segundoContexto);

        Assert.True(await primeiro.TentarAdquirirLockReconciliacaoAsync());
        Assert.False(await segundo.TentarAdquirirLockReconciliacaoAsync());
        await primeiro.LiberarLockReconciliacaoAsync();
        Assert.True(await segundo.TentarAdquirirLockReconciliacaoAsync());
        await segundo.LiberarLockReconciliacaoAsync();
    }

    [Fact]
    public async Task ResgateConcorrente_UltimoEstoque_AceitaSomenteUmaSolicitacao()
    {
        await using (var dbContext = fixture.CriarContexto())
        {
            var usuario1 = CriarUsuarioComSaldo(dbContext, "atleta-1", 20);
            var usuario2 = CriarUsuarioComSaldo(dbContext, "atleta-2", 20);
            var beneficio = CriarBeneficio("beneficio-concorrente", pontos: 10, quantidadeDisponivel: 1);
            dbContext.Add(beneficio);
            await dbContext.SaveChangesAsync();

            var inicio = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var primeiraSolicitacao = SolicitarResgateComResultadoAsync(usuario1, beneficio.Id, inicio.Task);
            var segundaSolicitacao = SolicitarResgateComResultadoAsync(usuario2, beneficio.Id, inicio.Task);

            inicio.SetResult();
            var resultados = await Task.WhenAll(primeiraSolicitacao, segundaSolicitacao);

            Assert.Single(resultados.Where(x => x.Sucesso));
            var falha = Assert.Single(resultados.Where(x => !x.Sucesso));
            Assert.Equal("Benefício indisponível no momento.", falha.MensagemErro);

            await ValidarEstadoUltimoEstoqueAsync(
                beneficio.Id,
                [usuario1.AtletaId!.Value, usuario2.AtletaId!.Value]);
        }
    }

    [Fact]
    public async Task ResgateConcorrente_MesmoAtletaEBeneficio_NaoDuplicaDebitoNemEstoque()
    {
        await using (var dbContext = fixture.CriarContexto())
        {
            var usuario = CriarUsuarioComSaldo(dbContext, "atleta-duplicado", 30);
            var beneficio = CriarBeneficio("beneficio-duplicidade", pontos: 10, quantidadeDisponivel: 2);
            dbContext.Add(beneficio);
            await dbContext.SaveChangesAsync();

            var inicio = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var primeiraSolicitacao = SolicitarResgateComResultadoAsync(usuario, beneficio.Id, inicio.Task);
            var segundaSolicitacao = SolicitarResgateComResultadoAsync(usuario, beneficio.Id, inicio.Task);

            inicio.SetResult();
            var resultados = await Task.WhenAll(primeiraSolicitacao, segundaSolicitacao);

            Assert.Single(resultados.Where(x => x.Sucesso));
            var falha = Assert.Single(resultados.Where(x => !x.Sucesso));
            Assert.Equal("Já existe um resgate solicitado para este benefício.", falha.MensagemErro);

            await using var verificacao = fixture.CriarContexto();
            var beneficioPersistido = await verificacao.BeneficiosPontuacao
                .AsNoTracking()
                .SingleAsync(x => x.Id == beneficio.Id);
            var saldo = await verificacao.PontuacoesBeneficiosAtletas
                .AsNoTracking()
                .SingleAsync(x => x.AtletaId == usuario.AtletaId);
            var resgates = await verificacao.ResgatesBeneficiosPontuacao
                .AsNoTracking()
                .Where(x => x.BeneficioId == beneficio.Id)
                .ToListAsync();
            var extratos = await verificacao.ExtratosPontuacaoBeneficio
                .AsNoTracking()
                .Where(x =>
                    x.TipoEvento == TipoEventoPontuacaoBeneficio.ResgateBeneficio &&
                    x.ResgateId != null &&
                    resgates.Select(r => r.Id).Contains(x.ResgateId.Value))
                .ToListAsync();

            Assert.Equal(1, beneficioPersistido.QuantidadeDisponivel);
            Assert.Equal(20, saldo.SaldoAtual);
            Assert.Equal(10, saldo.TotalResgatado);
            Assert.Single(resgates);
            Assert.Single(extratos);
            Assert.All(extratos, extrato => Assert.Equal(-10, extrato.Pontos));
        }
    }

    private async Task ValidarEstadoUltimoEstoqueAsync(Guid beneficioId, IReadOnlyCollection<Guid> atletasIds)
    {
        await using var verificacao = fixture.CriarContexto();
        var beneficio = await verificacao.BeneficiosPontuacao
            .AsNoTracking()
            .SingleAsync(x => x.Id == beneficioId);
        var saldos = await verificacao.PontuacoesBeneficiosAtletas
            .AsNoTracking()
            .Where(x => atletasIds.Contains(x.AtletaId))
            .ToListAsync();
        var resgates = await verificacao.ResgatesBeneficiosPontuacao
            .AsNoTracking()
            .Where(x => x.BeneficioId == beneficioId)
            .ToListAsync();
        var resgatesIds = resgates.Select(x => x.Id).ToHashSet();
        var extratos = await verificacao.ExtratosPontuacaoBeneficio
            .AsNoTracking()
            .Where(x =>
                x.TipoEvento == TipoEventoPontuacaoBeneficio.ResgateBeneficio &&
                x.ResgateId != null &&
                resgatesIds.Contains(x.ResgateId.Value))
            .ToListAsync();

        Assert.Equal(0, beneficio.QuantidadeDisponivel);
        Assert.DoesNotContain(saldos, x => x.SaldoAtual < 0);
        Assert.Equal(30, saldos.Sum(x => x.SaldoAtual));
        Assert.Equal(10, saldos.Sum(x => x.TotalResgatado));
        var resgate = Assert.Single(resgates);
        Assert.Equal(StatusResgateBeneficioPontuacao.Solicitado, resgate.Status);
        var extrato = Assert.Single(extratos);
        Assert.Equal(-10, extrato.Pontos);
        Assert.Equal(resgate.Id, extrato.ResgateId);
    }

    private async Task<ResultadoSolicitacao> SolicitarResgateComResultadoAsync(
        Usuario usuario,
        Guid beneficioId,
        Task inicio)
    {
        await inicio;
        await using var dbContext = fixture.CriarContexto();
        var servico = CriarServico(dbContext, usuario);

        try
        {
            var resgate = await servico.SolicitarResgateAsync(
                beneficioId,
                new SolicitarResgateBeneficioDto(null));
            return ResultadoSolicitacao.Ok(resgate.Id);
        }
        catch (RegraNegocioException ex)
        {
            return ResultadoSolicitacao.Falha(ex.Message);
        }
    }

    private PontuacaoBeneficioServico CriarServico(
        PlataformaFutevoleiDbContext dbContext,
        Usuario usuario)
    {
        return new PontuacaoBeneficioServico(
            new PontuacaoBeneficioRepositorio(dbContext),
            new UsuarioRepositorio(dbContext),
            new UnidadeTrabalho(dbContext),
            new AutorizacaoUsuarioServicoFake(usuario),
            NullLogger<PontuacaoBeneficioServico>.Instance);
    }

    private Usuario CriarUsuarioComSaldo(
        PlataformaFutevoleiDbContext dbContext,
        string sufixo,
        int saldoInicial)
    {
        var atleta = new Atleta
        {
            Nome = $"{prefixo}-{sufixo}",
            Apelido = sufixo,
            Email = Email(sufixo)
        };
        var usuario = new Usuario
        {
            Nome = $"{prefixo}-{sufixo}",
            Email = Email($"usuario-{sufixo}"),
            SenhaHash = "hash-teste",
            Perfil = PerfilUsuario.Atleta,
            Ativo = true,
            AtletaId = atleta.Id,
            Atleta = atleta
        };
        var saldo = new PontuacaoBeneficioAtleta
        {
            AtletaId = atleta.Id,
            Atleta = atleta,
            SaldoAtual = saldoInicial,
            TotalAcumulado = saldoInicial,
            TotalResgatado = 0
        };

        dbContext.AddRange(atleta, usuario, saldo);
        return usuario;
    }

    private BeneficioPontuacao CriarBeneficio(
        string sufixo,
        int pontos,
        int? quantidadeDisponivel)
    {
        return new BeneficioPontuacao
        {
            Titulo = $"{prefixo}-{sufixo}",
            Descricao = "Benefício promocional para teste de integração.",
            Tipo = TipoBeneficioPontuacao.Brinde,
            PontosNecessarios = pontos,
            Ativo = true,
            QuantidadeDisponivel = quantidadeDisponivel,
            Ordem = 999
        };
    }

    private string Email(string sufixo) => $"{prefixo}-{sufixo}@example.com";

    private sealed record ResultadoSolicitacao(bool Sucesso, Guid? ResgateId, string? MensagemErro)
    {
        public static ResultadoSolicitacao Ok(Guid resgateId) => new(true, resgateId, null);
        public static ResultadoSolicitacao Falha(string mensagemErro) => new(false, null, mensagemErro);
    }

    private sealed class AutorizacaoUsuarioServicoFake(Usuario usuario) : IAutorizacaoUsuarioServico
    {
        public Task<Usuario?> ObterUsuarioAtualAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<Usuario?>(usuario);

        public Task<Usuario> ObterUsuarioAtualObrigatorioAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(usuario);

        public Task GarantirAdministradorAsync(CancellationToken cancellationToken = default)
            => usuario.Perfil == PerfilUsuario.Administrador
                ? Task.CompletedTask
                : throw new AcessoNegadoException("Apenas administradores podem executar esta operação.");

        public Task GarantirAdminOuOrganizadorAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task GarantirAcessoAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task GarantirGestaoCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task GarantirGestaoGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
