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
