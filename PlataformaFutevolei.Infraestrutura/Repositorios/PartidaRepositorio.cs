using Microsoft.EntityFrameworkCore;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;
using PlataformaFutevolei.Infraestrutura.Persistencia;

namespace PlataformaFutevolei.Infraestrutura.Repositorios;

public class PartidaRepositorio(PlataformaFutevoleiDbContext dbContext) : IPartidaRepositorio
{
    private const string NomeCategoriaSemCategoria = "Sem categoria";

    public async Task<IReadOnlyList<Partida>> ListarPorCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default)
    {
        return await CriarConsultaDetalhadaPartidas()
            .Where(x => x.CategoriaCompeticao.CompeticaoId == competicaoId)
            .OrderBy(x => x.CategoriaCompeticao.Nome)
            .ThenBy(x => x.Status)
            .ThenBy(x => x.FaseCampeonato)
            .ThenBy(x => x.DataPartida ?? DateTime.MaxValue)
            .ThenBy(x => x.DataCriacao)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Partida>> ListarPorCategoriaAsync(Guid categoriaId, CancellationToken cancellationToken = default)
    {
        return await CriarConsultaDetalhadaPartidas()
            .Where(x => x.CategoriaCompeticaoId == categoriaId)
            .OrderBy(x => x.Status)
            .ThenBy(x => x.FaseCampeonato)
            .ThenBy(x => x.DataPartida ?? DateTime.MaxValue)
            .ThenBy(x => x.DataCriacao)
            .ToListAsync(cancellationToken);
    }

    public Task<Partida?> ObterUltimaDoGrupoAsync(Guid competicaoId, CancellationToken cancellationToken = default)
    {
        return CriarConsultaDetalhadaPartidas()
            .Where(x => x.CategoriaCompeticao.CompeticaoId == competicaoId)
            .Where(x => x.Status == StatusPartida.Encerrada)
            .Where(x => x.DuplaAId.HasValue && x.DuplaBId.HasValue)
            .OrderByDescending(x => x.DataPartida ?? x.DataCriacao)
            .ThenByDescending(x => x.DataCriacao)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Partida>> ListarComAtletasPendentesPorUsuarioCriadorAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        return await CriarConsultaDetalhadaPartidas()
            .Where(x => x.CriadoPorUsuarioId == usuarioId)
            .Where(x =>
                x.DuplaA.Atleta1.Usuario == null ||
                x.DuplaA.Atleta2.Usuario == null ||
                x.DuplaB.Atleta1.Usuario == null ||
                x.DuplaB.Atleta2.Usuario == null)
            .OrderByDescending(x => x.DataPartida ?? x.DataCriacao)
            .ThenByDescending(x => x.DataCriacao)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Partida>> ListarComPendenteDeVinculoPorAtletaAsync(
        Guid atletaId,
        CancellationToken cancellationToken = default)
    {
        return await CriarConsultaDetalhadaPartidas(usarNoTracking: false)
            .Where(x => x.Status == StatusPartida.Encerrada)
            .Where(x => x.StatusAprovacao == StatusAprovacaoPartida.PendenteDeVinculos)
            .Where(x =>
                x.DuplaA.Atleta1Id == atletaId ||
                x.DuplaA.Atleta2Id == atletaId ||
                x.DuplaB.Atleta1Id == atletaId ||
                x.DuplaB.Atleta2Id == atletaId)
            .OrderByDescending(x => x.DataPartida ?? x.DataCriacao)
            .ThenByDescending(x => x.DataCriacao)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExisteAtletaPendenteEmPartidaCriadaPorUsuarioAsync(
        Guid usuarioId,
        Guid atletaId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Partidas
            .AsNoTracking()
            .AnyAsync(
                x => x.CriadoPorUsuarioId == usuarioId &&
                     (x.DuplaA.Atleta1Id == atletaId ||
                      x.DuplaA.Atleta2Id == atletaId ||
                      x.DuplaB.Atleta1Id == atletaId ||
                      x.DuplaB.Atleta2Id == atletaId) &&
                     (x.DuplaA.Atleta1Id == atletaId && x.DuplaA.Atleta1.Usuario == null ||
                      x.DuplaA.Atleta2Id == atletaId && x.DuplaA.Atleta2.Usuario == null ||
                      x.DuplaB.Atleta1Id == atletaId && x.DuplaB.Atleta1.Usuario == null ||
                      x.DuplaB.Atleta2Id == atletaId && x.DuplaB.Atleta2.Usuario == null),
                cancellationToken);
    }

    public async Task<IReadOnlyList<Partida>> ListarParaRankingPorLigaAsync(Guid ligaId, CancellationToken cancellationToken = default)
    {
        return await CriarConsultaRanking()
            .Where(x => x.StatusAprovacao != StatusAprovacaoPartida.Contestada)
            .Where(x => x.CategoriaCompeticao.Competicao.LigaId == ligaId)
            .OrderByDescending(x => x.DataPartida)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Partida>> ListarParaRankingGeralAsync(
        Guid? usuarioOrganizadorId,
        CancellationToken cancellationToken = default)
    {
        var consulta = CriarConsultaRanking()
            .Where(x => x.StatusAprovacao != StatusAprovacaoPartida.Contestada);

        if (usuarioOrganizadorId.HasValue)
        {
            consulta = consulta.Where(x => x.CategoriaCompeticao.Competicao.UsuarioOrganizadorId == usuarioOrganizadorId.Value);
        }

        return await consulta
            .OrderByDescending(x => x.DataPartida)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Partida>> ListarParaRankingSemCompeticaoOuCategoriaAsync(
        Guid? usuarioOrganizadorId,
        CancellationToken cancellationToken = default)
    {
        var consulta = CriarConsultaRanking()
            .Where(x => !x.CategoriaCompeticao.Competicao.LigaId.HasValue)
            .Where(x => x.CategoriaCompeticao.Nome == NomeCategoriaSemCategoria)
            .Where(x => x.StatusAprovacao != StatusAprovacaoPartida.Contestada);

        if (usuarioOrganizadorId.HasValue)
        {
            consulta = consulta.Where(x => x.CategoriaCompeticao.Competicao.UsuarioOrganizadorId == usuarioOrganizadorId.Value);
        }

        return await consulta
            .OrderByDescending(x => x.DataPartida)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Partida>> ListarParaRankingPorCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default)
    {
        return await CriarConsultaRanking()
            .Where(x => x.StatusAprovacao != StatusAprovacaoPartida.Contestada)
            .Where(x => x.CategoriaCompeticao.CompeticaoId == competicaoId)
            .OrderByDescending(x => x.DataPartida)
            .ToListAsync(cancellationToken);
    }

    public async Task<Guid?> ObterUltimaCompeticaoComPartidaEncerradaAsync(
        Guid? usuarioOrganizadorId,
        Guid? atletaId,
        CancellationToken cancellationToken = default)
    {
        var consulta = dbContext.Partidas
            .AsNoTracking()
            .Where(x => x.Status == StatusPartida.Encerrada)
            .Where(x =>
                x.StatusAprovacao == StatusAprovacaoPartida.Aprovada ||
                (!x.CategoriaCompeticao.Competicao.LigaId.HasValue &&
                 x.CategoriaCompeticao.Nome == NomeCategoriaSemCategoria &&
                 x.StatusAprovacao != StatusAprovacaoPartida.Contestada));

        if (atletaId.HasValue)
        {
            consulta = consulta.Where(x =>
                x.DuplaA.Atleta1Id == atletaId.Value ||
                x.DuplaA.Atleta2Id == atletaId.Value ||
                x.DuplaB.Atleta1Id == atletaId.Value ||
                x.DuplaB.Atleta2Id == atletaId.Value);
        }
        else if (usuarioOrganizadorId.HasValue)
        {
            consulta = consulta.Where(x => x.CategoriaCompeticao.Competicao.UsuarioOrganizadorId == usuarioOrganizadorId.Value);
        }

        return await consulta
            .OrderByDescending(x => x.DataPartida ?? x.DataCriacao)
            .ThenByDescending(x => x.DataCriacao)
            .Select(x => (Guid?)x.CategoriaCompeticao.CompeticaoId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<UsuarioResumoDto> ObterResumoUsuarioPorAtletaAsync(
        Guid atletaId,
        CancellationToken cancellationToken = default)
    {
        var duplasDoAtleta = dbContext.Duplas
            .AsNoTracking()
            .Where(x => x.Atleta1Id == atletaId || x.Atleta2Id == atletaId)
            .Select(x => x.Id);

        var partidasDoAtleta = dbContext.Partidas
            .AsNoTracking()
            .Where(x =>
                (x.DuplaAId.HasValue && duplasDoAtleta.Contains(x.DuplaAId.Value)) ||
                (x.DuplaBId.HasValue && duplasDoAtleta.Contains(x.DuplaBId.Value)));

        var partidasAprovadas = partidasDoAtleta
            .Where(x => x.Status == StatusPartida.Encerrada)
            .Where(x => x.StatusAprovacao == StatusAprovacaoPartida.Aprovada)
            .Where(x => x.DuplaVencedoraId.HasValue);

        var totalPartidas = await partidasAprovadas.CountAsync(cancellationToken);
        var totalVitorias = await partidasAprovadas
            .CountAsync(
                x => duplasDoAtleta.Contains(x.DuplaVencedoraId!.Value),
                cancellationToken);
        var totalDerrotas = totalPartidas - totalVitorias;
        var totalPartidasPendentes = await partidasDoAtleta
            .Where(x => x.Status == StatusPartida.Encerrada)
            .CountAsync(
                x => x.StatusAprovacao == StatusAprovacaoPartida.PendenteAprovacao ||
                     x.StatusAprovacao == StatusAprovacaoPartida.PendenteDeVinculos,
                cancellationToken);
        var percentualAproveitamento = totalPartidas == 0
            ? 0
            : decimal.Round(totalVitorias * 100m / totalPartidas, 2);

        return new UsuarioResumoDto(
            totalPartidas,
            totalVitorias,
            totalDerrotas,
            percentualAproveitamento,
            totalPartidasPendentes);
    }

    public Task<Partida?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return CriarConsultaDetalhadaPartidas(usarNoTracking: false)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task AdicionarAsync(Partida partida, CancellationToken cancellationToken = default)
    {
        // A partida é persistida pelas FKs. Limpar as navegações evita que o EF tente
        // anexar novamente grafos já materializados com instâncias duplicadas de Atleta.
        partida.CategoriaCompeticao = null!;
        partida.CriadoPorUsuario = null;
        partida.DuplaA = null!;
        partida.DuplaB = null!;
        partida.DuplaVencedora = null;

        await dbContext.Partidas.AddAsync(partida, cancellationToken);
    }

    public void Atualizar(Partida partida)
    {
        var partidaPersistida = dbContext.ChangeTracker
            .Entries<Partida>()
            .FirstOrDefault(x => x.Entity.Id == partida.Id)?
            .Entity;

        if (partidaPersistida is not null)
        {
            if (!ReferenceEquals(partidaPersistida, partida))
            {
                dbContext.Entry(partidaPersistida).CurrentValues.SetValues(partida);
            }

            return;
        }

        // A atualização usa as FKs. Limpar as navegações evita que o EF tente anexar
        // outro grafo materializado para a mesma partida durante a progressão da chave.
        partida.CategoriaCompeticao = null!;
        partida.CriadoPorUsuario = null;
        partida.DuplaA = null;
        partida.DuplaB = null;
        partida.DuplaVencedora = null;
        dbContext.Partidas.Update(partida);
    }

    public void Remover(Partida partida)
    {
        var partidaPersistida = dbContext.ChangeTracker
            .Entries<Partida>()
            .FirstOrDefault(x => x.Entity.Id == partida.Id)?
            .Entity;

        if (partidaPersistida is not null)
        {
            dbContext.Partidas.Remove(partidaPersistida);
            return;
        }

        partida.CategoriaCompeticao = null!;
        partida.DuplaA = null!;
        partida.DuplaB = null!;
        partida.DuplaVencedora = null;
        dbContext.Entry(partida).State = EntityState.Deleted;
    }

    private IQueryable<Partida> CriarConsultaDetalhadaPartidas(bool usarNoTracking = true)
    {
        var consulta = dbContext.Partidas
            .Include(x => x.CategoriaCompeticao)
                .ThenInclude(x => x.Competicao)
            .Include(x => x.DuplaA)
                .ThenInclude(x => x.Atleta1)
                    .ThenInclude(x => x.Usuario)
            .Include(x => x.DuplaA)
                .ThenInclude(x => x.Atleta2)
                    .ThenInclude(x => x.Usuario)
            .Include(x => x.DuplaB)
                .ThenInclude(x => x.Atleta1)
                    .ThenInclude(x => x.Usuario)
            .Include(x => x.DuplaB)
                .ThenInclude(x => x.Atleta2)
                    .ThenInclude(x => x.Usuario)
            .Include(x => x.DuplaVencedora);

        return usarNoTracking ? consulta.AsNoTracking() : consulta;
    }

    private IQueryable<Partida> CriarConsultaRanking()
    {
        return dbContext.Partidas
            .AsNoTracking()
            .Include(x => x.CategoriaCompeticao)
                .ThenInclude(x => x.Competicao)
                    .ThenInclude(x => x.RegraCompeticao)
            .Include(x => x.DuplaA)
                .ThenInclude(x => x.Atleta1)
                    .ThenInclude(x => x.Usuario)
            .Include(x => x.DuplaA)
                .ThenInclude(x => x.Atleta2)
                    .ThenInclude(x => x.Usuario)
            .Include(x => x.DuplaB)
                .ThenInclude(x => x.Atleta1)
                    .ThenInclude(x => x.Usuario)
            .Include(x => x.DuplaB)
                .ThenInclude(x => x.Atleta2)
                    .ThenInclude(x => x.Usuario)
            .Where(x => x.Status == StatusPartida.Encerrada);
    }
}
