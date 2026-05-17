using Microsoft.EntityFrameworkCore;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;
using PlataformaFutevolei.Infraestrutura.Persistencia;

namespace PlataformaFutevolei.Infraestrutura.Repositorios;

public class SolicitacaoAcessoRepositorio(PlataformaFutevoleiDbContext dbContext) : ISolicitacaoAcessoRepositorio
{
    public async Task<IReadOnlyList<SolicitacaoAcesso>> ListarAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.SolicitacoesAcesso
            .AsNoTracking()
            .OrderByDescending(x => x.DataCriacao)
            .ToListAsync(cancellationToken);
    }

    public Task<SolicitacaoAcesso?> ObterPorIdParaAtualizacaoAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.SolicitacoesAcesso
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<bool> ExistePendentePorEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return dbContext.SolicitacoesAcesso
            .AsNoTracking()
            .AnyAsync(
                x => x.Email == email && x.Status == StatusSolicitacaoAcesso.Pendente,
                cancellationToken);
    }

    public async Task AdicionarAsync(SolicitacaoAcesso solicitacaoAcesso, CancellationToken cancellationToken = default)
    {
        await dbContext.SolicitacoesAcesso.AddAsync(solicitacaoAcesso, cancellationToken);
    }

    public void Atualizar(SolicitacaoAcesso solicitacaoAcesso)
    {
        solicitacaoAcesso.AtualizarDataModificacao();
        dbContext.SolicitacoesAcesso.Update(solicitacaoAcesso);
    }
}
