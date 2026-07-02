using Microsoft.EntityFrameworkCore;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;
using PlataformaFutevolei.Infraestrutura.Persistencia;

namespace PlataformaFutevolei.Infraestrutura.Repositorios;

public class CodigoAcessoEmailRepositorio(PlataformaFutevoleiDbContext dbContext) : ICodigoAcessoEmailRepositorio
{
    public async Task<IReadOnlyList<CodigoAcessoEmail>> ListarPendentesPorEmailFinalidadeParaAtualizacaoAsync(
        string emailNormalizado,
        FinalidadeCodigoAcessoEmail finalidade,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.CodigosAcessoEmail
            .Where(x =>
                x.EmailNormalizado == emailNormalizado &&
                x.Finalidade == finalidade &&
                x.ConsumidoEmUtc == null)
            .OrderByDescending(x => x.DataCriacao)
            .ToListAsync(cancellationToken);
    }

    public async Task<CodigoAcessoEmail?> ObterAtivoPorEmailFinalidadeParaAtualizacaoAsync(
        string emailNormalizado,
        FinalidadeCodigoAcessoEmail finalidade,
        DateTime dataUtc,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.CodigosAcessoEmail
            .Where(x =>
                x.EmailNormalizado == emailNormalizado &&
                x.Finalidade == finalidade &&
                x.ConsumidoEmUtc == null &&
                x.ExpiraEmUtc >= dataUtc)
            .OrderByDescending(x => x.DataCriacao)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<CodigoAcessoEmail?> ObterPorCadastroTokenHashParaAtualizacaoAsync(
        string cadastroTokenHash,
        DateTime dataUtc,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.CodigosAcessoEmail
            .Where(x =>
                x.Finalidade == FinalidadeCodigoAcessoEmail.CadastroPublico &&
                x.CadastroTokenHash == cadastroTokenHash &&
                x.CadastroTokenExpiraEmUtc != null &&
                x.CadastroTokenExpiraEmUtc >= dataUtc)
            .OrderByDescending(x => x.DataCriacao)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<CodigoAcessoEmail?> ObterPorTokenTemporarioHashParaAtualizacaoAsync(
        string tokenHash,
        DateTime dataUtc,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.CodigosAcessoEmail
            .Where(x =>
                x.CadastroTokenHash == tokenHash &&
                x.CadastroTokenExpiraEmUtc != null &&
                x.CadastroTokenExpiraEmUtc >= dataUtc)
            .OrderByDescending(x => x.DataCriacao)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AdicionarAsync(CodigoAcessoEmail codigoAcessoEmail, CancellationToken cancellationToken = default)
    {
        await dbContext.CodigosAcessoEmail.AddAsync(codigoAcessoEmail, cancellationToken);
    }

    public void Atualizar(CodigoAcessoEmail codigoAcessoEmail)
    {
        dbContext.CodigosAcessoEmail.Update(codigoAcessoEmail);
    }
}
