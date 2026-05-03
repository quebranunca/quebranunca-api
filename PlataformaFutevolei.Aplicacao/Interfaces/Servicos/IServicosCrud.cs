using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Dominio.Entidades;

namespace PlataformaFutevolei.Aplicacao.Interfaces.Servicos;

public interface IAtletaServico
{
    Task<IReadOnlyList<AtletaDto>> ListarAsync(
        bool somenteInscritosMinhasCompeticoes = false,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AtletaResumoDto>> BuscarAsync(string? termo, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AtletaResumoDto>> BuscarSugestoesPorCompeticaoAsync(
        Guid competicaoId,
        string? termo,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AtletaPendenciaDto>> ListarPendenciasAsync(CancellationToken cancellationToken = default);
    Task<AtletaDto> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AtletaDto?> ObterMeuAsync(CancellationToken cancellationToken = default);
    Task<AtletaDto> CriarAsync(CriarAtletaDto dto, CancellationToken cancellationToken = default);
    Task<AtletaDto> SalvarMeuAsync(AtualizarAtletaDto dto, CancellationToken cancellationToken = default);
    Task<AtletaDto> AtualizarAsync(Guid id, AtualizarAtletaDto dto, CancellationToken cancellationToken = default);
    Task<AtletaPendenciaDto> InformarEmailPendenteAsync(
        Guid atletaId,
        AtualizarEmailAtletaPendenteDto dto,
        CancellationToken cancellationToken = default);
    Task RemoverAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IConviteCadastroServico
{
    Task<IReadOnlyList<ConviteCadastroDto>> ListarAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AtletaElegivelConviteCadastroDto>> ListarAtletasElegiveisAsync(CancellationToken cancellationToken = default);
    Task<ConviteCadastroDto> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ConviteCadastroLinkAceiteDto> ObterLinkAceiteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ConviteCadastroPublicoDto> ObterPublicoAsync(string identificadorOuToken, CancellationToken cancellationToken = default);
    Task<ConviteCadastroDto> CriarAsync(CriarConviteCadastroDto dto, CancellationToken cancellationToken = default);
    Task<ConviteCadastroDto> EnviarEmailAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ConviteCadastroDto> EnviarWhatsappAsync(Guid id, CancellationToken cancellationToken = default);
    Task DesativarAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IDuplaServico
{
    Task<IReadOnlyList<DuplaDto>> ListarAsync(
        bool somenteInscritasMinhasCompeticoes = false,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DuplaDto>> ListarPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default);
    Task<DuplaDto> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DuplaDto> CriarAsync(CriarDuplaDto dto, CancellationToken cancellationToken = default);
    Task<DuplaDto> AtualizarAsync(Guid id, AtualizarDuplaDto dto, CancellationToken cancellationToken = default);
    Task RemoverAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface ICompeticaoServico
{
    Task<IReadOnlyList<CompeticaoDto>> ListarAsync(
        bool incluirPublicas = false,
        CancellationToken cancellationToken = default);
    Task<ResumoCompeticoesPublicoDto> ObterResumoPublicoAsync(CancellationToken cancellationToken = default);
    Task<CompeticaoDto> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CompeticaoDto> CriarAsync(CriarCompeticaoDto dto, CancellationToken cancellationToken = default);
    Task<CompeticaoDto> AtualizarAsync(Guid id, AtualizarCompeticaoDto dto, CancellationToken cancellationToken = default);
    Task RemoverAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IGrupoResumoUsuarioServico
{
    Task<GrupoResumoUsuarioDto?> ObterMeuResumoAsync(CancellationToken cancellationToken = default);
}

public interface IGrupoAtletaServico
{
    Task<IReadOnlyList<GrupoAtletaDto>> ListarPorCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default);
    Task<GrupoAtletaDto> CriarAsync(Guid competicaoId, CriarGrupoAtletaDto dto, CancellationToken cancellationToken = default);
    Task<GrupoAtletaDto> CompletarEmailAsync(Guid competicaoId, Guid id, AtualizarEmailGrupoAtletaDto dto, CancellationToken cancellationToken = default);
    Task RemoverAsync(Guid competicaoId, Guid id, CancellationToken cancellationToken = default);
    Task<UsuarioLogadoDto> AssumirMeuNomeNoGrupoAsync(Guid competicaoId, Guid id, CancellationToken cancellationToken = default);
}

public interface IFormatoCampeonatoServico
{
    Task<IReadOnlyList<FormatoCampeonatoDto>> ListarAsync(CancellationToken cancellationToken = default);
    Task<FormatoCampeonatoDto> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<FormatoCampeonatoDto> CriarAsync(CriarFormatoCampeonatoDto dto, CancellationToken cancellationToken = default);
    Task<FormatoCampeonatoDto> AtualizarAsync(Guid id, AtualizarFormatoCampeonatoDto dto, CancellationToken cancellationToken = default);
    Task RemoverAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IRegraCompeticaoServico
{
    Task<IReadOnlyList<RegraCompeticaoDto>> ListarAsync(CancellationToken cancellationToken = default);
    Task<RegraCompeticaoDto> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<RegraCompeticaoDto> CriarAsync(CriarRegraCompeticaoDto dto, CancellationToken cancellationToken = default);
    Task<RegraCompeticaoDto> AtualizarAsync(Guid id, AtualizarRegraCompeticaoDto dto, CancellationToken cancellationToken = default);
    Task RemoverAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface ICategoriaCompeticaoServico
{
    Task<IReadOnlyList<CategoriaCompeticaoDto>> ListarPorCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default);
    Task<CategoriaCompeticaoDto> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CategoriaCompeticaoDto> CriarAsync(CriarCategoriaCompeticaoDto dto, CancellationToken cancellationToken = default);
    Task<CategoriaCompeticaoDto> AtualizarAsync(Guid id, AtualizarCategoriaCompeticaoDto dto, CancellationToken cancellationToken = default);
    Task<CategoriaCompeticaoDto> AprovarTabelaJogosAsync(Guid id, CancellationToken cancellationToken = default);
    Task RemoverAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IPartidaServico
{
    Task<IReadOnlyList<PartidaDto>> ListarPorCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PartidaDto>> ListarPorCategoriaAsync(Guid categoriaId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PartidaDto>> ListarMinhasAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RodadaEstruturaCompeticaoDto>> ListarEstruturaPorCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RodadaEstruturaCompeticaoDto>> ListarEstruturaPorCategoriaAsync(Guid categoriaId, CancellationToken cancellationToken = default);
    Task<ChaveamentoCategoriaDto> ObterChaveamentoPorCategoriaAsync(Guid categoriaId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SituacaoDuplaCompeticaoDto>> ListarSituacaoDuplasPorCategoriaAsync(Guid categoriaId, CancellationToken cancellationToken = default);
    Task<PartidaDto> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<GeracaoTabelaCategoriaDto> GerarTabelaCategoriaAsync(
        Guid categoriaId,
        GerarTabelaCategoriaDto dto,
        CancellationToken cancellationToken = default);
    Task<RemocaoTabelaCategoriaDto> RemoverTabelaCategoriaAsync(Guid categoriaId, CancellationToken cancellationToken = default);
    Task<PartidaDto> CriarAsync(CriarPartidaDto dto, CancellationToken cancellationToken = default);
    Task<PartidaDto> AtualizarAsync(Guid id, AtualizarPartidaDto dto, CancellationToken cancellationToken = default);
    Task RemoverAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IPendenciaServico
{
    Task<IReadOnlyList<PendenciaUsuarioDto>> ListarMinhasAsync(CancellationToken cancellationToken = default);
    Task<PendenciaUsuarioDto> AprovarPartidaAsync(
        Guid pendenciaId,
        ResponderPendenciaPartidaDto dto,
        CancellationToken cancellationToken = default);
    Task<PendenciaUsuarioDto> ContestarPartidaAsync(
        Guid pendenciaId,
        ResponderPendenciaPartidaDto dto,
        CancellationToken cancellationToken = default);
    Task<PendenciaUsuarioDto> CompletarContatoAsync(
        Guid pendenciaId,
        AtualizarContatoPendenciaDto dto,
        CancellationToken cancellationToken = default);
    Task InicializarFluxoPartidaAsync(
        Partida partida,
        Guid usuarioRegistradorId,
        CancellationToken cancellationToken = default);
    Task SincronizarAposVinculoAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default);
}

public interface IResolvedorAtletaDuplaServico
{
    Task<Atleta> ObterAtletaExistenteAsync(
        Guid atletaId,
        string mensagemQuandoInvalido,
        CancellationToken cancellationToken = default);

    Task<Atleta> ResolverAtletaAsync(
        Guid? atletaId,
        string? nomeInformado,
        string? apelidoInformado,
        string mensagemQuandoInvalido,
        bool cadastroPendente,
        CancellationToken cancellationToken = default);

    Task<Atleta> ObterOuCriarAtletaAsync(
        string? nomeInformado,
        string? apelidoInformado,
        bool cadastroPendente,
        CancellationToken cancellationToken = default);

    Task<Atleta> ObterOuCriarAtletaParaUsuarioAsync(
        string nomeInformado,
        string emailInformado,
        CancellationToken cancellationToken = default);

    Task<Dupla> ObterOuCriarDuplaAsync(
        Atleta atleta1,
        Atleta atleta2,
        CancellationToken cancellationToken = default);

    Task<GrupoAtleta> GarantirAtletaNoGrupoAsync(
        Guid competicaoId,
        Atleta atleta,
        CancellationToken cancellationToken = default);
}

public interface IInscricaoCampeonatoServico
{
    Task<IReadOnlyList<InscricaoCampeonatoDto>> ListarPorCampeonatoAsync(
        Guid campeonatoId,
        Guid? categoriaId,
        CancellationToken cancellationToken = default);
    Task<InscricaoCampeonatoDto> ObterPorIdAsync(
        Guid campeonatoId,
        Guid inscricaoId,
        CancellationToken cancellationToken = default);
    Task<InscricaoCampeonatoDto> CriarAsync(
        Guid campeonatoId,
        CriarInscricaoCampeonatoDto dto,
        CancellationToken cancellationToken = default);
    Task<InscricaoCampeonatoDto> AtualizarAsync(
        Guid campeonatoId,
        Guid inscricaoId,
        CriarInscricaoCampeonatoDto dto,
        CancellationToken cancellationToken = default);
    Task<InscricaoCampeonatoDto> AprovarAsync(
        Guid campeonatoId,
        Guid inscricaoId,
        CancellationToken cancellationToken = default);
    Task RemoverAsync(
        Guid campeonatoId,
        Guid inscricaoId,
        CancellationToken cancellationToken = default);
}

public interface IRankingServico
{
    Task<RankingFiltroInicialDto> ObterFiltroInicialAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RankingCategoriaDto>> ListarAtletasGeralAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RankingCategoriaDto>> ListarAtletasPorLigaAsync(
        Guid ligaId,
        CancellationToken cancellationToken = default);

    Task<RankingRegiaoFiltroDto> ListarRegioesDisponiveisAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RankingCategoriaDto>> ListarAtletasPorRegiaoAsync(
        string? estado,
        string? cidade,
        string? bairro,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RankingCategoriaDto>> ListarAtletasPorCompeticaoAsync(
        Guid competicaoId,
        CancellationToken cancellationToken = default);
}

public interface IImportacaoServico
{
    Task<ImportacaoResultadoDto> ImportarAsync(
        string tipo,
        Stream arquivoStream,
        string? nomeArquivo,
        Guid? campeonatoId,
        CancellationToken cancellationToken = default);
}

public interface IUsuarioServico
{
    Task<UsuarioLogadoDto> ObterMeuUsuarioAsync(CancellationToken cancellationToken = default);
    Task<UsuarioResumoDto> ObterMeuResumoAsync(CancellationToken cancellationToken = default);
    Task<UsuarioLogadoDto> AtualizarMeuUsuarioAsync(AtualizarMeuUsuarioDto dto, CancellationToken cancellationToken = default);
    Task<UsuarioLogadoDto> VincularMeuAtletaAsync(VincularAtletaUsuarioDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UsuarioDto>> ListarAsync(string? nome, string? email, CancellationToken cancellationToken = default);
    Task<UsuarioDto> AtualizarAsync(Guid id, AtualizarUsuarioDto dto, CancellationToken cancellationToken = default);
}
