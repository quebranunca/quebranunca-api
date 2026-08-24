using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Aplicacao.DTOs;

public record NotificacaoUsuarioDto(
    Guid Id,
    string Origem,
    TipoNotificacaoUsuario Tipo,
    PrioridadePendenciaUsuario Prioridade,
    string Titulo,
    string Mensagem,
    string? LinkAcao,
    string? TextoAcao,
    string? ReferenciaTipo,
    string? ReferenciaId,
    bool Lida,
    DateTime DataCriacao,
    DateTime? LidaEmUtc);

public record NotificacoesResumoDto(int TotalNaoLidas, int AltaPrioridade);
