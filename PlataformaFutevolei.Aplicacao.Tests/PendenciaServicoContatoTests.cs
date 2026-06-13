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

public class PendenciaServicoContatoTests
{
    [Fact]
    public async Task AprovarPartidaAsync_AprovacaoDaDuplaValidanteEncerraPendenciasCorrelatasEAprovaPartida()
    {
        var cenario = CenarioAprovacao.Criar();

        await cenario.Servico.AprovarPartidaAsync(
            cenario.PendenciaValidante1.Id,
            new ResponderPendenciaPartidaDto("resultado confere"));

        Assert.Equal(StatusAprovacaoPartida.Aprovada, cenario.Partida.StatusAprovacao);
        Assert.Equal(StatusPendenciaUsuario.Concluida, cenario.PendenciaValidante1.Status);
        Assert.Equal(StatusPendenciaUsuario.Cancelada, cenario.PendenciaValidante2.Status);
        Assert.Equal(StatusPartidaAprovacao.Aprovada, cenario.AprovacaoValidante1.Status);
        Assert.Equal(StatusPartidaAprovacao.Pendente, cenario.AprovacaoValidante2.Status);
    }

    [Fact]
    public async Task AprovarPartidaAsync_NaoExigeAprovacaoDeTodosQuandoDuplaValidanteJaRespondeu()
    {
        var cenario = CenarioAprovacao.Criar();

        await cenario.Servico.AprovarPartidaAsync(
            cenario.PendenciaValidante1.Id,
            new ResponderPendenciaPartidaDto(null));

        Assert.Equal(StatusAprovacaoPartida.Aprovada, cenario.Partida.StatusAprovacao);
        Assert.Single(cenario.Aprovacoes.Itens.Where(x => x.Status == StatusPartidaAprovacao.Aprovada));
        Assert.Contains(cenario.Aprovacoes.Itens, x =>
            x.AtletaId == cenario.AtletaValidante2.Id &&
            x.Status == StatusPartidaAprovacao.Pendente);
    }

    [Fact]
    public async Task ContestarPartidaAsync_ContestacaoEncerraPendenciasCorrelatasEContestaPartida()
    {
        var cenario = CenarioAprovacao.Criar();

        await cenario.Servico.ContestarPartidaAsync(
            cenario.PendenciaValidante1.Id,
            new ResponderPendenciaPartidaDto("placar incorreto"));

        Assert.Equal(StatusAprovacaoPartida.Contestada, cenario.Partida.StatusAprovacao);
        Assert.Equal(StatusPendenciaUsuario.Concluida, cenario.PendenciaValidante1.Status);
        Assert.Equal(StatusPendenciaUsuario.Cancelada, cenario.PendenciaValidante2.Status);
        Assert.Equal(StatusPartidaAprovacao.Contestada, cenario.AprovacaoValidante1.Status);
        Assert.Equal(StatusPartidaAprovacao.Pendente, cenario.AprovacaoValidante2.Status);
    }

    [Fact]
    public async Task AprovarPartidaAsync_AtletaForaDaDuplaValidanteNaoPodeResponder()
    {
        var cenario = CenarioAprovacao.CriarComUsuarioForaDaDuplaValidante();

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.AprovarPartidaAsync(
                cenario.PendenciaAtletaNaoAutorizado!.Id,
                new ResponderPendenciaPartidaDto(null)));

