using Microsoft.EntityFrameworkCore;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;
using PlataformaFutevolei.Infraestrutura.Persistencia;

namespace PlataformaFutevolei.Infraestrutura.Repositorios;

public class ArenaResponsavelRepositorio(PlataformaFutevoleiDbContext dbContext) : IArenaResponsavelRepositorio
{
    public Task<bool> UsuarioPodeGerenciarAsync(Guid arenaId, Guid usuarioId, CancellationToken cancellationToken = default)
        => dbContext.ArenaResponsaveis.AnyAsync(
            x => x.ArenaId == arenaId &&
                 x.UsuarioId == usuarioId &&
                 x.Papel == PapelArenaResponsavel.ArenaAdmin &&
                 x.Ativo,
            cancellationToken);

    public async Task AdicionarAsync(ArenaResponsavel responsavel, CancellationToken cancellationToken = default)
    {
        await dbContext.ArenaResponsaveis.AddAsync(responsavel, cancellationToken);
    }
}
