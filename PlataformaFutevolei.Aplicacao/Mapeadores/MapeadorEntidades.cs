using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Utilitarios;
using PlataformaFutevolei.Dominio.Entidades;

namespace PlataformaFutevolei.Aplicacao.Mapeadores;

internal static class MapeadorEntidades
{
    private const string MarcadorMetadadosChave = "[[chave:";
    private const string MarcadorMetadadosRodada = "[[rodada:";
    private const string MarcadorMetadadosLados = "[[lados:";

    public static UsuarioLogadoDto ParaDto(this Usuario usuario)
        => new(
            usuario.Id,
            usuario.Nome,
            usuario.Email,
            usuario.Perfil,
            usuario.Ativo,
            usuario.AtletaId,
            usuario.Atleta?.ParaResumoDto()
        );

    public static UsuarioDto ParaAdminDto(this Usuario usuario)
        => new(
            usuario.Id,
            usuario.Nome,
            usuario.Email,
            usuario.Perfil,
            usuario.Ativo,
            usuario.AtletaId,
            usuario.Atleta?.ParaResumoDto(),
            usuario.DataCriacao,
            usuario.DataAtualizacao
        );

    public static ConviteCadastroDto ParaDto(this ConviteCadastro conviteCadastro)
    {
        var agoraUtc = DateTime.UtcNow;

        return new ConviteCadastroDto(
            conviteCadastro.Id,
            conviteCadastro.Email,
            conviteCadastro.Telefone,
            conviteCadastro.PerfilDestino,
            conviteCadastro.ExpiraEmUtc,
            conviteCadastro.UsadoEmUtc,
            conviteCadastro.Ativo,
            conviteCadastro.CriadoPorUsuarioId,
            conviteCadastro.CriadoPorUsuario?.Nome,
            conviteCadastro.CanalEnvio,
            conviteCadastro.ObterSituacao(agoraUtc),
            conviteCadastro.PodeSerUsado(agoraUtc),
            conviteCadastro.ObterSituacaoEnvioEmail(),
            conviteCadastro.UltimaTentativaEnvioEmailEmUtc,
            conviteCadastro.EmailEnviadoEmUtc,
            conviteCadastro.ErroEnvioEmail,
            conviteCadastro.ObterSituacaoEnvioWhatsapp(),
            conviteCadastro.UltimaTentativaEnvioWhatsappEmUtc,
            conviteCadastro.WhatsappEnviadoEmUtc,
            conviteCadastro.ErroEnvioWhatsapp,
            conviteCadastro.DataCriacao,
            conviteCadastro.DataAtualizacao
        );
    }

    public static ConviteCadastroPublicoDto ParaPublicoDto(this ConviteCadastro conviteCadastro)
    {
        var agoraUtc = DateTime.UtcNow;

        return new ConviteCadastroPublicoDto(
            conviteCadastro.Id,
            conviteCadastro.IdentificadorPublico,
            conviteCadastro.Email,
            conviteCadastro.PerfilDestino,
            conviteCadastro.ExpiraEmUtc,
            conviteCadastro.ObterSituacao(agoraUtc),
            conviteCadastro.PodeSerUsado(agoraUtc)
        );
    }

    public static AtletaResumoDto ParaResumoDto(this Atleta atleta)
        => new(
            atleta.Id,
            atleta.Nome,
            atleta.Apelido,
            atleta.CadastroPendente,
            atleta.Lado
        );

    public static AtletaDto ParaDto(this Atleta atleta)
        => new(
            atleta.Id,
            atleta.Nome,
            atleta.Apelido,
            atleta.Telefone,
            atleta.Email,
            atleta.Instagram,
            atleta.Cpf,
            atleta.CadastroPendente,
            atleta.Bairro,
            atleta.Cidade,
            atleta.Estado,
            atleta.Nivel,
            atleta.Lado,
            atleta.DataNascimento,
            atleta.DataCriacao,
            atleta.DataAtualizacao
        );

