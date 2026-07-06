using Microsoft.EntityFrameworkCore;
using PlataformaFutevolei.Aplicacao.Configuracoes;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;
using PlataformaFutevolei.Infraestrutura.Persistencia;

namespace PlataformaFutevolei.Infraestrutura.Repositorios;

public class PontuacaoBeneficioRepositorio(PlataformaFutevoleiDbContext dbContext) : IPontuacaoBeneficioRepositorio
{
    public Task<PontuacaoBeneficioAtleta?> ObterSaldoPorAtletaAsync(
        Guid atletaId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.PontuacoesBeneficiosAtletas
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.AtletaId == atletaId, cancellationToken);
    }

    public async Task<PontuacaoBeneficioAtleta?> ObterSaldoPorAtletaParaAtualizacaoAsync(
        Guid atletaId,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM pontuacoes_beneficios_atletas WHERE atleta_id = {atletaId} FOR UPDATE",
            cancellationToken);

        return await dbContext.PontuacoesBeneficiosAtletas
            .FirstOrDefaultAsync(x => x.AtletaId == atletaId, cancellationToken);
    }

    public Task AdicionarSaldoAsync(PontuacaoBeneficioAtleta saldo, CancellationToken cancellationToken = default)
    {
        return dbContext.PontuacoesBeneficiosAtletas.AddAsync(saldo, cancellationToken).AsTask();
    }

    public Task<bool> ExisteExtratoPorChaveAsync(
        string chaveIdempotencia,
        CancellationToken cancellationToken = default)
    {
        return dbContext.ExtratosPontuacaoBeneficio
            .AsNoTracking()
            .AnyAsync(x => x.ChaveIdempotencia == chaveIdempotencia, cancellationToken);
    }

    public async Task<IReadOnlyList<ExtratoPontuacaoBeneficio>> ListarExtratoAsync(
        Guid atletaId,
        TipoEventoPontuacaoBeneficio? tipo,
        DateTime? dataInicial,
        DateTime? dataFinal,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var consulta = dbContext.ExtratosPontuacaoBeneficio
            .AsNoTracking()
            .Where(x => x.AtletaId == atletaId);

        if (tipo.HasValue)
        {
            consulta = consulta.Where(x => x.TipoEvento == tipo.Value);
        }

        if (dataInicial.HasValue)
        {
            consulta = consulta.Where(x => x.DataCriacao >= dataInicial.Value);
        }

        if (dataFinal.HasValue)
        {
            consulta = consulta.Where(x => x.DataCriacao <= dataFinal.Value);
        }

        return await consulta
            .OrderByDescending(x => x.DataCriacao)
            .ThenByDescending(x => x.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task<int> ContarEventosAsync(
        Guid atletaId,
        IReadOnlyCollection<TipoEventoPontuacaoBeneficio> tipos,
        DateTime dataInicial,
        DateTime dataFinal,
        CancellationToken cancellationToken = default)
    {
        return FiltrarEventos(atletaId, tipos, dataInicial, dataFinal)
            .CountAsync(cancellationToken);
    }

    public Task<int> SomarPontosAsync(
        Guid atletaId,
        IReadOnlyCollection<TipoEventoPontuacaoBeneficio> tipos,
        DateTime dataInicial,
        DateTime dataFinal,
        CancellationToken cancellationToken = default)
    {
        return SomarPontosFiltradosAsync(atletaId, tipos, dataInicial, dataFinal, cancellationToken);
    }

    public async Task<IReadOnlyList<ExtratoPontuacaoBeneficio>> ListarExtratoPorPartidaAsync(
        Guid partidaId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.ExtratosPontuacaoBeneficio
            .AsNoTracking()
            .Where(x => x.PartidaId == partidaId)
            .OrderBy(x => x.DataCriacao)
            .ToListAsync(cancellationToken);
    }

    private async Task<int> SomarPontosFiltradosAsync(
        Guid atletaId,
        IReadOnlyCollection<TipoEventoPontuacaoBeneficio> tipos,
        DateTime dataInicial,
        DateTime dataFinal,
        CancellationToken cancellationToken)
    {
        return await FiltrarEventos(atletaId, tipos, dataInicial, dataFinal)
            .SumAsync(x => (int?)x.Pontos, cancellationToken)
            ?? 0;
    }

    public Task AdicionarExtratoAsync(ExtratoPontuacaoBeneficio extrato, CancellationToken cancellationToken = default)
    {
        return dbContext.ExtratosPontuacaoBeneficio.AddAsync(extrato, cancellationToken).AsTask();
    }

    public async Task<IReadOnlyList<SaldoInicialRetroativoAtletaDto>> CalcularSaldosIniciaisRetroativosAsync(
        CancellationToken cancellationToken = default)
    {
        var atletas = await dbContext.Atletas
            .AsNoTracking()
            .Select(x => new { x.Id, x.Nome, x.Email, x.Nivel, x.Sexo, x.DataNascimento })
            .ToListAsync(cancellationToken);
        var nomesAtletas = atletas.ToDictionary(x => x.Id, x => x.Nome);
        var calculos = new Dictionary<Guid, SaldoInicialRetroativoAcumulador>();

        SaldoInicialRetroativoAcumulador ObterAcumulador(Guid atletaId)
        {
            if (!calculos.TryGetValue(atletaId, out var acumulador))
            {
                acumulador = new SaldoInicialRetroativoAcumulador(
                    atletaId,
                    nomesAtletas.GetValueOrDefault(atletaId) ?? "Atleta");
                calculos[atletaId] = acumulador;
            }

            return acumulador;
        }

        foreach (var atleta in atletas.Where(x => PerfilCompleto(
            x.Nome,
            x.Email,
            x.Nivel,
            x.Sexo,
            x.DataNascimento)))
        {
            ObterAcumulador(atleta.Id).PerfilCompleto = true;
        }

        var partidasValidas = await dbContext.Partidas
            .AsNoTracking()
            .Include(x => x.DuplaA)
            .Include(x => x.DuplaB)
            .Include(x => x.CriadoPorUsuario)
            .Where(x =>
                x.Ativa &&
                x.Status == StatusPartida.Encerrada &&
                x.StatusAprovacao == StatusAprovacaoPartida.Aprovada &&
                x.DuplaAId != null &&
                x.DuplaBId != null &&
                x.DuplaVencedoraId != null)
            .ToListAsync(cancellationToken);

        foreach (var partida in partidasValidas)
        {
            foreach (var atletaId in ObterAtletasPartida(partida).Distinct())
            {
                ObterAcumulador(atletaId).PartidasParticipadas++;
            }

            if (partida.CriadoPorUsuario?.AtletaId is Guid atletaRegistradorId)
            {
                var acumuladorRegistrador = ObterAcumulador(atletaRegistradorId);
                acumuladorRegistrador.PartidasRegistradas++;
                if (partida.PossuiPlacarDetalhado())
                {
                    acumuladorRegistrador.PartidasComPlacar++;
                }
            }

            foreach (var atletaId in ObterAtletasDuplaVencedora(partida))
            {
                ObterAcumulador(atletaId).Vitorias++;
            }
        }

        var gruposPorAtleta = await dbContext.GruposAtletas
            .AsNoTracking()
            .Select(x => new { x.AtletaId, x.GrupoId })
            .Distinct()
            .GroupBy(x => x.AtletaId)
            .Select(x => new { AtletaId = x.Key, Total = x.Count() })
            .ToListAsync(cancellationToken);
        foreach (var grupo in gruposPorAtleta)
        {
            ObterAcumulador(grupo.AtletaId).Grupos = grupo.Total;
        }

        var pendenciasResolvidas = await dbContext.PendenciasUsuarios
            .AsNoTracking()
            .Where(x =>
                x.Status == StatusPendenciaUsuario.Concluida &&
                x.Tipo == TipoPendenciaUsuario.CompletarContatoAtletaDaPartida &&
                x.AtletaId != null)
            .GroupBy(x => x.AtletaId!.Value)
            .Select(x => new { AtletaId = x.Key, Total = x.Count() })
            .ToListAsync(cancellationToken);
        foreach (var pendencia in pendenciasResolvidas)
        {
            ObterAcumulador(pendencia.AtletaId).PendenciasResolvidas = pendencia.Total;
        }

        return calculos.Values
            .Select(x => x.ParaDto())
            .Where(x => x.TotalCalculado > 0)
            .OrderByDescending(x => x.TotalCalculado)
            .ThenBy(x => x.NomeAtleta)
            .ToList();
    }

    public async Task<IReadOnlySet<Guid>> ListarAtletasComSaldoInicialRetroativoAsync(CancellationToken cancellationToken = default)
    {
        var atletas = await dbContext.ExtratosPontuacaoBeneficio
            .AsNoTracking()
            .Where(x => x.TipoEvento == TipoEventoPontuacaoBeneficio.SaldoInicialRetroativo)
            .Select(x => x.AtletaId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return atletas.ToHashSet();
    }

    public async Task<IReadOnlyList<BeneficioPontuacao>> ListarBeneficiosAtivosAsync(
        TipoBeneficioPontuacao? tipo,
        bool? disponivel,
        bool? destaque,
        CancellationToken cancellationToken = default)
    {
        var consulta = dbContext.BeneficiosPontuacao
            .AsNoTracking()
            .Where(x => x.Ativo);

        if (tipo.HasValue)
        {
            consulta = consulta.Where(x => x.Tipo == tipo.Value);
        }

        if (disponivel == true)
        {
            consulta = consulta.Where(x => x.QuantidadeDisponivel == null || x.QuantidadeDisponivel > 0);
        }

        if (destaque.HasValue)
        {
            consulta = consulta.Where(x => x.Destaque == destaque.Value);
        }

        return await consulta
            .OrderBy(x => x.Ordem)
            .ThenBy(x => x.PontosNecessarios)
            .ThenBy(x => x.Titulo)
            .ToListAsync(cancellationToken);
    }

    public Task<BeneficioPontuacao?> ObterBeneficioPorIdAsync(Guid beneficioId, CancellationToken cancellationToken = default)
    {
        return dbContext.BeneficiosPontuacao
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == beneficioId, cancellationToken);
    }

    public async Task<BeneficioPontuacao?> ObterBeneficioPorIdParaAtualizacaoAsync(
        Guid beneficioId,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM beneficios_pontuacao WHERE id = {beneficioId} FOR UPDATE",
            cancellationToken);

        return await dbContext.BeneficiosPontuacao
            .FirstOrDefaultAsync(x => x.Id == beneficioId, cancellationToken);
    }

    public async Task<IReadOnlyList<ResgateBeneficioPontuacao>> ListarResgatesPorAtletaAsync(
        Guid atletaId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.ResgatesBeneficiosPontuacao
            .AsNoTracking()
            .Include(x => x.Beneficio)
            .Where(x => x.AtletaId == atletaId)
            .OrderByDescending(x => x.SolicitadoEm)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ResgateBeneficioPontuacao>> ListarResgatesAdministracaoAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.ResgatesBeneficiosPontuacao
            .AsNoTracking()
            .Include(x => x.Beneficio)
            .OrderByDescending(x => x.SolicitadoEm)
            .Take(200)
            .ToListAsync(cancellationToken);
    }

    public Task<ResgateBeneficioPontuacao?> ObterResgatePorIdAsync(Guid resgateId, CancellationToken cancellationToken = default)
    {
        return dbContext.ResgatesBeneficiosPontuacao
            .AsNoTracking()
            .Include(x => x.Beneficio)
            .FirstOrDefaultAsync(x => x.Id == resgateId, cancellationToken);
    }

    public Task<ResgateBeneficioPontuacao?> ObterResgatePorIdParaAtualizacaoAsync(
        Guid resgateId,
        CancellationToken cancellationToken = default)
    {
        return ObterResgatePorIdParaAtualizacaoInternoAsync(resgateId, cancellationToken);
    }

    public Task<bool> ExisteResgateSolicitadoAsync(
        Guid atletaId,
        Guid beneficioId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.ResgatesBeneficiosPontuacao
            .AsNoTracking()
            .AnyAsync(x =>
                x.AtletaId == atletaId &&
                x.BeneficioId == beneficioId &&
                x.Status == StatusResgateBeneficioPontuacao.Solicitado,
                cancellationToken);
    }

    public Task AdicionarResgateAsync(ResgateBeneficioPontuacao resgate, CancellationToken cancellationToken = default)
    {
        return dbContext.ResgatesBeneficiosPontuacao.AddAsync(resgate, cancellationToken).AsTask();
    }

    public void AtualizarSaldo(PontuacaoBeneficioAtleta saldo)
    {
        if (dbContext.Entry(saldo).State == EntityState.Added)
        {
            return;
        }

        dbContext.PontuacoesBeneficiosAtletas.Update(saldo);
    }

    public void AtualizarResgate(ResgateBeneficioPontuacao resgate)
    {
        dbContext.ResgatesBeneficiosPontuacao.Update(resgate);
    }

    private async Task<ResgateBeneficioPontuacao?> ObterResgatePorIdParaAtualizacaoInternoAsync(
        Guid resgateId,
        CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM resgates_beneficios_pontuacao WHERE id = {resgateId} FOR UPDATE",
            cancellationToken);

        var resgate = await dbContext.ResgatesBeneficiosPontuacao
            .FirstOrDefaultAsync(x => x.Id == resgateId, cancellationToken);
        if (resgate is not null)
        {
            await dbContext.Entry(resgate)
                .Reference(x => x.Beneficio)
                .LoadAsync(cancellationToken);
        }

        return resgate;
    }

    private IQueryable<ExtratoPontuacaoBeneficio> FiltrarEventos(
        Guid atletaId,
        IReadOnlyCollection<TipoEventoPontuacaoBeneficio> tipos,
        DateTime dataInicial,
        DateTime dataFinal)
    {
        return dbContext.ExtratosPontuacaoBeneficio
            .AsNoTracking()
            .Where(x => x.AtletaId == atletaId)
            .Where(x => tipos.Contains(x.TipoEvento))
            .Where(x => x.DataCriacao >= dataInicial && x.DataCriacao < dataFinal);
    }

    private static IReadOnlyList<Guid> ObterAtletasPartida(Partida partida)
    {
        if (partida.DuplaA is null || partida.DuplaB is null)
        {
            return [];
        }

        return
        [
            partida.DuplaA.Atleta1Id,
            partida.DuplaA.Atleta2Id,
            partida.DuplaB.Atleta1Id,
            partida.DuplaB.Atleta2Id
        ];
    }

    private static IReadOnlyList<Guid> ObterAtletasDuplaVencedora(Partida partida)
    {
        if (partida.DuplaVencedoraId == partida.DuplaAId && partida.DuplaA is not null)
        {
            return [partida.DuplaA.Atleta1Id, partida.DuplaA.Atleta2Id];
        }

        if (partida.DuplaVencedoraId == partida.DuplaBId && partida.DuplaB is not null)
        {
            return [partida.DuplaB.Atleta1Id, partida.DuplaB.Atleta2Id];
        }

        return [];
    }

    private sealed class SaldoInicialRetroativoAcumulador(Guid atletaId, string nomeAtleta)
    {
        public int PartidasParticipadas { get; set; }
        public int PartidasRegistradas { get; set; }
        public int PartidasComPlacar { get; set; }
        public int Vitorias { get; set; }
        public int Grupos { get; set; }
        public int PendenciasResolvidas { get; set; }
        public bool PerfilCompleto { get; set; }

        public SaldoInicialRetroativoAtletaDto ParaDto()
        {
            var total =
                PartidasParticipadas * PontuacaoBeneficioRegras.PartidaParticipante +
                PartidasRegistradas * PontuacaoBeneficioRegras.PartidaRegistrador +
                PartidasComPlacar * PontuacaoBeneficioRegras.PartidaPlacarCompleto +
                Vitorias * PontuacaoBeneficioRegras.PartidaVitoria +
                Grupos * PontuacaoBeneficioRegras.EntradaGrupo +
                PendenciasResolvidas * PontuacaoBeneficioRegras.PendenciaResolvida +
                (PerfilCompleto ? PontuacaoBeneficioRegras.PerfilCompleto : 0);

            return new SaldoInicialRetroativoAtletaDto(
                atletaId,
                nomeAtleta,
                PartidasParticipadas,
                PartidasRegistradas,
                PartidasComPlacar,
                Vitorias,
                Grupos,
                PendenciasResolvidas,
                PerfilCompleto,
                total,
                false);
        }
    }

    private static bool PerfilCompleto(
        string nome,
        string? email,
        NivelAtleta? nivel,
        SexoAtleta? sexo,
        DateTime? dataNascimento)
    {
        return !string.IsNullOrWhiteSpace(nome) &&
            !string.IsNullOrWhiteSpace(email) &&
            nivel.HasValue &&
            sexo.HasValue &&
            dataNascimento.HasValue;
    }
}
