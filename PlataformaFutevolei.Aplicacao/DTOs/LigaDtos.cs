namespace PlataformaFutevolei.Aplicacao.DTOs;

public record CriarLigaDto(
    string Nome,
    string? Descricao
);

public record AtualizarLigaDto(
    string Nome,
    string? Descricao
);

public record LigaDto(
    Guid Id,
    string Nome,
    string? Descricao,
    DateTime DataCriacao,
    DateTime DataAtualizacao
);
