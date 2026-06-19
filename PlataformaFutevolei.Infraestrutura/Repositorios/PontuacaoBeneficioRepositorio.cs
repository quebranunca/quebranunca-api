using Microsoft.EntityFrameworkCore;
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

    public Task<PontuacaoBeneficioAtleta?> ObterSaldoPorAtletaParaAtualizacaoAsync(
        Guid atletaId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.PontuacoesBeneficiosAtletas
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
        return dbContext.ResgatesBeneficiosPontuacao
            .Include(x => x.Beneficio)
            .FirstOrDefaultAsync(x => x.Id == resgateId, cancellationToken);
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
        dbContext.PontuacoesBeneficiosAtletas.Update(saldo);
    }

    public void AtualizarResgate(ResgateBeneficioPontuacao resgate)
    {
        dbContext.ResgatesBeneficiosPontuacao.Update(resgate);
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
}
