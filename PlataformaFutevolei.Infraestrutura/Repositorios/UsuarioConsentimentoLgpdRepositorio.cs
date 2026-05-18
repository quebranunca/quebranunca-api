using Microsoft.EntityFrameworkCore;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Infraestrutura.Persistencia;

namespace PlataformaFutevolei.Infraestrutura.Repositorios;

public class UsuarioConsentimentoLgpdRepositorio(PlataformaFutevoleiDbContext dbContext) : IUsuarioConsentimentoLgpdRepositorio
{
    public async Task<UsuarioConsentimentoLgpd?> ObterUltimoPorUsuarioAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.UsuariosConsentimentosLgpd
            .AsNoTracking()
            .Where(x => x.UsuarioId == usuarioId)
            .OrderByDescending(x => x.AceitoEm)
            .ThenByDescending(x => x.DataCriacao)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AdicionarAsync(UsuarioConsentimentoLgpd consentimento, CancellationToken cancellationToken = default)
    {
        await dbContext.UsuariosConsentimentosLgpd.AddAsync(consentimento, cancellationToken);
    }
}
