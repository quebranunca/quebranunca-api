using Microsoft.EntityFrameworkCore;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;
using PlataformaFutevolei.Infraestrutura.Persistencia;

namespace PlataformaFutevolei.Infraestrutura.Repositorios;

public class PresencaGrupoRepositorio(PlataformaFutevoleiDbContext dbContext) : IPresencaGrupoRepositorio
{
    public async Task<IReadOnlyList<Grupo>> ListarGruposComAgendaAsync(CancellationToken cancellationToken = default)
    {
        return await ConsultaGruposBase()
            .Where(x =>
                x.DiasDaSemana != null &&
                x.HorarioInicio != null &&
                x.HorarioFim != null)
            .AsNoTracking()
            .OrderBy(x => x.Nome)
            .ToListAsync(cancellationToken);
    }

    public Task<Grupo?> ObterGrupoComAgendaAsync(Guid grupoId, CancellationToken cancellationToken = default)
    {
        return ConsultaGruposBase()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == grupoId && x.Ativo, cancellationToken);
    }

    public Task<EncontroGrupo?> ObterEncontroAsync(
        Guid grupoId,
        DateOnly dataJogo,
        CancellationToken cancellationToken = default)
    {
        return dbContext.EncontrosGrupos
            .Include(x => x.Grupo)
                .ThenInclude(x => x.Arena)
            .Include(x => x.Grupo)
                .ThenInclude(x => x.Atletas)
            .Include(x => x.Arena)
            .Include(x => x.Confirmacoes)
                .ThenInclude(x => x.Atleta)
                    .ThenInclude(x => x.Usuario)
            .AsSplitQuery()
            .FirstOrDefaultAsync(
                x => x.GrupoId == grupoId && x.DataJogo == dataJogo,
                cancellationToken);
    }

    public Task<ConfirmacaoPresencaGrupo?> ObterConfirmacaoPorCodigoAsync(
        string codigo,
        CancellationToken cancellationToken = default)
    {
        return dbContext.ConfirmacoesPresencaGrupos
            .Include(x => x.Atleta)
                .ThenInclude(x => x.Usuario)
            .Include(x => x.EncontroGrupo)
                .ThenInclude(x => x.Grupo)
                    .ThenInclude(x => x.Atletas)
            .Include(x => x.EncontroGrupo)
                .ThenInclude(x => x.Arena)
            .FirstOrDefaultAsync(x => x.CodigoAcesso == codigo, cancellationToken);
    }

    public async Task AdicionarEncontroAsync(
        EncontroGrupo encontro,
        CancellationToken cancellationToken = default)
    {
        await dbContext.EncontrosGrupos.AddAsync(encontro, cancellationToken);
    }

    public async Task AdicionarConfirmacaoAsync(
        ConfirmacaoPresencaGrupo confirmacao,
        CancellationToken cancellationToken = default)
    {
        await dbContext.ConfirmacoesPresencaGrupos.AddAsync(confirmacao, cancellationToken);
    }

    public async Task<bool> TentarReservarEnvioWhatsappAsync(
        Guid confirmacaoId,
        DateTime agoraUtc,
        TimeSpan intervaloMinimo,
        int maximoTentativas,
        CancellationToken cancellationToken = default)
    {
        var limiteNovaTentativa = agoraUtc - intervaloMinimo;
        var atualizadas = await dbContext.ConfirmacoesPresencaGrupos
            .Where(x =>
                x.Id == confirmacaoId &&
                x.Status == StatusConfirmacaoPresencaGrupo.Pendente &&
                !x.WhatsappEnviadoEmUtc.HasValue &&
                x.ExpiraEmUtc >= agoraUtc &&
                x.TentativasEnvioWhatsapp < maximoTentativas &&
                (!x.UltimaTentativaEnvioWhatsappEmUtc.HasValue ||
                 x.UltimaTentativaEnvioWhatsappEmUtc <= limiteNovaTentativa))
            .ExecuteUpdateAsync(
                atualizacao => atualizacao
                    .SetProperty(x => x.UltimaTentativaEnvioWhatsappEmUtc, agoraUtc)
                    .SetProperty(x => x.DataAtualizacao, agoraUtc),
                cancellationToken);

        return atualizadas == 1;
    }

    public void AtualizarConfirmacao(ConfirmacaoPresencaGrupo confirmacao)
    {
        dbContext.ConfirmacoesPresencaGrupos.Update(confirmacao);
    }

    private IQueryable<Grupo> ConsultaGruposBase()
    {
        return dbContext.Grupos
            .Include(x => x.Arena)
            .Include(x => x.Atletas)
                .ThenInclude(x => x.Atleta)
                    .ThenInclude(x => x.Usuario)
            .Where(x => x.Ativo);
    }
}
