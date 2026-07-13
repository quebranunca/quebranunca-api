using Microsoft.Extensions.Logging.Abstractions;
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

public class PartidaCancelamentoServicoTests
{
    [Fact]
    public async Task SolicitarAsync_SolicitanteDaDuplaA_GeraPendenciasParaDuplaB()
    {
        var cenario = new Cenario();
        cenario.Autorizacao.UsuarioAtual = cenario.UsuarioA1;

        var solicitacao = await cenario.Servico.SolicitarAsync(
            cenario.Partida.Id,
            new SolicitarCancelamentoPartidaDto(MotivoCancelamentoPartida.PartidaDuplicada, null));

        Assert.Equal(StatusSolicitacaoCancelamentoPartida.Pendente, solicitacao.Status);
        Assert.Equal(cenario.DuplaA.Id, solicitacao.DuplaSolicitanteId);
        Assert.Equal(cenario.DuplaB.Id, solicitacao.DuplaAdversariaId);
        Assert.Equal(2, cenario.Pendencias.Pendencias.Count);
        Assert.All(cenario.Pendencias.Pendencias, pendencia =>
        {
            Assert.Equal(TipoPendenciaUsuario.ResponderCancelamentoPartida, pendencia.Tipo);
            Assert.Equal(cenario.Partida.Id, pendencia.PartidaId);
            Assert.Equal(solicitacao.Id, pendencia.SolicitacaoCancelamentoPartidaId);
            Assert.Contains(pendencia.AtletaId, new Guid?[] { cenario.AtletaB1.Id, cenario.AtletaB2.Id });
        });
    }

    [Fact]
    public async Task SolicitarAsync_SolicitanteDaDuplaB_GeraPendenciasParaDuplaA()
    {
        var cenario = new Cenario();
        cenario.Autorizacao.UsuarioAtual = cenario.UsuarioB2;

        var solicitacao = await cenario.Servico.SolicitarAsync(
            cenario.Partida.Id,
            new SolicitarCancelamentoPartidaDto(MotivoCancelamentoPartida.ResultadoIncorreto, "Resultado informado errado."));

        Assert.Equal(cenario.DuplaB.Id, solicitacao.DuplaSolicitanteId);
        Assert.Equal(cenario.DuplaA.Id, solicitacao.DuplaAdversariaId);
        Assert.Equal(2, cenario.Pendencias.Pendencias.Count);
        Assert.All(cenario.Pendencias.Pendencias, pendencia =>
            Assert.Contains(pendencia.AtletaId, new Guid?[] { cenario.AtletaA1.Id, cenario.AtletaA2.Id }));
    }

    [Fact]
    public async Task SolicitarAsync_OutroMotivoSemObservacao_Bloqueia()
    {
        var cenario = new Cenario();
        cenario.Autorizacao.UsuarioAtual = cenario.UsuarioA1;

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.SolicitarAsync(
                cenario.Partida.Id,
                new SolicitarCancelamentoPartidaDto(MotivoCancelamentoPartida.Outro, "   ")));

