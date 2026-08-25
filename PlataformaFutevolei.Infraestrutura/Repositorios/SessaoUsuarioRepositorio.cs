using Microsoft.EntityFrameworkCore;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Infraestrutura.Persistencia;

namespace PlataformaFutevolei.Infraestrutura.Repositorios;

public sealed class SessaoUsuarioRepositorio(PlataformaFutevoleiDbContext dbContext) : ISessaoUsuarioRepositorio
{
    public Task<SessaoUsuario?> ObterPorIdParaAtualizacaoAsync(Guid id, CancellationToken cancellationToken = default)
        => dbContext.SessoesUsuarios.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task AdicionarAsync(SessaoUsuario sessao, CancellationToken cancellationToken = default)
        => dbContext.SessoesUsuarios.AddAsync(sessao, cancellationToken).AsTask();

    public void Atualizar(SessaoUsuario sessao) => dbContext.SessoesUsuarios.Update(sessao);

    public async Task<bool> RotacionarAsync(
        Guid sessaoId,
        string hashAtual,
        string novoHash,
        DateTime agoraUtc,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var alteradas = await dbContext.SessoesUsuarios
            .Where(x => x.Id == sessaoId && x.RefreshTokenHash == hashAtual && x.RevogadaEmUtc == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.RefreshTokenHash, novoHash)
                .SetProperty(x => x.UltimoUsoEmUtc, agoraUtc)
                .SetProperty(x => x.IpAddress, x => ipAddress ?? x.IpAddress)
                .SetProperty(x => x.UserAgent, x => userAgent ?? x.UserAgent)
                .SetProperty(x => x.DataAtualizacao, agoraUtc), cancellationToken);
        return alteradas == 1;
    }

    public async Task RevogarTodasAsync(Guid usuarioId, DateTime agoraUtc, CancellationToken cancellationToken = default)
    {
        var sessoes = await dbContext.SessoesUsuarios
            .Where(x => x.UsuarioId == usuarioId && x.RevogadaEmUtc == null)
            .ToListAsync(cancellationToken);
        foreach (var sessao in sessoes)
        {
            sessao.Revogar(agoraUtc);
        }
    }
}
