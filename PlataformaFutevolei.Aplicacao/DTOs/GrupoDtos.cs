namespace PlataformaFutevolei.Aplicacao.DTOs;

public record CriarGrupoDto(
    string Nome,
    string? Descricao,
    string? Link,
    DateTime DataInicio,
    DateTime? DataFim,
    Guid? LocalId
);

public record AtualizarGrupoDto(
    string Nome,
    string? Descricao,
    string? Link,
    DateTime DataInicio,
    DateTime? DataFim,
    Guid? LocalId
);

public record GrupoDto(
    Guid Id,
    string Nome,
    string? Descricao,
    string? Link,
    DateTime DataInicio,
    DateTime? DataFim,
    Guid? LocalId,
    Guid? UsuarioOrganizadorId,
    string? NomeLocal,
    string? NomeUsuarioOrganizador,
    DateTime DataCriacao,
    DateTime DataAtualizacao
);
