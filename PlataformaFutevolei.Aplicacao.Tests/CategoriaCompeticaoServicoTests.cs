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

public class CategoriaCompeticaoServicoTests
{
    [Fact]
    public async Task CriarAsync_CategoriaValida_CriaComNomeAutomaticoEInscricoesAbertas()
    {
        var cenario = new Cenario();
        var competicao = cenario.AdicionarCompeticao(TipoCompeticao.Campeonato);

        var categoria = await cenario.Servico.CriarAsync(new CriarCategoriaCompeticaoDto(
            competicao.Id,
            FormatoCampeonatoId: null,
            Nome: "",
            GeneroCategoria.Masculino,
            NivelCategoria.Intermediario,
            PesoRanking: null,
            QuantidadeMaximaDuplas: 16,
            InscricoesEncerradas: false));

        Assert.Equal("Intermediário Masculino", categoria.Nome);
        Assert.Equal(1m, categoria.PesoRanking);
        Assert.Equal(16, categoria.QuantidadeMaximaDuplas);
        Assert.False(categoria.InscricoesEncerradas);
        Assert.Equal(StatusInscricoesCategoriaCampeonato.Aberta, categoria.StatusInscricao);
        Assert.Single(cenario.Categorias.Itens);
    }

    [Fact]
    public async Task CriarAsync_ComFormatoInativo_Bloqueia()
    {
        var cenario = new Cenario();
        var competicao = cenario.AdicionarCompeticao(TipoCompeticao.Campeonato);
        var formato = cenario.AdicionarFormato(TipoFormatoCampeonato.Chave, ativo: false);

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.CriarAsync(new CriarCategoriaCompeticaoDto(
                competicao.Id,
                formato.Id,
                "Categoria",
                GeneroCategoria.Misto,
                NivelCategoria.Livre,
                1m,
                null,
                false)));