        Assert.Equal("Descreva o motivo do cancelamento.", excecao.Message);
        Assert.Empty(cenario.Pendencias.Pendencias);
    }

    [Fact]
    public async Task SolicitarAsync_UsuarioForaDaPartida_Bloqueia()
    {
        var cenario = new Cenario();
        cenario.Autorizacao.UsuarioAtual = cenario.UsuarioExterno;

        var excecao = await Assert.ThrowsAsync<AcessoNegadoException>(() =>
            cenario.Servico.SolicitarAsync(
                cenario.Partida.Id,
                new SolicitarCancelamentoPartidaDto(MotivoCancelamentoPartida.GrupoIncorreto, null)));

        Assert.Equal("Somente atletas participantes da partida podem solicitar cancelamento.", excecao.Message);
    }

    [Fact]
    public async Task SolicitarAsync_ComSolicitacaoPendente_BloqueiaSegundaSolicitacao()
    {
        var cenario = new Cenario();
        cenario.Autorizacao.UsuarioAtual = cenario.UsuarioA1;
        await cenario.Servico.SolicitarAsync(
            cenario.Partida.Id,
            new SolicitarCancelamentoPartidaDto(MotivoCancelamentoPartida.PartidaDuplicada, null));

        cenario.Autorizacao.UsuarioAtual = cenario.UsuarioB1;
        var excecao = await Assert.ThrowsAsync<ConflitoEstadoException>(() =>
            cenario.Servico.SolicitarAsync(
                cenario.Partida.Id,
                new SolicitarCancelamentoPartidaDto(MotivoCancelamentoPartida.ResultadoIncorreto, null)));

        Assert.Equal("Já existe uma solicitação de cancelamento pendente para esta partida.", excecao.Message);
        Assert.Single(cenario.Solicitacoes.Solicitacoes);
        Assert.Equal(2, cenario.Pendencias.Pendencias.Count);
    }

    [Fact]
    public async Task AprovarAsync_AdversarioCancelaPartidaEstornaQnEEncerraOutraPendencia()
    {
        var cenario = new Cenario();
        cenario.Autorizacao.UsuarioAtual = cenario.UsuarioA1;
        var solicitacao = await cenario.Servico.SolicitarAsync(
            cenario.Partida.Id,
            new SolicitarCancelamentoPartidaDto(MotivoCancelamentoPartida.PartidaDuplicada, null));
        cenario.Autorizacao.UsuarioAtual = cenario.UsuarioB1;

        var resposta = await cenario.Servico.AprovarAsync(cenario.Partida.Id, solicitacao.Id);

        Assert.Equal(StatusSolicitacaoCancelamentoPartida.Aprovada, resposta.Status);
        Assert.True(cenario.Partida.Cancelada);
        Assert.Equal(solicitacao.Id, cenario.Partida.SolicitacaoCancelamentoOrigemId);
        Assert.NotNull(cenario.Partida.CanceladaEm);
        Assert.Equal(cenario.Partida.Id, Assert.Single(cenario.Pontuacao.EstornosPartida));
        Assert.Single(cenario.Pendencias.Pendencias, x =>
            x.UsuarioId == cenario.UsuarioB1.Id && x.Status == StatusPendenciaUsuario.Concluida);
        Assert.Single(cenario.Pendencias.Pendencias, x =>
            x.UsuarioId == cenario.UsuarioB2.Id && x.Status == StatusPendenciaUsuario.Cancelada);
    }

    [Fact]
    public async Task AprovarAsync_ParceiroDoSolicitanteNaoPodeResponder()
    {
        var cenario = new Cenario();
        cenario.Autorizacao.UsuarioAtual = cenario.UsuarioA1;
        var solicitacao = await cenario.Servico.SolicitarAsync(
            cenario.Partida.Id,
            new SolicitarCancelamentoPartidaDto(MotivoCancelamentoPartida.PartidaDuplicada, null));
        cenario.Autorizacao.UsuarioAtual = cenario.UsuarioA2;

        var excecao = await Assert.ThrowsAsync<AcessoNegadoException>(() =>
            cenario.Servico.AprovarAsync(cenario.Partida.Id, solicitacao.Id));

        Assert.Equal("Somente atletas da dupla adversária podem responder ao cancelamento.", excecao.Message);
        Assert.False(cenario.Partida.Cancelada);
        Assert.Empty(cenario.Pontuacao.EstornosPartida);
    }

    [Fact]
    public async Task AprovarAsync_RespostaDuplicadaRecebeConflitoENaoDuplicaEstorno()
    {
        var cenario = new Cenario();
        cenario.Autorizacao.UsuarioAtual = cenario.UsuarioA1;
        var solicitacao = await cenario.Servico.SolicitarAsync(
            cenario.Partida.Id,
            new SolicitarCancelamentoPartidaDto(MotivoCancelamentoPartida.PartidaDuplicada, null));
        cenario.Autorizacao.UsuarioAtual = cenario.UsuarioB1;
        await cenario.Servico.AprovarAsync(cenario.Partida.Id, solicitacao.Id);

        cenario.Autorizacao.UsuarioAtual = cenario.UsuarioB2;
        var excecao = await Assert.ThrowsAsync<ConflitoEstadoException>(() =>
            cenario.Servico.AprovarAsync(cenario.Partida.Id, solicitacao.Id));

        Assert.Equal("Esta solicitação de cancelamento já foi processada.", excecao.Message);
        Assert.True(cenario.Partida.Cancelada);
        Assert.Equal(cenario.Partida.Id, Assert.Single(cenario.Pontuacao.EstornosPartida));
    }

    [Fact]
    public async Task RecusarAsync_AposAprovacaoRecebeConflitoENaoReabrePartida()
    {
        var cenario = new Cenario();
        cenario.Autorizacao.UsuarioAtual = cenario.UsuarioA1;
        var solicitacao = await cenario.Servico.SolicitarAsync(
            cenario.Partida.Id,
            new SolicitarCancelamentoPartidaDto(MotivoCancelamentoPartida.PartidaDuplicada, null));
        cenario.Autorizacao.UsuarioAtual = cenario.UsuarioB1;
        await cenario.Servico.AprovarAsync(cenario.Partida.Id, solicitacao.Id);

        cenario.Autorizacao.UsuarioAtual = cenario.UsuarioB2;
        var excecao = await Assert.ThrowsAsync<ConflitoEstadoException>(() =>
            cenario.Servico.RecusarAsync(cenario.Partida.Id, solicitacao.Id));

        Assert.Equal("Esta solicitação de cancelamento já foi processada.", excecao.Message);
        Assert.True(cenario.Partida.Cancelada);
        Assert.Equal(StatusSolicitacaoCancelamentoPartida.Aprovada, cenario.Solicitacoes.Solicitacoes.Single().Status);
        Assert.Equal(cenario.Partida.Id, Assert.Single(cenario.Pontuacao.EstornosPartida));
    }

    [Fact]
    public async Task RecusarAsync_AdversarioEncerraSolicitacaoSemAlterarPartidaOuQn()
    {
        var cenario = new Cenario();
        cenario.Autorizacao.UsuarioAtual = cenario.UsuarioA1;
        var solicitacao = await cenario.Servico.SolicitarAsync(
            cenario.Partida.Id,
            new SolicitarCancelamentoPartidaDto(MotivoCancelamentoPartida.JogoNaoAconteceu, null));
        cenario.Autorizacao.UsuarioAtual = cenario.UsuarioB2;

        var resposta = await cenario.Servico.RecusarAsync(cenario.Partida.Id, solicitacao.Id);

        Assert.Equal(StatusSolicitacaoCancelamentoPartida.Recusada, resposta.Status);
        Assert.False(cenario.Partida.Cancelada);
        Assert.Empty(cenario.Pontuacao.EstornosPartida);
        Assert.Single(cenario.Pendencias.Pendencias, x =>
            x.UsuarioId == cenario.UsuarioB2.Id && x.Status == StatusPendenciaUsuario.Concluida);
        Assert.Single(cenario.Pendencias.Pendencias, x =>
            x.UsuarioId == cenario.UsuarioB1.Id && x.Status == StatusPendenciaUsuario.Cancelada);
    }

    [Fact]
    public async Task CancelarSolicitacaoAsync_SolicitanteEncerraSemAlterarPartidaOuQn()
    {
        var cenario = new Cenario();
        cenario.Autorizacao.UsuarioAtual = cenario.UsuarioA1;
        var solicitacao = await cenario.Servico.SolicitarAsync(
            cenario.Partida.Id,
            new SolicitarCancelamentoPartidaDto(MotivoCancelamentoPartida.AtletasIncorretos, null));

        var resposta = await cenario.Servico.CancelarSolicitacaoAsync(cenario.Partida.Id, solicitacao.Id);

        Assert.Equal(StatusSolicitacaoCancelamentoPartida.CanceladaPeloSolicitante, resposta.Status);
        Assert.False(cenario.Partida.Cancelada);
        Assert.Empty(cenario.Pontuacao.EstornosPartida);
        Assert.All(cenario.Pendencias.Pendencias, x => Assert.Equal(StatusPendenciaUsuario.Cancelada, x.Status));
    }

    [Fact]
    public async Task CancelarSolicitacaoAsync_AposRecusaRecebeConflitoENaoAlteraPartidaOuQn()
    {
        var cenario = new Cenario();
        cenario.Autorizacao.UsuarioAtual = cenario.UsuarioA1;
        var solicitacao = await cenario.Servico.SolicitarAsync(
            cenario.Partida.Id,
            new SolicitarCancelamentoPartidaDto(MotivoCancelamentoPartida.AtletasIncorretos, null));
        cenario.Autorizacao.UsuarioAtual = cenario.UsuarioB1;
        await cenario.Servico.RecusarAsync(cenario.Partida.Id, solicitacao.Id);

        cenario.Autorizacao.UsuarioAtual = cenario.UsuarioA1;
        var excecao = await Assert.ThrowsAsync<ConflitoEstadoException>(() =>
            cenario.Servico.CancelarSolicitacaoAsync(cenario.Partida.Id, solicitacao.Id));

        Assert.Equal("Esta solicitação de cancelamento já foi processada.", excecao.Message);
        Assert.False(cenario.Partida.Cancelada);
        Assert.Equal(StatusSolicitacaoCancelamentoPartida.Recusada, cenario.Solicitacoes.Solicitacoes.Single().Status);
        Assert.Empty(cenario.Pontuacao.EstornosPartida);
    }

    [Fact]
    public async Task CancelarDiretamenteAsync_RegistradorCancelaEstornaQnERegistraHistorico()
    {
        var cenario = new Cenario();
        cenario.Autorizacao.UsuarioAtual = cenario.UsuarioA1;

        var partida = await cenario.Servico.CancelarDiretamenteAsync(
            cenario.Partida.Id,
            new CancelarPartidaDto("Partida registrada por engano."));

        Assert.True(partida.Cancelada);
        Assert.True(cenario.Partida.Cancelada);
        Assert.NotNull(cenario.Partida.CanceladaEm);
        Assert.Equal(cenario.Partida.Id, Assert.Single(cenario.Pontuacao.EstornosPartida));
        Assert.Contains(cenario.Historicos.Historicos, x => x.Acao == "CancelamentoDireto");
    }

    [Fact]
    public async Task CancelarDiretamenteAsync_ParticipanteNaoRegistradorNaoCancela()
    {
        var cenario = new Cenario();
        cenario.Autorizacao.UsuarioAtual = cenario.UsuarioB1;

        await Assert.ThrowsAsync<AcessoNegadoException>(() =>
            cenario.Servico.CancelarDiretamenteAsync(
                cenario.Partida.Id,
                new CancelarPartidaDto("Não concordo com a partida.")));

        Assert.False(cenario.Partida.Cancelada);
        Assert.Empty(cenario.Pontuacao.EstornosPartida);
    }

    [Fact]
    public async Task CancelarDiretamenteAsync_RepetidoNaoDuplicaEstorno()
    {
        var cenario = new Cenario();
        cenario.Autorizacao.UsuarioAtual = cenario.UsuarioA1;

        await cenario.Servico.CancelarDiretamenteAsync(
            cenario.Partida.Id,
            new CancelarPartidaDto("Partida registrada por engano."));
        await cenario.Servico.CancelarDiretamenteAsync(
            cenario.Partida.Id,
            new CancelarPartidaDto("Partida registrada por engano."));

        Assert.True(cenario.Partida.Cancelada);
        Assert.Equal(cenario.Partida.Id, Assert.Single(cenario.Pontuacao.EstornosPartida));
    }

    [Fact]
    public async Task ExcluirDefinitivamenteAsync_RegistradorOuAdminExcluiComMotivo()
    {
        var cenario = new Cenario();
        cenario.Autorizacao.UsuarioAtual = cenario.UsuarioB1;

        await Assert.ThrowsAsync<AcessoNegadoException>(() =>
            cenario.Servico.ExcluirDefinitivamenteAsync(
                cenario.Partida.Id,
                new ExcluirPartidaDefinitivamenteDto("Auditoria")));

        cenario.Autorizacao.UsuarioAtual = cenario.Admin;
        await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.ExcluirDefinitivamenteAsync(
                cenario.Partida.Id,
                new ExcluirPartidaDefinitivamenteDto(" ")));

        cenario.Autorizacao.UsuarioAtual = cenario.UsuarioA1;
        await cenario.Servico.ExcluirDefinitivamenteAsync(
            cenario.Partida.Id,
            new ExcluirPartidaDefinitivamenteDto("Remoção solicitada pelo registrador."));

        Assert.False(cenario.Partida.Ativa);
        Assert.NotNull(cenario.Partida.ExcluidaDefinitivamenteEm);
        Assert.Equal(cenario.UsuarioA1.Id, cenario.Partida.ExcluidaDefinitivamentePorUsuarioId);
        Assert.Equal("Remoção solicitada pelo registrador.", cenario.Partida.MotivoExclusaoDefinitiva);
        Assert.Equal(cenario.Partida.Id, Assert.Single(cenario.Pontuacao.EstornosPartida));
        Assert.Contains(cenario.Historicos.Historicos, x => x.Acao == "ExclusaoDefinitiva");
    }

    private sealed class Cenario
    {
        public Cenario()
        {
            AtletaA1 = CriarAtletaComUsuario("Gustavo", out var usuarioA1);
            AtletaA2 = CriarAtletaComUsuario("Bruno", out var usuarioA2);
            AtletaB1 = CriarAtletaComUsuario("Rafa", out var usuarioB1);
            AtletaB2 = CriarAtletaComUsuario("Teteu", out var usuarioB2);
            UsuarioA1 = usuarioA1;
            UsuarioA2 = usuarioA2;
            UsuarioB1 = usuarioB1;
            UsuarioB2 = usuarioB2;
            UsuarioExterno = CriarUsuarioComAtleta("Externo");
            Admin = new Usuario
            {
                Nome = "Admin",
                Email = "admin@qnf.test",
                Perfil = PerfilUsuario.Administrador
            };

            DuplaA = CriarDupla(AtletaA1, AtletaA2);
            DuplaB = CriarDupla(AtletaB1, AtletaB2);
            Partida = new Partida
            {
                GrupoId = Guid.NewGuid(),
                CriadoPorUsuarioId = UsuarioA1.Id,
                CriadoPorUsuario = UsuarioA1,
                DuplaAId = DuplaA.Id,
                DuplaA = DuplaA,
                DuplaBId = DuplaB.Id,
                DuplaB = DuplaB,
                DuplaVencedoraId = DuplaA.Id,
                DuplaVencedora = DuplaA,
                Status = StatusPartida.Encerrada,
                StatusAprovacao = StatusAprovacaoPartida.Aprovada,
                TipoRegistroResultado = TipoRegistroResultado.PlacarDetalhado,
                PlacarDuplaA = 21,
                PlacarDuplaB = 18,
                DataPartida = DateTime.UtcNow.AddDays(-1),
                Ativa = true
            };

            Partidas = new PartidaRepositorioFake(Partida);
            Solicitacoes = new SolicitacaoCancelamentoRepositorioFake();
            Pendencias = new PendenciaUsuarioRepositorioFake();
            Historicos = new HistoricoPartidaRepositorioFake();
            UnidadeTrabalho = new UnidadeTrabalhoFake();
            Autorizacao = new AutorizacaoFake(UsuarioA1);
            Pontuacao = new PontuacaoBeneficioServicoFake();

            Servico = new PartidaCancelamentoServico(
                Partidas,
                Solicitacoes,
                Pendencias,
                Historicos,
                UnidadeTrabalho,
                Autorizacao,
                Pontuacao,
                NullLogger<PartidaCancelamentoServico>.Instance);
        }

        public Atleta AtletaA1 { get; }
        public Atleta AtletaA2 { get; }
        public Atleta AtletaB1 { get; }
        public Atleta AtletaB2 { get; }
        public Usuario UsuarioA1 { get; }
        public Usuario UsuarioA2 { get; }
        public Usuario UsuarioB1 { get; }
        public Usuario UsuarioB2 { get; }
        public Usuario UsuarioExterno { get; }
        public Usuario Admin { get; }
        public Dupla DuplaA { get; }
        public Dupla DuplaB { get; }
        public Partida Partida { get; }
        public PartidaRepositorioFake Partidas { get; }
        public SolicitacaoCancelamentoRepositorioFake Solicitacoes { get; }
        public PendenciaUsuarioRepositorioFake Pendencias { get; }
        public HistoricoPartidaRepositorioFake Historicos { get; }
        public UnidadeTrabalhoFake UnidadeTrabalho { get; }
        public AutorizacaoFake Autorizacao { get; }
        public PontuacaoBeneficioServicoFake Pontuacao { get; }
        public PartidaCancelamentoServico Servico { get; }

        private static Atleta CriarAtletaComUsuario(string nome, out Usuario usuario)
        {
            var atleta = new Atleta { Nome = nome, Email = $"{nome.ToLowerInvariant()}@qnf.test" };
            usuario = new Usuario
            {
                Nome = nome,
                Email = atleta.Email!,
                Perfil = PerfilUsuario.Atleta,
                AtletaId = atleta.Id,
                Atleta = atleta
            };
            atleta.Usuario = usuario;
            return atleta;
        }

        private static Usuario CriarUsuarioComAtleta(string nome)
        {
            var atleta = CriarAtletaComUsuario(nome, out var usuario);
            usuario.Atleta = atleta;
            return usuario;
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
    }

    private sealed class PartidaRepositorioFake(Partida partida) : IPartidaRepositorio
    {
        public Task<Partida?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(id == partida.Id ? partida : null);

        public Task<Partida?> ObterPorIdParaAtualizacaoAsync(Guid id, CancellationToken cancellationToken = default)
            => ObterPorIdAsync(id, cancellationToken);

        public void Atualizar(Partida partidaAtualizada)
        {
        }

        public Task<IReadOnlyList<Partida>> ListarPorCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<int> ContarRegistradasAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Partida>> ListarPorGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Partida>> ListarPorCategoriaAsync(Guid categoriaId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Partida>> ListarPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Partida>> ListarPorUsuarioCriadorAsync(Guid usuarioId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Partida>> ListarAdministracaoAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Partida>> ListarFeedAsync(int skip, int take, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Partida>> ListarPorDiaAsync(DateTime inicioUtc, DateTime fimUtc, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Partida>> ListarPorAtletaParaRemocaoAsync(Guid atletaId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Partida>> ListarReferenciandoPartidasAsync(IReadOnlyCollection<Guid> partidaIds, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Partida?> ObterUltimaDoGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Partida?> ObterUltimaDoAtletaNoGrupoAsync(Guid grupoId, Guid atletaId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Partida>> ListarComAtletasPendentesPorUsuarioCriadorAsync(Guid usuarioId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Partida>> ListarComPendenteDeVinculoPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> ExisteAtletaPendenteEmPartidaCriadaPorUsuarioAsync(Guid usuarioId, Guid atletaId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Partida>> ListarParaRankingGeralAsync(Guid? usuarioOrganizadorId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Partida>> ListarParaRankingPorLigaAsync(Guid ligaId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Partida>> ListarParaRankingSemCompeticaoOuCategoriaAsync(Guid? usuarioOrganizadorId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Partida>> ListarParaRankingPorCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Partida>> ListarParaRankingPorGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Guid?> ObterUltimaCompeticaoComPartidaEncerradaAsync(Guid? usuarioOrganizadorId, Guid? atletaId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<AtletasSugestoesPartidaDto> ObterSugestoesPartidaAsync(Guid atletaId, Guid? grupoId, int limitePorSecao, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<UsuarioResumoDto> ObterResumoUsuarioPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AdicionarAsync(Partida partidaNova, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public void Remover(Partida partidaRemovida) => throw new NotImplementedException();
    }

    private sealed class SolicitacaoCancelamentoRepositorioFake : ISolicitacaoCancelamentoPartidaRepositorio
    {
        public List<SolicitacaoCancelamentoPartida> Solicitacoes { get; } = [];

        public Task AdicionarAsync(SolicitacaoCancelamentoPartida solicitacao, CancellationToken cancellationToken = default)
        {
            Solicitacoes.Add(solicitacao);
            solicitacao.Partida.SolicitacoesCancelamento.Add(solicitacao);
            return Task.CompletedTask;
        }

        public void Atualizar(SolicitacaoCancelamentoPartida solicitacao)
        {
        }

        public Task<IReadOnlyList<SolicitacaoCancelamentoPartida>> ListarPorPartidaAsync(Guid partidaId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<SolicitacaoCancelamentoPartida>>(Solicitacoes.Where(x => x.PartidaId == partidaId).ToList());

        public Task<SolicitacaoCancelamentoPartida?> ObterPendentePorPartidaAsync(Guid partidaId, CancellationToken cancellationToken = default)
            => Task.FromResult(Solicitacoes.FirstOrDefault(x => x.PartidaId == partidaId && x.Status == StatusSolicitacaoCancelamentoPartida.Pendente));

        public Task<SolicitacaoCancelamentoPartida?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Solicitacoes.FirstOrDefault(x => x.Id == id));

        public Task<SolicitacaoCancelamentoPartida?> ObterPorIdParaAtualizacaoAsync(Guid id, CancellationToken cancellationToken = default)
            => ObterPorIdAsync(id, cancellationToken);
    }

    private sealed class PendenciaUsuarioRepositorioFake : IPendenciaUsuarioRepositorio
    {
        public List<PendenciaUsuario> Pendencias { get; } = [];

        public Task AdicionarAsync(PendenciaUsuario pendencia, CancellationToken cancellationToken = default)
        {
            Pendencias.Add(pendencia);
            return Task.CompletedTask;
        }

        public void Atualizar(PendenciaUsuario pendencia)
        {
        }

        public Task<IReadOnlyList<PendenciaUsuario>> ListarPendentesPorPartidaAsync(Guid partidaId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PendenciaUsuario>>(
                Pendencias.Where(x => x.PartidaId == partidaId && x.Status == StatusPendenciaUsuario.Pendente).ToList());

        public Task<IReadOnlyList<PendenciaUsuario>> ListarPendentesPorUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<PendenciaUsuario>> ListarPendentesPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<PendenciaUsuario>> ListarPendentesPorUsuarioParaAtualizacaoAsync(Guid usuarioId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PendenciaUsuario?> ObterPendenteAsync(TipoPendenciaUsuario tipo, Guid usuarioId, Guid? partidaId, Guid? atletaId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> ExistePendentePorUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PendenciaUsuario?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class HistoricoPartidaRepositorioFake : IHistoricoPartidaRepositorio
    {
        public List<HistoricoPartida> Historicos { get; } = [];

        public Task AdicionarAsync(HistoricoPartida historico, CancellationToken cancellationToken = default)
        {
            Historicos.Add(historico);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<HistoricoPartida>> ListarPorPartidaAsync(Guid partidaId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<HistoricoPartida>>(
                Historicos.Where(x => x.PartidaIdOriginal == partidaId).ToList());
    }

    private sealed class UnidadeTrabalhoFake : IUnidadeTrabalho
    {
        public int Salvamentos { get; private set; }

        public async Task ExecutarEmTransacaoAsync(Func<CancellationToken, Task> operacao, CancellationToken cancellationToken = default)
        {
            await operacao(cancellationToken);
        }

        public Task<int> SalvarAlteracoesAsync(CancellationToken cancellationToken = default)
        {
            Salvamentos++;
            return Task.FromResult(1);
        }
    }

    private sealed class AutorizacaoFake(Usuario usuarioAtual) : IAutorizacaoUsuarioServico
    {
        public Usuario UsuarioAtual { get; set; } = usuarioAtual;

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

        public Task GarantirAdminOuOrganizadorAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task GarantirAcessoAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task GarantirGestaoCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task GarantirGestaoGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class PontuacaoBeneficioServicoFake : IPontuacaoBeneficioServico
    {
        public List<Guid> EstornosPartida { get; } = [];

        public Task EstornarPartidaAsync(Guid partidaId, CancellationToken cancellationToken = default)
        {
            if (!EstornosPartida.Contains(partidaId))
            {
                EstornosPartida.Add(partidaId);
            }

            return Task.CompletedTask;
        }

        public Task<GamificacaoResumoDto> ObterResumoAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ExtratoPontuacaoBeneficioListaDto> ListarExtratoAsync(TipoEventoPontuacaoBeneficio? tipo, DateTime? dataInicial, DateTime? dataFinal, int pagina, int quantidadePorPagina, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<BeneficioPontuacaoDto>> ListarBeneficiosAsync(TipoBeneficioPontuacao? tipo, bool? disponivel, bool? destaque, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ResgateBeneficioPontuacaoDto> SolicitarResgateAsync(Guid beneficioId, SolicitarResgateBeneficioDto dto, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<ResgateBeneficioPontuacaoDto>> ListarMeusResgatesAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<ResgateBeneficioPontuacaoDto>> ListarResgatesAdministracaoAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ResgateBeneficioPontuacaoDto> AprovarResgateAsync(Guid resgateId, AtualizarStatusResgateBeneficioDto dto, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ResgateBeneficioPontuacaoDto> RejeitarResgateAsync(Guid resgateId, AtualizarStatusResgateBeneficioDto dto, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ResgateBeneficioPontuacaoDto> CancelarResgateAsync(Guid resgateId, AtualizarStatusResgateBeneficioDto dto, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<MissaoPontuacaoDto>> ListarMissoesAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<ConquistaAtletaDto>> ListarConquistasAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task RegistrarCompartilhamentoAsync(RegistrarCompartilhamentoGamificacaoDto dto, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task PontuarPartidaValidadaAsync(Partida partida, Guid? usuarioRegistradorId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task PontuarConfirmacaoAprovacaoPartidaAsync(Guid partidaId, Guid atletaId, Guid pendenciaId, Guid usuarioId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task PontuarPendenciaResolvidaAsync(Guid pendenciaId, Guid atletaId, Guid? partidaId, Guid usuarioId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task PontuarPerfilCompletoAsync(Atleta atleta, Guid usuarioId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<RecalculoSaldoInicialPontuacaoResultadoDto> RecalcularSaldoInicialRetroativoAsync(bool dryRun, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
