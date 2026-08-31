using Microsoft.Extensions.Logging.Abstractions;
using PlataformaFutevolei.Aplicacao.Configuracoes;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Interfaces.Seguranca;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Aplicacao.Servicos;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;
using Xunit;

namespace PlataformaFutevolei.Aplicacao.Tests;

public class PresencaGrupoServicoTests
{
    [Fact]
    public async Task ProcessarAgendaDoDiaAsync_CriaListaEEnviaLinkIndividualUmaUnicaVez()
    {
        var usuario = new Usuario
        {
            Nome = "Gustavo",
            Email = "gustavo@example.test",
            Ativo = true
        };
        var atleta = new Atleta
        {
            Nome = "Gustavo Drager",
            Apelido = "Gus",
            TelefoneNormalizado = "5548999999999",
            Usuario = usuario
        };
        usuario.Atleta = atleta;
        usuario.AtletaId = atleta.Id;
        var arena = new Arena { Nome = "Arena Long Beach" };
        var grupo = new Grupo
        {
            Nome = "Grupo de Quarta",
            DataInicio = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ArenaId = arena.Id,
            Arena = arena,
            DiasDaSemana = ["Quarta"],
            HorarioInicio = new TimeOnly(19, 0),
            HorarioFim = new TimeOnly(21, 0)
        };
        grupo.Atletas.Add(new GrupoAtleta
        {
            GrupoId = grupo.Id,
            Grupo = grupo,
            AtletaId = atleta.Id,
            Atleta = atleta
        });

        var repositorio = new PresencaGrupoRepositorioMemoria([grupo]);
        var notificacoes = new NotificacaoUsuarioRepositorioMemoria();
        var entrega = new EntregaNotificacaoExternaStub();
        var servico = CriarServico(repositorio, notificacoes, entrega);
        var agoraUtc = new DateTime(2026, 9, 2, 9, 0, 0, DateTimeKind.Utc);

        await servico.ProcessarAgendaDoDiaAsync(agoraUtc);
        await servico.ProcessarAgendaDoDiaAsync(agoraUtc.AddMinutes(15));

        var encontro = Assert.Single(repositorio.Encontros);
        Assert.Equal(new DateOnly(2026, 9, 2), encontro.DataJogo);
        Assert.Equal("Arena Long Beach", encontro.LocalSnapshot);
        var confirmacao = Assert.Single(encontro.Confirmacoes);
        Assert.Equal(48, confirmacao.CodigoAcesso.Length);
        Assert.NotNull(confirmacao.WhatsappEnviadoEmUtc);

        var notificacao = Assert.Single(notificacoes.Notificacoes);
        Assert.Equal(usuario.Id, notificacao.UsuarioId);
        Assert.StartsWith("/presenca#", notificacao.LinkAcao);

        var solicitacao = Assert.Single(entrega.Solicitacoes);
        Assert.Equal("qnf.grupo.presenca.v1", solicitacao.TemplateKey);
        Assert.Equal("5548999999999", solicitacao.Destinatario);
        Assert.Equal("Arena Long Beach", solicitacao.Dados["localJogo"]);
        Assert.StartsWith("https://app.quebranunca.test/presenca#", solicitacao.Dados["linkConfirmacao"]);
    }

