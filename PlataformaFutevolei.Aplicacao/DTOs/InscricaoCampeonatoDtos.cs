using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Aplicacao.DTOs;

public record CriarInscricaoCampeonatoDto(
    Guid CategoriaId,
    Guid? DuplaId,
    Guid? Atleta1Id,
    Guid? Atleta2Id,
    string? NomeAtleta1,
    string? ApelidoAtleta1,
    string? NomeAtleta2,
    string? ApelidoAtleta2,
    string? Observacao,
    bool Pago = false,
    bool Atleta1CadastroPendente = false,
    bool Atleta2CadastroPendente = false
);

public record InscricaoCampeonatoDto(
    Guid Id,
    Guid CampeonatoId,
    string NomeCampeonato,
    Guid CategoriaId,
    string NomeCategoria,
    Guid DuplaId,
    string NomeDupla,
    Guid Atleta1Id,
    string NomeAtleta1,
    Guid Atleta2Id,
    string NomeAtleta2,
    StatusInscricaoCampeonato Status,
    bool Pago,
    string? Observacao,
    DateTime DataInscricaoUtc,
    DateTime DataCriacao,
    DateTime DataAtualizacao
);
