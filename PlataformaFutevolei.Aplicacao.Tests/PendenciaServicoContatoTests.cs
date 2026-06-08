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
    public async Task CompletarContatoAsync_VinculaPartidaAoAtletaExistentePorEmail()
    {
        var cenario = Cenario.Criar();
        var atletaExistente = cenario.CriarAtleta("Atleta Existente", "existente@teste.com", possuiUsuario: true);

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
    public async Task CompletarContatoAsync_EmailInexistente_MantemFluxoAtualComConvite()
    {
        var cenario = Cenario.Criar();

        await cenario.Servico.CompletarContatoAsync(
            cenario.Pendencia.Id,
            new AtualizarContatoPendenciaDto(" novo@teste.com "));

        Assert.Equal("novo@teste.com", cenario.AtletaPendente.Email);
        Assert.Equal(StatusPendenciaUsuario.Concluida, cenario.Pendencia.Status);
        Assert.Single(cenario.Convites.Criados);
        Assert.Equal(cenario.AtletaPendente.Id, cenario.Convites.Criados[0].AtletaId);
        Assert.Contains(cenario.AtletaPendente.Id, ObterAtletasPartida(cenario.Partida));
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
                new GrupoAtletaRepositorioStub(),
                new UnidadeTrabalhoStub(),
                new AutorizacaoUsuarioServicoStub(cenario.UsuarioAtual),
                resolvedor,
                cenario.Convites);

            return cenario;
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