    [Fact]
    public async Task ResponderAsync_ConfirmaPresencaEPermiteAlterarResposta()
    {
        var atleta = new Atleta { Nome = "João Silva" };
        var grupo = new Grupo
        {
            Nome = "Grupo de Quarta",
            DataInicio = DateTime.UtcNow.AddYears(-1),
            DiasDaSemana = ["Quarta"],
            HorarioInicio = new TimeOnly(19, 0),
            HorarioFim = new TimeOnly(21, 0),
            LocalPrincipal = "Arena Long Beach"
        };
        var encontro = new EncontroGrupo
        {
            GrupoId = grupo.Id,
            Grupo = grupo,
            DataJogo = DateOnly.FromDateTime(DateTime.UtcNow),
            HorarioInicio = new TimeOnly(19, 0),
            HorarioFim = new TimeOnly(21, 0),
            LocalSnapshot = "Arena Long Beach"
        };
        grupo.Atletas.Add(new GrupoAtleta
        {
            GrupoId = grupo.Id,
            Grupo = grupo,
            AtletaId = atleta.Id,
            Atleta = atleta
        });
        var confirmacao = new ConfirmacaoPresencaGrupo
        {
            EncontroGrupoId = encontro.Id,
            EncontroGrupo = encontro,
            AtletaId = atleta.Id,
            Atleta = atleta,
            CodigoAcesso = new string('b', 48),
            ExpiraEmUtc = DateTime.UtcNow.AddHours(2)
        };
        encontro.Confirmacoes.Add(confirmacao);
        var repositorio = new PresencaGrupoRepositorioMemoria([grupo], [encontro]);
        var servico = CriarServico(
            repositorio,
            new NotificacaoUsuarioRepositorioMemoria(),
            new EntregaNotificacaoExternaStub());

        var confirmada = await servico.ResponderAsync(confirmacao.CodigoAcesso, true);
        var recusada = await servico.ResponderAsync(confirmacao.CodigoAcesso, false);

        Assert.Equal("Confirmada", confirmada.Status);
        Assert.Equal("Não vai", recusada.Status);
        Assert.Equal(StatusConfirmacaoPresencaGrupo.NaoVai, confirmacao.Status);
        Assert.NotNull(confirmacao.RespondidaEmUtc);
    }

    [Fact]
    public async Task ProcessarAgendaDoDiaAsync_NaoNotificaAtletaQueSaiuDoGrupo()
    {
        var atleta = new Atleta
        {
            Nome = "Ex-integrante",
            TelefoneNormalizado = "5548999999999"
        };
        var grupo = new Grupo
        {
            Nome = "Grupo de Quarta",
            DataInicio = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            DiasDaSemana = ["Quarta"],
            HorarioInicio = new TimeOnly(19, 0),
            HorarioFim = new TimeOnly(21, 0),
            LocalPrincipal = "Arena Long Beach"
        };
        var encontro = new EncontroGrupo
        {
            GrupoId = grupo.Id,
            Grupo = grupo,
            DataJogo = new DateOnly(2026, 9, 2),
            HorarioInicio = new TimeOnly(19, 0),
            HorarioFim = new TimeOnly(21, 0),
            LocalSnapshot = "Arena Long Beach"
        };
        encontro.Confirmacoes.Add(new ConfirmacaoPresencaGrupo
        {
            EncontroGrupoId = encontro.Id,
            EncontroGrupo = encontro,
            AtletaId = atleta.Id,
            Atleta = atleta,
            CodigoAcesso = new string('c', 48),
            ExpiraEmUtc = new DateTime(2026, 9, 2, 21, 0, 0, DateTimeKind.Utc)
        });

        var repositorio = new PresencaGrupoRepositorioMemoria([grupo], [encontro]);
        var notificacoes = new NotificacaoUsuarioRepositorioMemoria();
        var entrega = new EntregaNotificacaoExternaStub();

        await CriarServico(repositorio, notificacoes, entrega).ProcessarAgendaDoDiaAsync(
            new DateTime(2026, 9, 2, 9, 0, 0, DateTimeKind.Utc));

        Assert.Empty(notificacoes.Notificacoes);
        Assert.Empty(entrega.Solicitacoes);
    }

