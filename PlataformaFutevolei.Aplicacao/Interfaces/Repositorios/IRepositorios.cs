using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;

public interface IUsuarioRepositorio
{
    Task<IReadOnlyList<Usuario>> ListarAsync(string? nome, string? email, CancellationToken cancellationToken = default);
    Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Usuario?> ObterPorEmailParaAtualizacaoAsync(string email, CancellationToken cancellationToken = default);
    Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Usuario?> ObterPorIdParaAtualizacaoAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Usuario?> ObterPorAtletaIdAsync(Guid atletaId, CancellationToken cancellationToken = default);
    Task AdicionarAsync(Usuario usuario, CancellationToken cancellationToken = default);
    void Atualizar(Usuario usuario);
}

public interface IConviteCadastroRepositorio
{
    Task<IReadOnlyList<ConviteCadastro>> ListarAsync(CancellationToken cancellationToken = default);
    Task<ConviteCadastro?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ConviteCadastro?> ObterPorIdParaAtualizacaoAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ConviteCadastro?> ObterPorIdentificadorPublicoAsync(string identificadorPublico, CancellationToken cancellationToken = default);
    Task<ConviteCadastro?> ObterPorIdentificadorPublicoParaAtualizacaoAsync(string identificadorPublico, CancellationToken cancellationToken = default);
    Task AdicionarAsync(ConviteCadastro conviteCadastro, CancellationToken cancellationToken = default);
    void Atualizar(ConviteCadastro conviteCadastro);
}

