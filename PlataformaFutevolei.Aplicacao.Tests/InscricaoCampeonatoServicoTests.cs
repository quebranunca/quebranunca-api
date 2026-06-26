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

public class InscricaoCampeonatoServicoTests
{
    [Fact]
    public async Task CriarAsync_AdminComDuplaExistente_CriaInscricaoAtivaEPaga()
    {
        var cenario = new Cenario();
        var dupla = cenario.AdicionarDupla();

        var inscricao = await cenario.Servico.CriarAsync(cenario.Competicao.Id, NovoDto(cenario.Categoria.Id, dupla.Id, pago: true));

        Assert.Equal(StatusInscricaoCampeonato.Ativa, inscricao.Status);
        Assert.True(inscricao.Pago);
        Assert.Equal(dupla.Id, inscricao.DuplaId);
        Assert.Single(cenario.Inscricoes.Itens);
    }

    [Fact]
    public async Task CriarAsync_DuplaDuplicadaNaCategoria_Bloqueia()
    {
        var cenario = new Cenario();
        var dupla = cenario.AdicionarDupla();
        cenario.AdicionarInscricao(dupla, StatusInscricaoCampeonato.Ativa);

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.CriarAsync(cenario.Competicao.Id, NovoDto(cenario.Categoria.Id, dupla.Id)));