    [Fact]
    public async Task ProcessarAgendaDoDiaAsync_NaoCriaListaSeAplicacaoIniciarDepoisDoJogo()
    {
        var grupo = new Grupo
        {
            Nome = "Grupo de Quarta",
            DataInicio = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            DiasDaSemana = ["Quarta"],
            HorarioInicio = new TimeOnly(19, 0),
            HorarioFim = new TimeOnly(21, 0),
            LocalPrincipal = "Arena Long Beach"
        };
        var repositorio = new PresencaGrupoRepositorioMemoria([grupo]);

        await CriarServico(
            repositorio,
            new NotificacaoUsuarioRepositorioMemoria(),
            new EntregaNotificacaoExternaStub()).ProcessarAgendaDoDiaAsync(
                new DateTime(2026, 9, 2, 22, 0, 0, DateTimeKind.Utc));

        Assert.Empty(repositorio.Encontros);
    }

    private static PresencaGrupoServico CriarServico(
        PresencaGrupoRepositorioMemoria repositorio,
        NotificacaoUsuarioRepositorioMemoria notificacoes,
        EntregaNotificacaoExternaStub entrega)
    {
        return new PresencaGrupoServico(
            repositorio,
            notificacoes,
            entrega,
            new UnidadeTrabalhoStub(),
            new AutorizacaoUsuarioServicoStub(),
            new AgendaPresencaGrupoConfiguracao
            {
                FusoHorario = "UTC",
                HoraEnvioLocal = "08:00",
                UrlApp = "https://app.quebranunca.test"
            },
            NullLogger<PresencaGrupoServico>.Instance);
    }

