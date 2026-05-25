using Microsoft.EntityFrameworkCore;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;
using PlataformaFutevolei.Infraestrutura.Persistencia;

namespace PlataformaFutevolei.Infraestrutura.Repositorios;

public class ArenaRepositorio(PlataformaFutevoleiDbContext dbContext) : IArenaRepositorio
{
    public async Task<IReadOnlyList<ArenaListagemPublicaResponse>> ListarPublicasAsync(
        ArenaFiltroPublicoRequest filtro,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Arenas
            .AsNoTracking()
            .Where(x => x.Ativa && x.Publica);

        if (filtro.Cidade is not null)
        {
            query = query.Where(x => x.Cidade != null && EF.Functions.ILike(x.Cidade, filtro.Cidade));
        }

        if (filtro.Estado is not null)
        {
            query = query.Where(x => x.Estado != null && EF.Functions.ILike(x.Estado, filtro.Estado));
        }

        if (filtro.TipoArena.HasValue)
        {
            query = query.Where(x => x.TipoArena == filtro.TipoArena.Value);
        }

        if (filtro.TermoBusca is not null)
        {
            var termo = $"%{filtro.TermoBusca}%";
            query = query.Where(x =>
                EF.Functions.ILike(x.Nome, termo) ||
                (x.Cidade != null && EF.Functions.ILike(x.Cidade, termo)) ||
                (x.Estado != null && EF.Functions.ILike(x.Estado, termo)));
        }

        return await query
            .OrderBy(x => x.Nome)
            .Select(x => new ArenaListagemPublicaResponse(
                x.Id,
                x.Nome,
                x.Slug,
                x.Descricao,
                x.TipoArena,
                x.Cidade,
                x.Estado,
                x.EnderecoResumo,
                x.QuantidadeEspacos,
                x.LogoUrl,
                x.CapaUrl,
                x.Instagram,
                x.Whatsapp,
                x.Publica,
                x.Ativa))
            .ToListAsync(cancellationToken);
    }

    public Task<ArenaDetalhePublicoResponse?> ObterPublicaPorSlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        var slugNormalizado = slug.ToLowerInvariant();

        return dbContext.Arenas
            .AsNoTracking()
            .Where(x => x.Ativa && x.Publica && x.Slug.ToLower() == slugNormalizado)
            .Select(x => new ArenaDetalhePublicoResponse(
                x.Id,
                x.Nome,
                x.Slug,
                x.Descricao,
                x.TipoArena,
                x.Cidade,
                x.Estado,
                x.Endereco,
                x.EnderecoResumo,
                x.Latitude,
                x.Longitude,
                x.Whatsapp,
                x.Instagram,
                x.Site,
                x.QuantidadeEspacos,
                x.LogoUrl,
                x.CapaUrl,
                x.Publica,
                x.Ativa))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<ArenaResumoPublicoResponse?> ObterResumoPublicoAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Arenas
            .AsNoTracking()
            .Where(x => x.Id == id && x.Ativa && x.Publica)
            .Select(x => new ArenaResumoPublicoResponse(
                x.Id,
                x.Nome,
                x.Slug,
                x.TipoArena,
                x.Cidade,
                x.Estado,
                x.EnderecoResumo,
                x.LogoUrl,
                x.QuantidadeEspacos))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Arena>> ListarAdministradasAsync(
        Guid usuarioId,
        bool incluirTodas,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Arenas
            .AsNoTracking()
            .Include(x => x.Responsaveis)
                .ThenInclude(x => x.Usuario)
            .AsQueryable();

        if (!incluirTodas)
        {
            query = query.Where(x => x.Responsaveis.Any(r =>
                r.UsuarioId == usuarioId &&
                r.Papel == PapelArenaResponsavel.ArenaAdmin &&
                r.Ativo));
        }

        return await query.OrderBy(x => x.Nome).ToListAsync(cancellationToken);
    }

    public Task<Arena?> ObterAdminPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Arenas
            .AsNoTracking()
            .Include(x => x.Responsaveis.Where(r => r.Ativo))
                .ThenInclude(x => x.Usuario)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Arena>> ListarAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Arenas
            .AsNoTracking()
            .Include(x => x.Responsaveis)
                .ThenInclude(x => x.Usuario)
            .OrderBy(x => x.Nome)
            .ToListAsync(cancellationToken);
    }

    public Task<Arena?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Arenas
            .Include(x => x.Responsaveis)
                .ThenInclude(x => x.Usuario)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<Arena?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default)
    {
        var nomeNormalizado = nome.Trim().ToLowerInvariant();
        return dbContext.Arenas.FirstOrDefaultAsync(x => x.Nome.ToLower() == nomeNormalizado, cancellationToken);
    }

    public Task<bool> ExisteSlugAsync(string slug, Guid? idIgnorado, CancellationToken cancellationToken = default)
        => dbContext.Arenas.AnyAsync(x => x.Slug == slug && x.Id != idIgnorado, cancellationToken);

    public async Task<IReadOnlyList<ArenaEspaco>> ListarEspacosPorArenaAsync(
        Guid arenaId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.ArenaEspacos
            .AsNoTracking()
            .Where(x => x.ArenaId == arenaId)
            .OrderBy(x => x.OrdemExibicao ?? int.MaxValue)
            .ThenBy(x => x.Nome)
            .ToListAsync(cancellationToken);
    }

    public Task<ArenaEspaco?> ObterEspacoPorIdEArenaAsync(
        Guid arenaId,
        Guid espacoId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.ArenaEspacos
            .FirstOrDefaultAsync(x => x.ArenaId == arenaId && x.Id == espacoId, cancellationToken);
    }

    public async Task AdicionarAsync(Arena arena, CancellationToken cancellationToken = default)
    {
        await dbContext.Arenas.AddAsync(arena, cancellationToken);
    }

    public async Task AdicionarEspacoAsync(ArenaEspaco espaco, CancellationToken cancellationToken = default)
    {
        await dbContext.ArenaEspacos.AddAsync(espaco, cancellationToken);
    }

    public void Atualizar(Arena arena)
    {
        dbContext.Arenas.Update(arena);
    }

    public void AtualizarEspaco(ArenaEspaco espaco)
    {
        dbContext.ArenaEspacos.Update(espaco);
    }

    public void Remover(Arena arena)
    {
        dbContext.Arenas.Remove(arena);
    }
}
