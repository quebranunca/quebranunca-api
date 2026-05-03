namespace PlataformaFutevolei.Aplicacao.DTOs;

public record CriarGrupoAtletaDto(
    string NomeAtleta,
    string? ApelidoAtleta,
    string? Email
);

public record AtualizarEmailGrupoAtletaDto(
    string Email
);

public record GrupoAtletaDto(
    Guid Id,
    Guid GrupoId,
    Guid AtletaId,
    string NomeAtleta,
    string? ApelidoAtleta,
    string? EmailAtleta,
    bool CadastroPendente,
    bool VinculadoAUsuario,
    DateTime DataCriacao,
    DateTime DataAtualizacao
);