        Assert.Equal("Esta dupla já está inscrita nesta categoria do campeonato.", excecao.Message);
    }

    [Fact]
    public async Task CriarAsync_CategoriaComInscricoesEncerradas_Bloqueia()
    {
        var cenario = new Cenario();
        cenario.Categoria.InscricoesEncerradas = true;
        var dupla = cenario.AdicionarDupla();

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.CriarAsync(cenario.Competicao.Id, NovoDto(cenario.Categoria.Id, dupla.Id)));

        Assert.Equal("As inscrições desta categoria estão encerradas.", excecao.Message);
    }

    [Fact]
    public async Task CriarAsync_CategoriaLotada_Bloqueia()
    {
        var cenario = new Cenario();
        cenario.Categoria.QuantidadeMaximaDuplas = 1;
        cenario.AdicionarInscricao(cenario.AdicionarDupla(), StatusInscricaoCampeonato.Ativa);
        var novaDupla = cenario.AdicionarDupla("Nova dupla");

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.CriarAsync(cenario.Competicao.Id, NovoDto(cenario.Categoria.Id, novaDupla.Id)));

        Assert.Equal("A categoria já atingiu a quantidade máxima de duplas inscritas.", excecao.Message);
    }

    [Fact]
    public async Task ListarPorCampeonatoAsync_Atleta_RetornaSomenteInscricoesDaSuaDupla()
    {
        var atletaUsuario = new Atleta { Nome = "Atleta usuário" };
        var usuario = new Usuario { Nome = "Atleta", Perfil = PerfilUsuario.Atleta, Ativo = true, AtletaId = atletaUsuario.Id };
        var cenario = new Cenario(usuario);
        var minhaDupla = cenario.AdicionarDupla("Minha dupla", atletaUsuario);
        var outraDupla = cenario.AdicionarDupla("Outra dupla");
        cenario.AdicionarInscricao(minhaDupla, StatusInscricaoCampeonato.Ativa);
        cenario.AdicionarInscricao(outraDupla, StatusInscricaoCampeonato.Ativa);

        var inscricoes = await cenario.Servico.ListarPorCampeonatoAsync(cenario.Competicao.Id, null);

        var inscricao = Assert.Single(inscricoes);
        Assert.Equal(minhaDupla.Id, inscricao.DuplaId);
    }

    [Fact]
    public async Task AprovarAsync_InscricaoPendente_AtivaInscricao()
    {
        var cenario = new Cenario();
        var inscricao = cenario.AdicionarInscricao(cenario.AdicionarDupla(), StatusInscricaoCampeonato.PendenteAprovacao);

        var aprovada = await cenario.Servico.AprovarAsync(cenario.Competicao.Id, inscricao.Id);

        Assert.Equal(StatusInscricaoCampeonato.Ativa, aprovada.Status);
    }

    [Fact]
    public async Task AprovarAsync_InscricaoCancelada_Bloqueia()
    {
        var cenario = new Cenario();
        var inscricao = cenario.AdicionarInscricao(cenario.AdicionarDupla(), StatusInscricaoCampeonato.Cancelada);

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.AprovarAsync(cenario.Competicao.Id, inscricao.Id));

        Assert.Equal("Inscrições canceladas não podem ser aprovadas.", excecao.Message);
    }

    [Fact]
    public async Task RemoverAsync_AtletaDeOutraDupla_Bloqueia()
    {
        var usuario = new Usuario { Nome = "Atleta", Perfil = PerfilUsuario.Atleta, Ativo = true, AtletaId = Guid.NewGuid() };
        var cenario = new Cenario(usuario);
        var inscricao = cenario.AdicionarInscricao(cenario.AdicionarDupla(), StatusInscricaoCampeonato.Ativa);

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.RemoverAsync(cenario.Competicao.Id, inscricao.Id));

        Assert.Equal("Você só pode excluir as suas próprias inscrições.", excecao.Message);
    }

    private static CriarInscricaoCampeonatoDto NovoDto(Guid categoriaId, Guid duplaId, bool pago = false)
        => new(
            categoriaId,
            duplaId,
            Atleta1Id: null,
            Atleta2Id: null,
            NomeAtleta1: null,
            ApelidoAtleta1: null,
            NomeAtleta2: null,
            ApelidoAtleta2: null,
            Observacao: " observação ",
            Pago: pago);

    private sealed class Cenario
    {
        public Cenario(Usuario? usuario = null)
        {
            Usuario = usuario ?? new Usuario { Nome = "Admin", Perfil = PerfilUsuario.Administrador, Ativo = true };
            Competicao = new Competicao
            {
                Nome = "Campeonato",
                Tipo = TipoCompeticao.Campeonato,
                DataInicio = DateTime.UtcNow,
                InscricoesAbertas = true
            };
            Categoria = new CategoriaCompeticao
            {
                CompeticaoId = Competicao.Id,
                Competicao = Competicao,
                Nome = "Categoria",
                Genero = GeneroCategoria.Misto,
                Nivel = NivelCategoria.Livre,
                StatusInscricao = StatusInscricoesCategoriaCampeonato.Aberta
            };
            Competicoes = new CompeticaoRepositorioStub(Competicao);
            Categorias = new CategoriaCompeticaoRepositorioStub(Categoria);
            Duplas = new DuplaRepositorioStub();
            Inscricoes = new InscricaoCampeonatoRepositorioStub();
            Servico = new InscricaoCampeonatoServico(
                Inscricoes,
                Competicoes,
                Categorias,
                Duplas,
                new UnidadeTrabalhoStub(),
                new AutorizacaoUsuarioServicoStub(Usuario),
                new ResolvedorAtletaDuplaServicoStub(Duplas));
        }

        public Usuario Usuario { get; }
        public Competicao Competicao { get; }
        public CategoriaCompeticao Categoria { get; }
        public CompeticaoRepositorioStub Competicoes { get; }
        public CategoriaCompeticaoRepositorioStub Categorias { get; }
        public DuplaRepositorioStub Duplas { get; }
        public InscricaoCampeonatoRepositorioStub Inscricoes { get; }
        public InscricaoCampeonatoServico Servico { get; }

        public Dupla AdicionarDupla(string nome = "Dupla", Atleta? atleta1 = null)
        {
            atleta1 ??= new Atleta { Nome = $"{nome} A" };
            var atleta2 = new Atleta { Nome = $"{nome} B" };
            var dupla = new Dupla
            {
                Nome = nome,
                Atleta1Id = atleta1.Id,
                Atleta1 = atleta1,
                Atleta2Id = atleta2.Id,
                Atleta2 = atleta2
            };
            Duplas.Itens.Add(dupla);
            return dupla;
        }

        public InscricaoCampeonato AdicionarInscricao(Dupla dupla, StatusInscricaoCampeonato status)
        {
            var inscricao = new InscricaoCampeonato
            {
                CompeticaoId = Competicao.Id,
                Competicao = Competicao,
                CategoriaCompeticaoId = Categoria.Id,
                CategoriaCompeticao = Categoria,
                DuplaId = dupla.Id,
                Dupla = dupla,
                Status = status
            };
            Inscricoes.Itens.Add(inscricao);
            return inscricao;
        }
    }

    private sealed class InscricaoCampeonatoRepositorioStub : IInscricaoCampeonatoRepositorio
    {
        public List<InscricaoCampeonato> Itens { get; } = [];
        public Task<IReadOnlyList<InscricaoCampeonato>> ListarPorCampeonatoAsync(Guid campeonatoId, Guid? categoriaId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<InscricaoCampeonato>>(Itens.Where(x => x.CompeticaoId == campeonatoId && (!categoriaId.HasValue || x.CategoriaCompeticaoId == categoriaId)).ToList());
        public Task<int> ContarPorCategoriaAsync(Guid categoriaId, Guid? ignorarInscricaoId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(Itens.Count(x => x.CategoriaCompeticaoId == categoriaId && x.Id != ignorarInscricaoId));
        public Task<InscricaoCampeonato?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Itens.FirstOrDefault(x => x.Id == id));
        public Task<InscricaoCampeonato?> ObterDuplicadaAsync(Guid categoriaId, Guid duplaId, CancellationToken cancellationToken = default)
            => Task.FromResult(Itens.FirstOrDefault(x => x.CategoriaCompeticaoId == categoriaId && x.DuplaId == duplaId));
        public Task<IReadOnlyList<InscricaoCampeonato>> ListarPorDuplasParaAtualizacaoAsync(IReadOnlyCollection<Guid> duplaIds, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<InscricaoCampeonato>>(Itens.Where(x => duplaIds.Contains(x.DuplaId)).ToList());
        public Task AdicionarAsync(InscricaoCampeonato inscricao, CancellationToken cancellationToken = default)
        {
            Itens.Add(inscricao);
            return Task.CompletedTask;
        }
        public void Atualizar(InscricaoCampeonato inscricao) { }
        public void Remover(InscricaoCampeonato inscricao) => Itens.Remove(inscricao);
    }

    private sealed class CompeticaoRepositorioStub(Competicao competicao) : ICompeticaoRepositorio
    {
        public Task<IReadOnlyList<Competicao>> ListarAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Competicao>>([competicao]);
        public Task<Competicao?> ObterGrupoResumoUsuarioAsync(Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult<Competicao?>(competicao);
        public Task<IReadOnlyList<Guid>> ListarIdsComAcessoAtletaAsync(Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Guid>>([competicao.Id]);
        public Task<bool> AtletaPossuiAcessoAsync(Guid competicaoId, Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<Competicao?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default) => Task.FromResult(competicao.Nome == nome ? competicao : null);
        public Task<Competicao?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(id == competicao.Id ? competicao : null);
        public Task<Competicao?> ObterPorIdComCategoriasAsync(Guid id, CancellationToken cancellationToken = default) => ObterPorIdAsync(id, cancellationToken);
        public Task AdicionarAsync(Competicao competicao, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Atualizar(Competicao competicao) { }
        public void Remover(Competicao competicao) { }
    }

    private sealed class CategoriaCompeticaoRepositorioStub(CategoriaCompeticao categoria) : ICategoriaCompeticaoRepositorio
    {
        public Task<IReadOnlyList<CategoriaCompeticao>> ListarPorCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<CategoriaCompeticao>>([categoria]);
        public Task<IReadOnlyList<CategoriaCompeticao>> ListarDisponiveisParaVinculoAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<CategoriaCompeticao>>([categoria]);
        public Task<CategoriaCompeticao?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(id == categoria.Id ? categoria : null);
        public Task AdicionarAsync(CategoriaCompeticao categoria, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Atualizar(CategoriaCompeticao categoria) { }
        public void Remover(CategoriaCompeticao categoria) { }
    }

    private sealed class DuplaRepositorioStub : IDuplaRepositorio
    {
        public List<Dupla> Itens { get; } = [];
        public Task<IReadOnlyList<Dupla>> ListarAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Dupla>>(Itens);
        public Task<IReadOnlyList<Dupla>> ListarInscritasPorOrganizadorAsync(Guid usuarioOrganizadorId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Dupla>>(Itens);
        public Task<bool> PertenceAoOrganizadorAsync(Guid duplaId, Guid usuarioOrganizadorId, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<IReadOnlyList<Dupla>> ListarPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Dupla>>(Itens.Where(x => x.Atleta1Id == atletaId || x.Atleta2Id == atletaId).ToList());
        public Task<IReadOnlyList<Dupla>> ListarPorAtletaParaAtualizacaoAsync(Guid atletaId, CancellationToken cancellationToken = default) => ListarPorAtletaAsync(atletaId, cancellationToken);
        public Task<Dupla?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Itens.FirstOrDefault(x => x.Id == id));
        public Task<Dupla?> ObterPorAtletasAsync(Guid atleta1Id, Guid atleta2Id, CancellationToken cancellationToken = default) => Task.FromResult(Itens.FirstOrDefault(x => x.Atleta1Id == atleta1Id && x.Atleta2Id == atleta2Id));
        public Task AdicionarAsync(Dupla dupla, CancellationToken cancellationToken = default)
        {
            Itens.Add(dupla);
            return Task.CompletedTask;
        }
        public void Atualizar(Dupla dupla) { }
        public void Remover(Dupla dupla) => Itens.Remove(dupla);
    }

    private sealed class UnidadeTrabalhoStub : IUnidadeTrabalho
    {
        public Task<int> SalvarAlteracoesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
        public Task ExecutarEmTransacaoAsync(Func<CancellationToken, Task> operacao, CancellationToken cancellationToken = default) => operacao(cancellationToken);
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

    private sealed class ResolvedorAtletaDuplaServicoStub(DuplaRepositorioStub duplas) : IResolvedorAtletaDuplaServico
    {
        public Task<Atleta> ObterAtletaExistenteAsync(Guid atletaId, string mensagemQuandoInvalido, CancellationToken cancellationToken = default)
            => Task.FromResult(new Atleta { Nome = "Atleta existente" });
        public Task<Atleta> ResolverAtletaAsync(Guid? atletaId, string? nomeInformado, string? apelidoInformado, string mensagemQuandoInvalido, bool cadastroPendente, CancellationToken cancellationToken = default)
            => Task.FromResult(new Atleta { Nome = nomeInformado ?? "Atleta resolvido", CadastroPendente = cadastroPendente });
        public Task<Atleta> ObterOuCriarAtletaAsync(string? nomeInformado, string? apelidoInformado, bool cadastroPendente, CancellationToken cancellationToken = default)
            => Task.FromResult(new Atleta { Nome = nomeInformado ?? "Atleta pendente", CadastroPendente = cadastroPendente });
        public Task<Atleta> ObterOuCriarAtletaParaUsuarioAsync(string nomeInformado, string emailInformado, string? apelidoInformado = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new Atleta { Nome = nomeInformado, Email = emailInformado });
        public Task<Dupla> ObterOuCriarDuplaAsync(Atleta atleta1, Atleta atleta2, CancellationToken cancellationToken = default)
        {
            var dupla = new Dupla { Nome = $"{atleta1.Nome} / {atleta2.Nome}", Atleta1Id = atleta1.Id, Atleta1 = atleta1, Atleta2Id = atleta2.Id, Atleta2 = atleta2 };
            duplas.Itens.Add(dupla);
            return Task.FromResult(dupla);
        }
        public Task<GrupoAtleta> GarantirAtletaNoGrupoAsync(Guid grupoId, Atleta atleta, CancellationToken cancellationToken = default)
            => Task.FromResult(new GrupoAtleta { GrupoId = grupoId, AtletaId = atleta.Id, Atleta = atleta });
    }
}
