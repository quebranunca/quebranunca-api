using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Interfaces.Seguranca;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Aplicacao.Servicos;

public class NotificacaoUsuarioServico(
    INotificacaoUsuarioRepositorio notificacaoRepositorio,
    IPendenciaServico pendenciaServico,
    IAutorizacaoUsuarioServico autorizacaoUsuarioServico,
    IUnidadeTrabalho unidadeTrabalho) : INotificacaoUsuarioServico
{
    private const string OrigemPendencias = "pendencias";

    public async Task<IReadOnlyList<NotificacaoUsuarioDto>> ListarMinhasAsync(
        bool somenteNaoLidas = false,
        int limite = 50,
        CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        await SincronizarPendenciasAsync(usuario.Id, cancellationToken);
        var itens = await notificacaoRepositorio.ListarPorUsuarioAsync(
            usuario.Id, somenteNaoLidas, Math.Clamp(limite, 1, 100), cancellationToken);
        return itens.Select(ParaDto).ToList();
    }

    public async Task<NotificacoesResumoDto> ObterResumoAsync(CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        await SincronizarPendenciasAsync(usuario.Id, cancellationToken);
        var naoLidas = await notificacaoRepositorio.ListarPorUsuarioAsync(usuario.Id, true, 100, cancellationToken);
        return new NotificacoesResumoDto(
            await notificacaoRepositorio.ContarNaoLidasAsync(usuario.Id, cancellationToken),
            naoLidas.Count(x => x.Prioridade == PrioridadePendenciaUsuario.Alta));
    }

    public async Task<NotificacaoUsuarioDto> MarcarComoLidaAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        var notificacao = await notificacaoRepositorio.ObterPorIdAsync(id, usuario.Id, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Notificação não encontrada.");
        if (notificacao.LidaEmUtc is null)
        {
            notificacao.LidaEmUtc = DateTime.UtcNow;
            notificacao.AtualizarDataModificacao();
            notificacaoRepositorio.Atualizar(notificacao);
            await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
        }
        return ParaDto(notificacao);
    }

    public async Task MarcarTodasComoLidasAsync(CancellationToken cancellationToken = default)
    {
        var usuario = await autorizacaoUsuarioServico.ObterUsuarioAtualObrigatorioAsync(cancellationToken);
        await SincronizarPendenciasAsync(usuario.Id, cancellationToken);
        await notificacaoRepositorio.MarcarTodasComoLidasAsync(usuario.Id, DateTime.UtcNow, cancellationToken);
    }

    private async Task SincronizarPendenciasAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        var pendencias = await pendenciaServico.ListarMinhasAsync(cancellationToken);
        var chavesExistentes = await notificacaoRepositorio.ListarChavesDaOrigemAsync(
            usuarioId, OrigemPendencias, cancellationToken);
        var novas = pendencias
            .Where(x => x.Status == StatusPendenciaUsuario.Pendente && !chavesExistentes.Contains(x.Id.ToString("N")))
            .Select(x => CriarDaPendencia(usuarioId, x))
            .ToList();
        if (novas.Count == 0)
            return;

        await notificacaoRepositorio.AdicionarIntervaloAsync(novas, cancellationToken);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
    }

    private static NotificacaoUsuario CriarDaPendencia(Guid usuarioId, PendenciaUsuarioDto pendencia)
    {
        var (titulo, mensagem) = pendencia.Tipo switch
        {
            TipoPendenciaUsuario.AprovarPartida => ("Resultado aguardando aprovação", "Revise o resultado registrado e confirme se está correto."),
            TipoPendenciaUsuario.ConfirmarParticipacaoPartida => ("Confirme sua participação", "Uma partida foi registrada com seu nome e precisa da sua confirmação."),
            TipoPendenciaUsuario.ResponderCancelamentoPartida => ("Pedido de cancelamento", "Uma partida possui um pedido de cancelamento aguardando sua resposta."),
            TipoPendenciaUsuario.CompletarContatoAtletaDaPartida => ("Complete o vínculo de atleta", "Informe o contato ou vincule o atleta correto para concluir o cadastro."),
            _ => ("Ação necessária", "Há uma nova pendência aguardando sua atenção.")
        };

        return new NotificacaoUsuario
        {
            UsuarioId = usuarioId,
            Origem = OrigemPendencias,
            ChaveIdempotencia = pendencia.Id.ToString("N"),
            Tipo = TipoNotificacaoUsuario.AcaoNecessaria,
            Prioridade = pendencia.Prioridade,
            Titulo = titulo,
            Mensagem = mensagem,
            LinkAcao = "/app/pendencias",
            TextoAcao = "Resolver agora",
            ReferenciaTipo = "pendencia",
            ReferenciaId = pendencia.Id.ToString()
        };
    }

    private static NotificacaoUsuarioDto ParaDto(NotificacaoUsuario item) => new(
        item.Id, item.Origem, item.Tipo, item.Prioridade, item.Titulo, item.Mensagem,
        item.LinkAcao, item.TextoAcao, item.ReferenciaTipo, item.ReferenciaId,
        item.LidaEmUtc.HasValue, item.DataCriacao, item.LidaEmUtc);
}
