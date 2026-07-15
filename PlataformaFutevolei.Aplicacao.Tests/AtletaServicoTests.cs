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

public class AtletaServicoTests
{
    [Fact]
    public async Task ListarPublicoAsync_ComAtletas_RetornaDtosPublicos()
    {
        var cenario = new Cenario();
        cenario.Atletas.Itens.Add(new Atleta { Nome = "Ana Silva", Apelido = "Ana", Instagram = "@ana" });
        cenario.Atletas.Itens.Add(new Atleta { Nome = "Bruno Costa", Apelido = "Bruno" });

        var resultado = await cenario.Servico.ListarPublicoAsync();

        Assert.Equal(2, resultado.Count);
        Assert.Equal("Ana Silva", resultado[0].Nome);
        Assert.Equal("@ana", resultado[0].Instagram);
        Assert.Equal("Bruno", resultado[1].Apelido);
    }

    [Fact]
    public async Task BuscarAsync_ComTermo_RetornaResumoComQuantidadeJogos()
    {
        var cenario = new Cenario();
        var atleta = new Atleta { Nome = "Ana Silva", Apelido = "Canhota", Email = "ana@qnf.test" };
        cenario.Atletas.Itens.Add(atleta);
        cenario.Atletas.QuantidadeJogos[atleta.Id] = 7;

        var resultado = await cenario.Servico.BuscarAsync("  ana  ");

        var resumo = Assert.Single(resultado);
        Assert.Equal(atleta.Id, resumo.Id);
        Assert.Equal("Ana Silva", resumo.Nome);
        Assert.Equal("Canhota", resumo.Apelido);
        Assert.Equal(7, resumo.QuantidadeJogos);
        Assert.Equal("  ana  ", cenario.Atletas.UltimoTermoBusca);
    }

    [Fact]
    public async Task BuscarAsync_SemAtletas_RetornaVazio()
    {
        var cenario = new Cenario();

        var resultado = await cenario.Servico.BuscarAsync("inexistente");

        Assert.Empty(resultado);
    }

    [Fact]
    public async Task ListarGerencialAsync_Administrador_RetornaTodosAtletas()
    {
        var cenario = new Cenario();
        cenario.Autorizacao.UsuarioAtual.Perfil = PerfilUsuario.Administrador;
        cenario.Atletas.Itens.Add(new Atleta { Nome = "Ana", Email = "ana@qnf.test" });
        cenario.Atletas.Itens.Add(new Atleta { Nome = "Bruno", Email = "bruno@qnf.test" });

        var resultado = await cenario.Servico.ListarGerencialAsync();

        Assert.Equal(2, resultado.Count);
        Assert.Equal("Ana", resultado[0].Nome);
        Assert.Equal("bruno@qnf.test", resultado[1].Email);
    }

    [Fact]
    public async Task ListarGerencialAsync_UsuarioAtleta_Bloqueia()
    {
        var cenario = new Cenario();
        cenario.Autorizacao.UsuarioAtual.Perfil = PerfilUsuario.Atleta;

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.ListarGerencialAsync());

