using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Aplicacao.DTOs;

public record CriarFormatoCampeonatoDto(
    string Nome,
    string? Descricao,
    TipoFormatoCampeonato TipoFormato,
    bool Ativo,
    int? QuantidadeGrupos,
    int? ClassificadosPorGrupo,
    bool GeraMataMataAposGrupos,
    bool TurnoEVolta,
    string? TipoChave,
    int? QuantidadeDerrotasParaEliminacao,
    bool PermiteCabecaDeChave,
    bool DisputaTerceiroLugar
);

public record AtualizarFormatoCampeonatoDto(
    string Nome,
    string? Descricao,
    TipoFormatoCampeonato TipoFormato,
    bool Ativo,
    int? QuantidadeGrupos,
    int? ClassificadosPorGrupo,
    bool GeraMataMataAposGrupos,
    bool TurnoEVolta,
    string? TipoChave,
    int? QuantidadeDerrotasParaEliminacao,
    bool PermiteCabecaDeChave,
    bool DisputaTerceiroLugar
);

public record FormatoCampeonatoDto(
    Guid Id,
    string Nome,
    string? Descricao,
    TipoFormatoCampeonato TipoFormato,
    bool Ativo,
    int? QuantidadeGrupos,
    int? ClassificadosPorGrupo,
    bool GeraMataMataAposGrupos,
    bool TurnoEVolta,
    string? TipoChave,
    int? QuantidadeDerrotasParaEliminacao,
    bool PermiteCabecaDeChave,
    bool DisputaTerceiroLugar,
    bool EhPadrao,
    DateTime DataCriacao,
    DateTime DataAtualizacao
);