    private sealed class PresencaGrupoRepositorioMemoria(
        IReadOnlyList<Grupo> grupos,
        IReadOnlyList<EncontroGrupo>? encontrosIniciais = null) : IPresencaGrupoRepositorio
    {
        private readonly List<Grupo> grupos = grupos.ToList();
        public List<EncontroGrupo> Encontros { get; } = encontrosIniciais?.ToList() ?? [];

        public Task<IReadOnlyList<Grupo>> ListarGruposComAgendaAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Grupo>>(grupos);

        public Task<Grupo?> ObterGrupoComAgendaAsync(Guid grupoId, CancellationToken cancellationToken = default)
            => Task.FromResult(grupos.FirstOrDefault(x => x.Id == grupoId));

        public Task<EncontroGrupo?> ObterEncontroAsync(Guid grupoId, DateOnly dataJogo, CancellationToken cancellationToken = default)
            => Task.FromResult(Encontros.FirstOrDefault(x => x.GrupoId == grupoId && x.DataJogo == dataJogo));

        public Task<ConfirmacaoPresencaGrupo?> ObterConfirmacaoPorCodigoAsync(string codigo, CancellationToken cancellationToken = default)
            => Task.FromResult(Encontros.SelectMany(x => x.Confirmacoes).FirstOrDefault(x => x.CodigoAcesso == codigo));

        public Task AdicionarEncontroAsync(EncontroGrupo encontro, CancellationToken cancellationToken = default)
        {
            encontro.Grupo = grupos.Single(x => x.Id == encontro.GrupoId);
            encontro.Arena = encontro.Grupo.Arena;
            Encontros.Add(encontro);
            encontro.Grupo.Encontros.Add(encontro);
            return Task.CompletedTask;
        }

        public Task AdicionarConfirmacaoAsync(ConfirmacaoPresencaGrupo confirmacao, CancellationToken cancellationToken = default)
        {
            var encontro = Encontros.Single(x => x.Id == confirmacao.EncontroGrupoId);
            var atleta = encontro.Grupo.Atletas.Single(x => x.AtletaId == confirmacao.AtletaId).Atleta;
            confirmacao.EncontroGrupo = encontro;
            confirmacao.Atleta = atleta;
            encontro.Confirmacoes.Add(confirmacao);
            atleta.ConfirmacoesPresencaGrupo.Add(confirmacao);
            return Task.CompletedTask;
        }

        public Task<bool> TentarReservarEnvioWhatsappAsync(
            Guid confirmacaoId,
            DateTime agoraUtc,
            TimeSpan intervaloMinimo,
            int maximoTentativas,
            CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        public void AtualizarConfirmacao(ConfirmacaoPresencaGrupo confirmacao)
        {
        }
    }

    private sealed class NotificacaoUsuarioRepositorioMemoria : INotificacaoUsuarioRepositorio
    {
        public List<NotificacaoUsuario> Notificacoes { get; } = [];

        public Task AdicionarIntervaloAsync(IEnumerable<NotificacaoUsuario> notificacoes, CancellationToken cancellationToken = default)
        {
            foreach (var notificacao in notificacoes)
            {
                if (Notificacoes.All(x => x.Origem != notificacao.Origem || x.ChaveIdempotencia != notificacao.ChaveIdempotencia))
                {
                    Notificacoes.Add(notificacao);
                }
            }

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<NotificacaoUsuario>> ListarPorUsuarioAsync(Guid usuarioId, bool somenteNaoLidas, int limite, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<NotificacaoUsuario>>(Notificacoes.Where(x => x.UsuarioId == usuarioId).Take(limite).ToList());

        public Task<NotificacaoUsuario?> ObterPorIdAsync(Guid id, Guid usuarioId, CancellationToken cancellationToken = default)
            => Task.FromResult(Notificacoes.FirstOrDefault(x => x.Id == id && x.UsuarioId == usuarioId));

        public Task<IReadOnlySet<string>> ListarChavesDaOrigemAsync(Guid usuarioId, string origem, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlySet<string>>(Notificacoes.Where(x => x.UsuarioId == usuarioId && x.Origem == origem).Select(x => x.ChaveIdempotencia).ToHashSet());

        public Task<int> ContarNaoLidasAsync(Guid usuarioId, CancellationToken cancellationToken = default)
            => Task.FromResult(Notificacoes.Count(x => x.UsuarioId == usuarioId && !x.LidaEmUtc.HasValue));

        public Task MarcarTodasComoLidasAsync(Guid usuarioId, DateTime dataUtc, CancellationToken cancellationToken = default)
        {
            foreach (var notificacao in Notificacoes.Where(x => x.UsuarioId == usuarioId))
            {
                notificacao.LidaEmUtc = dataUtc;
            }

            return Task.CompletedTask;
        }

        public void Atualizar(NotificacaoUsuario notificacao)
        {
        }
    }

    private sealed class EntregaNotificacaoExternaStub : IEntregaNotificacaoExternaServico
    {
        public List<SolicitacaoEntregaNotificacaoDto> Solicitacoes { get; } = [];

        public Task<ResultadoEntregaNotificacaoDto> EnviarAsync(SolicitacaoEntregaNotificacaoDto solicitacao, CancellationToken cancellationToken = default)
        {
            Solicitacoes.Add(solicitacao);
            return Task.FromResult(new ResultadoEntregaNotificacaoDto(true, true, null, "whatsapp-1"));
        }
    }

    private sealed class UnidadeTrabalhoStub : IUnidadeTrabalho
    {
        public Task<int> SalvarAlteracoesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);

        public async Task ExecutarEmTransacaoAsync(Func<CancellationToken, Task> operacao, CancellationToken cancellationToken = default)
            => await operacao(cancellationToken);
    }

    private sealed class AutorizacaoUsuarioServicoStub : IAutorizacaoUsuarioServico
    {
        private readonly Usuario usuario = new() { Nome = "Administrador", Ativo = true, Perfil = PerfilUsuario.Administrador };

        public Task<Usuario?> ObterUsuarioAtualAsync(CancellationToken cancellationToken = default) => Task.FromResult<Usuario?>(usuario);
        public Task<Usuario> ObterUsuarioAtualObrigatorioAsync(CancellationToken cancellationToken = default) => Task.FromResult(usuario);
        public Task GarantirAdministradorAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task GarantirAdminOuOrganizadorAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task GarantirAcessoAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task GarantirGestaoCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task GarantirGestaoGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
