using Microsoft.EntityFrameworkCore;
using Npgsql;
using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Servicos;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;
using PlataformaFutevolei.Infraestrutura.Persistencia;
using PlataformaFutevolei.Infraestrutura.Repositorios;
using Xunit;

namespace PlataformaFutevolei.Integracao.Tests;

[CollectionDefinition(nameof(PostgresIntegracaoCollection), DisableParallelization = true)]
public sealed class PostgresIntegracaoCollection : ICollectionFixture<PostgresIntegracaoFixture>;

[Collection(nameof(PostgresIntegracaoCollection))]
public class ConsolidacaoAtletaIntegracaoTests(PostgresIntegracaoFixture fixture) : IAsyncLifetime
{
    private readonly string prefixo = $"teste-consolidacao-atleta-{Guid.NewGuid():N}";

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await fixture.LimparDadosAsync(prefixo);
    }

    [Fact]
    public async Task MesmoEmail_AtletaComJogosVence_EPerfilApontaParaVencedor()
    {
        await using var dbContext = fixture.CriarContexto();
        var email = Email("mesmo-email");
        var atletaSemHistorico = CriarAtleta("Atleta Sem Historico", email.ToUpperInvariant());
        var atletaComJogos = CriarAtleta("Atleta Com Jogos", $" {email} ");
        var usuario = CriarUsuario("Usuario Teste", email, atletaSemHistorico);
        dbContext.AddRange(atletaSemHistorico, atletaComJogos, usuario);
        await dbContext.SaveChangesAsync();

        var parceiro = CriarAtleta("Parceiro", Email("parceiro"));
        var adversario1 = CriarAtleta("Adversario 1", Email("adversario-1"));
        var adversario2 = CriarAtleta("Adversario 2", Email("adversario-2"));
        var duplaComJogos = CriarDupla(atletaComJogos, parceiro);
        var duplaAdversaria = CriarDupla(adversario1, adversario2);
        var partida = CriarPartidaEncerrada(duplaComJogos, duplaAdversaria);
        dbContext.AddRange(parceiro, adversario1, adversario2, duplaComJogos, duplaAdversaria, partida);
        await dbContext.SaveChangesAsync();

        var servico = CriarServico(dbContext);
        var vencedor = await servico.ConsolidarCandidatosAsync(
            [atletaSemHistorico, atletaComJogos],
            atletaVinculadoConfiavelId: null,
            emailNormalizado: email);

        Assert.Equal(atletaComJogos.Id, vencedor.Id);
        Assert.False(await dbContext.Atletas.AnyAsync(x => x.Id == atletaSemHistorico.Id));
        Assert.Equal(1, await dbContext.Atletas.CountAsync(x => x.Email != null && x.Email.Trim().ToLower() == email));

        var usuarioAtualizado = await dbContext.Usuarios
            .Include(x => x.Atleta)
            .SingleAsync(x => x.Id == usuario.Id);
        Assert.Equal(atletaComJogos.Id, usuarioAtualizado.AtletaId);
        Assert.Equal(atletaComJogos.Id, usuarioAtualizado.Atleta!.Id);

        var metricas = await new ConsolidacaoAtletaRepositorio(dbContext).ObterMetricasAsync([atletaComJogos.Id]);
        Assert.Equal(1, metricas[atletaComJogos.Id].TotalPartidas);
        Assert.Equal(0, await ContarDuplicadosPorEmailAsync(dbContext, email));
    }

    [Fact]
    public async Task DuplasEquivalentes_ConsolidaReferenciasEPreservaPartidaValida()
    {
        await using var dbContext = fixture.CriarContexto();
        var vencedor = CriarAtleta("Vencedor", Email("vencedor"));
        var perdedor = CriarAtleta("Perdedor", Email("perdedor"));
        var parceiro = CriarAtleta("Parceiro", Email("parceiro"));
        var adversario1 = CriarAtleta("Adversario 1", Email("adversario-1"));
        var adversario2 = CriarAtleta("Adversario 2", Email("adversario-2"));
        var duplaDestino = CriarDupla(vencedor, parceiro);
        var duplaOrigem = CriarDupla(perdedor, parceiro);
        var duplaAdversaria = CriarDupla(adversario1, adversario2);
        var partida = CriarPartidaEncerrada(duplaOrigem, duplaAdversaria);
        dbContext.AddRange(vencedor, perdedor, parceiro, adversario1, adversario2, duplaDestino, duplaOrigem, duplaAdversaria, partida);
        await dbContext.SaveChangesAsync();

        var servico = CriarServico(dbContext);
        await servico.ConsolidarCandidatosAsync(
            [vencedor, perdedor],
            atletaVinculadoConfiavelId: vencedor.Id,
            emailNormalizado: vencedor.Email);

        Assert.False(await dbContext.Duplas.AnyAsync(x => x.Atleta1Id == perdedor.Id || x.Atleta2Id == perdedor.Id));
        Assert.Equal(1, await dbContext.Duplas.CountAsync(x =>
            (x.Atleta1Id == vencedor.Id && x.Atleta2Id == parceiro.Id) ||
            (x.Atleta1Id == parceiro.Id && x.Atleta2Id == vencedor.Id)));

        var partidaAtualizada = await dbContext.Partidas.SingleAsync(x => x.Id == partida.Id);
        Assert.Equal(duplaDestino.Id, partidaAtualizada.DuplaAId);
        Assert.NotEqual(partidaAtualizada.DuplaAId, partidaAtualizada.DuplaBId);
        Assert.False(await ExisteReferenciaAoAtletaAsync(dbContext, perdedor.Id));
    }

    [Fact]
    public async Task GrupoDuplicado_PreservaUmVinculoComDataMaisAntiga()
    {
        await using var dbContext = fixture.CriarContexto();
        var vencedor = CriarAtleta("Vencedor", Email("vencedor"));
        var perdedor = CriarAtleta("Perdedor", Email("perdedor"));
        var grupo = new Grupo
        {
            Nome = $"{prefixo}-grupo",
            DataInicio = DateTime.UtcNow.Date,
            Publico = false
        };
        var vinculoVencedor = new GrupoAtleta { Grupo = grupo, Atleta = vencedor };
        var vinculoPerdedor = new GrupoAtleta { Grupo = grupo, Atleta = perdedor };
        dbContext.AddRange(vencedor, perdedor, grupo, vinculoVencedor, vinculoPerdedor);
        await dbContext.SaveChangesAsync();

        var dataMaisAntiga = DateTime.UtcNow.AddDays(-20);
        var dataMaisNova = DateTime.UtcNow.AddDays(-2);
        dbContext.Entry(vinculoPerdedor).Property(nameof(GrupoAtleta.DataCriacao)).CurrentValue = dataMaisAntiga;
        dbContext.Entry(vinculoVencedor).Property(nameof(GrupoAtleta.DataCriacao)).CurrentValue = dataMaisNova;
        await dbContext.SaveChangesAsync();

        var servico = CriarServico(dbContext);
        await servico.ConsolidarCandidatosAsync(
            [vencedor, perdedor],
            atletaVinculadoConfiavelId: vencedor.Id,
            emailNormalizado: vencedor.Email);

        var vinculoFinal = await dbContext.GruposAtletas.SingleAsync(x => x.GrupoId == grupo.Id);
        Assert.Equal(vencedor.Id, vinculoFinal.AtletaId);
        Assert.Equal(dataMaisAntiga, vinculoFinal.DataCriacao);
        Assert.False(await dbContext.GruposAtletas.AnyAsync(x => x.AtletaId == perdedor.Id));
    }

    [Fact]
    public async Task PendenciaEConviteDuplicados_MesclaSemReabrirResolvidos()
    {
        await using var dbContext = fixture.CriarContexto();
        var vencedor = CriarAtleta("Vencedor", Email("vencedor"));
        var perdedor = CriarAtleta("Perdedor", Email("perdedor"));
        var usuario = CriarUsuario("Usuario Apoio", Email("usuario"), atleta: null);
        var conclusao = DateTime.UtcNow.AddDays(-1);
        var pendenciaVencedor = new PendenciaUsuario
        {
            Usuario = usuario,
            Atleta = vencedor,
            Tipo = TipoPendenciaUsuario.CompletarContatoAtletaDaPartida,
            Status = StatusPendenciaUsuario.Pendente
        };
        var pendenciaPerdedor = new PendenciaUsuario
        {
            Usuario = usuario,
            Atleta = perdedor,
            Tipo = TipoPendenciaUsuario.CompletarContatoAtletaDaPartida,
            Status = StatusPendenciaUsuario.Concluida,
            DataConclusao = conclusao,
            Observacao = "Concluida no teste"
        };
        var conviteVencedor = CriarConvite(usuario, vencedor, Email("convite"));
        var convitePerdedor = CriarConvite(usuario, perdedor, conviteVencedor.Email);
        convitePerdedor.UsadoEmUtc = conclusao;
        convitePerdedor.Ativo = false;
        dbContext.AddRange(vencedor, perdedor, usuario, pendenciaVencedor, pendenciaPerdedor, conviteVencedor, convitePerdedor);
        await dbContext.SaveChangesAsync();

        var servico = CriarServico(dbContext);
        await servico.ConsolidarCandidatosAsync(
            [vencedor, perdedor],
            atletaVinculadoConfiavelId: vencedor.Id,
            emailNormalizado: vencedor.Email);

        var pendenciaFinal = await dbContext.PendenciasUsuarios.SingleAsync(x => x.AtletaId == vencedor.Id);
        Assert.Equal(StatusPendenciaUsuario.Concluida, pendenciaFinal.Status);
        Assert.Equal(conclusao, pendenciaFinal.DataConclusao);

        var conviteFinal = await dbContext.ConvitesCadastro.SingleAsync(x => x.AtletaId == vencedor.Id && x.Email == conviteVencedor.Email);
        Assert.NotNull(conviteFinal.UsadoEmUtc);
        Assert.False(await dbContext.PendenciasUsuarios.AnyAsync(x => x.AtletaId == perdedor.Id));
        Assert.False(await dbContext.ConvitesCadastro.AnyAsync(x => x.AtletaId == perdedor.Id));
    }

    [Fact]
    public async Task FkResidualExterna_BloqueiaRemocaoERollbackMantemDados()
    {
        await using var dbContext = fixture.CriarContexto();
        var vencedor = CriarAtleta("Vencedor", Email("vencedor"));
        var perdedor = CriarAtleta("Perdedor", Email("perdedor"));
        dbContext.AddRange(vencedor, perdedor);
        await dbContext.SaveChangesAsync();

        await dbContext.Database.ExecuteSqlRawAsync("""
            create table if not exists teste_consolidacao_atleta_fk_residual (
                id uuid primary key,
                prefixo text not null,
                atleta_id uuid not null references atletas(id) on delete restrict
            )
            """);
        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            insert into teste_consolidacao_atleta_fk_residual (id, prefixo, atleta_id)
            values ({Guid.NewGuid()}, {prefixo}, {perdedor.Id})
            """);

        var servico = CriarServico(dbContext);
        await Assert.ThrowsAsync<DbUpdateException>(() =>
            servico.ConsolidarCandidatosAsync(
                [vencedor, perdedor],
                atletaVinculadoConfiavelId: vencedor.Id,
                emailNormalizado: vencedor.Email));

        await using var verificacao = fixture.CriarContexto();
        Assert.True(await verificacao.Atletas.AnyAsync(x => x.Id == vencedor.Id));
        Assert.True(await verificacao.Atletas.AnyAsync(x => x.Id == perdedor.Id));

        await verificacao.Database.ExecuteSqlInterpolatedAsync(
            $"delete from teste_consolidacao_atleta_fk_residual where prefixo = {prefixo}");
    }

    [Fact]
    public async Task Consolidar_PreservaSaldosExtratosEResgateSemAlterarEstoque()
    {
        Guid vencedorId;
        Guid perdedorId;
        Guid resgateId;
        Guid beneficioId;
        await using (var dbContext = fixture.CriarContexto())
        {
            var vencedor = CriarAtleta("Vencedor QN", Email("vencedor-qn"));
            var perdedor = CriarAtleta("Perdedor QN", Email("perdedor-qn"));
            var beneficio = CriarBeneficio("beneficio-qn", 3, 7);
            var saldoVencedor = CriarSaldo(vencedor, 10, 10, 0);
            var saldoPerdedor = CriarSaldo(perdedor, 2, 5, 3);
            var extratoVencedor = CriarExtrato(
                vencedor, 10, TipoEventoPontuacaoBeneficio.PerfilCompleto,
                $"PERFIL_COMPLETO:ATLETA:{vencedor.Id}");
            var resgate = new ResgateBeneficioPontuacao
            {
                Atleta = perdedor,
                Beneficio = beneficio,
                PontosUtilizados = 3,
                Status = StatusResgateBeneficioPontuacao.Aprovado,
                CodigoCupom = "QN-PRESERVAR",
                ObservacaoAtleta = "Observação atleta",
                ObservacaoAdmin = "Observação admin",
                SolicitadoEm = DateTime.UtcNow.AddDays(-2),
                AprovadoEm = DateTime.UtcNow.AddDays(-1)
            };
            var extratoCreditoPerdedor = CriarExtrato(
                perdedor, 5, TipoEventoPontuacaoBeneficio.EntradaGrupo,
                $"ENTRADA_GRUPO:{Guid.NewGuid()}:ATLETA:{perdedor.Id}");
            var extratoResgate = CriarExtrato(
                perdedor, -3, TipoEventoPontuacaoBeneficio.ResgateBeneficio,
                $"RESGATE:{resgate.Id}:ATLETA:{perdedor.Id}");
            extratoResgate.Resgate = resgate;
            dbContext.AddRange(
                vencedor, perdedor, beneficio, saldoVencedor, saldoPerdedor,
                extratoVencedor, resgate, extratoCreditoPerdedor, extratoResgate);
            await dbContext.SaveChangesAsync();
            vencedorId = vencedor.Id;
            perdedorId = perdedor.Id;
            resgateId = resgate.Id;
            beneficioId = beneficio.Id;

            await CriarServico(dbContext).ConsolidarCandidatosAsync(
                [vencedor, perdedor], vencedor.Id, vencedor.Email);
        }

        await using var verificacao = fixture.CriarContexto();
        Assert.False(await verificacao.Atletas.AnyAsync(x => x.Id == perdedorId));
        var saldo = await verificacao.PontuacoesBeneficiosAtletas.SingleAsync(x => x.AtletaId == vencedorId);
        Assert.Equal(12, saldo.SaldoAtual);
        Assert.Equal(15, saldo.TotalAcumulado);
        Assert.Equal(3, saldo.TotalResgatado);
        Assert.Equal(3, await verificacao.ExtratosPontuacaoBeneficio.CountAsync(x => x.AtletaId == vencedorId));
        Assert.False(await verificacao.ExtratosPontuacaoBeneficio.AnyAsync(x => x.AtletaId == perdedorId));
        var resgatePersistido = await verificacao.ResgatesBeneficiosPontuacao.SingleAsync(x => x.Id == resgateId);
        Assert.Equal(vencedorId, resgatePersistido.AtletaId);
        Assert.Equal(StatusResgateBeneficioPontuacao.Aprovado, resgatePersistido.Status);
        Assert.Equal("QN-PRESERVAR", resgatePersistido.CodigoCupom);
        Assert.Equal(resgateId, await verificacao.ExtratosPontuacaoBeneficio
            .Where(x => x.Pontos == -3)
            .Select(x => x.ResgateId)
            .SingleAsync());
        Assert.Equal(7, await verificacao.BeneficiosPontuacao
            .Where(x => x.Id == beneficioId)
            .Select(x => x.QuantidadeDisponivel)
            .SingleAsync());
        Assert.False(await ExisteReferenciaAoAtletaAsync(verificacao, perdedorId));
    }

    [Fact]
    public async Task Consolidar_DuplicidadeExataMantemUmExtratoEChaveDoVencedor()
    {
        await using var dbContext = fixture.CriarContexto();
        var vencedor = CriarAtleta("Vencedor duplicidade", Email("vencedor-duplicidade"));
        var perdedor = CriarAtleta("Perdedor duplicidade", Email("perdedor-duplicidade"));
        dbContext.AddRange(
            vencedor,
            perdedor,
            CriarSaldo(vencedor, 10, 10, 0),
            CriarSaldo(perdedor, 10, 10, 0),
            CriarExtrato(vencedor, 10, TipoEventoPontuacaoBeneficio.PerfilCompleto, $"PERFIL_COMPLETO:ATLETA:{vencedor.Id}"),
            CriarExtrato(perdedor, 10, TipoEventoPontuacaoBeneficio.PerfilCompleto, $"PERFIL_COMPLETO:ATLETA:{perdedor.Id}"));
        await dbContext.SaveChangesAsync();

        await CriarServico(dbContext).ConsolidarCandidatosAsync(
            [vencedor, perdedor], vencedor.Id, vencedor.Email);

        await using var verificacao = fixture.CriarContexto();
        var extrato = await verificacao.ExtratosPontuacaoBeneficio.SingleAsync(x => x.AtletaId == vencedor.Id);
        Assert.Equal($"PERFIL_COMPLETO:ATLETA:{vencedor.Id}", extrato.ChaveIdempotencia);
        Assert.Equal(10, (await verificacao.PontuacoesBeneficiosAtletas.SingleAsync(x => x.AtletaId == vencedor.Id)).SaldoAtual);
    }

    [Fact]
    public async Task Consolidar_ColisaoAmbiguaFazRollbackIntegral()
    {
        Guid vencedorId;
        Guid perdedorId;
        await using (var dbContext = fixture.CriarContexto())
        {
            var vencedor = CriarAtleta("Vencedor conflito", Email("vencedor-conflito"));
            var perdedor = CriarAtleta("Perdedor conflito", Email("perdedor-conflito"));
            dbContext.AddRange(
                vencedor,
                perdedor,
                CriarSaldo(vencedor, 10, 10, 0),
                CriarSaldo(perdedor, 11, 11, 0),
                CriarExtrato(vencedor, 10, TipoEventoPontuacaoBeneficio.PerfilCompleto, $"PERFIL_COMPLETO:ATLETA:{vencedor.Id}"),
                CriarExtrato(perdedor, 11, TipoEventoPontuacaoBeneficio.PerfilCompleto, $"PERFIL_COMPLETO:ATLETA:{perdedor.Id}"));
            await dbContext.SaveChangesAsync();
            vencedorId = vencedor.Id;
            perdedorId = perdedor.Id;

            await Assert.ThrowsAsync<RegraNegocioException>(() =>
                CriarServico(dbContext).ConsolidarCandidatosAsync(
                    [vencedor, perdedor], vencedor.Id, vencedor.Email));
        }

        await using var verificacao = fixture.CriarContexto();
        Assert.True(await verificacao.Atletas.AnyAsync(x => x.Id == vencedorId));
        Assert.True(await verificacao.Atletas.AnyAsync(x => x.Id == perdedorId));
        Assert.Equal(2, await verificacao.PontuacoesBeneficiosAtletas.CountAsync(x => x.AtletaId == vencedorId || x.AtletaId == perdedorId));
        Assert.Equal(2, await verificacao.ExtratosPontuacaoBeneficio.CountAsync(x => x.AtletaId == vencedorId || x.AtletaId == perdedorId));
    }

    private ConsolidacaoAtletaServico CriarServico(PlataformaFutevoleiDbContext dbContext)
    {
        var atletaRepositorio = new AtletaRepositorio(dbContext);
        var consolidacaoRepositorio = new ConsolidacaoAtletaRepositorio(dbContext);
        var unidadeTrabalho = new UnidadeTrabalho(dbContext);
        return new ConsolidacaoAtletaServico(atletaRepositorio, consolidacaoRepositorio, unidadeTrabalho);
    }

    private string Email(string sufixo) => $"{prefixo}-{sufixo}@example.com";

    private static Atleta CriarAtleta(string nome, string email)
    {
        return new Atleta
        {
            Nome = nome,
            Apelido = nome,
            Email = email
        };
    }

    private static Usuario CriarUsuario(string nome, string email, Atleta? atleta)
    {
        return new Usuario
        {
            Nome = nome,
            Email = email,
            SenhaHash = "hash-teste",
            Perfil = PerfilUsuario.Atleta,
            Ativo = true,
            Atleta = atleta
        };
    }

    private static Dupla CriarDupla(Atleta atleta1, Atleta atleta2)
    {
        var atletas = new[] { atleta1, atleta2 }
            .OrderBy(x => x.Id)
            .ToArray();

        return new Dupla
        {
            Atleta1 = atletas[0],
            Atleta2 = atletas[1],
            Nome = $"{atletas[0].Nome} / {atletas[1].Nome}"
        };
    }

    private static Partida CriarPartidaEncerrada(Dupla duplaA, Dupla duplaB)
    {
        return new Partida
        {
            DuplaA = duplaA,
            DuplaB = duplaB,
            DuplaVencedora = duplaA,
            Status = StatusPartida.Encerrada,
            TipoRegistroResultado = TipoRegistroResultado.ApenasResultado,
            DataPartida = DateTime.UtcNow
        };
    }

    private ConviteCadastro CriarConvite(Usuario usuario, Atleta atleta, string email)
    {
        return new ConviteCadastro
        {
            Email = email,
            IdentificadorPublico = $"tc-{Guid.NewGuid():N}",
            PerfilDestino = PerfilUsuario.Atleta,
            ExpiraEmUtc = DateTime.UtcNow.AddDays(7),
            Ativo = true,
            CriadoPorUsuario = usuario,
            Atleta = atleta
        };
    }

    private BeneficioPontuacao CriarBeneficio(string sufixo, int pontos, int? estoque)
    {
        return new BeneficioPontuacao
        {
            Titulo = $"{prefixo}-{sufixo}",
            Descricao = "Benefício para teste de consolidação.",
            Tipo = TipoBeneficioPontuacao.Brinde,
            PontosNecessarios = pontos,
            Ativo = true,
            QuantidadeDisponivel = estoque,
            Ordem = 999
        };
    }

    private static PontuacaoBeneficioAtleta CriarSaldo(
        Atleta atleta, int saldoAtual, int totalAcumulado, int totalResgatado)
    {
        return new PontuacaoBeneficioAtleta
        {
            Atleta = atleta,
            SaldoAtual = saldoAtual,
            TotalAcumulado = totalAcumulado,
            TotalResgatado = totalResgatado
        };
    }

    private static ExtratoPontuacaoBeneficio CriarExtrato(
        Atleta atleta,
        int pontos,
        TipoEventoPontuacaoBeneficio tipoEvento,
        string chave)
    {
        return new ExtratoPontuacaoBeneficio
        {
            Atleta = atleta,
            Pontos = pontos,
            TipoEvento = tipoEvento,
            Descricao = "Movimentação de teste",
            Origem = "TesteConsolidacao",
            ChaveIdempotencia = chave
        };
    }

    private static async Task<int> ContarDuplicadosPorEmailAsync(
        PlataformaFutevoleiDbContext dbContext,
        string email)
    {
        var resultado = await dbContext.Database
            .SqlQueryRaw<int>(
                """
                select count(*)::int as "Value"
                from (
                    select lower(btrim(email)) as email_normalizado
                    from atletas
                    where email is not null and btrim(email) <> ''
                    group by lower(btrim(email))
                    having count(*) > 1
                ) duplicados
                where email_normalizado = {0}
                """,
                email)
            .SingleAsync();
        return resultado;
    }

    private static async Task<bool> ExisteReferenciaAoAtletaAsync(
        PlataformaFutevoleiDbContext dbContext,
        Guid atletaId)
    {
        return
            await dbContext.Usuarios.AnyAsync(x => x.AtletaId == atletaId) ||
            await dbContext.Duplas.AnyAsync(x => x.Atleta1Id == atletaId || x.Atleta2Id == atletaId) ||
            await dbContext.GruposAtletas.AnyAsync(x => x.AtletaId == atletaId) ||
            await dbContext.PartidasAprovacoes.AnyAsync(x => x.AtletaId == atletaId) ||
            await dbContext.PendenciasUsuarios.AnyAsync(x => x.AtletaId == atletaId) ||
            await dbContext.ConvitesCadastro.AnyAsync(x => x.AtletaId == atletaId) ||
            await dbContext.AtletasMedidas.AnyAsync(x => x.AtletaId == atletaId) ||
            await dbContext.PontuacoesBeneficiosAtletas.AnyAsync(x => x.AtletaId == atletaId) ||
            await dbContext.ExtratosPontuacaoBeneficio.AnyAsync(x => x.AtletaId == atletaId) ||
            await dbContext.ResgatesBeneficiosPontuacao.AnyAsync(x => x.AtletaId == atletaId);
    }
}

public sealed class PostgresIntegracaoFixture : IAsyncLifetime
{
    private const string PrefixoTeste = "teste-consolidacao-atleta-";
    private readonly string connectionString =
        Environment.GetEnvironmentVariable("QNF_TEST_DATABASE_URL") ??
        "Host=localhost;Port=55432;Database=plataforma_futevolei_test;Username=postgres;Password=postgres;Ssl Mode=Disable;Include Error Detail=true";

    public async Task InitializeAsync()
    {
        await GarantirBancoExisteAsync();
        await using var dbContext = CriarContexto();
        await dbContext.Database.MigrateAsync();
        await RemoverIndiceUnicoEmailNormalizadoParaSimularLegadoAsync(dbContext);
        await LimparDadosAsync(PrefixoTeste);
    }

    public async Task DisposeAsync()
    {
        await LimparDadosAsync(PrefixoTeste);
        await using var dbContext = CriarContexto();
        await RecriarIndiceUnicoEmailNormalizadoAsync(dbContext);
    }

    public PlataformaFutevoleiDbContext CriarContexto()
    {
        var options = new DbContextOptionsBuilder<PlataformaFutevoleiDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new PlataformaFutevoleiDbContext(options);
    }

    public async Task LimparDadosAsync(string prefixo)
    {
        await using var dbContext = CriarContexto();
        await dbContext.Database.ExecuteSqlRawAsync("""
            create table if not exists teste_consolidacao_atleta_fk_residual (
                id uuid primary key,
                prefixo text not null,
                atleta_id uuid not null references atletas(id) on delete restrict
            )
            """);
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"delete from teste_consolidacao_atleta_fk_residual where prefixo like {prefixo + "%"}");
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"delete from extratos_pontuacao_beneficio where atleta_id in (select id from atletas where lower(btrim(email)) like {prefixo + "%@example.com"}) or resgate_id in (select r.id from resgates_beneficios_pontuacao r join beneficios_pontuacao b on b.id = r.beneficio_id where b.titulo like {prefixo + "%"})");
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"delete from resgates_beneficios_pontuacao where atleta_id in (select id from atletas where lower(btrim(email)) like {prefixo + "%@example.com"}) or beneficio_id in (select id from beneficios_pontuacao where titulo like {prefixo + "%"})");
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"delete from pontuacoes_beneficios_atletas where atleta_id in (select id from atletas where lower(btrim(email)) like {prefixo + "%@example.com"})");
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"delete from beneficios_pontuacao where titulo like {prefixo + "%"}");
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"delete from pendencias_usuarios where atleta_id in (select id from atletas where lower(btrim(email)) like {prefixo + "%@example.com"}) or usuario_id in (select id from usuarios where email like {prefixo + "%@example.com"})");
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"delete from convites_cadastro where email like {prefixo + "%@example.com"} or atleta_id in (select id from atletas where lower(btrim(email)) like {prefixo + "%@example.com"}) or criado_por_usuario_id in (select id from usuarios where email like {prefixo + "%@example.com"})");
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"delete from partidas_aprovacoes where atleta_id in (select id from atletas where lower(btrim(email)) like {prefixo + "%@example.com"}) or usuario_id in (select id from usuarios where email like {prefixo + "%@example.com"})");
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"delete from partidas where dupla_a_id in (select d.id from duplas d join atletas a1 on a1.id = d.atleta1_id join atletas a2 on a2.id = d.atleta2_id where lower(btrim(a1.email)) like {prefixo + "%@example.com"} or lower(btrim(a2.email)) like {prefixo + "%@example.com"}) or dupla_b_id in (select d.id from duplas d join atletas a1 on a1.id = d.atleta1_id join atletas a2 on a2.id = d.atleta2_id where lower(btrim(a1.email)) like {prefixo + "%@example.com"} or lower(btrim(a2.email)) like {prefixo + "%@example.com"}) or dupla_vencedora_id in (select d.id from duplas d join atletas a1 on a1.id = d.atleta1_id join atletas a2 on a2.id = d.atleta2_id where lower(btrim(a1.email)) like {prefixo + "%@example.com"} or lower(btrim(a2.email)) like {prefixo + "%@example.com"})");
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"delete from inscricoes_campeonato where dupla_id in (select id from duplas where nome like {prefixo + "%"})");
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"delete from duplas where nome like {prefixo + "%"} or atleta1_id in (select id from atletas where lower(btrim(email)) like {prefixo + "%@example.com"}) or atleta2_id in (select id from atletas where lower(btrim(email)) like {prefixo + "%@example.com"})");
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"delete from grupos_atletas where atleta_id in (select id from atletas where lower(btrim(email)) like {prefixo + "%@example.com"}) or grupo_id in (select id from grupos where nome like {prefixo + "%"})");
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"delete from grupos where nome like {prefixo + "%"}");
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"delete from atletas_medidas where atleta_id in (select id from atletas where lower(btrim(email)) like {prefixo + "%@example.com"})");
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"update usuarios set atleta_id = null where atleta_id in (select id from atletas where lower(btrim(email)) like {prefixo + "%@example.com"})");
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"delete from usuarios where email like {prefixo + "%@example.com"}");
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"delete from atletas where lower(btrim(email)) like {prefixo + "%@example.com"}");
    }

    private async Task GarantirBancoExisteAsync()
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var database = builder.Database;
        builder.Database = "postgres";

        await using var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync();

        await using var existe = new NpgsqlCommand("select 1 from pg_database where datname = @database", connection);
        existe.Parameters.AddWithValue("database", database!);
        var resultado = await existe.ExecuteScalarAsync();
        if (resultado is not null)
        {
            return;
        }

        var databaseSeguro = database!.Replace("\"", "\"\"");
        await using var criar = new NpgsqlCommand($"create database \"{databaseSeguro}\"", connection);
        await criar.ExecuteNonQueryAsync();
    }

    private static Task RemoverIndiceUnicoEmailNormalizadoParaSimularLegadoAsync(
        PlataformaFutevoleiDbContext dbContext)
    {
        return dbContext.Database.ExecuteSqlRawAsync("drop index if exists ix_atletas_email_normalizado_unico;");
    }

    private static Task RecriarIndiceUnicoEmailNormalizadoAsync(
        PlataformaFutevoleiDbContext dbContext)
    {
        return dbContext.Database.ExecuteSqlRawAsync(
            """
            create unique index if not exists ix_atletas_email_normalizado_unico
            on atletas (lower(btrim(email)))
            where email is not null and btrim(email) <> '';
            """);
    }
}
