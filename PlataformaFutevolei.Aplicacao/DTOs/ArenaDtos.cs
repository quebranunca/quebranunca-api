using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Aplicacao.DTOs;

public record CriarArenaDto(
    string Nome,
    string? Descricao,
    TipoArena TipoArena,
    int QuantidadeEspacos,
    string? Endereco,
    string? EnderecoResumo,
    string? Cidade,
    string? Estado,
    double? Latitude,
    double? Longitude,
    string? Whatsapp,
    string? Instagram,
    string? Site,
    string? LogoUrl,
    string? LogoPublicId,
    string? CapaUrl,
    string? CapaPublicId,
    bool? Publica,
    bool? Ativa
);

public record AtualizarArenaDto(
    string Nome,
    string? Descricao,
    TipoArena TipoArena,
    int QuantidadeEspacos,
    string? Endereco,
    string? EnderecoResumo,
    string? Cidade,
    string? Estado,
    double? Latitude,
    double? Longitude,
    string? Whatsapp,
    string? Instagram,
    string? Site,
    string? LogoUrl,
    string? LogoPublicId,
    string? CapaUrl,
    string? CapaPublicId,
    bool Publica,
    bool Ativa
);

public record ArenaDto(
    Guid Id,
    string Nome,
    string Slug,
    string? Descricao,
    TipoArena TipoArena,
    int QuantidadeEspacos,
    string? Endereco,
    string? EnderecoResumo,
    string? Cidade,
    string? Estado,
    double? Latitude,
    double? Longitude,
    string? Whatsapp,
    string? Instagram,
    string? Site,
    string? LogoUrl,
    string? LogoPublicId,
    string? CapaUrl,
    string? CapaPublicId,
    bool Publica,
    bool Ativa,
    Guid? UsuarioResponsavelId,
    string? NomeUsuarioResponsavel,
    DateTime DataCriacao,
    DateTime DataAtualizacao
);

public record ArenaFiltroPublicoRequest(
    string? Cidade,
    string? Estado,
    TipoArena? TipoArena,
    string? TermoBusca
);

public record ArenaListagemPublicaResponse(
    Guid Id,
    string Nome,
    string Slug,
    string? DescricaoResumo,
    TipoArena TipoArena,
    string? Cidade,
    string? Estado,
    string? EnderecoResumo,
    int QuantidadeEspacos,
    string? LogoUrl,
    string? CapaUrl,
    string? Instagram,
    string? Whatsapp,
    bool Publica,
    bool Ativa
);

public record ArenaDetalhePublicoResponse(
    Guid Id,
    string Nome,
    string Slug,
    string? Descricao,
    TipoArena TipoArena,
    string? Cidade,
    string? Estado,
    string? Endereco,
    string? EnderecoResumo,
    double? Latitude,
    double? Longitude,
    string? Whatsapp,
    string? Instagram,
    string? Site,
    int QuantidadeEspacos,
    string? LogoUrl,
    string? CapaUrl,
    bool Publica,
    bool Ativa
);

public record ArenaResumoPublicoResponse(
    Guid Id,
    string Nome,
    string Slug,
    TipoArena TipoArena,
    string? Cidade,
    string? Estado,
    string? EnderecoResumo,
    string? LogoUrl,
    int QuantidadeEspacos
);

public record CriarArenaRequest(
    string Nome,
    string? Descricao,
    TipoArena TipoArena,
    string? Endereco,
    string? EnderecoResumo,
    string? Cidade,
    string? Estado,
    double? Latitude,
    double? Longitude,
    string? Whatsapp,
    string? Instagram,
    string? Site,
    int QuantidadeEspacos,
    bool Publica,
    bool PossuiIluminacao,
    bool PossuiEstacionamento,
    bool PossuiVestiario,
    bool PossuiDucha,
    bool PossuiBarRestaurante,
    bool PossuiLoja,
    bool PossuiCobertura
);

public record AtualizarArenaRequest(
    string Nome,
    string? Descricao,
    TipoArena TipoArena,
    string? Endereco,
    string? EnderecoResumo,
    string? Cidade,
    string? Estado,
    double? Latitude,
    double? Longitude,
    string? Whatsapp,
    string? Instagram,
    string? Site,
    int QuantidadeEspacos,
    bool Publica,
    bool PossuiIluminacao,
    bool PossuiEstacionamento,
    bool PossuiVestiario,
    bool PossuiDucha,
    bool PossuiBarRestaurante,
    bool PossuiLoja,
    bool PossuiCobertura
);

public record AtualizarStatusArenaRequest(bool Ativa);

public record AtualizarVisibilidadeArenaRequest(bool Publica);

public record CriarArenaEspacoRequest(
    string Nome,
    TipoEspaco TipoEspaco,
    string? Descricao,
    bool PossuiIluminacao,
    bool PossuiCobertura,
    bool Ativo,
    int? OrdemExibicao);

public record AtualizarArenaEspacoRequest(
    string Nome,
    TipoEspaco TipoEspaco,
    string? Descricao,
    bool PossuiIluminacao,
    bool PossuiCobertura,
    bool Ativo,
    int? OrdemExibicao);

public record AtualizarStatusArenaEspacoRequest(bool Ativo);

public record ArenaEspacoAdminResponse(
    Guid Id,
    Guid ArenaId,
    string Nome,
    TipoEspaco TipoEspaco,
    string? Descricao,
    bool PossuiIluminacao,
    bool PossuiCobertura,
    bool Ativo,
    int? OrdemExibicao);

public record ArenaAdminResumoResponse(
    Guid Id,
    string Nome,
    string Slug,
    TipoArena TipoArena,
    string? Cidade,
    string? Estado,
    string? EnderecoResumo,
    string? LogoUrl,
    string? CapaUrl,
    bool Publica,
    bool Ativa,
    int QuantidadeEspacos,
    PapelArenaResponsavel? PapelUsuario
);

public record ArenaResponsavelResponse(
    Guid UsuarioId,
    string Nome,
    string Email,
    PapelArenaResponsavel Papel
);

public record ArenaAdminDetalheResponse(
    Guid Id,
    string Nome,
    string Slug,
    string? Descricao,
    TipoArena TipoArena,
    string? Endereco,
    string? EnderecoResumo,
    string? Cidade,
    string? Estado,
    double? Latitude,
    double? Longitude,
    string? Whatsapp,
    string? Instagram,
    string? Site,
    int QuantidadeEspacos,
    string? LogoUrl,
    string? CapaUrl,
    bool Publica,
    bool Ativa,
    bool PossuiIluminacao,
    bool PossuiEstacionamento,
    bool PossuiVestiario,
    bool PossuiDucha,
    bool PossuiBarRestaurante,
    bool PossuiLoja,
    bool PossuiCobertura,
    IReadOnlyList<ArenaResponsavelResponse> Responsaveis
);
