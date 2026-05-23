namespace PlataformaFutevolei.Aplicacao.DTOs;

public record CriarGrupoDto(
    string Nome,
    string? Descricao,
    string? Link,
    DateTime DataInicio,
    DateTime? DataFim,
    Guid? LocalId,
    string? Privacidade = null,
    string? ImagemUrl = null
);

public record AtualizarGrupoDto(
    string Nome,
    string? Descricao,
    string? Link,
    DateTime DataInicio,
    DateTime? DataFim,
    Guid? LocalId,
    string? Privacidade = null,
    string? ImagemUrl = null
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
    string Privacidade,
    string? ImagemUrl,
    DateTime DataCriacao,
    DateTime DataAtualizacao
);

public record GrupoSelecaoDto(
    Guid Id,
    string Nome,
    int QuantidadeAtletas,
    string? ImagemUrl,
    string Privacidade
);

public record GrupoNomeSimilarDto(
    Guid Id,
    string Nome,
    int QuantidadeAtletas,
    string Privacidade
);

public record GrupoVerificacaoNomeDto(
    bool Disponivel,
    bool ExisteExato,
    IReadOnlyList<GrupoNomeSimilarDto> Similares
);
