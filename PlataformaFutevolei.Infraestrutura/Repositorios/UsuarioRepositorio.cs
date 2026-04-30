using Microsoft.EntityFrameworkCore;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Infraestrutura.Persistencia;

namespace PlataformaFutevolei.Infraestrutura.Repositorios;

public class UsuarioRepositorio(PlataformaFutevoleiDbContext dbContext) : IUsuarioRepositorio
{
    public async Task<IReadOnlyList<Usuario>> ListarAsync(string? nome, string? email, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Usuarios
            .AsNoTracking()
            .Include(x => x.Atleta)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(nome))
        {
            var nomeNormalizado = nome.Trim().ToLowerInvariant();
            query = query.Where(x => x.Nome.ToLower().Contains(nomeNormalizado));
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            var emailNormalizado = email.Trim().ToLowerInvariant();
            query = query.Where(x => x.Email.ToLower().Contains(emailNormalizado));
        }

        return await query
            .OrderBy(x => x.Nome)
            .ThenBy(x => x.Email)
            .ToListAsync(cancellationToken);
    }

    public Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return dbContext.Usuarios
            .AsNoTracking()
            //.Include(x => x.Atleta)
            .FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
    }

    public Task<Usuario?> ObterPorEmailParaAtualizacaoAsync(string email, CancellationToken cancellationToken = default)
    {
        return dbContext.Usuarios
            .Include(x => x.Atleta)
            .FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
    }

    public Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Usuarios
            .AsNoTracking()
            .Include(x => x.Atleta)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<Usuario?> ObterPorIdParaAtualizacaoAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Usuarios
            .Include(x => x.Atleta)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<Usuario?> ObterPorAtletaIdAsync(Guid atletaId, CancellationToken cancellationToken = default)
    {
        return dbContext.Usuarios
            .AsNoTracking()
            .Include(x => x.Atleta)
            .FirstOrDefaultAsync(x => x.AtletaId == atletaId, cancellationToken);
    }

    public async Task AdicionarAsync(Usuario usuario, CancellationToken cancellationToken = default)
    {
        await dbContext.Usuarios.AddAsync(usuario, cancellationToken);
    }

    public void Atualizar(Usuario usuario)
    {
        dbContext.Usuarios.Update(usuario);
    }
}