        Assert.Equal("Apenas administradores ou organizadores podem executar esta operação.", excecao.Message);
    }

    [Fact]
    public async Task ObterMeuAsync_UsuarioSemAtleta_RetornaNull()
    {
        var cenario = new Cenario();
        cenario.Autorizacao.UsuarioAtual.AtletaId = null;

        var resultado = await cenario.Servico.ObterMeuAsync();

        Assert.Null(resultado);
    }

    [Fact]
    public async Task ObterMeuAsync_UsuarioComAtleta_RetornaDto()
    {
        var cenario = new Cenario();
        var atleta = new Atleta { Nome = "Meu Atleta", Email = "meu@qnf.test" };
        cenario.Atletas.Itens.Add(atleta);
        cenario.Autorizacao.UsuarioAtual.AtletaId = atleta.Id;

        var resultado = await cenario.Servico.ObterMeuAsync();

        Assert.NotNull(resultado);
        Assert.Equal(atleta.Id, resultado.Id);
        Assert.Equal("Meu Atleta", resultado.Nome);
    }

    [Fact]
    public async Task ObterSugestoesPartidaAsync_UsuarioSemAtleta_RetornaVazio()
    {
        var cenario = new Cenario();
        cenario.Autorizacao.UsuarioAtual.AtletaId = null;

        var resultado = await cenario.Servico.ObterSugestoesPartidaAsync(Guid.NewGuid());

        Assert.Empty(resultado.ParceirosFrequentes);
        Assert.Empty(resultado.RivaisFrequentes);
    }

    [Fact]
    public async Task ObterSugestoesPartidaAsync_UsuarioComAtleta_RepassaGrupoELimite()
    {
        var cenario = new Cenario();
        var grupoId = Guid.NewGuid();
        var atletaId = Guid.NewGuid();
        cenario.Autorizacao.UsuarioAtual.Perfil = PerfilUsuario.Atleta;
        cenario.Autorizacao.UsuarioAtual.AtletaId = atletaId;
        cenario.Partidas.Sugestoes = new AtletasSugestoesPartidaDto(
            [new AtletaSugestaoPartidaDto(Guid.NewGuid(), "Parceiro", 3, null)],
            [new AtletaSugestaoPartidaDto(Guid.NewGuid(), "Rival", 2, null)]);

        var resultado = await cenario.Servico.ObterSugestoesPartidaAsync(grupoId);

        Assert.Single(resultado.ParceirosFrequentes);
        Assert.Single(resultado.RivaisFrequentes);
        Assert.Equal(atletaId, cenario.Partidas.UltimoAtletaSugestoesId);
        Assert.Equal(grupoId, cenario.Partidas.UltimoGrupoSugestoesId);
        Assert.Equal(3, cenario.Partidas.UltimoLimiteSugestoes);
    }

    [Fact]
    public async Task BuscarSugestoesPorCompeticaoAsync_TermoCurto_RetornaVazioSemConsultarRepositorio()
    {
        var cenario = new Cenario();

        var resultado = await cenario.Servico.BuscarSugestoesPorCompeticaoAsync(Guid.NewGuid(), "ab");

        Assert.Empty(resultado);
        Assert.Null(cenario.Atletas.UltimoTermoSugestaoCompeticao);
    }

    [Fact]
    public async Task BuscarSugestoesPorCompeticaoAsync_AtletaSemAcesso_Bloqueia()
    {
        var cenario = new Cenario();
        cenario.Autorizacao.UsuarioAtual.Perfil = PerfilUsuario.Atleta;
        cenario.Autorizacao.UsuarioAtual.AtletaId = Guid.NewGuid();
        cenario.Competicoes.AtletaPossuiAcesso = false;

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.BuscarSugestoesPorCompeticaoAsync(Guid.NewGuid(), "ana"));

        Assert.Equal("Você só pode consultar atletas de competições em que participa.", excecao.Message);
    }

    [Fact]
    public async Task BuscarSugestoesPorCompeticaoAsync_TermoValido_NormalizaEMapeiaResumo()
    {
        var cenario = new Cenario();
        var atleta = new Atleta { Nome = "Ana Silva", Apelido = "Ana" };
        cenario.Atletas.SugestoesCompeticao.Add(atleta);

        var resultado = await cenario.Servico.BuscarSugestoesPorCompeticaoAsync(Guid.NewGuid(), "  ANA  ");

        var resumo = Assert.Single(resultado);
        Assert.Equal(atleta.Id, resumo.Id);
        Assert.Equal("Ana Silva", resumo.Nome);
        Assert.Equal("ANA", cenario.Atletas.UltimoTermoSugestaoCompeticao);
    }

    [Fact]
    public async Task ObterPublicoPorIdAsync_AtletaExistente_RetornaDadosBasicos()
    {
        var cenario = new Cenario();
        var atleta = new Atleta
        {
            Nome = "Carla Lima",
            Apelido = "Carla",
            Cidade = "Santos",
            Estado = "SP",
            Nivel = NivelAtleta.Intermediario,
            Lado = LadoAtleta.Esquerdo
        };
        cenario.Atletas.Itens.Add(atleta);

        var resultado = await cenario.Servico.ObterPublicoPorIdAsync(atleta.Id);

        Assert.Equal(atleta.Id, resultado.Id);
        Assert.Equal("Carla Lima", resultado.Nome);
        Assert.Equal("Carla", resultado.Apelido);
        Assert.Equal("Santos", resultado.Cidade);
        Assert.Equal(NivelAtleta.Intermediario, resultado.Nivel);
        Assert.Equal(LadoAtleta.Esquerdo, resultado.Lado);
    }

    [Fact]
    public async Task ObterPublicoPorIdAsync_AtletaInexistente_Bloqueia()
    {
        var cenario = new Cenario();

        var excecao = await Assert.ThrowsAsync<EntidadeNaoEncontradaException>(() =>
            cenario.Servico.ObterPublicoPorIdAsync(Guid.NewGuid()));

        Assert.Equal("Atleta não encontrado.", excecao.Message);
    }

    [Fact]
    public async Task CriarAsync_DadosValidos_NormalizaEmailESalva()
    {
        var cenario = new Cenario();
        cenario.Autorizacao.UsuarioAtual.Perfil = PerfilUsuario.Administrador;

        var resultado = await cenario.Servico.CriarAsync(CriarAtleta(
            nome: "  Daniel Rocha  ",
            apelido: "  Dani  ",
            email: "  DANIEL@QNF.TEST  "));

        var atleta = Assert.Single(cenario.Atletas.Itens);
        Assert.Equal(atleta.Id, resultado.Id);
        Assert.Equal("Daniel Rocha", atleta.Nome);
        Assert.Equal("Dani", atleta.Apelido);
        Assert.Equal("daniel@qnf.test", atleta.Email);
        Assert.False(atleta.CadastroPendente);
        Assert.Equal(cenario.Autorizacao.UsuarioAtual.Id, atleta.UsuarioCriadorId);
        Assert.Equal(1, cenario.Atletas.Adicionados);
        Assert.Equal(1, cenario.UnidadeTrabalho.Salvamentos);
    }

    [Fact]
    public async Task CriarAsync_NomeVazio_Bloqueia()
    {
        var cenario = new Cenario();
        cenario.Autorizacao.UsuarioAtual.Perfil = PerfilUsuario.Administrador;

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.CriarAsync(CriarAtleta(nome: "   ", email: "atleta@qnf.test")));

        Assert.Equal("Nome do atleta é obrigatório.", excecao.Message);
        Assert.Empty(cenario.Atletas.Itens);
        Assert.Equal(0, cenario.UnidadeTrabalho.Salvamentos);
    }

    [Fact]
    public async Task CriarAsync_EmailDuplicado_Bloqueia()
    {
        var cenario = new Cenario();
        cenario.Autorizacao.UsuarioAtual.Perfil = PerfilUsuario.Administrador;
        cenario.Atletas.Itens.Add(new Atleta { Nome = "Atleta Existente", Email = "existe@qnf.test" });

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.CriarAsync(CriarAtleta(email: " EXISTE@QNF.TEST ")));

        Assert.Contains("Já existe um atleta cadastrado com este e-mail.", excecao.Message);
        Assert.Single(cenario.Atletas.Itens);
        Assert.Equal(0, cenario.UnidadeTrabalho.Salvamentos);
    }

    [Fact]
    public async Task CriarAsync_CadastroPendenteSemIdentificador_PermiteSalvarPendente()
    {
        var cenario = new Cenario();
        cenario.Autorizacao.UsuarioAtual.Perfil = PerfilUsuario.Administrador;

        var resultado = await cenario.Servico.CriarAsync(CriarAtleta(
            nome: "Atleta Pendente",
            email: null,
            cadastroPendente: true));

        var atleta = Assert.Single(cenario.Atletas.Itens);
        Assert.Equal(resultado.Id, atleta.Id);
        Assert.True(atleta.CadastroPendente);
        Assert.Null(atleta.Email);
        Assert.Equal(1, cenario.UnidadeTrabalho.Salvamentos);
    }

    [Fact]
    public async Task AtualizarAsync_ProprioAtleta_AtualizaDadosESalva()
    {
        var atleta = new Atleta { Nome = "Nome Antigo", Email = "antigo@qnf.test" };
        var cenario = new Cenario();
        cenario.Atletas.Itens.Add(atleta);
        cenario.Autorizacao.UsuarioAtual.Perfil = PerfilUsuario.Atleta;
        cenario.Autorizacao.UsuarioAtual.AtletaId = atleta.Id;
        cenario.Autorizacao.UsuarioAtual.Email = "usuario@qnf.test";

        var resultado = await cenario.Servico.AtualizarAsync(atleta.Id, AtualizarAtleta(
            nome: "Nome Ignorado Para Usuario Atleta",
            apelido: "  Novo  ",
            email: "outro@qnf.test"));

        Assert.Equal(atleta.Id, resultado.Id);
        Assert.Equal(cenario.Autorizacao.UsuarioAtual.Nome, atleta.Nome);
        Assert.Equal("Novo", atleta.Apelido);
        Assert.Equal("usuario@qnf.test", atleta.Email);
        Assert.Equal(1, cenario.Atletas.Atualizados);
        Assert.Equal(1, cenario.UnidadeTrabalho.Salvamentos);
    }

    [Fact]
    public async Task AtualizarAsync_UsuarioSemPermissao_Bloqueia()
    {
        var cenario = new Cenario();
        cenario.Autorizacao.UsuarioAtual.Perfil = PerfilUsuario.Atleta;
        cenario.Autorizacao.UsuarioAtual.AtletaId = Guid.NewGuid();
        var atleta = new Atleta { Nome = "Terceiro", Email = "terceiro@qnf.test", UsuarioCriadorId = Guid.NewGuid() };
        cenario.Atletas.Itens.Add(atleta);

        var excecao = await Assert.ThrowsAsync<AcessoNegadoException>(() =>
            cenario.Servico.AtualizarAsync(atleta.Id, AtualizarAtleta()));

        Assert.Equal("Você só pode editar o próprio atleta ou atletas cadastrados por você.", excecao.Message);
        Assert.Equal(0, cenario.Atletas.Atualizados);
        Assert.Equal(0, cenario.UnidadeTrabalho.Salvamentos);
    }

    [Fact]
    public async Task AtualizarAsync_EmailDuplicadoDeOutroAtleta_Bloqueia()
    {
        var cenario = new Cenario();
        cenario.Autorizacao.UsuarioAtual.Perfil = PerfilUsuario.Administrador;
        var atleta = new Atleta { Nome = "Atual", Email = "atual@qnf.test" };
        cenario.Atletas.Itens.Add(atleta);
        cenario.Atletas.Itens.Add(new Atleta { Nome = "Outro", Email = "duplicado@qnf.test" });

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.AtualizarAsync(atleta.Id, AtualizarAtleta(email: " DUPLICADO@QNF.TEST ")));

        Assert.Contains("Já existe um atleta cadastrado com este e-mail.", excecao.Message);
        Assert.Equal("atual@qnf.test", atleta.Email);
        Assert.Equal(0, cenario.Atletas.Atualizados);
        Assert.Equal(0, cenario.UnidadeTrabalho.Salvamentos);
    }

    [Fact]
    public async Task VerificarEmailAsync_EmailVazio_RetornaDisponivel()
    {
        var cenario = new Cenario();

        var resultado = await cenario.Servico.VerificarEmailAsync("   ");

        Assert.Equal(string.Empty, resultado.Email);
        Assert.True(resultado.Disponivel);
        Assert.Null(resultado.AtletaId);
    }

    [Fact]
    public async Task VerificarEmailAsync_EmailExistente_RetornaIndisponivel()
    {
        var cenario = new Cenario();
        var atleta = new Atleta { Nome = "Email Existente", Apelido = "Existente", Email = "existe@qnf.test" };
        cenario.Atletas.Itens.Add(atleta);

        var resultado = await cenario.Servico.VerificarEmailAsync(" EXISTE@QNF.TEST ");

        Assert.Equal("existe@qnf.test", resultado.Email);
        Assert.False(resultado.Disponivel);
        Assert.Equal(atleta.Id, resultado.AtletaId);
        Assert.Equal("Email Existente", resultado.Nome);
        Assert.Contains("Já existe um atleta cadastrado com este e-mail.", resultado.Mensagem);
    }

    [Fact]
    public async Task AtualizarMinhasMedidasAsync_SexoFeminino_NormalizaELimpaCamposMasculinos()
    {
        var atleta = new Atleta { Nome = "Ana Medidas", Email = "ana@qnf.test", Sexo = SexoAtleta.Feminino };
        var cenario = new Cenario();
        cenario.Atletas.Itens.Add(atleta);
        cenario.Autorizacao.UsuarioAtual.AtletaId = atleta.Id;

        var resultado = await cenario.Servico.AtualizarMinhasMedidasAsync(new AtualizarAtletaMedidasDto(
            " m ",
            " p ",
            " 38 ",
            " g ",
            " pp ",
            " gg "));

        Assert.Equal(atleta.Id, resultado.AtletaId);
        Assert.Equal("M", resultado.Camiseta);
        Assert.Equal("P", resultado.Regata);
        Assert.Equal("38", resultado.Short);
        Assert.Null(resultado.Sunga);
        Assert.Equal("PP", resultado.Top);
        Assert.Equal("GG", resultado.Biquini);
        Assert.NotNull(atleta.Medidas);
        Assert.Equal(1, cenario.Atletas.MedidasAdicionadas);
        Assert.Equal(1, cenario.Atletas.MedidasAtualizadas);
        Assert.Equal(1, cenario.Atletas.Atualizados);
        Assert.Equal(1, cenario.UnidadeTrabalho.Salvamentos);
    }

    [Fact]
    public async Task AtualizarMinhasMedidasAsync_UsuarioSemAtleta_Bloqueia()
    {
        var cenario = new Cenario();
        cenario.Autorizacao.UsuarioAtual.AtletaId = null;

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.AtualizarMinhasMedidasAsync(new AtualizarAtletaMedidasDto(null, null, null, null, null, null)));

        Assert.Equal("Crie ou complete o seu atleta antes de informar medidas.", excecao.Message);
        Assert.Equal(0, cenario.UnidadeTrabalho.Salvamentos);
    }

    private static CriarAtletaDto CriarAtleta(
        string nome = "Atleta Teste",
        string? apelido = null,
        string? email = "atleta@qnf.test",
        bool cadastroPendente = false)
        => new(
            nome,
            apelido,
            null,
            email,
            null,
            null,
            null,
            "Santos",
            "SP",
            cadastroPendente,
            NivelAtleta.Iniciante,
            LadoAtleta.Ambos,
            null);

    private static AtualizarAtletaDto AtualizarAtleta(
        string nome = "Atleta Atualizado",
        string? apelido = null,
        string? email = "atualizado@qnf.test",
        bool cadastroPendente = false)
        => new(
            nome,
            apelido,
            null,
            email,
            null,
            null,
            null,
            "Santos",
            "SP",
            cadastroPendente,
            NivelAtleta.Intermediario,
            LadoAtleta.Direito,
            null);

    private sealed class Cenario
    {
        public AtletaRepositorioMemoria Atletas { get; } = new();
        public PartidaRepositorioStub Partidas { get; } = new();
        public CompeticaoRepositorioStub Competicoes { get; } = new();
        public UsuarioRepositorioMemoria Usuarios { get; } = new();
        public UnidadeTrabalhoStub UnidadeTrabalho { get; } = new();
        public AutorizacaoUsuarioServicoStub Autorizacao { get; } = new();

        public Cenario()
        {
            Servico = new AtletaServico(
                Atletas,
                Partidas,
                new PartidaAprovacaoRepositorioStub(),
                new DuplaRepositorioStub(),
                new InscricaoCampeonatoRepositorioStub(),
                new GrupoRepositorioStub(),
                new GrupoAtletaRepositorioStub(),
                Competicoes,
                new ArenaRepositorioStub(),
                Usuarios,
                UnidadeTrabalho,
                Autorizacao,
                new ResolvedorAtletaDuplaServicoStub(Atletas),
                new ConsolidacaoAtletaServicoStub(),
                new PendenciaServicoStub(),
                new ConviteCadastroServicoStub());
        }

        public AtletaServico Servico { get; }
    }

    private sealed class AtletaRepositorioMemoria : IAtletaRepositorio
    {
        public List<Atleta> Itens { get; } = [];
        public List<Atleta> SugestoesCompeticao { get; } = [];
        public Dictionary<Guid, int> QuantidadeJogos { get; } = [];
        public int Adicionados { get; private set; }
        public int Atualizados { get; private set; }
        public int MedidasAdicionadas { get; private set; }
        public int MedidasAtualizadas { get; private set; }
        public string? UltimoTermoBusca { get; private set; }
        public string? UltimoTermoSugestaoCompeticao { get; private set; }

        public Task<IReadOnlyList<Atleta>> ListarAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Atleta>>(Itens.ToList());

        public Task<int> ContarAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Itens.Count);

        public Task<IReadOnlyList<Atleta>> ListarComEmailEmPartidasSemUsuarioAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Atleta>>([]);

        public Task<IReadOnlyList<Atleta>> ListarInscritosPorOrganizadorAsync(Guid usuarioOrganizadorId, CancellationToken cancellationToken = default)
            => ListarAsync(cancellationToken);

        public Task<bool> PertenceAoOrganizadorAsync(Guid atletaId, Guid usuarioOrganizadorId, CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        public Task<IReadOnlyList<Atleta>> BuscarAsync(string? termo, CancellationToken cancellationToken = default)
        {
            UltimoTermoBusca = termo;
            var termoNormalizado = (termo ?? string.Empty).Trim().ToLowerInvariant();
            var resultado = string.IsNullOrWhiteSpace(termoNormalizado)
                ? Itens
                : Itens.Where(x =>
                    x.Nome.Contains(termoNormalizado, StringComparison.OrdinalIgnoreCase) ||
                    (x.Apelido?.Contains(termoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.Email?.Contains(termoNormalizado, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();

            return Task.FromResult<IReadOnlyList<Atleta>>(resultado);
        }

        public Task<IDictionary<Guid, int>> ContarPartidasPorAtletasAsync(IEnumerable<Guid> atletaIds, CancellationToken cancellationToken = default)
            => Task.FromResult<IDictionary<Guid, int>>(QuantidadeJogos
                .Where(x => atletaIds.Contains(x.Key))
                .ToDictionary(x => x.Key, x => x.Value));

        public Task<IReadOnlyList<Atleta>> BuscarSugestoesPorCompeticaoAsync(Guid competicaoId, string termo, CancellationToken cancellationToken = default)
        {
            UltimoTermoSugestaoCompeticao = termo;
            return Task.FromResult<IReadOnlyList<Atleta>>(SugestoesCompeticao);
        }

        public Task<Atleta?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Itens.FirstOrDefault(x => x.Id == id));

        public Task<Atleta?> ObterPorIdParaAtualizacaoAsync(Guid id, CancellationToken cancellationToken = default)
            => ObterPorIdAsync(id, cancellationToken);

        public Task<Atleta?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default)
            => Task.FromResult(Itens.FirstOrDefault(x => string.Equals(x.Nome, nome, StringComparison.OrdinalIgnoreCase)));

        public Task<IReadOnlyList<Atleta>> ListarPorNomeAsync(string nome, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Atleta>>(Itens
                .Where(x => string.Equals(x.Nome, nome, StringComparison.OrdinalIgnoreCase))
                .ToList());

        public Task<IReadOnlyList<Atleta>> ListarPorEmailAsync(string email, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Atleta>>(Itens
                .Where(x => string.Equals(x.Email, email, StringComparison.OrdinalIgnoreCase))
                .ToList());

        public Task AdicionarAsync(Atleta atleta, CancellationToken cancellationToken = default)
        {
            Itens.Add(atleta);
            Adicionados++;
            return Task.CompletedTask;
        }

        public Task AdicionarMedidasAsync(AtletaMedidas medidas, CancellationToken cancellationToken = default)
        {
            var atleta = Itens.First(x => x.Id == medidas.AtletaId);
            atleta.Medidas = medidas;
            MedidasAdicionadas++;
            return Task.CompletedTask;
        }

        public void Atualizar(Atleta atleta) => Atualizados++;

        public void AtualizarMedidas(AtletaMedidas medidas) => MedidasAtualizadas++;

        public void Remover(Atleta atleta) => Itens.Remove(atleta);
    }

    private sealed class UsuarioRepositorioMemoria : IUsuarioRepositorio
    {
        public List<Usuario> Itens { get; } = [];

        public Task<IReadOnlyList<Usuario>> ListarAsync(string? nome, string? email, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Usuario>>(Itens);

        public Task<int> ContarAdministradoresAtivosAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Itens.Count(x => x.Perfil == PerfilUsuario.Administrador && x.Ativo));

        public Task<IReadOnlyList<Usuario>> ListarAdministradoresAtivosAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Usuario>>(Itens.Where(x => x.Perfil == PerfilUsuario.Administrador && x.Ativo).ToList());

        public Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default)
            => Task.FromResult(Itens.FirstOrDefault(x => string.Equals(x.Email, email, StringComparison.OrdinalIgnoreCase)));

        public Task<Usuario?> ObterPorEmailParaAtualizacaoAsync(string email, CancellationToken cancellationToken = default)
            => ObterPorEmailAsync(email, cancellationToken);

        public Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Itens.FirstOrDefault(x => x.Id == id));

        public Task<Usuario?> ObterPorIdParaAtualizacaoAsync(Guid id, CancellationToken cancellationToken = default)
            => ObterPorIdAsync(id, cancellationToken);

        public Task<Usuario?> ObterPorAtletaIdAsync(Guid atletaId, CancellationToken cancellationToken = default)
            => Task.FromResult(Itens.FirstOrDefault(x => x.AtletaId == atletaId));

        public Task<Usuario?> ObterPorAtletaIdParaAtualizacaoAsync(Guid atletaId, CancellationToken cancellationToken = default)
            => ObterPorAtletaIdAsync(atletaId, cancellationToken);

        public Task AdicionarAsync(Usuario usuario, CancellationToken cancellationToken = default)
        {
            Itens.Add(usuario);
            return Task.CompletedTask;
        }

        public void Atualizar(Usuario usuario)
        {
            if (Itens.All(x => x.Id != usuario.Id))
            {
                Itens.Add(usuario);
            }
        }
    }

    private sealed class AutorizacaoUsuarioServicoStub : IAutorizacaoUsuarioServico
    {
        public Usuario UsuarioAtual { get; } = new()
        {
            Nome = "Usuário Atual",
            Email = "usuario@qnf.test",
            Perfil = PerfilUsuario.Administrador,
            Ativo = true
        };

        public Task<Usuario?> ObterUsuarioAtualAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<Usuario?>(UsuarioAtual);

        public Task<Usuario> ObterUsuarioAtualObrigatorioAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(UsuarioAtual);

        public Task GarantirAdministradorAsync(CancellationToken cancellationToken = default)
        {
            if (UsuarioAtual.Perfil != PerfilUsuario.Administrador)
            {
                throw new AcessoNegadoException("Apenas administradores podem executar esta operação.");
            }

            return Task.CompletedTask;
        }

        public Task GarantirAdminOuOrganizadorAsync(CancellationToken cancellationToken = default)
        {
            if (UsuarioAtual.Perfil is not (PerfilUsuario.Administrador or PerfilUsuario.Organizador))
            {
                throw new RegraNegocioException("Apenas administradores ou organizadores podem executar esta operação.");
            }

            return Task.CompletedTask;
        }

        public Task GarantirAcessoAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default)
        {
            if (UsuarioAtual.AtletaId != atletaId)
            {
                throw new AcessoNegadoException("Acesso negado ao atleta.");
            }

            return Task.CompletedTask;
        }

        public Task GarantirGestaoCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task GarantirGestaoGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class UnidadeTrabalhoStub : IUnidadeTrabalho
    {
        public int Salvamentos { get; private set; }

        public Task<int> SalvarAlteracoesAsync(CancellationToken cancellationToken = default)
        {
            Salvamentos++;
            return Task.FromResult(1);
        }

        public Task ExecutarEmTransacaoAsync(Func<CancellationToken, Task> operacao, CancellationToken cancellationToken = default)
            => operacao(cancellationToken);
    }

    private sealed class ResolvedorAtletaDuplaServicoStub(AtletaRepositorioMemoria atletas) : IResolvedorAtletaDuplaServico
    {
        public Task<Atleta> ObterAtletaExistenteAsync(Guid atletaId, string mensagemQuandoInvalido, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<Atleta> ResolverAtletaAsync(Guid? atletaId, string? nomeInformado, string? apelidoInformado, string mensagemQuandoInvalido, bool cadastroPendente, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<Atleta> ObterOuCriarAtletaAsync(string? nomeInformado, string? apelidoInformado, bool cadastroPendente, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<Atleta> ObterOuCriarAtletaParaUsuarioAsync(string nomeInformado, string emailInformado, string? apelidoInformado = null, CancellationToken cancellationToken = default)
        {
            var atleta = atletas.Itens.FirstOrDefault(x => string.Equals(x.Email, emailInformado, StringComparison.OrdinalIgnoreCase));
            if (atleta is not null)
            {
                return Task.FromResult(atleta);
            }

            atleta = new Atleta { Nome = nomeInformado, Email = emailInformado };
            atletas.Itens.Add(atleta);
            return Task.FromResult(atleta);
        }

        public Task<Dupla> ObterOuCriarDuplaAsync(Atleta atleta1, Atleta atleta2, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<GrupoAtleta> GarantirAtletaNoGrupoAsync(Guid grupoId, Atleta atleta, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class ConsolidacaoAtletaServicoStub : IConsolidacaoAtletaServico
    {
        public Task<Atleta> ConsolidarCandidatosAsync(
            IEnumerable<Atleta?> candidatos,
            Guid? atletaVinculadoConfiavelId = null,
            string? emailNormalizado = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(candidatos.OfType<Atleta>().First());

        public Task<SaneamentoAtletasEmailResumoDto> ConsolidarDuplicadosPorEmailAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new SaneamentoAtletasEmailResumoDto(0, 0, 0, []));
    }

    private sealed class PendenciaServicoStub : IPendenciaServico
    {
        public Task<IReadOnlyList<PendenciaUsuarioDto>> ListarMinhasAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<PendenciaUsuarioDto>>([]);
        public Task<PendenciasResumoDto> ObterResumoAsync(CancellationToken cancellationToken = default) => Task.FromResult(new PendenciasResumoDto(0, 0, 0, 0));
        public Task<bool> ExistePendenciaAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<PendenciaUsuarioDto> AprovarPartidaAsync(Guid pendenciaId, ResponderPendenciaPartidaDto dto, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PendenciaUsuarioDto> ContestarPartidaAsync(Guid pendenciaId, ResponderPendenciaPartidaDto dto, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<AtualizarContatoPendenciaResultadoDto> CompletarContatoAsync(Guid pendenciaId, AtualizarContatoPendenciaDto dto, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PendenciaUsuarioDto> ConfirmarVinculoAtletaCadastradoAsync(Guid pendenciaId, ConfirmarVinculoAtletaPendenciaDto dto, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task InicializarFluxoPartidaAsync(Partida partida, Guid usuarioRegistradorId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SincronizarAposVinculoAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class ConviteCadastroServicoStub : IConviteCadastroServico
    {
        public Task<IReadOnlyList<ConviteCadastroDto>> ListarAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ConviteCadastroDto>>([]);
        public Task<IReadOnlyList<AtletaElegivelConviteCadastroDto>> ListarAtletasElegiveisAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<AtletaElegivelConviteCadastroDto>>([]);
        public Task<ConviteCadastroDto> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ConviteCadastroLinkAceiteDto> ObterLinkAceiteAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ConviteCadastroPublicoDto> ObterPublicoAsync(string identificadorOuToken, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ConviteCadastroDto> CriarAsync(CriarConviteCadastroDto dto, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ConvitePendenciaAtletaResultadoDto> CriarParaPendenciaAtletaAsync(CriarConvitePendenciaAtletaDto dto, CancellationToken cancellationToken = default) => Task.FromResult(new ConvitePendenciaAtletaResultadoDto(true, false, false, Guid.NewGuid()));
        public Task<ConviteCadastroDto> EnviarEmailAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ConviteCadastroDto> EnviarWhatsappAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DesativarAsync(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class ArenaRepositorioStub : IArenaRepositorio
    {
        public Task<IReadOnlyList<ArenaListagemPublicaResponse>> ListarPublicasAsync(ArenaFiltroPublicoRequest filtro, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ArenaListagemPublicaResponse>>([]);
        public Task<ArenaDetalhePublicoResponse?> ObterPublicaPorSlugAsync(string slug, CancellationToken cancellationToken = default) => Task.FromResult<ArenaDetalhePublicoResponse?>(null);
        public Task<ArenaResumoPublicoResponse?> ObterResumoPublicoAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<ArenaResumoPublicoResponse?>(null);
        public Task<IReadOnlyList<Arena>> ListarAdministradasAsync(Guid usuarioId, bool incluirTodas, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Arena>>([]);
        public Task<Arena?> ObterAdminPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Arena?>(null);
        public Task<IReadOnlyList<Arena>> ListarAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Arena>>([]);
        public Task<Arena?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Arena?>(null);
        public Task<Arena?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default) => Task.FromResult<Arena?>(null);
        public Task<bool> ExisteSlugAsync(string slug, Guid? idIgnorado, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<IReadOnlyList<ArenaEspaco>> ListarEspacosPorArenaAsync(Guid arenaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ArenaEspaco>>([]);
        public Task<ArenaEspaco?> ObterEspacoPorIdEArenaAsync(Guid arenaId, Guid espacoId, CancellationToken cancellationToken = default) => Task.FromResult<ArenaEspaco?>(null);
        public Task AdicionarAsync(Arena arena, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AdicionarEspacoAsync(ArenaEspaco espaco, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Atualizar(Arena arena) { }
        public void AtualizarEspaco(ArenaEspaco espaco) { }
        public void Remover(Arena arena) { }
    }

    private sealed class CompeticaoRepositorioStub : ICompeticaoRepositorio
    {
        public bool AtletaPossuiAcesso { get; set; } = true;

        public Task<IReadOnlyList<Competicao>> ListarAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Competicao>>([]);
        public Task<Competicao?> ObterGrupoResumoUsuarioAsync(Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult<Competicao?>(null);
        public Task<IReadOnlyList<Guid>> ListarIdsComAcessoAtletaAsync(Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Guid>>([]);
        public Task<bool> AtletaPossuiAcessoAsync(Guid competicaoId, Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult(AtletaPossuiAcesso);
        public Task<Competicao?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default) => Task.FromResult<Competicao?>(null);
        public Task<Competicao?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Competicao?>(null);
        public Task<Competicao?> ObterPorIdComCategoriasAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Competicao?>(null);
        public Task AdicionarAsync(Competicao competicao, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Atualizar(Competicao competicao) { }
        public void Remover(Competicao competicao) { }
    }

    private sealed class GrupoRepositorioStub : IGrupoRepositorio
    {
        public Task<IReadOnlyList<Grupo>> ListarAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Grupo>>([]);
        public Task<IReadOnlyList<Grupo>> ListarParaSelecaoAsync(Guid usuarioId, Guid? atletaId, bool incluirPrivadosDeTerceiros, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Grupo>>([]);
        public Task<int> ContarPublicosAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<Grupo?> ObterResumoUsuarioAsync(Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult<Grupo?>(null);
        public Task<IReadOnlyList<Grupo>> ListarResumosUsuarioAsync(Guid usuarioId, Guid? atletaId, int limite, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Grupo>>([]);
        public Task<IReadOnlyList<Grupo>> ListarDashboardUsuarioAsync(Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Grupo>>([]);
        public Task<IReadOnlyList<Guid>> ListarIdsComAcessoAtletaAsync(Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Guid>>([]);
        public Task<bool> AtletaPossuiAcessoAsync(Guid grupoId, Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<Grupo?> ObterPorNomeEOrganizadorAsync(string nome, Guid? usuarioOrganizadorId, CancellationToken cancellationToken = default) => Task.FromResult<Grupo?>(null);
        public Task<Grupo?> ObterPorNomeNormalizadoAsync(string nome, CancellationToken cancellationToken = default) => Task.FromResult<Grupo?>(null);
        public Task<IReadOnlyList<Grupo>> ListarPorUsuarioOrganizadorParaAtualizacaoAsync(Guid usuarioOrganizadorId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Grupo>>([]);
        public Task<Grupo?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Grupo?>(null);
        public Task AdicionarAsync(Grupo grupo, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Atualizar(Grupo grupo) { }
        public void Remover(Grupo grupo) { }
    }

    private sealed class GrupoAtletaRepositorioStub : IGrupoAtletaRepositorio
    {
        public Task<IReadOnlyList<GrupoAtleta>> ListarPorGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<GrupoAtleta>>([]);
        public Task<IReadOnlyList<GrupoAtleta>> ListarPorGrupoParaAtualizacaoAsync(Guid grupoId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<GrupoAtleta>>([]);
        public Task<IReadOnlyList<GrupoAtleta>> BuscarPorGrupoAsync(Guid grupoId, string termo, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<GrupoAtleta>>([]);
        public Task<IReadOnlyList<GrupoAtleta>> ListarPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<GrupoAtleta>>([]);
        public Task<IReadOnlyList<GrupoAtleta>> ListarPorAtletaParaAtualizacaoAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<GrupoAtleta>>([]);
        public Task<GrupoAtleta?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<GrupoAtleta?>(null);
        public Task<GrupoAtleta?> ObterPorGrupoEAtletaAsync(Guid grupoId, Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult<GrupoAtleta?>(null);
        public Task AdicionarAsync(GrupoAtleta grupoAtleta, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remover(GrupoAtleta grupoAtleta) { }
    }

    private sealed class PartidaRepositorioStub : IPartidaRepositorio
    {
        public AtletasSugestoesPartidaDto Sugestoes { get; set; } = new([], []);
        public Guid? UltimoAtletaSugestoesId { get; private set; }
        public Guid? UltimoGrupoSugestoesId { get; private set; }
        public int? UltimoLimiteSugestoes { get; private set; }

        public Task<IReadOnlyList<Partida>> ListarPorCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<int> ContarRegistradasAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<IReadOnlyList<Partida>> ListarPorGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarPorCategoriaAsync(Guid categoriaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
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
        public Task<AtletasSugestoesPartidaDto> ObterSugestoesPartidaAsync(Guid atletaId, Guid? grupoId, int limitePorSecao, CancellationToken cancellationToken = default)
        {
            UltimoAtletaSugestoesId = atletaId;
            UltimoGrupoSugestoesId = grupoId;
            UltimoLimiteSugestoes = limitePorSecao;
            return Task.FromResult(Sugestoes);
        }
        public Task<UsuarioResumoDto> ObterResumoUsuarioPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult(new UsuarioResumoDto("Usuario", 0, 0, 0, 0, 0, 0, 0));
        public Task<Partida?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Partida?>(null);
        public Task AdicionarAsync(Partida partida, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Atualizar(Partida partida) { }
        public void Remover(Partida partida) { }
    }

    private sealed class PartidaAprovacaoRepositorioStub : IPartidaAprovacaoRepositorio
    {
        public Task<IReadOnlyList<PartidaAprovacao>> ListarPorPartidaAsync(Guid partidaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<PartidaAprovacao>>([]);
        public Task<IReadOnlyList<PartidaAprovacao>> ListarPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<PartidaAprovacao>>([]);
        public Task<PartidaAprovacao?> ObterPorPartidaEAtletaAsync(Guid partidaId, Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult<PartidaAprovacao?>(null);
        public Task AdicionarAsync(PartidaAprovacao partidaAprovacao, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Atualizar(PartidaAprovacao partidaAprovacao) { }
        public void RemoverIntervalo(IEnumerable<PartidaAprovacao> aprovacoes) { }
    }

    private sealed class DuplaRepositorioStub : IDuplaRepositorio
    {
        public Task<IReadOnlyList<Dupla>> ListarAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Dupla>>([]);
        public Task<IReadOnlyList<Dupla>> ListarInscritasPorOrganizadorAsync(Guid usuarioOrganizadorId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Dupla>>([]);
        public Task<bool> PertenceAoOrganizadorAsync(Guid duplaId, Guid usuarioOrganizadorId, CancellationToken cancellationToken = default) => Task.FromResult(true);
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
}
