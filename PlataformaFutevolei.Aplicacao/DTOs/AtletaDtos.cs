using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Aplicacao.DTOs;

public record CriarAtletaDto(
    string Nome,
    string? Apelido,
    string? Telefone,
    string? Email,
    string? Instagram,
    string? Cpf,
    string? Bairro,
    string? Cidade,
    string? Estado,
    bool CadastroPendente,
    NivelAtleta? Nivel,
    LadoAtleta Lado,
    DateTime? DataNascimento
);

public record AtualizarAtletaDto(
    string Nome,
    string? Apelido,
    string? Telefone,
    string? Email,
    string? Instagram,
    string? Cpf,
    string? Bairro,
    string? Cidade,
    string? Estado,
    bool CadastroPendente,
    NivelAtleta? Nivel,
    LadoAtleta Lado,
    DateTime? DataNascimento
);

public record AtletaResumoDto(
    Guid Id,
    string Nome,
    string? Apelido,
    bool CadastroPendente,
    LadoAtleta Lado
);

public record AtletaDto(
    Guid Id,
    string Nome,
    string? Apelido,
    string? Telefone,
    string? Email,
    string? Instagram,
    string? Cpf,
    bool CadastroPendente,
    string? Bairro,
    string? Cidade,
    string? Estado,
    NivelAtleta? Nivel,
    LadoAtleta Lado,
    DateTime? DataNascimento,
    DateTime DataCriacao,
    DateTime DataAtualizacao
);

public record AtletaPendenciaDto(
    Guid AtletaId,
    string NomeAtleta,
    string? ApelidoAtleta,
    string? Email,
    bool CadastroPendente,
    bool PossuiUsuarioVinculado,
    bool TemEmail,
    string StatusPendencia,
    int QuantidadePartidas,
    IReadOnlyList<string> Competicoes
);

public record AtualizarEmailAtletaPendenteDto(
    string Email
);
