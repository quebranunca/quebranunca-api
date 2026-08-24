using Microsoft.EntityFrameworkCore;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Infraestrutura.Persistencia;

namespace PlataformaFutevolei.Infraestrutura.Repositorios;

public class NotificacaoUsuarioRepositorio(PlataformaFutevoleiDbContext dbContext) : INotificacaoUsuarioRepositorio
{
    public async Task<IReadOnlyList<NotificacaoUsuario>> ListarPorUsuarioAsync(
        Guid usuarioId, bool somenteNaoLidas, int limite, CancellationToken cancellationToken = default)
    {
        var consulta = dbContext.NotificacoesUsuarios.AsNoTracking()
            .Where(x => x.UsuarioId == usuarioId && x.ArquivadaEmUtc == null);
        if (somenteNaoLidas)
            consulta = consulta.Where(x => x.LidaEmUtc == null);

        return await consulta.OrderByDescending(x => x.DataCriacao)
            .Take(limite)
            .ToListAsync(cancellationToken);
    }

    public Task<NotificacaoUsuario?> ObterPorIdAsync(Guid id, Guid usuarioId, CancellationToken cancellationToken = default) =>
        dbContext.NotificacoesUsuarios.FirstOrDefaultAsync(x => x.Id == id && x.UsuarioId == usuarioId, cancellationToken);

    public async Task<IReadOnlySet<string>> ListarChavesDaOrigemAsync(
        Guid usuarioId, string origem, CancellationToken cancellationToken = default) =>
        (await dbContext.NotificacoesUsuarios.AsNoTracking()
            .Where(x => x.UsuarioId == usuarioId && x.Origem == origem)
            .Select(x => x.ChaveIdempotencia)
            .ToListAsync(cancellationToken)).ToHashSet(StringComparer.Ordinal);

    public Task<int> ContarNaoLidasAsync(Guid usuarioId, CancellationToken cancellationToken = default) =>
        dbContext.NotificacoesUsuarios.AsNoTracking()
            .CountAsync(x => x.UsuarioId == usuarioId && x.LidaEmUtc == null && x.ArquivadaEmUtc == null, cancellationToken);

    public async Task AdicionarIntervaloAsync(IEnumerable<NotificacaoUsuario> notificacoes, CancellationToken cancellationToken = default)
    {
        foreach (var item in notificacoes)
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO notificacoes_usuarios
                    (id, usuario_id, origem, chave_idempotencia, tipo, prioridade, titulo, mensagem,
                     link_acao, texto_acao, referencia_tipo, referencia_id, lida_em_utc, arquivada_em_utc,
                     data_criacao, data_atualizacao)
                VALUES
                    ({item.Id}, {item.UsuarioId}, {item.Origem}, {item.ChaveIdempotencia}, {(int)item.Tipo},
                     {(int)item.Prioridade}, {item.Titulo}, {item.Mensagem}, {item.LinkAcao}, {item.TextoAcao},
                     {item.ReferenciaTipo}, {item.ReferenciaId}, {item.LidaEmUtc}, {item.ArquivadaEmUtc},
                     {item.DataCriacao}, {item.DataAtualizacao})
                ON CONFLICT (usuario_id, origem, chave_idempotencia) DO NOTHING", cancellationToken);
        }
    }

    public Task MarcarTodasComoLidasAsync(Guid usuarioId, DateTime dataUtc, CancellationToken cancellationToken = default) =>
        dbContext.NotificacoesUsuarios
            .Where(x => x.UsuarioId == usuarioId && x.LidaEmUtc == null && x.ArquivadaEmUtc == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.LidaEmUtc, dataUtc)
                .SetProperty(x => x.DataAtualizacao, dataUtc), cancellationToken);

    public void Atualizar(NotificacaoUsuario notificacao) => dbContext.NotificacoesUsuarios.Update(notificacao);
}
