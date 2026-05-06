using Microsoft.EntityFrameworkCore;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;
using PlataformaFutevolei.Infraestrutura.Persistencia;

namespace PlataformaFutevolei.Infraestrutura.Repositorios;

public class PendenciaUsuarioRepositorio(PlataformaFutevoleiDbContext dbContext) : IPendenciaUsuarioRepositorio
{
    public async Task<IReadOnlyList<PendenciaUsuario>> ListarPendentesPorUsuarioAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        return await CriarConsultaDetalhada(usarNoTracking: true)
            .Where(x => x.UsuarioId == usuarioId && x.Status == StatusPendenciaUsuario.Pendente)
            .OrderByDescending(x => x.DataCriacao)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PendenciaUsuario>> ListarPendentesPorPartidaAsync(
        Guid partidaId,
        CancellationToken cancellationToken = default)
    {
        return await CriarConsultaDetalhada(usarNoTracking: false)
            .Where(x => x.PartidaId == partidaId && x.Status == StatusPendenciaUsuario.Pendente)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PendenciaUsuario>> ListarPendentesPorAtletaAsync(
        Guid atletaId,
        CancellationToken cancellationToken = default)
    {
        return await CriarConsultaDetalhada(usarNoTracking: false)
            .Where(x => x.AtletaId == atletaId && x.Status == StatusPendenciaUsuario.Pendente)
            .ToListAsync(cancellationToken);
    }

    public Task<PendenciaUsuario?> ObterPendenteAsync(
        TipoPendenciaUsuario tipo,
        Guid usuarioId,
        Guid? partidaId,
        Guid? atletaId,
        CancellationToken cancellationToken = default)
    {
        return CriarConsultaDetalhada(usarNoTracking: false)
            .FirstOrDefaultAsync(
                x => x.Tipo == tipo &&
                     x.UsuarioId == usuarioId &&
                     x.PartidaId == partidaId &&
                     x.AtletaId == atletaId &&
                     x.Status == StatusPendenciaUsuario.Pendente,
                cancellationToken);
    }

    public Task<PendenciaUsuario?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return CriarConsultaDetalhada(usarNoTracking: false)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<bool> ExistePendentePorUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken = default)
    {
        return await dbContext.PendenciasUsuarios
            .AsNoTracking()
            .AnyAsync(
                x => x.UsuarioId == usuarioId &&
                     x.Status == StatusPendenciaUsuario.Pendente,
                cancellationToken);
    }

    public async Task AdicionarAsync(PendenciaUsuario pendencia, CancellationToken cancellationToken = default)
    {
        await dbContext.PendenciasUsuarios.AddAsync(pendencia, cancellationToken);
    }

    public void Atualizar(PendenciaUsuario pendencia)
    {
        dbContext.PendenciasUsuarios.Update(pendencia);
    }

    private IQueryable<PendenciaUsuario> CriarConsultaDetalhada(bool usarNoTracking)
    {
        var consulta = dbContext.PendenciasUsuarios
            .Include(x => x.Usuario)
            .Include(x => x.Atleta)
                .ThenInclude(x => x!.Usuario)
            .Include(x => x.Partida)
                .ThenInclude(x => x!.CriadoPorUsuario)
            .Include(x => x.Partida)
                .ThenInclude(x => x!.DuplaA)
                    .ThenInclude(x => x.Atleta1)
            .Include(x => x.Partida)
                .ThenInclude(x => x!.DuplaA)
                    .ThenInclude(x => x.Atleta2)
            .Include(x => x.Partida)
                .ThenInclude(x => x!.DuplaB)
                    .ThenInclude(x => x.Atleta1)
            .Include(x => x.Partida)
                .ThenInclude(x => x!.DuplaB)
                    .ThenInclude(x => x.Atleta2);

        return usarNoTracking ? consulta.AsNoTracking() : consulta;
    }
}
