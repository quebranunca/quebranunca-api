namespace PlataformaFutevolei.Aplicacao.DTOs;

public record CriarGrupoAtletaDto(
    string NomeAtleta,
    string? ApelidoAtleta
);

public record GrupoAtletaDto(
    Guid Id,
    Guid CompeticaoId,
    Guid AtletaId,
    string NomeAtleta,
    string? ApelidoAtleta,
    bool CadastroPendente,
    bool VinculadoAUsuario,
    DateTime DataCriacao,
    DateTime DataAtualizacao
);