public interface IAtletaRepositorio
{
    Task<IReadOnlyList<Atleta>> ListarAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Atleta>> ListarComEmailEmPartidasSemUsuarioAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Atleta>> ListarInscritosPorOrganizadorAsync(Guid usuarioOrganizadorId, CancellationToken cancellationToken = default);
    Task<bool> PertenceAoOrganizadorAsync(Guid atletaId, Guid usuarioOrganizadorId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Atleta>> BuscarAsync(string? termo, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Atleta>> BuscarSugestoesPorCompeticaoAsync(Guid competicaoId, string termo, CancellationToken cancellationToken = default);
    Task<Atleta?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Atleta?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Atleta>> ListarPorNomeAsync(string nome, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Atleta>> ListarPorEmailAsync(string email, CancellationToken cancellationToken = default);
    Task AdicionarAsync(Atleta atleta, CancellationToken cancellationToken = default);
    void Atualizar(Atleta atleta);
    void Remover(Atleta atleta);
}

public interface IDuplaRepositorio
{
    Task<IReadOnlyList<Dupla>> ListarAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Dupla>> ListarInscritasPorOrganizadorAsync(Guid usuarioOrganizadorId, CancellationToken cancellationToken = default);
    Task<bool> PertenceAoOrganizadorAsync(Guid duplaId, Guid usuarioOrganizadorId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Dupla>> ListarPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default);
    Task<Dupla?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Dupla?> ObterPorAtletasAsync(Guid atleta1Id, Guid atleta2Id, CancellationToken cancellationToken = default);
    Task AdicionarAsync(Dupla dupla, CancellationToken cancellationToken = default);
    void Atualizar(Dupla dupla);
    void Remover(Dupla dupla);
}

public interface ILigaRepositorio
{
    Task<IReadOnlyList<Liga>> ListarAsync(CancellationToken cancellationToken = default);
    Task<Liga?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Liga?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default);
    Task AdicionarAsync(Liga liga, CancellationToken cancellationToken = default);
    void Atualizar(Liga liga);
    void Remover(Liga liga);
}

public interface ILocalRepositorio
{
    Task<IReadOnlyList<Local>> ListarAsync(CancellationToken cancellationToken = default);
    Task<Local?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Local?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default);
    Task AdicionarAsync(Local local, CancellationToken cancellationToken = default);
    void Atualizar(Local local);
    void Remover(Local local);
}

public interface ICompeticaoRepositorio
{
    Task<IReadOnlyList<Competicao>> ListarAsync(CancellationToken cancellationToken = default);
    Task<Competicao?> ObterGrupoResumoUsuarioAsync(
        Guid usuarioId,
        Guid? atletaId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> ListarIdsComAcessoAtletaAsync(
        Guid usuarioId,
        Guid? atletaId,
        CancellationToken cancellationToken = default);
    Task<bool> AtletaPossuiAcessoAsync(
        Guid competicaoId,
        Guid usuarioId,
        Guid? atletaId,
        CancellationToken cancellationToken = default);
    Task<Competicao?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default);
    Task<Competicao?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AdicionarAsync(Competicao competicao, CancellationToken cancellationToken = default);
    void Atualizar(Competicao competicao);
    void Remover(Competicao competicao);
}

public interface IGrupoRepositorio
{
    Task<IReadOnlyList<Grupo>> ListarAsync(CancellationToken cancellationToken = default);
    Task<Grupo?> ObterResumoUsuarioAsync(
        Guid usuarioId,
        Guid? atletaId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> ListarIdsComAcessoAtletaAsync(
        Guid usuarioId,
        Guid? atletaId,
        CancellationToken cancellationToken = default);
    Task<bool> AtletaPossuiAcessoAsync(
        Guid grupoId,
        Guid usuarioId,
        Guid? atletaId,
        CancellationToken cancellationToken = default);
    Task<Grupo?> ObterPorNomeEOrganizadorAsync(string nome, Guid? usuarioOrganizadorId, CancellationToken cancellationToken = default);
    Task<Grupo?> ObterPorNomeNormalizadoAsync(string nome, CancellationToken cancellationToken = default);
    Task<Grupo?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AdicionarAsync(Grupo grupo, CancellationToken cancellationToken = default);
    void Atualizar(Grupo grupo);
    void Remover(Grupo grupo);
}

public interface IGrupoAtletaRepositorio
{
    Task<IReadOnlyList<GrupoAtleta>> ListarPorGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GrupoAtleta>> ListarPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default);
    Task<GrupoAtleta?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<GrupoAtleta?> ObterPorGrupoEAtletaAsync(Guid grupoId, Guid atletaId, CancellationToken cancellationToken = default);
    Task AdicionarAsync(GrupoAtleta grupoAtleta, CancellationToken cancellationToken = default);
    void Remover(GrupoAtleta grupoAtleta);
}

public interface IFormatoCampeonatoRepositorio
{
    Task<IReadOnlyList<FormatoCampeonato>> ListarAsync(CancellationToken cancellationToken = default);
    Task<FormatoCampeonato?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<FormatoCampeonato?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default);
    Task AdicionarAsync(FormatoCampeonato formato, CancellationToken cancellationToken = default);
    void Atualizar(FormatoCampeonato formato);
    void Remover(FormatoCampeonato formato);
}

public interface IRegraCompeticaoRepositorio
{
    Task<IReadOnlyList<RegraCompeticao>> ListarAsync(CancellationToken cancellationToken = default);
    Task<RegraCompeticao?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<RegraCompeticao?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default);
    Task AdicionarAsync(RegraCompeticao regra, CancellationToken cancellationToken = default);
    void Atualizar(RegraCompeticao regra);
    void Remover(RegraCompeticao regra);
}

public interface ICategoriaCompeticaoRepositorio
{
    Task<IReadOnlyList<CategoriaCompeticao>> ListarPorCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default);
    Task<CategoriaCompeticao?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AdicionarAsync(CategoriaCompeticao categoria, CancellationToken cancellationToken = default);
    void Atualizar(CategoriaCompeticao categoria);
    void Remover(CategoriaCompeticao categoria);
}

