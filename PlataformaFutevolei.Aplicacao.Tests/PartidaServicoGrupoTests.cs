using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Interfaces.Seguranca;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Aplicacao.Servicos;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;
using Xunit;

namespace PlataformaFutevolei.Aplicacao.Tests;

public class PartidaServicoGrupoTests
{
    [Fact]
    public async Task CriarAsync_Bloqueia_AtletasForaDoGrupoPrivado()
    {
        var cenario = Cenario.Criar(publico: false);
        cenario.AdicionarMembro(cenario.Atletas[0]);
        cenario.AdicionarMembro(cenario.Atletas[1]);

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.CriarAsync(cenario.CriarDto(cenario.Grupo.Id)));

        Assert.Equal("Todos os atletas da partida precisam pertencer ao grupo selecionado.", excecao.Message);
        Assert.Equal(2, cenario.GruposAtletas.Vinculos.Count);
    }

    [Fact]
    public async Task CriarAsync_Permite_AtletasForaDoGrupoPublico()
    {
        var cenario = Cenario.Criar(publico: true);

        var partida = await cenario.Servico.CriarAsync(cenario.CriarDto(cenario.Grupo.Id));

        Assert.Equal(cenario.Grupo.Id, partida.GrupoId);
        Assert.Equal(4, cenario.GruposAtletas.Vinculos.Count);
    }

    [Fact]
    public async Task CriarAsync_GrupoPublico_AdicionaAutomaticamenteAtletasAusentes()
    {
        var cenario = Cenario.Criar(publico: true);
        cenario.AdicionarMembro(cenario.Atletas[0]);

        await cenario.Servico.CriarAsync(cenario.CriarDto(cenario.Grupo.Id));

        Assert.All(cenario.Atletas, atleta =>
            Assert.Contains(cenario.GruposAtletas.Vinculos, vinculo =>
                vinculo.GrupoId == cenario.Grupo.Id && vinculo.AtletaId == atleta.Id));
    }

    [Fact]
    public async Task CriarAsync_GrupoPublico_NaoDuplicaMembroExistente()
    {
        var cenario = Cenario.Criar(publico: true);
        cenario.AdicionarMembro(cenario.Atletas[0]);
        cenario.AdicionarMembro(cenario.Atletas[1]);

        await cenario.Servico.CriarAsync(cenario.CriarDto(cenario.Grupo.Id));

        Assert.Equal(4, cenario.GruposAtletas.Vinculos.Select(x => x.AtletaId).Distinct().Count());
        Assert.Equal(4, cenario.GruposAtletas.Vinculos.Count);
    }

    [Fact]
    public async Task CriarAsync_SemGrupoSelecionado_MantemFluxoAvulso()
    {
        var cenario = Cenario.Criar(publico: true);

        var partida = await cenario.Servico.CriarAsync(cenario.CriarDto(grupoId: null));

        Assert.Equal(cenario.GrupoGeral.Id, partida.GrupoId);
        Assert.Equal(4, cenario.GruposAtletas.Vinculos.Count);
    }

    private sealed class Cenario
    {
        private Cenario(bool publico)
        {
            Usuario = new Usuario
            {
                Nome = "Usuário Teste",
                Email = "usuario@teste.com",
                Perfil = PerfilUsuario.Atleta
            };
            Grupo = new Grupo { Nome = "AD7", Publico = publico, DataInicio = DateTime.UtcNow };
            GrupoGeral = new Grupo { Nome = "Geral", Publico = true, DataInicio = DateTime.UtcNow };
            Atletas =
            [
                new Atleta { Nome = "Alan Silva" },
                new Atleta { Nome = "Bruno Souza" },
                new Atleta { Nome = "Carlos Lima" },
                new Atleta { Nome = "Anda Costa" }
            ];

            GruposAtletas = new GrupoAtletaRepositorioMemoria();
            var partidaRepositorio = new PartidaRepositorioMemoria();
            var resolvedor = new ResolvedorAtletaDuplaMemoria(Atletas, GruposAtletas);

            Servico = new PartidaServico(
                partidaRepositorio,
                new CategoriaCompeticaoRepositorioStub(),
                new GrupoRepositorioStub(Grupo),
                GruposAtletas,
                new GrupoPadraoServicoStub(Grupo, GrupoGeral),
                new DuplaRepositorioStub(),
                new InscricaoCampeonatoRepositorioStub(),
                new UnidadeTrabalhoStub(),
                new AutorizacaoUsuarioServicoStub(Usuario),
                resolvedor,
                new PendenciaServicoStub(),
                new RankingServicoStub(),
                new MidiaPartidaServiceStub());
        }

        public Usuario Usuario { get; }
        public Grupo Grupo { get; }
        public Grupo GrupoGeral { get; }
        public IReadOnlyList<Atleta> Atletas { get; }
        public GrupoAtletaRepositorioMemoria GruposAtletas { get; }
        public PartidaServico Servico { get; }

        public static Cenario Criar(bool publico) => new(publico);

        public void AdicionarMembro(Atleta atleta)
            => GruposAtletas.Vinculos.Add(new GrupoAtleta { GrupoId = Grupo.Id, AtletaId = atleta.Id, Atleta = atleta });

        public CriarPartidaDto CriarDto(Guid? grupoId)
            => new(
                CompeticaoId: null,
                GrupoId: grupoId,
                NomeGrupo: null,
                CategoriaCompeticaoId: null,
                DuplaAId: null,
                DuplaBId: null,
                DuplaAAtleta1Id: Atletas[0].Id,
                DuplaAAtleta1Nome: null,
                DuplaAAtleta2Id: Atletas[1].Id,
                DuplaAAtleta2Nome: null,
                DuplaBAtleta1Id: Atletas[2].Id,
                DuplaBAtleta1Nome: null,
                DuplaBAtleta2Id: Atletas[3].Id,
                DuplaBAtleta2Nome: null,
                FaseCampeonato: null,
                Status: StatusPartida.Encerrada,
                PlacarDuplaA: 21,
                PlacarDuplaB: 18,
                DataPartida: DateTime.UtcNow,
                Observacoes: null);
    }

    private sealed class ResolvedorAtletaDuplaMemoria(
        IReadOnlyList<Atleta> atletas,
        GrupoAtletaRepositorioMemoria gruposAtletas) : IResolvedorAtletaDuplaServico
    {
        private readonly List<Dupla> duplas = [];

        public Task<Atleta> ObterAtletaExistenteAsync(Guid atletaId, string mensagemQuandoInvalido, CancellationToken cancellationToken = default)
            => Task.FromResult(atletas.First(x => x.Id == atletaId));

        public Task<Atleta> ResolverAtletaAsync(Guid? atletaId, string? nomeInformado, string? apelidoInformado, string mensagemQuandoInvalido, bool cadastroPendente, CancellationToken cancellationToken = default)
        {
            if (atletaId.HasValue)
            {
                return ObterAtletaExistenteAsync(atletaId.Value, mensagemQuandoInvalido, cancellationToken);
            }

            return Task.FromResult(atletas.First(x => x.Nome == nomeInformado));
        }

        public Task<Atleta> ObterOuCriarAtletaAsync(string? nomeInformado, string? apelidoInformado, bool cadastroPendente, CancellationToken cancellationToken = default)
            => Task.FromResult(atletas.First(x => x.Nome == nomeInformado));

        public Task<Atleta> ObterOuCriarAtletaParaUsuarioAsync(string nomeInformado, string emailInformado, CancellationToken cancellationToken = default)
            => Task.FromResult(atletas.First(x => x.Nome == nomeInformado));

        public Task<Dupla> ObterOuCriarDuplaAsync(Atleta atleta1, Atleta atleta2, CancellationToken cancellationToken = default)
        {
            var ids = atleta1.Id.CompareTo(atleta2.Id) <= 0
                ? (atleta1.Id, atleta2.Id, atleta1, atleta2)
                : (atleta2.Id, atleta1.Id, atleta2, atleta1);
            var dupla = duplas.FirstOrDefault(x => x.Atleta1Id == ids.Item1 && x.Atleta2Id == ids.Item2);
            if (dupla is not null)
            {
                return Task.FromResult(dupla);
            }

            dupla = new Dupla
            {
                Nome = $"{ids.Item3.Nome} / {ids.Item4.Nome}",
                Atleta1Id = ids.Item1,
                Atleta2Id = ids.Item2,
                Atleta1 = ids.Item3,
                Atleta2 = ids.Item4
            };
            duplas.Add(dupla);
            return Task.FromResult(dupla);
        }

        public async Task<GrupoAtleta> GarantirAtletaNoGrupoAsync(Guid grupoId, Atleta atleta, CancellationToken cancellationToken = default)
        {
            var existente = await gruposAtletas.ObterPorGrupoEAtletaAsync(grupoId, atleta.Id, cancellationToken);
            if (existente is not null)
            {
                return existente;
            }

            var vinculo = new GrupoAtleta { GrupoId = grupoId, AtletaId = atleta.Id, Atleta = atleta };
            await gruposAtletas.AdicionarAsync(vinculo, cancellationToken);
            return vinculo;
        }
    }

    public sealed class GrupoAtletaRepositorioMemoria : IGrupoAtletaRepositorio
    {
        public List<GrupoAtleta> Vinculos { get; } = [];

        public Task<IReadOnlyList<GrupoAtleta>> ListarPorGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<GrupoAtleta>>(Vinculos.Where(x => x.GrupoId == grupoId).ToList());

        public Task<IReadOnlyList<GrupoAtleta>> ListarPorGrupoParaAtualizacaoAsync(Guid grupoId, CancellationToken cancellationToken = default)
            => ListarPorGrupoAsync(grupoId, cancellationToken);

        public Task<IReadOnlyList<GrupoAtleta>> BuscarPorGrupoAsync(Guid grupoId, string termo, CancellationToken cancellationToken = default)
            => ListarPorGrupoAsync(grupoId, cancellationToken);

        public Task<IReadOnlyList<GrupoAtleta>> ListarPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<GrupoAtleta>>(Vinculos.Where(x => x.AtletaId == atletaId).ToList());

        public Task<IReadOnlyList<GrupoAtleta>> ListarPorAtletaParaAtualizacaoAsync(Guid atletaId, CancellationToken cancellationToken = default)
            => ListarPorAtletaAsync(atletaId, cancellationToken);

        public Task<GrupoAtleta?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Vinculos.FirstOrDefault(x => x.Id == id));

        public Task<GrupoAtleta?> ObterPorGrupoEAtletaAsync(Guid grupoId, Guid atletaId, CancellationToken cancellationToken = default)
            => Task.FromResult(Vinculos.FirstOrDefault(x => x.GrupoId == grupoId && x.AtletaId == atletaId));

        public Task AdicionarAsync(GrupoAtleta grupoAtleta, CancellationToken cancellationToken = default)
        {
            Vinculos.Add(grupoAtleta);
            return Task.CompletedTask;
        }

        public void Remover(GrupoAtleta grupoAtleta) => Vinculos.Remove(grupoAtleta);
    }

    private sealed class PartidaRepositorioMemoria : IPartidaRepositorio
    {
        private readonly List<Partida> partidas = [];

        public Task<IReadOnlyList<Partida>> ListarPorDiaAsync(DateTime inicioUtc, DateTime fimUtc, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Partida>>([]);

        public Task<Partida?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(partidas.FirstOrDefault(x => x.Id == id));

        public Task AdicionarAsync(Partida partida, CancellationToken cancellationToken = default)
        {
            partidas.Add(partida);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Partida>> ListarPorCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<int> ContarRegistradasAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<IReadOnlyList<Partida>> ListarPorGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarPorCategoriaAsync(Guid categoriaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarPorUsuarioCriadorAsync(Guid usuarioId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarFeedAsync(int skip, int take, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarPorAtletaParaRemocaoAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarReferenciandoPartidasAsync(IReadOnlyCollection<Guid> partidaIds, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<Partida?> ObterUltimaDoGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default) => Task.FromResult<Partida?>(null);
        public Task<Partida?> ObterUltimaDoAtletaNoGrupoAsync(Guid grupoId, Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult<Partida?>(null);
        public Task<IReadOnlyList<Partida>> ListarComAtletasPendentesPorUsuarioCriadorAsync(Guid usuarioId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarComPendenteDeVinculoPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<bool> ExisteAtletaPendenteEmPartidaCriadaPorUsuarioAsync(Guid usuarioId, Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<IReadOnlyList<Partida>> ListarParaRankingGeralAsync(Guid? usuarioOrganizadorId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarParaRankingPorLigaAsync(Guid ligaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarParaRankingSemCompeticaoOuCategoriaAsync(Guid? usuarioOrganizadorId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarParaRankingPorCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarParaRankingPorGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<Guid?> ObterUltimaCompeticaoComPartidaEncerradaAsync(Guid? usuarioOrganizadorId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult<Guid?>(null);
        public Task<AtletasSugestoesPartidaDto> ObterSugestoesPartidaAsync(Guid atletaId, Guid? grupoId, int limitePorSecao, CancellationToken cancellationToken = default) => Task.FromResult(new AtletasSugestoesPartidaDto([], []));
        public Task<UsuarioResumoDto> ObterResumoUsuarioPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void Atualizar(Partida partida) { }
        public void Remover(Partida partida) => partidas.Remove(partida);
    }

    private sealed class GrupoPadraoServicoStub(Grupo grupo, Grupo grupoGeral) : IGrupoPadraoServico
    {
        public string NomeGrupoGeral => "Geral";
        public Task<Grupo> ObterOuCriarGrupoGeralAsync(CancellationToken cancellationToken = default) => Task.FromResult(grupoGeral);
        public Task<Grupo> ResolverGrupoRegistroPartidaAsync(Guid? grupoId, string? nomeNovoGrupo, CancellationToken cancellationToken = default)
            => Task.FromResult(grupoId.HasValue ? grupo : grupoGeral);
        public Task ValidarNomeDisponivelOuAcessivelAsync(string nome, Guid? grupoIgnoradoId = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class GrupoRepositorioStub(Grupo grupo) : IGrupoRepositorio
    {
        public Task<Grupo?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Grupo?>(id == grupo.Id ? grupo : null);
        public Task<Grupo?> ObterPorNomeNormalizadoAsync(string nome, CancellationToken cancellationToken = default) => Task.FromResult<Grupo?>(null);
        public Task<IReadOnlyList<Grupo>> ListarAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Grupo>>([]);
        public Task<IReadOnlyList<Grupo>> ListarParaSelecaoAsync(Guid usuarioId, Guid? atletaId, bool incluirPrivadosDeTerceiros, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Grupo>>([]);
        public Task<int> ContarPublicosAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<Grupo?> ObterResumoUsuarioAsync(Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult<Grupo?>(null);
        public Task<IReadOnlyList<Grupo>> ListarResumosUsuarioAsync(Guid usuarioId, Guid? atletaId, int limite, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Grupo>>([]);
        public Task<IReadOnlyList<Grupo>> ListarDashboardUsuarioAsync(Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Grupo>>([]);
        public Task<IReadOnlyList<Guid>> ListarIdsComAcessoAtletaAsync(Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Guid>>([]);
        public Task<bool> AtletaPossuiAcessoAsync(Guid grupoId, Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<Grupo?> ObterPorNomeEOrganizadorAsync(string nome, Guid? usuarioOrganizadorId, CancellationToken cancellationToken = default) => Task.FromResult<Grupo?>(null);
        public Task<IReadOnlyList<Grupo>> ListarPorUsuarioOrganizadorParaAtualizacaoAsync(Guid usuarioOrganizadorId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Grupo>>([]);
        public Task AdicionarAsync(Grupo grupo, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Atualizar(Grupo grupo) { }
        public void Remover(Grupo grupo) { }
    }

    private sealed class AutorizacaoUsuarioServicoStub(Usuario usuario) : IAutorizacaoUsuarioServico
    {
        public Task<Usuario?> ObterUsuarioAtualAsync(CancellationToken cancellationToken = default) => Task.FromResult<Usuario?>(usuario);
        public Task<Usuario> ObterUsuarioAtualObrigatorioAsync(CancellationToken cancellationToken = default) => Task.FromResult(usuario);
        public Task GarantirAdministradorAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task GarantirAdminOuOrganizadorAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task GarantirAcessoAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task GarantirGestaoCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task GarantirGestaoGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class UnidadeTrabalhoStub : IUnidadeTrabalho
    {
        public Task<int> SalvarAlteracoesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
        public Task ExecutarEmTransacaoAsync(Func<CancellationToken, Task> operacao, CancellationToken cancellationToken = default) => operacao(cancellationToken);
    }

    private sealed class CategoriaCompeticaoRepositorioStub : ICategoriaCompeticaoRepositorio
    {
        public Task<IReadOnlyList<CategoriaCompeticao>> ListarPorCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<CategoriaCompeticao>>([]);
        public Task<IReadOnlyList<CategoriaCompeticao>> ListarDisponiveisParaVinculoAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<CategoriaCompeticao>>([]);
        public Task<CategoriaCompeticao?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<CategoriaCompeticao?>(null);
        public Task AdicionarAsync(CategoriaCompeticao categoria, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Atualizar(CategoriaCompeticao categoria) { }
        public void Remover(CategoriaCompeticao categoria) { }
    }

    private sealed class DuplaRepositorioStub : IDuplaRepositorio
    {
        public Task<IReadOnlyList<Dupla>> ListarAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Dupla>>([]);
        public Task<IReadOnlyList<Dupla>> ListarInscritasPorOrganizadorAsync(Guid usuarioOrganizadorId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Dupla>>([]);
        public Task<bool> PertenceAoOrganizadorAsync(Guid duplaId, Guid usuarioOrganizadorId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<IReadOnlyList<Dupla>> ListarPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Dupla>>([]);
        public Task<IReadOnlyList<Dupla>> ListarPorAtletaParaAtualizacaoAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Dupla>>([]);
        public Task<Dupla?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Dupla?>(null);
        public Task<Dupla?> ObterPorAtletasAsync(Guid atleta1Id, Guid atleta2Id, CancellationToken cancellationToken = default) => Task.FromResult<Dupla?>(null);
        public Task AdicionarAsync(Dupla dupla, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Atualizar(Dupla dupla) { }
        public void Remover(Dupla dupla) { }
    }

    private sealed class InscricaoCampeonatoRepositorioStub : IInscricaoCampeonatoRepositorio
    {
        public Task<IReadOnlyList<InscricaoCampeonato>> ListarPorCampeonatoAsync(Guid campeonatoId, Guid? categoriaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<InscricaoCampeonato>>([]);
        public Task<int> ContarPorCategoriaAsync(Guid categoriaId, Guid? ignorarInscricaoId = null, CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<InscricaoCampeonato?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<InscricaoCampeonato?>(null);
        public Task<InscricaoCampeonato?> ObterDuplicadaAsync(Guid categoriaId, Guid duplaId, CancellationToken cancellationToken = default) => Task.FromResult<InscricaoCampeonato?>(null);
        public Task<IReadOnlyList<InscricaoCampeonato>> ListarPorDuplasParaAtualizacaoAsync(IReadOnlyCollection<Guid> duplaIds, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<InscricaoCampeonato>>([]);
        public Task AdicionarAsync(InscricaoCampeonato inscricao, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Atualizar(InscricaoCampeonato inscricao) { }
        public void Remover(InscricaoCampeonato inscricao) { }
    }

    private sealed class PendenciaServicoStub : IPendenciaServico
    {
        public Task InicializarFluxoPartidaAsync(Partida partida, Guid usuarioRegistradorId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<PendenciaUsuarioDto>> ListarMinhasAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<PendenciaUsuarioDto>>([]);
        public Task<PendenciasResumoDto> ObterResumoAsync(CancellationToken cancellationToken = default) => Task.FromResult(new PendenciasResumoDto(0, 0, 0, 0));
        public Task<bool> ExistePendenciaAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<PendenciaUsuarioDto> AprovarPartidaAsync(Guid pendenciaId, ResponderPendenciaPartidaDto dto, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PendenciaUsuarioDto> ContestarPartidaAsync(Guid pendenciaId, ResponderPendenciaPartidaDto dto, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<AtualizarContatoPendenciaResultadoDto> CompletarContatoAsync(Guid pendenciaId, AtualizarContatoPendenciaDto dto, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PendenciaUsuarioDto> ConfirmarVinculoAtletaCadastradoAsync(Guid pendenciaId, ConfirmarVinculoAtletaPendenciaDto dto, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SincronizarAposVinculoAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class RankingServicoStub : IRankingServico
    {
        public Task<IReadOnlyList<RankingCategoriaDto>> ListarAtletasPorGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<RankingCategoriaDto>>([]);
        public Task<RankingFiltroInicialDto> ObterFiltroInicialAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<RankingCategoriaDto>> ListarAtletasGeralAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<RankingCategoriaDto>>([]);
        public Task<IReadOnlyList<RankingCategoriaDto>> ListarAtletasPorLigaAsync(Guid ligaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<RankingCategoriaDto>>([]);
        public Task<RankingRegiaoFiltroDto> ListarRegioesDisponiveisAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<RankingCategoriaDto>> ListarAtletasPorRegiaoAsync(string? estado, string? cidade, string? bairro, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<RankingCategoriaDto>>([]);
        public Task<IReadOnlyList<RankingCategoriaDto>> ListarAtletasPorCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<RankingCategoriaDto>>([]);
    }

    private sealed class MidiaPartidaServiceStub : IMidiaPartidaService
    {
        public Task<MidiaPartidaUploadDto> EnviarAsync(ArquivoMidiaPartidaDto arquivo, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task RemoverAsync(string publicId, MidiaPartidaTipo? tipo, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