    public static DuplaDto ParaDto(this Dupla dupla)
        => new(
            dupla.Id,
            dupla.Nome,
            dupla.Atleta1Id,
            dupla.Atleta1?.Nome ?? string.Empty,
            dupla.Atleta2Id,
            dupla.Atleta2?.Nome ?? string.Empty,
            dupla.DataCriacao,
            dupla.DataAtualizacao
        );

    public static LigaDto ParaDto(this Liga liga)
        => new(
            liga.Id,
            liga.Nome,
            liga.Descricao,
            liga.DataCriacao,
            liga.DataAtualizacao
        );

    public static LocalDto ParaDto(this Local local)
        => new(
            local.Id,
            local.Nome,
            local.Tipo,
            local.QuantidadeQuadras,
            local.UsuarioCriadorId,
            local.UsuarioCriador?.Nome,
            local.DataCriacao,
            local.DataAtualizacao
        );

    public static FormatoCampeonatoDto ParaDto(this FormatoCampeonato formato)
        => new(
            formato.Id,
            formato.Nome,
            formato.Descricao,
            formato.TipoFormato,
            formato.Ativo,
            formato.QuantidadeGrupos,
            formato.ClassificadosPorGrupo,
            formato.GeraMataMataAposGrupos,
            formato.TurnoEVolta,
            formato.TipoChave,
            formato.QuantidadeDerrotasParaEliminacao,
            formato.PermiteCabecaDeChave,
            formato.DisputaTerceiroLugar,
            FormatosCampeonatoPadrao.EhPadrao(formato.Nome),
            formato.DataCriacao,
            formato.DataAtualizacao
        );

    public static CompeticaoDto ParaDto(this Competicao competicao)
        => new(
            competicao.Id,
            competicao.Nome,
            competicao.Tipo,
            competicao.Descricao,
            competicao.Link,
            competicao.DataInicio,
            competicao.DataFim,
            competicao.LigaId,
            competicao.LocalId,
            competicao.FormatoCampeonatoId,
            competicao.RegraCompeticaoId,
            competicao.UsuarioOrganizadorId,
            competicao.Liga?.Nome,
            competicao.Local?.Nome,
            competicao.FormatoCampeonato?.Nome,
            competicao.RegraCompeticao?.Nome,
            competicao.UsuarioOrganizador?.Nome,
            competicao.LigaId.HasValue,
            competicao.InscricoesAbertas,
            competicao.PossuiFinalReset,
            competicao.ObterPontosMinimosPartida(),
            competicao.ObterDiferencaMinimaPartida(),
            competicao.ObterPermiteEmpate(),
            competicao.ObterPontosVitoria(),
            competicao.ObterPontosDerrota(),
            competicao.ObterPontosParticipacao(),
            competicao.DataCriacao,
            competicao.DataAtualizacao
        );

    public static GrupoDto ParaDto(this Grupo grupo)
        => new(
            grupo.Id,
            grupo.Nome,
            grupo.Descricao,
            grupo.Link,
            grupo.DataInicio,
            grupo.DataFim,
            grupo.LocalId,
            grupo.UsuarioOrganizadorId,
            grupo.Local?.Nome,
            grupo.UsuarioOrganizador?.Nome,
            grupo.DataCriacao,
            grupo.DataAtualizacao
        );

    public static GrupoAtletaDto ParaDto(this GrupoAtleta grupoAtleta)
        => new(
            grupoAtleta.Id,
            grupoAtleta.GrupoId,
            grupoAtleta.AtletaId,
            grupoAtleta.Atleta?.Nome ?? string.Empty,
            grupoAtleta.Atleta?.Apelido,
            grupoAtleta.Atleta?.Email,
            grupoAtleta.Atleta?.CadastroPendente ?? false,
            grupoAtleta.Atleta?.Usuario is not null,
            grupoAtleta.DataCriacao,
            grupoAtleta.DataAtualizacao
        );

