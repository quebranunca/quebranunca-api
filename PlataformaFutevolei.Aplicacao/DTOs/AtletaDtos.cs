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
    DateTime? DataNascimento,
    SexoAtleta? Sexo = null,
    PeDominanteAtleta? PeDominante = null,
    TempoPraticaAtleta? TempoPratica = null,
    Guid? ArenaPrincipalId = null,
    ObjetivoAtualAtleta? ObjetivoAtual = null
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
    DateTime? DataNascimento,
    SexoAtleta? Sexo = null,
    PeDominanteAtleta? PeDominante = null,
    TempoPraticaAtleta? TempoPratica = null,
    Guid? ArenaPrincipalId = null,
    ObjetivoAtualAtleta? ObjetivoAtual = null
);

public record AtletaResumoDto(
    Guid Id,
    string Nome,
    string? Apelido,
    bool CadastroPendente,
    LadoAtleta Lado,
    string? Cidade,
    string? Estado,
    int QuantidadeJogos,
    string? AvatarUrl
);

public record AtletaSugestaoPartidaDto(
    Guid Id,
    string Nome,
    int TotalPartidas,
    string? FotoPerfilUrl
);

public record AtletasSugestoesPartidaDto(
    IReadOnlyList<AtletaSugestaoPartidaDto> ParceirosFrequentes,
    IReadOnlyList<AtletaSugestaoPartidaDto> RivaisFrequentes
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
    Guid? UsuarioCriadorId,
    string? NomeUsuarioCriador,
    string? FotoPerfilUrl,
    SexoAtleta? Sexo,
    PeDominanteAtleta? PeDominante,
    TempoPraticaAtleta? TempoPratica,
    Guid? ArenaPrincipalId,
    string? ArenaPrincipalNome,
    ObjetivoAtualAtleta? ObjetivoAtual,
    AtletaMedidasDto? Medidas,
    DateTime DataCriacao,
    DateTime DataAtualizacao
);

public record AtletaPublicoDto(
    Guid Id,
    string Nome,
    string? Apelido,
    string? Instagram,
    bool CadastroPendente,
    string? Bairro,
    string? Cidade,
    string? Estado,
    NivelAtleta? Nivel,
    LadoAtleta Lado,
    Guid? UsuarioCriadorId,
    string? NomeUsuarioCriador,
    string? FotoPerfilUrl,
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

public record AtletaMedidasDto(
    Guid AtletaId,
    string? Camiseta,
    string? Regata,
    string? Short,
    string? Sunga,
    string? Top,
    string? Biquini,
    DateTime? AtualizadoEm
);

public record AtualizarAtletaMedidasDto(
    string? Camiseta,
    string? Regata,
    string? Short,
    string? Sunga,
    string? Top,
    string? Biquini
);

public record AtletaEmailDisponibilidadeDto(
    string Email,
    bool Disponivel,
    Guid? AtletaId,
    string? Nome,
    string? Apelido,
    string? Mensagem
);

public record SaneamentoAtletasEmailResumoDto(
    int EmailsAnalisados,
    int GruposDuplicados,
    int AtletasDuplicadosUnificados,
    IReadOnlyList<SaneamentoAtletasEmailGrupoDto> Grupos
);

public record SaneamentoAtletasEmailGrupoDto(
    string Email,
    Guid AtletaPrincipalId,
    IReadOnlyList<Guid> AtletasDuplicadosIds,
    SaneamentoAtletasEmailContadoresDto RegistrosMigrados
);

public record SaneamentoAtletasEmailContadoresDto(
    int DuplasAtualizadas,
    int DuplasConsolidadas,
    int PartidasAtualizadas,
    int InscricoesAtualizadas,
    int InscricoesConsolidadas,
    int GruposAtualizados,
    int GruposConsolidados,
    int AprovacoesAtualizadas,
    int AprovacoesConsolidadas,
    int PendenciasAtualizadas,
    int ConvitesAtualizados,
    int UsuariosAtualizados,
    int AtletasRemovidos
);
