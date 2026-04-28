using Microsoft.EntityFrameworkCore;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Utilitarios;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;
using PlataformaFutevolei.Infraestrutura.Persistencia;

namespace PlataformaFutevolei.Infraestrutura.Repositorios;

public class AtletaRepositorio(PlataformaFutevoleiDbContext dbContext) : IAtletaRepositorio
{
    public async Task<IReadOnlyList<Atleta>> ListarAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Atletas
            .AsNoTracking()
            .OrderBy(x => x.Nome)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Atleta>> ListarComEmailEmPartidasSemUsuarioAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Atletas
            .AsNoTracking()
            .Where(x => x.Email != null && x.Email != string.Empty)
            .Where(x => x.Usuario == null)
            .Where(x => !dbContext.Usuarios.Any(usuario => usuario.Email.ToLower() == x.Email!.ToLower()))
            .Where(x =>
                x.DuplasComoAtleta1.Any(dupla =>
                    dupla.PartidasComoDuplaA.Any() ||
                    dupla.PartidasComoDuplaB.Any()) ||
                x.DuplasComoAtleta2.Any(dupla =>
                    dupla.PartidasComoDuplaA.Any() ||
                    dupla.PartidasComoDuplaB.Any()))
            .OrderBy(x => x.Nome)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Atleta>> ListarInscritosPorOrganizadorAsync(
        Guid usuarioOrganizadorId,
        CancellationToken cancellationToken = default)
    {
        var atleta1Ids = dbContext.InscricoesCampeonato
            .AsNoTracking()
            .Where(x =>
                x.Status == StatusInscricaoCampeonato.Ativa &&
                x.Competicao.UsuarioOrganizadorId == usuarioOrganizadorId)
            .Select(x => x.Dupla.Atleta1Id);

        var atleta2Ids = dbContext.InscricoesCampeonato
            .AsNoTracking()
            .Where(x =>
                x.Status == StatusInscricaoCampeonato.Ativa &&
                x.Competicao.UsuarioOrganizadorId == usuarioOrganizadorId)
            .Select(x => x.Dupla.Atleta2Id);

        return await dbContext.Atletas
            .AsNoTracking()
            .Where(x => atleta1Ids.Contains(x.Id) || atleta2Ids.Contains(x.Id))
            .OrderBy(x => x.Nome)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> PertenceAoOrganizadorAsync(
        Guid atletaId,
        Guid usuarioOrganizadorId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.InscricoesCampeonato
            .AsNoTracking()
            .AnyAsync(
                x => x.Status == StatusInscricaoCampeonato.Ativa &&
                     x.Competicao.UsuarioOrganizadorId == usuarioOrganizadorId &&
                     (x.Dupla.Atleta1Id == atletaId || x.Dupla.Atleta2Id == atletaId),
                cancellationToken);
    }

    public async Task<IReadOnlyList<Atleta>> BuscarAsync(string? termo, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Atletas
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(termo))
        {
            var termoNormalizado = termo.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.Nome.ToLower().Contains(termoNormalizado) ||
                (x.Apelido != null && x.Apelido.ToLower().Contains(termoNormalizado)) ||
                (x.Email != null && x.Email.ToLower().Contains(termoNormalizado)) ||
                (x.Telefone != null && x.Telefone.ToLower().Contains(termoNormalizado)) ||
                (x.Cpf != null && x.Cpf.ToLower().Contains(termoNormalizado)) ||
                (x.Instagram != null && x.Instagram.ToLower().Contains(termoNormalizado)));
        }

        return await query
            .OrderBy(x => x.Nome)
            .Take(20)
            .ToListAsync(cancellationToken);
    }

    public Task<Atleta?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Atletas
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<Atleta?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default)
    {
        var nomeNormalizado = NormalizadorNomeAtleta.NormalizarTexto(nome);
        var chave = nomeNormalizado.ToLower();

        return dbContext.Atletas
            .FirstOrDefaultAsync(x => x.Nome.ToLower() == chave, cancellationToken);
    }

    public async Task<IReadOnlyList<Atleta>> ListarPorNomeAsync(string nome, CancellationToken cancellationToken = default)
    {
        var nomeNormalizado = NormalizadorNomeAtleta.NormalizarTexto(nome);
        var chave = nomeNormalizado.ToLowerInvariant();

        return await dbContext.Atletas
            .Include(x => x.Usuario)
            .Where(x => x.Nome.ToLower() == chave)
            .OrderBy(x => x.DataCriacao)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Atleta>> ListarPorEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var emailNormalizado = NormalizadorNomeAtleta.NormalizarTexto(email).ToLowerInvariant();

        return await dbContext.Atletas
            .Include(x => x.Usuario)
            .Where(x => x.Email != null && x.Email.ToLower() == emailNormalizado)
            .OrderBy(x => x.DataCriacao)
            .ToListAsync(cancellationToken);
    }

    public async Task AdicionarAsync(Atleta atleta, CancellationToken cancellationToken = default)
    {
        await dbContext.Atletas.AddAsync(atleta, cancellationToken);
    }

    public void Atualizar(Atleta atleta)
    {
        dbContext.Atletas.Update(atleta);
    }

    public void Remover(Atleta atleta)
    {
        dbContext.Atletas.Remove(atleta);
    }
}