    public static RegraCompeticaoDto ParaDto(this RegraCompeticao regra)
        => new(
            regra.Id,
            regra.Nome,
            regra.Descricao,
            regra.PontosMinimosPartida,
            regra.DiferencaMinimaPartida,
            regra.PermiteEmpate,
            regra.PontosVitoria,
            regra.PontosDerrota,
            regra.PontosParticipacao,
            regra.PontosPrimeiroLugar,
            regra.PontosSegundoLugar,
            regra.PontosTerceiroLugar,
            RegrasCompeticaoPadrao.EhPadrao(regra.Nome),
            regra.UsuarioCriadorId,
            regra.UsuarioCriador?.Nome,
            regra.DataCriacao,
            regra.DataAtualizacao
        );

    public static CategoriaCompeticaoDto ParaDto(this CategoriaCompeticao categoria)
    {
        var formatoEfetivo = categoria.ObterFormatoCampeonatoEfetivo();
        var usaFormatoCampeonatoDaCompeticao = categoria.FormatoCampeonatoId is null &&
            categoria.Competicao?.FormatoCampeonatoId is not null;

        return new CategoriaCompeticaoDto(
            categoria.Id,
            categoria.CompeticaoId,
            categoria.FormatoCampeonatoId,
            formatoEfetivo?.Id,
            categoria.TabelaJogosAprovada,
            categoria.TabelaJogosAprovadaPorUsuarioId,
            categoria.TabelaJogosAprovadaEmUtc,
            categoria.Competicao?.Nome ?? string.Empty,
            categoria.FormatoCampeonato?.Nome,
            formatoEfetivo?.Nome,
            usaFormatoCampeonatoDaCompeticao,
            categoria.Nome,
            categoria.Genero,
            categoria.Nivel,
            categoria.PesoRanking,
            categoria.QuantidadeMaximaDuplas,
            categoria.InscricoesEncerradas,
            categoria.Inscricoes?.Count ?? 0,
            categoria.DataCriacao,
            categoria.DataAtualizacao
        );
    }

    public static PartidaDto ParaDto(this Partida partida)
    {
        var metadadosLados = ExtrairMetadadosLados(partida.Observacoes);
        var duplaAAtleta1Id = metadadosLados?.DuplaADireitaId ?? partida.DuplaA?.Atleta1Id;
        var duplaAAtleta2Id = metadadosLados?.DuplaAEsquerdaId ?? partida.DuplaA?.Atleta2Id;
        var duplaBAtleta1Id = metadadosLados?.DuplaBDireitaId ?? partida.DuplaB?.Atleta1Id;
        var duplaBAtleta2Id = metadadosLados?.DuplaBEsquerdaId ?? partida.DuplaB?.Atleta2Id;
        var atletasPendentes = ObterAtletasPendentesPartida(partida);

        return new PartidaDto(
            partida.Id,
            partida.CategoriaCompeticaoId,
            partida.GrupoId,
            partida.Grupo?.Nome,
            partida.CategoriaCompeticao?.Nome ?? string.Empty,
            partida.CriadoPorUsuarioId,
            partida.CriadoPorUsuario?.Nome,
            partida.DuplaAId,
            partida.DuplaA?.Nome ?? "A definir",
            duplaAAtleta1Id,
            ObterNomeAtletaDupla(partida.DuplaA, duplaAAtleta1Id),
            duplaAAtleta2Id,
            ObterNomeAtletaDupla(partida.DuplaA, duplaAAtleta2Id),
            partida.DuplaBId,
            partida.DuplaB?.Nome ?? "A definir",
            duplaBAtleta1Id,
            ObterNomeAtletaDupla(partida.DuplaB, duplaBAtleta1Id),
            duplaBAtleta2Id,
            ObterNomeAtletaDupla(partida.DuplaB, duplaBAtleta2Id),
            partida.FaseCampeonato,
            partida.LadoDaChave,
            partida.Rodada,
            partida.PosicaoNaChave,
            partida.PartidaOrigemParticipanteAId,
            partida.OrigemParticipanteATipo,
            partida.PartidaOrigemParticipanteBId,
            partida.OrigemParticipanteBTipo,
            partida.ProximaPartidaVencedorId,
            partida.ProximaPartidaPerdedorId,
            partida.SlotDestinoVencedor,
            partida.SlotDestinoPerdedor,
            partida.Ativa,
            partida.EhPreliminar,
            partida.EhFinal,
            partida.EhFinalissima,
            partida.Status,
            partida.PlacarDuplaA,
            partida.PlacarDuplaB,
            partida.DuplaVencedoraId,
            partida.DuplaVencedora?.Nome,
            partida.StatusAprovacao,
            partida.CategoriaCompeticao?.PesoRanking ?? 1m,
            partida.CalcularPontosRankingVitoria(),
            partida.DataPartida,
            LimparObservacoesSistema(partida.Observacoes),
            partida.DataCriacao,
            partida.DataAtualizacao,
            atletasPendentes.Count,
            atletasPendentes.Count(x => !x.TemEmail),
            atletasPendentes
        );
    }