        Assert.Equal("Apenas atletas da Dupla 2 podem validar esta partida.", excecao.Message);
        Assert.Equal(StatusAprovacaoPartida.PendenteAprovacao, cenario.Partida.StatusAprovacao);
        Assert.Equal(StatusPendenciaUsuario.Pendente, cenario.PendenciaAtletaNaoAutorizado!.Status);
    }

    [Fact]
    public async Task ContestarPartidaAsync_AtletaForaDaDuplaValidanteNaoPodeResponder()
    {
        var cenario = CenarioAprovacao.CriarComUsuarioForaDaDuplaValidante();

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.ContestarPartidaAsync(
                cenario.PendenciaAtletaNaoAutorizado!.Id,
                new ResponderPendenciaPartidaDto(null)));

        Assert.Equal("Apenas atletas da Dupla 2 podem validar esta partida.", excecao.Message);
        Assert.Equal(StatusAprovacaoPartida.PendenteAprovacao, cenario.Partida.StatusAprovacao);
        Assert.Equal(StatusPendenciaUsuario.Pendente, cenario.PendenciaAtletaNaoAutorizado!.Status);
    }

    [Fact]
    public async Task CompletarContatoAsync_VinculaPartidaAoAtletaExistentePorEmail()
    {
        var cenario = Cenario.Criar();
        var atletaExistente = cenario.CriarAtleta("Atleta Existente", "existente@teste.com", possuiUsuario: true);
        cenario.AdicionarAoGrupo(atletaExistente);

        var resultado = await cenario.Servico.CompletarContatoAsync(
            cenario.Pendencia.Id,
            new AtualizarContatoPendenciaDto(" EXISTENTE@TESTE.COM "));

        Assert.False(resultado.UsuarioJaCadastrado);
        Assert.Equal(StatusPendenciaUsuario.Concluida, cenario.Pendencia.Status);
        Assert.Contains(atletaExistente.Id, ObterAtletasPartida(cenario.Partida));
        Assert.DoesNotContain(cenario.AtletaPendente.Id, ObterAtletasPartida(cenario.Partida));
        Assert.Equal(cenario.Partida.DuplaAId, cenario.Partida.DuplaVencedoraId);
        Assert.Single(cenario.Partidas.ListarPorAtleta(atletaExistente.Id));
        Assert.Equal(StatusAprovacaoPartida.PendenteAprovacao, cenario.Partida.StatusAprovacao);
    }

    [Fact]
    public async Task CompletarContatoAsync_AtletaIdValidoDoGrupo_VinculaPartidaAoAtleta()
    {
        var cenario = Cenario.Criar();
        var atletaExistente = cenario.CriarAtleta("Atleta Existente", "existente@teste.com", possuiUsuario: true);
        cenario.AdicionarAoGrupo(atletaExistente);

        await cenario.Servico.CompletarContatoAsync(
            cenario.Pendencia.Id,
            new AtualizarContatoPendenciaDto(null, atletaExistente.Id));

        Assert.Equal(StatusPendenciaUsuario.Concluida, cenario.Pendencia.Status);
        Assert.Contains(atletaExistente.Id, ObterAtletasPartida(cenario.Partida));
        Assert.DoesNotContain(cenario.AtletaPendente.Id, ObterAtletasPartida(cenario.Partida));
        Assert.Single(cenario.Partidas.ListarPorAtleta(atletaExistente.Id));
    }

    [Fact]
    public async Task CompletarContatoAsync_AtletaIdForaDoGrupo_Rejeita()
    {
        var cenario = Cenario.Criar();
        var atletaForaDoGrupo = cenario.CriarAtleta("Atleta Fora", "fora@teste.com", possuiUsuario: true);

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.CompletarContatoAsync(
                cenario.Pendencia.Id,
                new AtualizarContatoPendenciaDto(null, atletaForaDoGrupo.Id)));

        Assert.Equal("Selecione um atleta cadastrado e ativo no grupo desta partida.", excecao.Message);
        Assert.Equal(StatusPendenciaUsuario.Pendente, cenario.Pendencia.Status);
        Assert.Contains(cenario.AtletaPendente.Id, ObterAtletasPartida(cenario.Partida));
    }

    [Fact]
    public async Task CompletarContatoAsync_AtletaIdSemCadastroAtivo_Rejeita()
    {
        var cenario = Cenario.Criar();
        var atletaSemUsuario = cenario.CriarAtleta("Atleta Sem Usuario", "semusuario@teste.com", possuiUsuario: false);
        cenario.AdicionarAoGrupo(atletaSemUsuario);

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.CompletarContatoAsync(
                cenario.Pendencia.Id,
                new AtualizarContatoPendenciaDto(null, atletaSemUsuario.Id)));

        Assert.Equal("Selecione um atleta cadastrado e ativo no grupo desta partida.", excecao.Message);
        Assert.Equal(StatusPendenciaUsuario.Pendente, cenario.Pendencia.Status);
    }

    [Fact]
    public async Task CompletarContatoAsync_SemAtletaIdESemEmail_Rejeita()
    {
        var cenario = Cenario.Criar();

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.CompletarContatoAsync(
                cenario.Pendencia.Id,
                new AtualizarContatoPendenciaDto(null, null)));

        Assert.Equal("Informe atletaId ou e-mail, mas não os dois.", excecao.Message);
        Assert.Equal(StatusPendenciaUsuario.Pendente, cenario.Pendencia.Status);
    }

    [Fact]
    public async Task CompletarContatoAsync_ComAtletaIdEEmail_Rejeita()
    {
        var cenario = Cenario.Criar();
        var atletaExistente = cenario.CriarAtleta("Atleta Existente", "existente@teste.com", possuiUsuario: true);
        cenario.AdicionarAoGrupo(atletaExistente);

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.CompletarContatoAsync(
                cenario.Pendencia.Id,
                new AtualizarContatoPendenciaDto("existente@teste.com", atletaExistente.Id)));

        Assert.Equal("Informe atletaId ou e-mail, mas não os dois.", excecao.Message);
        Assert.Equal(StatusPendenciaUsuario.Pendente, cenario.Pendencia.Status);
    }

    [Fact]
    public async Task CompletarContatoAsync_EmailInexistente_DeixaAguardandoCadastroComConvite()
    {
        var cenario = Cenario.Criar();

        await cenario.Servico.CompletarContatoAsync(
            cenario.Pendencia.Id,
            new AtualizarContatoPendenciaDto(" novo@teste.com "));

        Assert.Equal("novo@teste.com", cenario.AtletaPendente.Email);
        Assert.Equal(StatusPendenciaUsuario.AguardandoCadastro, cenario.Pendencia.Status);
        Assert.Single(cenario.Convites.Criados);
        Assert.Equal(cenario.AtletaPendente.Id, cenario.Convites.Criados[0].AtletaId);
        Assert.Contains(cenario.AtletaPendente.Id, ObterAtletasPartida(cenario.Partida));
        Assert.Single(cenario.Partidas.ListarPorAtleta(cenario.AtletaPendente.Id));
        Assert.Equal("Pendente Sem Email", cenario.AtletaPendente.Nome);
    }

    [Fact]
    public async Task CompletarContatoAsync_BloqueiaQuandoEmailPertenceAAtletaDaMesmaPartida()
    {
        var cenario = Cenario.Criar();
        cenario.AtletaOponente1.Email = "oponente@teste.com";

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.CompletarContatoAsync(
                cenario.Pendencia.Id,
                new AtualizarContatoPendenciaDto("oponente@teste.com")));

        Assert.Equal(
            "Este atleta já está participando desta partida. Não é possível vincular o mesmo atleta duas vezes.",
            excecao.Message);
        Assert.Equal(StatusPendenciaUsuario.Pendente, cenario.Pendencia.Status);
        Assert.Contains(cenario.AtletaPendente.Id, ObterAtletasPartida(cenario.Partida));
    }

    private static IReadOnlyList<Guid> ObterAtletasPartida(Partida partida)
    {
        return new[]
        {
            partida.DuplaA?.Atleta1Id,
            partida.DuplaA?.Atleta2Id,
            partida.DuplaB?.Atleta1Id,
            partida.DuplaB?.Atleta2Id
        }.OfType<Guid>().ToList();
    }

    private sealed class CenarioAprovacao
    {
        private readonly List<PendenciaUsuario> pendencias = [];

        public Usuario UsuarioAtual { get; private set; } = default!;
        public Atleta AtletaCriador1 { get; private set; } = default!;
        public Atleta AtletaCriador2 { get; private set; } = default!;
        public Atleta AtletaValidante1 { get; private set; } = default!;
        public Atleta AtletaValidante2 { get; private set; } = default!;
        public Partida Partida { get; private set; } = default!;
        public PartidaAprovacao AprovacaoValidante1 { get; private set; } = default!;
        public PartidaAprovacao AprovacaoValidante2 { get; private set; } = default!;
        public PendenciaUsuario PendenciaValidante1 { get; private set; } = default!;
        public PendenciaUsuario PendenciaValidante2 { get; private set; } = default!;
        public PendenciaUsuario? PendenciaAtletaNaoAutorizado { get; private set; }
        public PartidaAprovacaoRepositorioMemoria Aprovacoes { get; } = new();
        public PendenciaServico Servico { get; private set; } = default!;

        public static CenarioAprovacao Criar()
        {
            var cenario = new CenarioAprovacao();
            cenario.Montar();
            cenario.UsuarioAtual = cenario.AtletaValidante1.Usuario!;
            cenario.CriarServico();
            return cenario;
        }

        public static CenarioAprovacao CriarComUsuarioForaDaDuplaValidante()
        {
            var cenario = new CenarioAprovacao();
            cenario.Montar();
            cenario.UsuarioAtual = cenario.AtletaCriador1.Usuario!;
            cenario.PendenciaAtletaNaoAutorizado = cenario.CriarPendenciaAprovacao(cenario.AtletaCriador1);
            cenario.Aprovacoes.Itens.Add(cenario.CriarAprovacao(cenario.AtletaCriador1));
            cenario.CriarServico();
            return cenario;
        }

        private void Montar()
        {
            AtletaCriador1 = CriarAtleta("Criador Um");
            AtletaCriador2 = CriarAtleta("Criador Dois");
            AtletaValidante1 = CriarAtleta("Validante Um");
            AtletaValidante2 = CriarAtleta("Validante Dois");
            var duplaA = CriarDupla(AtletaCriador1, AtletaCriador2);
            var duplaB = CriarDupla(AtletaValidante1, AtletaValidante2);
            Partida = new Partida
            {
                CriadoPorUsuarioId = AtletaCriador1.Usuario!.Id,
                CriadoPorUsuario = AtletaCriador1.Usuario,
                DuplaAId = duplaA.Id,
                DuplaA = duplaA,
                DuplaBId = duplaB.Id,
                DuplaB = duplaB,
                DuplaVencedoraId = duplaA.Id,
                DuplaVencedora = duplaA,
                PlacarDuplaA = 21,
                PlacarDuplaB = 18,
                Status = StatusPartida.Encerrada,
                StatusAprovacao = StatusAprovacaoPartida.PendenteAprovacao,
                DataPartida = DateTime.UtcNow.AddDays(-1)
            };

            AprovacaoValidante1 = CriarAprovacao(AtletaValidante1);
            AprovacaoValidante2 = CriarAprovacao(AtletaValidante2);
            Aprovacoes.Itens.AddRange([AprovacaoValidante1, AprovacaoValidante2]);
            PendenciaValidante1 = CriarPendenciaAprovacao(AtletaValidante1);
            PendenciaValidante2 = CriarPendenciaAprovacao(AtletaValidante2);
        }

        private void CriarServico()
        {
            var partidas = new PartidaRepositorioMemoria([Partida]);
            var usuarios = new UsuarioRepositorioMemoria(ObterAtletas().Select(x => x.Usuario!).ToList());
            var atletas = new AtletaRepositorioMemoria(ObterAtletas().ToList());
            var resolvedor = new ResolvedorAtletaDuplaMemoria([Partida.DuplaA!, Partida.DuplaB!]);

            Servico = new PendenciaServico(
                partidas,
                Aprovacoes,
                new PendenciaUsuarioRepositorioMemoria(pendencias),
                usuarios,
                atletas,
                new GrupoAtletaRepositorioStub(),
                new UnidadeTrabalhoStub(),
                new AutorizacaoUsuarioServicoStub(UsuarioAtual),
                resolvedor,
                new ConsolidacaoAtletaServicoMemoria([Partida.DuplaA!, Partida.DuplaB!]),
                new ConviteCadastroServicoStub());
        }

        private IReadOnlyList<Atleta> ObterAtletas()
            => [AtletaCriador1, AtletaCriador2, AtletaValidante1, AtletaValidante2];

        private static Atleta CriarAtleta(string nome)
        {
            var atleta = new Atleta { Nome = nome, Apelido = nome.Split(' ')[0], Email = $"{nome.Replace(" ", ".").ToLowerInvariant()}@teste.com" };
            var usuario = new Usuario
            {
                Nome = nome,
                Email = atleta.Email,
                Ativo = true,
                AtletaId = atleta.Id,
                Atleta = atleta
            };
            atleta.Usuario = usuario;
            return atleta;
        }

        private static Dupla CriarDupla(Atleta atleta1, Atleta atleta2)
            => new()
            {
                Nome = $"{atleta1.Nome} / {atleta2.Nome}",
                Atleta1Id = atleta1.Id,
                Atleta1 = atleta1,
                Atleta2Id = atleta2.Id,
                Atleta2 = atleta2
            };

        private PartidaAprovacao CriarAprovacao(Atleta atleta)
            => new()
            {
                PartidaId = Partida.Id,
                Partida = Partida,
                AtletaId = atleta.Id,
                Atleta = atleta,
                UsuarioId = atleta.Usuario!.Id,
                Usuario = atleta.Usuario,
                Status = StatusPartidaAprovacao.Pendente,
                DataSolicitacao = DateTime.UtcNow.AddMinutes(-10)
            };

        private PendenciaUsuario CriarPendenciaAprovacao(Atleta atleta)
        {
            var pendencia = new PendenciaUsuario
            {
                Tipo = TipoPendenciaUsuario.AprovarPartida,
                UsuarioId = atleta.Usuario!.Id,
                Usuario = atleta.Usuario,
                AtletaId = atleta.Id,
                Atleta = atleta,
                PartidaId = Partida.Id,
                Partida = Partida,
                Status = StatusPendenciaUsuario.Pendente
            };
            pendencias.Add(pendencia);
            return pendencia;
        }
    }

    private sealed class Cenario
    {
        public Usuario UsuarioAtual { get; } = new()
        {
            Nome = "Registrador",
            Email = "registrador@teste.com",
            Ativo = true
        };

        public Atleta AtletaPendente { get; private set; } = default!;
        public Atleta AtletaParceiro { get; private set; } = default!;
        public Atleta AtletaOponente1 { get; private set; } = default!;
        public Atleta AtletaOponente2 { get; private set; } = default!;
        public Partida Partida { get; private set; } = default!;
        public PendenciaUsuario Pendencia { get; private set; } = default!;
        public List<Atleta> Atletas { get; } = [];
        public List<Usuario> Usuarios { get; } = [];
        public List<Dupla> Duplas { get; } = [];
        public Guid GrupoId { get; } = Guid.NewGuid();
        public GrupoAtletaRepositorioStub GruposAtletas { get; } = new();
        public PartidaRepositorioMemoria Partidas { get; private set; } = default!;
        public PartidaAprovacaoRepositorioMemoria Aprovacoes { get; } = new();
        public ConviteCadastroServicoStub Convites { get; } = new();
        public PendenciaServico Servico { get; private set; } = default!;

        public static Cenario Criar()
        {
            var cenario = new Cenario();
            cenario.Usuarios.Add(cenario.UsuarioAtual);

            cenario.AtletaPendente = cenario.CriarAtleta("Pendente Sem Email", null, possuiUsuario: false);
            cenario.AtletaParceiro = cenario.CriarAtleta("Parceiro", "parceiro@teste.com", possuiUsuario: true);
            cenario.AtletaOponente1 = cenario.CriarAtleta("Oponente Um", "oponente1@teste.com", possuiUsuario: true);
            cenario.AtletaOponente2 = cenario.CriarAtleta("Oponente Dois", "oponente2@teste.com", possuiUsuario: true);
            cenario.AdicionarAoGrupo(cenario.AtletaParceiro);
            cenario.AdicionarAoGrupo(cenario.AtletaOponente1);
            cenario.AdicionarAoGrupo(cenario.AtletaOponente2);

            var duplaA = cenario.CriarDupla(cenario.AtletaPendente, cenario.AtletaParceiro);
            var duplaB = cenario.CriarDupla(cenario.AtletaOponente1, cenario.AtletaOponente2);
            cenario.Partida = new Partida
            {
                CriadoPorUsuarioId = cenario.UsuarioAtual.Id,
                DuplaAId = duplaA.Id,
                DuplaA = duplaA,
                DuplaBId = duplaB.Id,
                DuplaB = duplaB,
                DuplaVencedoraId = duplaA.Id,
                DuplaVencedora = duplaA,
                PlacarDuplaA = 21,
                PlacarDuplaB = 18,
                Status = StatusPartida.Encerrada,
                StatusAprovacao = StatusAprovacaoPartida.PendenteDeVinculos,
                GrupoId = cenario.GrupoId,
                DataPartida = DateTime.UtcNow.AddDays(-1)
            };

            cenario.Pendencia = new PendenciaUsuario
            {
                Tipo = TipoPendenciaUsuario.CompletarContatoAtletaDaPartida,
                UsuarioId = cenario.UsuarioAtual.Id,
                Usuario = cenario.UsuarioAtual,
                AtletaId = cenario.AtletaPendente.Id,
                Atleta = cenario.AtletaPendente,
                PartidaId = cenario.Partida.Id,
                Partida = cenario.Partida,
                Status = StatusPendenciaUsuario.Pendente
            };

            var pendencias = new PendenciaUsuarioRepositorioMemoria([cenario.Pendencia]);
            cenario.Partidas = new PartidaRepositorioMemoria([cenario.Partida]);
            var atletas = new AtletaRepositorioMemoria(cenario.Atletas);
            var usuarios = new UsuarioRepositorioMemoria(cenario.Usuarios);
            var resolvedor = new ResolvedorAtletaDuplaMemoria(cenario.Duplas);

            cenario.Servico = new PendenciaServico(
                cenario.Partidas,
                cenario.Aprovacoes,
                pendencias,
                usuarios,
                atletas,
                cenario.GruposAtletas,
                new UnidadeTrabalhoStub(),
                new AutorizacaoUsuarioServicoStub(cenario.UsuarioAtual),
                resolvedor,
                new ConsolidacaoAtletaServicoMemoria(cenario.Duplas),
                cenario.Convites);

            return cenario;
        }

        public void AdicionarAoGrupo(Atleta atleta)
        {
            GruposAtletas.Vinculos.Add(new GrupoAtleta
            {
                GrupoId = GrupoId,
                AtletaId = atleta.Id,
                Atleta = atleta
            });
        }

        public Atleta CriarAtleta(string nome, string? email, bool possuiUsuario)
        {
            var atleta = new Atleta { Nome = nome, Apelido = nome.Split(' ')[0], Email = email };
            Atletas.Add(atleta);

            if (possuiUsuario)
            {
                var usuario = new Usuario
                {
                    Nome = nome,
                    Email = email ?? $"{atleta.Id:N}@teste.com",
                    Ativo = true,
                    AtletaId = atleta.Id,
                    Atleta = atleta
                };
                atleta.Usuario = usuario;
                Usuarios.Add(usuario);
            }

            return atleta;
        }

        private Dupla CriarDupla(Atleta atleta1, Atleta atleta2)
        {
            var dupla = new Dupla
            {
                Nome = $"{atleta1.Nome} / {atleta2.Nome}",
                Atleta1Id = atleta1.Id,
                Atleta1 = atleta1,
                Atleta2Id = atleta2.Id,
                Atleta2 = atleta2
            };
            Duplas.Add(dupla);
            return dupla;
        }
    }

    private sealed class UsuarioRepositorioMemoria(List<Usuario> usuarios) : IUsuarioRepositorio
    {
        public Task<IReadOnlyList<Usuario>> ListarAsync(string? nome, string? email, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Usuario>>(usuarios);
        public Task<int> ContarAdministradoresAtivosAsync(CancellationToken cancellationToken = default) => Task.FromResult(usuarios.Count(x => x.Perfil == PerfilUsuario.Administrador && x.Ativo));
        public Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default) => Task.FromResult(usuarios.FirstOrDefault(x => x.Email == email && !x.DadosAnonimizados));
        public Task<Usuario?> ObterPorEmailParaAtualizacaoAsync(string email, CancellationToken cancellationToken = default) => ObterPorEmailAsync(email, cancellationToken);
        public Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(usuarios.FirstOrDefault(x => x.Id == id));
        public Task<Usuario?> ObterPorIdParaAtualizacaoAsync(Guid id, CancellationToken cancellationToken = default) => ObterPorIdAsync(id, cancellationToken);
        public Task<Usuario?> ObterPorAtletaIdAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult(usuarios.FirstOrDefault(x => x.AtletaId == atletaId));
        public Task<Usuario?> ObterPorAtletaIdParaAtualizacaoAsync(Guid atletaId, CancellationToken cancellationToken = default) => ObterPorAtletaIdAsync(atletaId, cancellationToken);
        public Task AdicionarAsync(Usuario usuario, CancellationToken cancellationToken = default) { usuarios.Add(usuario); return Task.CompletedTask; }
        public void Atualizar(Usuario usuario) { }
    }

    private sealed class AtletaRepositorioMemoria(List<Atleta> atletas) : IAtletaRepositorio
    {
        public Task<IReadOnlyList<Atleta>> ListarAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Atleta>>(atletas);
        public Task<int> ContarAsync(CancellationToken cancellationToken = default) => Task.FromResult(atletas.Count);
        public Task<IReadOnlyList<Atleta>> ListarComEmailEmPartidasSemUsuarioAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Atleta>>([]);
        public Task<IReadOnlyList<Atleta>> ListarInscritosPorOrganizadorAsync(Guid usuarioOrganizadorId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Atleta>>([]);
        public Task<bool> PertenceAoOrganizadorAsync(Guid atletaId, Guid usuarioOrganizadorId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<IReadOnlyList<Atleta>> BuscarAsync(string? termo, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Atleta>>(atletas);
        public Task<IDictionary<Guid, int>> ContarPartidasPorAtletasAsync(IEnumerable<Guid> atletaIds, CancellationToken cancellationToken = default) => Task.FromResult<IDictionary<Guid, int>>(new Dictionary<Guid, int>());
        public Task<IReadOnlyList<Atleta>> BuscarSugestoesPorCompeticaoAsync(Guid competicaoId, string termo, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Atleta>>([]);
        public Task<Atleta?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(atletas.FirstOrDefault(x => x.Id == id));
        public Task<Atleta?> ObterPorIdParaAtualizacaoAsync(Guid id, CancellationToken cancellationToken = default) => ObterPorIdAsync(id, cancellationToken);
        public Task<Atleta?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default) => Task.FromResult(atletas.FirstOrDefault(x => x.Nome == nome));
        public Task<IReadOnlyList<Atleta>> ListarPorNomeAsync(string nome, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Atleta>>(atletas.Where(x => x.Nome == nome).ToList());
        public Task<IReadOnlyList<Atleta>> ListarPorEmailAsync(string email, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Atleta>>(atletas.Where(x => string.Equals(x.Email, email, StringComparison.OrdinalIgnoreCase)).ToList());
        public Task AdicionarAsync(Atleta atleta, CancellationToken cancellationToken = default) { atletas.Add(atleta); return Task.CompletedTask; }
        public Task AdicionarMedidasAsync(AtletaMedidas medidas, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Atualizar(Atleta atleta) { }
        public void AtualizarMedidas(AtletaMedidas medidas) { }
        public void Remover(Atleta atleta) => atletas.Remove(atleta);
    }

    private sealed class PartidaRepositorioMemoria(List<Partida> partidas) : IPartidaRepositorio
    {
        public IReadOnlyList<Partida> ListarPorAtleta(Guid atletaId) => partidas.Where(x =>
            x.DuplaA?.Atleta1Id == atletaId ||
            x.DuplaA?.Atleta2Id == atletaId ||
            x.DuplaB?.Atleta1Id == atletaId ||
            x.DuplaB?.Atleta2Id == atletaId).ToList();

        public Task<IReadOnlyList<Partida>> ListarPorCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<int> ContarRegistradasAsync(CancellationToken cancellationToken = default) => Task.FromResult(partidas.Count);
        public Task<IReadOnlyList<Partida>> ListarPorGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarPorCategoriaAsync(Guid categoriaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        Task<IReadOnlyList<Partida>> IPartidaRepositorio.ListarPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken) => Task.FromResult(ListarPorAtleta(atletaId));
        public Task<IReadOnlyList<Partida>> ListarPorUsuarioCriadorAsync(Guid usuarioId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>(partidas.Where(x => x.CriadoPorUsuarioId == usuarioId).ToList());
        public Task<IReadOnlyList<Partida>> ListarAdministracaoAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>(partidas);
        public Task<IReadOnlyList<Partida>> ListarFeedAsync(int skip, int take, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarPorDiaAsync(DateTime inicioUtc, DateTime fimUtc, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarPorAtletaParaRemocaoAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult(ListarPorAtleta(atletaId));
        public Task<IReadOnlyList<Partida>> ListarReferenciandoPartidasAsync(IReadOnlyCollection<Guid> partidaIds, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<Partida?> ObterUltimaDoGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default) => Task.FromResult<Partida?>(null);
        public Task<Partida?> ObterUltimaDoAtletaNoGrupoAsync(Guid grupoId, Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult<Partida?>(null);
        public Task<IReadOnlyList<Partida>> ListarComAtletasPendentesPorUsuarioCriadorAsync(Guid usuarioId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarComPendenteDeVinculoPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>(ListarPorAtleta(atletaId).Where(x => x.StatusAprovacao == StatusAprovacaoPartida.PendenteDeVinculos).ToList());
        public Task<bool> ExisteAtletaPendenteEmPartidaCriadaPorUsuarioAsync(Guid usuarioId, Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<IReadOnlyList<Partida>> ListarParaRankingGeralAsync(Guid? usuarioOrganizadorId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>(partidas);
        public Task<IReadOnlyList<Partida>> ListarParaRankingPorLigaAsync(Guid ligaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarParaRankingSemCompeticaoOuCategoriaAsync(Guid? usuarioOrganizadorId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>(partidas);
        public Task<IReadOnlyList<Partida>> ListarParaRankingPorCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarParaRankingPorGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<Guid?> ObterUltimaCompeticaoComPartidaEncerradaAsync(Guid? usuarioOrganizadorId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult<Guid?>(null);
        public Task<AtletasSugestoesPartidaDto> ObterSugestoesPartidaAsync(Guid atletaId, Guid? grupoId, int limitePorSecao, CancellationToken cancellationToken = default) => Task.FromResult(new AtletasSugestoesPartidaDto([], []));
        public Task<UsuarioResumoDto> ObterResumoUsuarioPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult(new UsuarioResumoDto("Usuario", 0, 0, 0, 0, 0, 0, 0));
        public Task<Partida?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(partidas.FirstOrDefault(x => x.Id == id));
        public Task AdicionarAsync(Partida partida, CancellationToken cancellationToken = default) { partidas.Add(partida); return Task.CompletedTask; }
        public void Atualizar(Partida partida) { }
        public void Remover(Partida partida) => partidas.Remove(partida);
    }

    private sealed class PendenciaUsuarioRepositorioMemoria(List<PendenciaUsuario> pendencias) : IPendenciaUsuarioRepositorio
    {
        public Task<IReadOnlyList<PendenciaUsuario>> ListarPendentesPorUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<PendenciaUsuario>>(pendencias.Where(x => x.UsuarioId == usuarioId && x.Status == StatusPendenciaUsuario.Pendente).ToList());
        public Task<IReadOnlyList<PendenciaUsuario>> ListarPendentesPorPartidaAsync(Guid partidaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<PendenciaUsuario>>(pendencias.Where(x => x.PartidaId == partidaId && x.Status == StatusPendenciaUsuario.Pendente).ToList());
        public Task<IReadOnlyList<PendenciaUsuario>> ListarPendentesPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<PendenciaUsuario>>(pendencias.Where(x => x.AtletaId == atletaId && x.Status == StatusPendenciaUsuario.Pendente).ToList());
        public Task<IReadOnlyList<PendenciaUsuario>> ListarPendentesPorUsuarioParaAtualizacaoAsync(Guid usuarioId, CancellationToken cancellationToken = default) => ListarPendentesPorUsuarioAsync(usuarioId, cancellationToken);
        public Task<PendenciaUsuario?> ObterPendenteAsync(TipoPendenciaUsuario tipo, Guid usuarioId, Guid? partidaId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult(pendencias.FirstOrDefault(x => x.Tipo == tipo && x.UsuarioId == usuarioId && x.PartidaId == partidaId && x.AtletaId == atletaId && x.Status == StatusPendenciaUsuario.Pendente));
        public Task<PendenciaUsuario?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(pendencias.FirstOrDefault(x => x.Id == id));
        public Task<bool> ExistePendentePorUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken = default) => Task.FromResult(pendencias.Any(x => x.UsuarioId == usuarioId && x.Status == StatusPendenciaUsuario.Pendente));
        public Task AdicionarAsync(PendenciaUsuario pendencia, CancellationToken cancellationToken = default) { pendencias.Add(pendencia); return Task.CompletedTask; }
        public void Atualizar(PendenciaUsuario pendencia) { }
    }

    private sealed class PartidaAprovacaoRepositorioMemoria : IPartidaAprovacaoRepositorio
    {
        public List<PartidaAprovacao> Itens { get; } = [];
        public Task<IReadOnlyList<PartidaAprovacao>> ListarPorPartidaAsync(Guid partidaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<PartidaAprovacao>>(Itens.Where(x => x.PartidaId == partidaId).ToList());
        public Task<IReadOnlyList<PartidaAprovacao>> ListarPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<PartidaAprovacao>>(Itens.Where(x => x.AtletaId == atletaId).ToList());
        public Task<PartidaAprovacao?> ObterPorPartidaEAtletaAsync(Guid partidaId, Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult(Itens.FirstOrDefault(x => x.PartidaId == partidaId && x.AtletaId == atletaId));
        public Task AdicionarAsync(PartidaAprovacao partidaAprovacao, CancellationToken cancellationToken = default) { Itens.Add(partidaAprovacao); return Task.CompletedTask; }
        public void Atualizar(PartidaAprovacao partidaAprovacao) { }
        public void RemoverIntervalo(IEnumerable<PartidaAprovacao> aprovacoes) => Itens.RemoveAll(aprovacoes.Contains);
    }

    private sealed class ResolvedorAtletaDuplaMemoria(List<Dupla> duplas) : IResolvedorAtletaDuplaServico
    {
        public Task<Atleta> ObterAtletaExistenteAsync(Guid atletaId, string mensagemQuandoInvalido, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Atleta> ResolverAtletaAsync(Guid? atletaId, string? nomeInformado, string? apelidoInformado, string mensagemQuandoInvalido, bool cadastroPendente, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Atleta> ObterOuCriarAtletaAsync(string? nomeInformado, string? apelidoInformado, bool cadastroPendente, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Atleta> ObterOuCriarAtletaParaUsuarioAsync(string nomeInformado, string emailInformado, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Dupla> ObterOuCriarDuplaAsync(Atleta atleta1, Atleta atleta2, CancellationToken cancellationToken = default)
        {
            var ids = new[] { atleta1.Id, atleta2.Id }.OrderBy(x => x).ToArray();
            var dupla = duplas.FirstOrDefault(x => x.Atleta1Id == ids[0] && x.Atleta2Id == ids[1]);
            if (dupla is not null)
            {
                return Task.FromResult(dupla);
            }

            var atletaOrdenado1 = atleta1.Id == ids[0] ? atleta1 : atleta2;
            var atletaOrdenado2 = atleta1.Id == ids[1] ? atleta1 : atleta2;
            dupla = new Dupla
            {
                Nome = $"{atletaOrdenado1.Nome} / {atletaOrdenado2.Nome}",
                Atleta1Id = atletaOrdenado1.Id,
                Atleta1 = atletaOrdenado1,
                Atleta2Id = atletaOrdenado2.Id,
                Atleta2 = atletaOrdenado2
            };
            duplas.Add(dupla);
            return Task.FromResult(dupla);
        }

        public Task<GrupoAtleta> GarantirAtletaNoGrupoAsync(Guid grupoId, Atleta atleta, CancellationToken cancellationToken = default) => Task.FromResult(new GrupoAtleta { GrupoId = grupoId, AtletaId = atleta.Id, Atleta = atleta });
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

    private sealed class GrupoAtletaRepositorioStub : IGrupoAtletaRepositorio
    {
        public List<GrupoAtleta> Vinculos { get; } = [];

        public Task<IReadOnlyList<GrupoAtleta>> ListarPorGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<GrupoAtleta>>(Vinculos.Where(x => x.GrupoId == grupoId).ToList());
        public Task<IReadOnlyList<GrupoAtleta>> ListarPorGrupoParaAtualizacaoAsync(Guid grupoId, CancellationToken cancellationToken = default) => ListarPorGrupoAsync(grupoId, cancellationToken);
        public Task<IReadOnlyList<GrupoAtleta>> BuscarPorGrupoAsync(Guid grupoId, string termo, CancellationToken cancellationToken = default) => ListarPorGrupoAsync(grupoId, cancellationToken);
        public Task<IReadOnlyList<GrupoAtleta>> ListarPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<GrupoAtleta>>(Vinculos.Where(x => x.AtletaId == atletaId).ToList());
        public Task<IReadOnlyList<GrupoAtleta>> ListarPorAtletaParaAtualizacaoAsync(Guid atletaId, CancellationToken cancellationToken = default) => ListarPorAtletaAsync(atletaId, cancellationToken);
        public Task<GrupoAtleta?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<GrupoAtleta?>(null);
        public Task<GrupoAtleta?> ObterPorGrupoEAtletaAsync(Guid grupoId, Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult(Vinculos.FirstOrDefault(x => x.GrupoId == grupoId && x.AtletaId == atletaId));
        public Task AdicionarAsync(GrupoAtleta grupoAtleta, CancellationToken cancellationToken = default) { Vinculos.Add(grupoAtleta); return Task.CompletedTask; }
        public void Remover(GrupoAtleta grupoAtleta) => Vinculos.Remove(grupoAtleta);
    }

    private sealed class ConsolidacaoAtletaServicoMemoria(List<Dupla> duplas) : IConsolidacaoAtletaServico
    {
        public Task<Atleta> ConsolidarCandidatosAsync(
            IEnumerable<Atleta?> candidatos,
            Guid? atletaVinculadoConfiavelId = null,
            string? emailNormalizado = null,
            CancellationToken cancellationToken = default)
        {
            var atletas = candidatos.OfType<Atleta>().DistinctBy(x => x.Id).ToList();
            var vencedor = atletas
                .OrderByDescending(x => atletaVinculadoConfiavelId.HasValue && x.Id == atletaVinculadoConfiavelId.Value)
                .ThenByDescending(x => x.Usuario is not null)
                .ThenBy(x => x.DataCriacao)
                .First();

            if (!string.IsNullOrWhiteSpace(emailNormalizado))
            {
                vencedor.Email = emailNormalizado.Trim().ToLowerInvariant();
            }

            foreach (var perdedor in atletas.Where(x => x.Id != vencedor.Id))
            {
                foreach (var dupla in duplas)
                {
                    if (dupla.Atleta1Id == perdedor.Id)
                    {
                        dupla.Atleta1Id = vencedor.Id;
                        dupla.Atleta1 = vencedor;
                    }

                    if (dupla.Atleta2Id == perdedor.Id)
                    {
                        dupla.Atleta2Id = vencedor.Id;
                        dupla.Atleta2 = vencedor;
                    }
                }
            }

            return Task.FromResult(vencedor);
        }

        public Task<SaneamentoAtletasEmailResumoDto> ConsolidarDuplicadosPorEmailAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new SaneamentoAtletasEmailResumoDto(0, 0, 0, []));
        }
    }

    private sealed class ConviteCadastroServicoStub : IConviteCadastroServico
    {
        public List<CriarConvitePendenciaAtletaDto> Criados { get; } = [];
        public Task<IReadOnlyList<ConviteCadastroDto>> ListarAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ConviteCadastroDto>>([]);
        public Task<IReadOnlyList<AtletaElegivelConviteCadastroDto>> ListarAtletasElegiveisAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<AtletaElegivelConviteCadastroDto>>([]);
        public Task<ConviteCadastroDto> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ConviteCadastroLinkAceiteDto> ObterLinkAceiteAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ConviteCadastroPublicoDto> ObterPublicoAsync(string identificadorOuToken, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ConviteCadastroDto> CriarAsync(CriarConviteCadastroDto dto, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ConvitePendenciaAtletaResultadoDto> CriarParaPendenciaAtletaAsync(CriarConvitePendenciaAtletaDto dto, CancellationToken cancellationToken = default)
        {
            Criados.Add(dto);
            return Task.FromResult(new ConvitePendenciaAtletaResultadoDto(true, false, false, Guid.NewGuid()));
        }

        public Task<ConviteCadastroDto> EnviarEmailAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ConviteCadastroDto> EnviarWhatsappAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DesativarAsync(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