        Assert.Equal("O formato de campeonato informado está inativo.", excecao.Message);
    }

    [Fact]
    public async Task CriarAsync_GrupoComFormatoDiferenteDePontosCorridos_Bloqueia()
    {
        var cenario = new Cenario();
        var competicao = cenario.AdicionarCompeticao(TipoCompeticao.Grupo);
        var formato = cenario.AdicionarFormato(TipoFormatoCampeonato.Chave);

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.CriarAsync(new CriarCategoriaCompeticaoDto(
                competicao.Id,
                formato.Id,
                "Categoria",
                GeneroCategoria.Misto,
                NivelCategoria.Livre,
                1m,
                null,
                false)));

        Assert.Equal("Categorias de grupos só podem usar formato padrão de pontos corridos.", excecao.Message);
    }

    [Fact]
    public async Task CriarAsync_DuplicidadeGeneroENivelSemNome_Bloqueia()
    {
        var cenario = new Cenario();
        var competicao = cenario.AdicionarCompeticao(TipoCompeticao.Campeonato);
        cenario.AdicionarCategoria(competicao, "Intermediário Masculino", GeneroCategoria.Masculino, NivelCategoria.Intermediario);

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.CriarAsync(new CriarCategoriaCompeticaoDto(
                competicao.Id,
                null,
                "",
                GeneroCategoria.Masculino,
                NivelCategoria.Intermediario,
                1m,
                null,
                false)));

        Assert.Equal(
            "Informe o nome da categoria quando já existir outra com o mesmo gênero e nível técnico nesta competição.",
            excecao.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CriarAsync_PesoRankingInvalido_Bloqueia(decimal pesoRanking)
    {
        var cenario = new Cenario();
        var competicao = cenario.AdicionarCompeticao(TipoCompeticao.Campeonato);

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.CriarAsync(new CriarCategoriaCompeticaoDto(
                competicao.Id,
                null,
                "Categoria",
                GeneroCategoria.Misto,
                NivelCategoria.Livre,
                pesoRanking,
                null,
                false)));

        Assert.Equal("Peso de ranking da categoria deve ser maior que zero.", excecao.Message);
    }

    [Fact]
    public async Task AtualizarAsync_CategoriaValida_AtualizaCamposEEncerraInscricoes()
    {
        var cenario = new Cenario();
        var competicao = cenario.AdicionarCompeticao(TipoCompeticao.Campeonato);
        var formato = cenario.AdicionarFormato(TipoFormatoCampeonato.Chave);
        var categoria = cenario.AdicionarCategoria(competicao, "Antiga", GeneroCategoria.Feminino, NivelCategoria.Iniciante);

        var atualizada = await cenario.Servico.AtualizarAsync(categoria.Id, new AtualizarCategoriaCompeticaoDto(
            formato.Id,
            "Nova categoria",
            GeneroCategoria.Misto,
            NivelCategoria.Livre,
            2m,
            8,
            InscricoesEncerradas: true));

        Assert.Equal("Nova categoria", atualizada.Nome);
        Assert.Equal(formato.Id, atualizada.FormatoCampeonatoId);
        Assert.Equal(2m, atualizada.PesoRanking);
        Assert.Equal(8, atualizada.QuantidadeMaximaDuplas);
        Assert.True(atualizada.InscricoesEncerradas);
        Assert.Equal(StatusInscricoesCategoriaCampeonato.Encerrada, atualizada.StatusInscricao);
    }

    [Fact]
    public async Task AtualizarAsync_LimiteMenorQueInscricoes_Bloqueia()
    {
        var cenario = new Cenario();
        var competicao = cenario.AdicionarCompeticao(TipoCompeticao.Campeonato);
        var categoria = cenario.AdicionarCategoria(competicao, "Categoria", GeneroCategoria.Misto, NivelCategoria.Livre);
        cenario.Inscricoes.QuantidadePorCategoria[categoria.Id] = 5;

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.AtualizarAsync(categoria.Id, new AtualizarCategoriaCompeticaoDto(
                null,
                "Categoria",
                GeneroCategoria.Misto,
                NivelCategoria.Livre,
                1m,
                4,
                false)));

        Assert.Equal("A categoria já possui mais duplas inscritas do que o novo limite informado.", excecao.Message);
    }

    [Fact]
    public async Task ListarPorCompeticaoAsync_VisitanteEmCampeonato_RetornaCategorias()
    {
        var cenario = new Cenario(usuarioAutenticado: false);
        var competicao = cenario.AdicionarCompeticao(TipoCompeticao.Campeonato);
        cenario.AdicionarCategoria(competicao, "Categoria", GeneroCategoria.Misto, NivelCategoria.Livre);

        var categorias = await cenario.Servico.ListarPorCompeticaoAsync(competicao.Id);

        Assert.Single(categorias);
    }

    [Fact]
    public async Task ListarPorCompeticaoAsync_VisitanteEmGrupo_Bloqueia()
    {
        var cenario = new Cenario(usuarioAutenticado: false);
        var competicao = cenario.AdicionarCompeticao(TipoCompeticao.Grupo);

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.ListarPorCompeticaoAsync(competicao.Id));

        Assert.Equal("Visitantes só podem visualizar categorias de campeonatos e eventos.", excecao.Message);
    }

    [Fact]
    public async Task ListarDisponiveisParaVinculoAsync_CategoriasDuplicadas_RetornaUmaPorGrupoOrdenada()
    {
        var cenario = new Cenario();
        var competicao = cenario.AdicionarCompeticao(TipoCompeticao.Campeonato);
        cenario.AdicionarCategoria(competicao, "A", GeneroCategoria.Misto, NivelCategoria.Livre);
        cenario.AdicionarCategoria(competicao, " a ", GeneroCategoria.Misto, NivelCategoria.Livre);
        cenario.AdicionarCategoria(competicao, "C", GeneroCategoria.Feminino, NivelCategoria.Iniciante);

        var categorias = await cenario.Servico.ListarDisponiveisParaVinculoAsync();

        Assert.Equal([" a ", "C"], categorias.Select(x => x.Nome).ToArray());
    }

    [Fact]
    public async Task AprovarTabelaJogosAsync_ComPartidas_PreencheAprovacao()
    {
        var usuario = new Usuario { Nome = "Admin", Perfil = PerfilUsuario.Administrador, Ativo = true };
        var cenario = new Cenario(usuario);
        var competicao = cenario.AdicionarCompeticao(TipoCompeticao.Campeonato);
        var categoria = cenario.AdicionarCategoria(competicao, "Categoria", GeneroCategoria.Misto, NivelCategoria.Livre);
        cenario.Partidas.Partidas.Add(new Partida { CategoriaCompeticaoId = categoria.Id, CategoriaCompeticao = categoria });

        var aprovada = await cenario.Servico.AprovarTabelaJogosAsync(categoria.Id);

        Assert.True(aprovada.TabelaJogosAprovada);
        Assert.Equal(usuario.Id, aprovada.TabelaJogosAprovadaPorUsuarioId);
        Assert.NotNull(aprovada.TabelaJogosAprovadaEmUtc);
    }

    [Fact]
    public async Task AprovarTabelaJogosAsync_SemPartidas_Bloqueia()
    {
        var cenario = new Cenario();
        var competicao = cenario.AdicionarCompeticao(TipoCompeticao.Campeonato);
        var categoria = cenario.AdicionarCategoria(competicao, "Categoria", GeneroCategoria.Misto, NivelCategoria.Livre);

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.AprovarTabelaJogosAsync(categoria.Id));

        Assert.Equal("Gere a tabela de jogos da categoria antes de aprovar o sorteio.", excecao.Message);
    }

    [Fact]
    public async Task AprovarTabelaJogosAsync_Grupo_Bloqueia()
    {
        var cenario = new Cenario();
        var competicao = cenario.AdicionarCompeticao(TipoCompeticao.Grupo);
        var categoria = cenario.AdicionarCategoria(competicao, "Categoria", GeneroCategoria.Misto, NivelCategoria.Livre);

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.AprovarTabelaJogosAsync(categoria.Id));

        Assert.Equal("A aprovação do sorteio está disponível apenas para campeonatos e eventos.", excecao.Message);
    }

    [Fact]
    public async Task RemoverAsync_CategoriaExistente_Remove()
    {
        var cenario = new Cenario();
        var competicao = cenario.AdicionarCompeticao(TipoCompeticao.Campeonato);
        var categoria = cenario.AdicionarCategoria(competicao, "Categoria", GeneroCategoria.Misto, NivelCategoria.Livre);

        await cenario.Servico.RemoverAsync(categoria.Id);

        Assert.Empty(cenario.Categorias.Itens);
    }

    private sealed class Cenario
    {
        public Cenario(Usuario? usuario = null, bool usuarioAutenticado = true)
        {
            Usuario = usuarioAutenticado
                ? usuario ?? new Usuario { Nome = "Admin", Perfil = PerfilUsuario.Administrador, Ativo = true }
                : null;
            Categorias = new CategoriaCompeticaoRepositorioStub();
            Competicoes = new CompeticaoRepositorioStub();
            Formatos = new FormatoCampeonatoRepositorioStub();
            Inscricoes = new InscricaoCampeonatoRepositorioStub();
            Partidas = new PartidaRepositorioStub();
            Servico = new CategoriaCompeticaoServico(
                Categorias,
                Competicoes,
                Formatos,
                Inscricoes,
                Partidas,
                new UnidadeTrabalhoStub(),
                new AutorizacaoUsuarioServicoStub(Usuario));
        }

        public Usuario? Usuario { get; }
        public CategoriaCompeticaoRepositorioStub Categorias { get; }
        public CompeticaoRepositorioStub Competicoes { get; }
        public FormatoCampeonatoRepositorioStub Formatos { get; }
        public InscricaoCampeonatoRepositorioStub Inscricoes { get; }
        public PartidaRepositorioStub Partidas { get; }
        public CategoriaCompeticaoServico Servico { get; }

        public Competicao AdicionarCompeticao(TipoCompeticao tipo)
        {
            var competicao = new Competicao
            {
                Nome = $"Competição {tipo}",
                Tipo = tipo,
                DataInicio = DateTime.UtcNow
            };
            Competicoes.Itens.Add(competicao);
            return competicao;
        }

        public FormatoCampeonato AdicionarFormato(TipoFormatoCampeonato tipo, bool ativo = true)
        {
            var formato = new FormatoCampeonato
            {
                Nome = $"Formato {Guid.NewGuid():N}",
                TipoFormato = tipo,
                Ativo = ativo
            };
            Formatos.Itens.Add(formato);
            return formato;
        }

        public CategoriaCompeticao AdicionarCategoria(
            Competicao competicao,
            string nome,
            GeneroCategoria genero,
            NivelCategoria nivel)
        {
            var categoria = new CategoriaCompeticao
            {
                CompeticaoId = competicao.Id,
                Competicao = competicao,
                Nome = nome,
                Genero = genero,
                Nivel = nivel,
                PesoRanking = 1m,
                StatusInscricao = StatusInscricoesCategoriaCampeonato.Aberta
            };
            competicao.Categorias.Add(categoria);
            Categorias.Itens.Add(categoria);
            return categoria;
        }
    }

    private sealed class CategoriaCompeticaoRepositorioStub : ICategoriaCompeticaoRepositorio
    {
        public List<CategoriaCompeticao> Itens { get; } = [];
        public Task<IReadOnlyList<CategoriaCompeticao>> ListarPorCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<CategoriaCompeticao>>(Itens.Where(x => x.CompeticaoId == competicaoId).ToList());
        public Task<IReadOnlyList<CategoriaCompeticao>> ListarDisponiveisParaVinculoAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<CategoriaCompeticao>>(Itens);
        public Task<CategoriaCompeticao?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Itens.FirstOrDefault(x => x.Id == id));
        public Task AdicionarAsync(CategoriaCompeticao categoria, CancellationToken cancellationToken = default)
        {
            Itens.Add(categoria);
            return Task.CompletedTask;
        }
        public void Atualizar(CategoriaCompeticao categoria) { }
        public void Remover(CategoriaCompeticao categoria) => Itens.Remove(categoria);
    }

    private sealed class CompeticaoRepositorioStub : ICompeticaoRepositorio
    {
        public List<Competicao> Itens { get; } = [];
        public Task<IReadOnlyList<Competicao>> ListarAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Competicao>>(Itens);
        public Task<Competicao?> ObterGrupoResumoUsuarioAsync(Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult(Itens.FirstOrDefault());
        public Task<IReadOnlyList<Guid>> ListarIdsComAcessoAtletaAsync(Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Guid>>(Itens.Select(x => x.Id).ToList());
        public Task<bool> AtletaPossuiAcessoAsync(Guid competicaoId, Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<Competicao?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default) => Task.FromResult(Itens.FirstOrDefault(x => x.Nome == nome));
        public Task<Competicao?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Itens.FirstOrDefault(x => x.Id == id));
        public Task<Competicao?> ObterPorIdComCategoriasAsync(Guid id, CancellationToken cancellationToken = default) => ObterPorIdAsync(id, cancellationToken);
        public Task AdicionarAsync(Competicao competicao, CancellationToken cancellationToken = default)
        {
            Itens.Add(competicao);
            return Task.CompletedTask;
        }
        public void Atualizar(Competicao competicao) { }
        public void Remover(Competicao competicao) => Itens.Remove(competicao);
    }

    private sealed class FormatoCampeonatoRepositorioStub : IFormatoCampeonatoRepositorio
    {
        public List<FormatoCampeonato> Itens { get; } = [];
        public Task<IReadOnlyList<FormatoCampeonato>> ListarAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<FormatoCampeonato>>(Itens);
        public Task<FormatoCampeonato?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Itens.FirstOrDefault(x => x.Id == id));
        public Task<FormatoCampeonato?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default) => Task.FromResult(Itens.FirstOrDefault(x => x.Nome == nome));
        public Task AdicionarAsync(FormatoCampeonato formato, CancellationToken cancellationToken = default)
        {
            Itens.Add(formato);
            return Task.CompletedTask;
        }
        public void Atualizar(FormatoCampeonato formato) { }
        public void Remover(FormatoCampeonato formato) => Itens.Remove(formato);
    }

    private sealed class InscricaoCampeonatoRepositorioStub : IInscricaoCampeonatoRepositorio
    {
        public Dictionary<Guid, int> QuantidadePorCategoria { get; } = [];
        public Task<IReadOnlyList<InscricaoCampeonato>> ListarPorCampeonatoAsync(Guid campeonatoId, Guid? categoriaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<InscricaoCampeonato>>([]);
        public Task<int> ContarPorCategoriaAsync(Guid categoriaId, Guid? ignorarInscricaoId = null, CancellationToken cancellationToken = default) => Task.FromResult(QuantidadePorCategoria.GetValueOrDefault(categoriaId));
        public Task<InscricaoCampeonato?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<InscricaoCampeonato?>(null);
        public Task<InscricaoCampeonato?> ObterDuplicadaAsync(Guid categoriaId, Guid duplaId, CancellationToken cancellationToken = default) => Task.FromResult<InscricaoCampeonato?>(null);
        public Task<IReadOnlyList<InscricaoCampeonato>> ListarPorDuplasParaAtualizacaoAsync(IReadOnlyCollection<Guid> duplaIds, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<InscricaoCampeonato>>([]);
        public Task AdicionarAsync(InscricaoCampeonato inscricao, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Atualizar(InscricaoCampeonato inscricao) { }
        public void Remover(InscricaoCampeonato inscricao) { }
    }

    private sealed class PartidaRepositorioStub : IPartidaRepositorio
    {
        public List<Partida> Partidas { get; } = [];
        public Task<IReadOnlyList<Partida>> ListarPorCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<int> ContarRegistradasAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<IReadOnlyList<Partida>> ListarPorGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarPorCategoriaAsync(Guid categoriaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>(Partidas.Where(x => x.CategoriaCompeticaoId == categoriaId).ToList());
        public Task<IReadOnlyList<Partida>> ListarPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarPorUsuarioCriadorAsync(Guid usuarioId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarAdministracaoAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarFeedAsync(int skip, int take, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarPorDiaAsync(DateTime inicioUtc, DateTime fimUtc, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
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
        public Task<AtletasSugestoesPartidaDto> ObterSugestoesPartidaAsync(Guid atletaId, Guid? grupoId, int limitePorSecao, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<UsuarioResumoDto> ObterResumoUsuarioPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Partida?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Partida?>(null);
        public Task AdicionarAsync(Partida partida, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Atualizar(Partida partida) { }
        public void Remover(Partida partida) { }
    }

    private sealed class UnidadeTrabalhoStub : IUnidadeTrabalho
    {
        public Task<int> SalvarAlteracoesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
        public Task ExecutarEmTransacaoAsync(Func<CancellationToken, Task> operacao, CancellationToken cancellationToken = default) => operacao(cancellationToken);
    }

    private sealed class AutorizacaoUsuarioServicoStub(Usuario? usuario) : IAutorizacaoUsuarioServico
    {
        public bool EhAdministrador(Usuario? usuario) => usuario?.Perfil == PerfilUsuario.Administrador;
        public Task<Usuario?> ObterUsuarioAtualAsync(CancellationToken cancellationToken = default) => Task.FromResult(usuario);
        public Task<Usuario> ObterUsuarioAtualObrigatorioAsync(CancellationToken cancellationToken = default) => Task.FromResult(usuario ?? throw new RegraNegocioException("Usuário não autenticado."));
        public Task GarantirAdministradorAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task GarantirAdminOuOrganizadorAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task GarantirAcessoAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task GarantirGestaoCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task GarantirGestaoGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