    public static PendenciaUsuarioDto ParaDto(this PendenciaUsuario pendencia)
        => new(
            pendencia.Id,
            pendencia.Tipo,
            pendencia.Status,
            pendencia.DataCriacao,
            pendencia.DataConclusao,
            pendencia.Observacao,
            pendencia.UsuarioId,
            pendencia.AtletaId,
            pendencia.Atleta?.Nome,
            pendencia.Atleta?.Email,
            pendencia.Atleta is null ? null : StatusCadastroAtletaUtil.PossuiUsuarioVinculado(pendencia.Atleta),
            pendencia.PartidaId,
            pendencia.Partida?.DataPartida,
            pendencia.Partida?.Status,
            pendencia.Partida?.StatusAprovacao,
            pendencia.Partida?.DuplaA?.Nome,
            pendencia.Partida?.DuplaA?.Atleta1?.Nome,
            pendencia.Partida?.DuplaA?.Atleta2?.Nome,
            pendencia.Partida?.DuplaB?.Nome,
            pendencia.Partida?.DuplaB?.Atleta1?.Nome,
            pendencia.Partida?.DuplaB?.Atleta2?.Nome,
            pendencia.Partida?.Status == Dominio.Enums.StatusPartida.Encerrada ? pendencia.Partida.PlacarDuplaA : null,
            pendencia.Partida?.Status == Dominio.Enums.StatusPartida.Encerrada ? pendencia.Partida.PlacarDuplaB : null,
            pendencia.Partida?.CriadoPorUsuarioId,
            pendencia.Partida?.CriadoPorUsuario?.Nome
        );

    public static InscricaoCampeonatoDto ParaDto(this InscricaoCampeonato inscricao)
        => new(
            inscricao.Id,
            inscricao.CompeticaoId,
            inscricao.Competicao?.Nome ?? string.Empty,
            inscricao.CategoriaCompeticaoId,
            inscricao.CategoriaCompeticao?.Nome ?? string.Empty,
            inscricao.DuplaId,
            inscricao.Dupla?.Nome ?? string.Empty,
            inscricao.Dupla?.Atleta1Id ?? Guid.Empty,
            inscricao.Dupla?.Atleta1?.Nome ?? string.Empty,
            inscricao.Dupla?.Atleta2Id ?? Guid.Empty,
            inscricao.Dupla?.Atleta2?.Nome ?? string.Empty,
            inscricao.Status,
            inscricao.Pago,
            inscricao.Observacao,
            inscricao.DataInscricaoUtc,
            inscricao.DataCriacao,
            inscricao.DataAtualizacao
        );

    private static string? LimparObservacoesSistema(string? observacoes)
    {
        if (string.IsNullOrWhiteSpace(observacoes))
        {
            return null;
        }

        var linhas = observacoes
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x =>
                !x.StartsWith(MarcadorMetadadosChave, StringComparison.Ordinal) &&
                !x.StartsWith(MarcadorMetadadosRodada, StringComparison.Ordinal) &&
                !x.StartsWith(MarcadorMetadadosLados, StringComparison.Ordinal))
            .ToList();

