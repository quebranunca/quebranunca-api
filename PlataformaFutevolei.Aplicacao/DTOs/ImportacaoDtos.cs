namespace PlataformaFutevolei.Aplicacao.DTOs;

public record ImportacaoLinhaErroDto(
    int Linha,
    string Mensagem
);

public record ImportacaoResultadoDto(
    string Tipo,
    int TotalLinhas,
    int RegistrosImportados,
    int RegistrosComErro,
    IReadOnlyList<ImportacaoLinhaErroDto> Erros
);