public interface IPartidaRepositorio
{
    Task<IReadOnlyList<Partida>> ListarPorCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Partida>> ListarPorGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Partida>> ListarPorCategoriaAsync(Guid categoriaId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Partida>> ListarPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default);
    Task<Partida?> ObterUltimaDoGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Partida>> ListarComAtletasPendentesPorUsuarioCriadorAsync(Guid usuarioId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Partida>> ListarComPendenteDeVinculoPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default);
    Task<bool> ExisteAtletaPendenteEmPartidaCriadaPorUsuarioAsync(
        Guid usuarioId,
        Guid atletaId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Partida>> ListarParaRankingGeralAsync(
        Guid? usuarioOrganizadorId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Partida>> ListarParaRankingPorLigaAsync(Guid ligaId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Partida>> ListarParaRankingSemCompeticaoOuCategoriaAsync(
        Guid? usuarioOrganizadorId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Partida>> ListarParaRankingPorCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Partida>> ListarParaRankingPorGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default);
    Task<Guid?> ObterUltimaCompeticaoComPartidaEncerradaAsync(
        Guid? usuarioOrganizadorId,
        Guid? atletaId,
        CancellationToken cancellationToken = default);
    Task<UsuarioResumoDto> ObterResumoUsuarioPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default);
    Task<Partida?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AdicionarAsync(Partida partida, CancellationToken cancellationToken = default);
    void Atualizar(Partida partida);
    void Remover(Partida partida);
}

public interface IPartidaAprovacaoRepositorio
{
    Task<IReadOnlyList<PartidaAprovacao>> ListarPorPartidaAsync(Guid partidaId, CancellationToken cancellationToken = default);
    Task<PartidaAprovacao?> ObterPorPartidaEAtletaAsync(
        Guid partidaId,
        Guid atletaId,
        CancellationToken cancellationToken = default);
    Task AdicionarAsync(PartidaAprovacao partidaAprovacao, CancellationToken cancellationToken = default);
    void Atualizar(PartidaAprovacao partidaAprovacao);
    void RemoverIntervalo(IEnumerable<PartidaAprovacao> aprovacoes);
}

public interface IPendenciaUsuarioRepositorio
{
    Task<IReadOnlyList<PendenciaUsuario>> ListarPendentesPorUsuarioAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PendenciaUsuario>> ListarPendentesPorPartidaAsync(
        Guid partidaId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PendenciaUsuario>> ListarPendentesPorAtletaAsync(
        Guid atletaId,
        CancellationToken cancellationToken = default);
    Task<PendenciaUsuario?> ObterPendenteAsync(
        TipoPendenciaUsuario tipo,
        Guid usuarioId,
        Guid? partidaId,
        Guid? atletaId,
        CancellationToken cancellationToken = default);
    Task<bool> ExistePendentePorUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken = default);
    Task<PendenciaUsuario?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AdicionarAsync(PendenciaUsuario pendencia, CancellationToken cancellationToken = default);
    void Atualizar(PendenciaUsuario pendencia);
}

public interface IInscricaoCampeonatoRepositorio
{
    Task<IReadOnlyList<InscricaoCampeonato>> ListarPorCampeonatoAsync(
        Guid campeonatoId,
        Guid? categoriaId,
        CancellationToken cancellationToken = default);
    Task<int> ContarPorCategoriaAsync(
        Guid categoriaId,
        Guid? ignorarInscricaoId = null,
        CancellationToken cancellationToken = default);
    Task<InscricaoCampeonato?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<InscricaoCampeonato?> ObterDuplicadaAsync(
        Guid categoriaId,
        Guid duplaId,
        CancellationToken cancellationToken = default);
    Task AdicionarAsync(InscricaoCampeonato inscricao, CancellationToken cancellationToken = default);
    void Atualizar(InscricaoCampeonato inscricao);
    void Remover(InscricaoCampeonato inscricao);
}

public interface IUnidadeTrabalho
{
    Task<int> SalvarAlteracoesAsync(CancellationToken cancellationToken = default);
}