        return linhas.Count == 0 ? null : string.Join(Environment.NewLine, linhas);
    }

    private static IReadOnlyList<PartidaAtletaPendenteDto> ObterAtletasPendentesPartida(Partida partida)
    {
        return new[]
        {
            partida.DuplaA?.Atleta1,
            partida.DuplaA?.Atleta2,
            partida.DuplaB?.Atleta1,
            partida.DuplaB?.Atleta2
        }
        .OfType<Atleta>()
        .Where(atleta => !StatusCadastroAtletaUtil.PossuiUsuarioVinculado(atleta))
        .DistinctBy(atleta => atleta.Id)
        .Select(atleta => new PartidaAtletaPendenteDto(
            atleta.Id,
            atleta.Nome,
            atleta.Email,
            StatusCadastroAtletaUtil.TemEmail(atleta),
            StatusCadastroAtletaUtil.ObterStatusPendencia(atleta)))
        .ToList();
    }

    private static string ObterNomeAtletaDupla(Dupla? dupla, Guid atletaId)
    {
        if (dupla is null || atletaId == Guid.Empty)
        {
            return string.Empty;
        }

        if (dupla.Atleta1Id == atletaId)
        {
            return dupla.Atleta1?.Nome ?? string.Empty;
        }

        if (dupla.Atleta2Id == atletaId)
        {
            return dupla.Atleta2?.Nome ?? string.Empty;
        }

        return string.Empty;
    }

    private static string? ObterNomeAtletaDupla(Dupla? dupla, Guid? atletaId)
    {
        if (!atletaId.HasValue || atletaId.Value == Guid.Empty)
        {
            return null;
        }

        var nome = ObterNomeAtletaDupla(dupla, atletaId.Value);
        return string.IsNullOrWhiteSpace(nome) ? null : nome;
    }

    private static string MascararEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return string.Empty;
        }

        var partes = email.Split('@', 2, StringSplitOptions.TrimEntries);
        if (partes.Length != 2 || string.IsNullOrWhiteSpace(partes[0]))
        {
            return email;
        }

        var usuario = partes[0];
        var dominio = partes[1];
        if (usuario.Length <= 2)
        {
            return $"{usuario[0]}***@{dominio}";
        }

        return $"{usuario[0]}***{usuario[^1]}@{dominio}";
    }

    private static MetadadosLados? ExtrairMetadadosLados(string? observacoes)
    {
        if (string.IsNullOrWhiteSpace(observacoes))
        {
            return null;
        }

        var linhas = observacoes
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var linhaMetadados = linhas.LastOrDefault(x => x.StartsWith(MarcadorMetadadosLados, StringComparison.Ordinal));
        if (string.IsNullOrWhiteSpace(linhaMetadados))
        {
            return null;
        }

        var conteudo = linhaMetadados
            .Replace(MarcadorMetadadosLados, string.Empty, StringComparison.Ordinal)
            .Replace("]]", string.Empty, StringComparison.Ordinal);
        var partes = conteudo.Split(';');
        if (partes.Length < 4 ||
            !Guid.TryParse(partes[0], out var duplaADireitaId) ||
            !Guid.TryParse(partes[1], out var duplaAEsquerdaId) ||
            !Guid.TryParse(partes[2], out var duplaBDireitaId) ||
            !Guid.TryParse(partes[3], out var duplaBEsquerdaId))
        {
            return null;
        }

        return new MetadadosLados(duplaADireitaId, duplaAEsquerdaId, duplaBDireitaId, duplaBEsquerdaId);
    }

    private sealed record MetadadosLados(Guid DuplaADireitaId, Guid DuplaAEsquerdaId, Guid DuplaBDireitaId, Guid DuplaBEsquerdaId);
}
