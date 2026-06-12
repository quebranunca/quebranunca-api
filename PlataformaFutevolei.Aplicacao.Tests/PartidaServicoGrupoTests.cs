using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Interfaces.Seguranca;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Aplicacao.Json;
using PlataformaFutevolei.Aplicacao.Servicos;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;
using System.Text.Json;
using Xunit;

namespace PlataformaFutevolei.Aplicacao.Tests;

public class PartidaServicoGrupoTests
{
    [Fact]
    public async Task CriarComResultadoAsync_UsuarioPertenceAoGrupoEAtletasJaPertencem_RegistraNormalmente()
    {
        var cenario = Cenario.Criar(publico: false);
        foreach (var atleta in cenario.Atletas)
        {
            cenario.AdicionarMembro(atleta);
        }

        var partida = await CriarPartidaAsync(cenario, cenario.CriarDto(cenario.Grupo.Id));

        Assert.Equal(cenario.Grupo.Id, partida.GrupoId);
        Assert.Equal(5, cenario.GruposAtletas.Vinculos.Count);
    }

    [Fact]
    public async Task CriarComResultadoAsync_UsuarioPertenceAoGrupoEAtletasAusentes_RegistraEVinculaAusentes()
    {
        var cenario = Cenario.Criar(publico: false);
        cenario.AdicionarMembro(cenario.Atletas[0]);
        cenario.AdicionarMembro(cenario.Atletas[1]);

        var partida = await CriarPartidaAsync(cenario, cenario.CriarDto(cenario.Grupo.Id));

        Assert.Equal(cenario.Grupo.Id, partida.GrupoId);
        Assert.All(cenario.Atletas, atleta =>
            Assert.Contains(cenario.GruposAtletas.Vinculos, vinculo =>
                vinculo.GrupoId == cenario.Grupo.Id && vinculo.AtletaId == atleta.Id));
        Assert.Equal(5, cenario.GruposAtletas.Vinculos.Count);
    }

    [Fact]
    public async Task CriarComResultadoAsync_UsuarioPertenceAoGrupoENenhumAtletaPertence_RegistraEVinculaTodos()
    {
        var cenario = Cenario.Criar(publico: false);

        var partida = await CriarPartidaAsync(cenario, cenario.CriarDto(cenario.Grupo.Id));

        Assert.Equal(cenario.Grupo.Id, partida.GrupoId);
        Assert.All(cenario.Atletas, atleta =>
            Assert.Contains(cenario.GruposAtletas.Vinculos, vinculo =>
                vinculo.GrupoId == cenario.Grupo.Id && vinculo.AtletaId == atleta.Id));
        Assert.Equal(5, cenario.GruposAtletas.Vinculos.Count);
    }

    [Fact]
    public async Task CriarComResultadoAsync_UsuarioNaoPertenceAoGrupo_BloqueiaRegistro()
    {
        var cenario = Cenario.Criar(publico: false, usuarioMembro: false);

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            CriarPartidaAsync(cenario, cenario.CriarDto(cenario.Grupo.Id)));

        Assert.Equal("Você precisa fazer parte deste grupo para registrar partidas nele.", excecao.Message);
        Assert.Empty(cenario.Partidas.Partidas);
        Assert.Empty(cenario.GruposAtletas.Vinculos);
    }

    [Fact]
    public async Task CriarComResultadoAsync_NaoDuplicaMembroExistente()
    {
        var cenario = Cenario.Criar(publico: true);
        cenario.AdicionarMembro(cenario.Atletas[0]);
        cenario.AdicionarMembro(cenario.Atletas[1]);

        await CriarPartidaAsync(cenario, cenario.CriarDto(cenario.Grupo.Id));

        Assert.Equal(5, cenario.GruposAtletas.Vinculos.Select(x => x.AtletaId).Distinct().Count());
        Assert.Equal(5, cenario.GruposAtletas.Vinculos.Count);
    }

    [Fact]
    public async Task CriarComResultadoAsync_SemGrupoInformado_UsaGrupoGeralELiberaRegistro()
    {
        var cenario = Cenario.Criar(publico: true, usuarioMembro: false);

        var partida = await CriarPartidaAsync(cenario, cenario.CriarDto(grupoId: null));

        Assert.Equal(cenario.GrupoGeral.Id, partida.GrupoId);
        Assert.Single(cenario.Partidas.Partidas);
        Assert.Equal(4, cenario.GruposAtletas.Vinculos.Count);
    }

    [Fact]
    public async Task CriarComResultadoAsync_UsuarioCriadorNaoParticipaDaPartida_VinculaCriadorCorreto()
    {
        var cenario = Cenario.Criar(publico: false);

        var partida = await CriarPartidaAsync(cenario, cenario.CriarDto(cenario.Grupo.Id));

        Assert.Equal(cenario.Usuario.Id, partida.CriadoPorUsuarioId);
        Assert.DoesNotContain(cenario.Atletas, atleta => atleta.Id == cenario.Usuario.AtletaId);
    }

    [Fact]
    public async Task CriarComResultadoAsync_SemDuplicidade_RegistraNormalmenteSemConfirmacao()
    {
        var cenario = Cenario.Criar(publico: true);

        var partida = await CriarPartidaAsync(cenario, cenario.CriarDto(cenario.Grupo.Id));

        Assert.Equal(cenario.Grupo.Id, partida.GrupoId);
        Assert.Single(cenario.Partidas.Partidas);
    }

    [Fact]
    public async Task CriarComResultadoAsync_SemDuplicidade_RetornaCriada()
    {
        var cenario = Cenario.Criar(publico: true);

        var resultado = await cenario.Servico.CriarComResultadoAsync(cenario.CriarDto(cenario.Grupo.Id));

        Assert.Equal(StatusCriacaoPartida.Criada, resultado.Status);
        Assert.NotNull(resultado.Partida);
        Assert.Null(resultado.Duplicidade);
        Assert.Null(resultado.Codigo);
        Assert.Equal(cenario.Grupo.Id, resultado.Partida!.GrupoId);
        Assert.Single(cenario.Partidas.Partidas);
    }

    [Fact]
    public async Task CriarComResultadoAsync_DuplicidadeSemConfirmacao_RetornaRequerConfirmacaoENaoSalva()
    {
        var cenario = Cenario.Criar(publico: true);
        var dto = cenario.CriarDto(cenario.Grupo.Id);
        await CriarPartidaAsync(cenario, dto);

        var resultado = await cenario.Servico.CriarComResultadoAsync(dto);

        Assert.Equal(StatusCriacaoPartida.RequerConfirmacaoDuplicidade, resultado.Status);
        Assert.Null(resultado.Partida);
        Assert.Equal(StatusCriacaoPartida.CodigoDuplicidadeConfirmar, resultado.Codigo);
        Assert.Equal("Já existe uma partida registrada hoje com os mesmos atletas e o mesmo placar.", resultado.Mensagem);
        Assert.NotNull(resultado.Duplicidade);
        Assert.True(resultado.Duplicidade!.RequerConfirmacao);
        Assert.Equal(StatusCriacaoPartida.CodigoDuplicidadeConfirmar, resultado.Duplicidade.Codigo);
        Assert.Single(cenario.Partidas.Partidas);
    }

    [Fact]
    public async Task CriarComResultadoAsync_DuplicidadeComConfirmacao_RetornaCriadaESalva()
    {
        var cenario = Cenario.Criar(publico: true);
        var dto = cenario.CriarDto(cenario.Grupo.Id);
        await CriarPartidaAsync(cenario, dto);

        var resultado = await cenario.Servico.CriarComResultadoAsync(dto with { ConfirmarDuplicidade = true });

        Assert.Equal(StatusCriacaoPartida.Criada, resultado.Status);
        Assert.NotNull(resultado.Partida);
        Assert.Null(resultado.Duplicidade);
        Assert.Equal(cenario.Grupo.Id, resultado.Partida!.GrupoId);
        Assert.Equal(2, cenario.Partidas.Partidas.Count);
    }

    [Fact]
    public async Task CriarComResultadoAsync_DuplicidadeSemConfirmacao_NaoSalva()
    {
        var cenario = Cenario.Criar(publico: true);
        var dto = cenario.CriarDto(cenario.Grupo.Id);
        await CriarPartidaAsync(cenario, dto);

        var resultado = await cenario.Servico.CriarComResultadoAsync(dto);

        Assert.Equal(StatusCriacaoPartida.RequerConfirmacaoDuplicidade, resultado.Status);
        Assert.Null(resultado.Partida);
        Assert.Equal("Já existe uma partida registrada hoje com os mesmos atletas e o mesmo placar.", resultado.Mensagem);
        Assert.Single(cenario.Partidas.Partidas);
        Assert.Equal(5, cenario.GruposAtletas.Vinculos.Count);
    }

    [Fact]
    public async Task CriarComResultadoAsync_DuplicidadeComConfirmacao_RegistraNovaPartida()
    {
        var cenario = Cenario.Criar(publico: true);
        var dto = cenario.CriarDto(cenario.Grupo.Id);
        await CriarPartidaAsync(cenario, dto);

        var partida = await CriarPartidaAsync(cenario, dto with { ConfirmarDuplicidade = true });

        Assert.Equal(cenario.Grupo.Id, partida.GrupoId);
        Assert.Equal(2, cenario.Partidas.Partidas.Count);
        Assert.Equal(5, cenario.GruposAtletas.Vinculos.Select(x => x.AtletaId).Distinct().Count());
        Assert.Equal(5, cenario.GruposAtletas.Vinculos.Count);
    }

    [Fact]
    public async Task CriarComResultadoAsync_DuplicidadeComPermissaoLegada_RegistraNovaPartida()
    {
        var cenario = Cenario.Criar(publico: true);
        var dto = cenario.CriarDto(cenario.Grupo.Id);
        await CriarPartidaAsync(cenario, dto);

        var partida = await CriarPartidaAsync(cenario, dto with { PermitirDuplicidade = true });

        Assert.Equal(cenario.Grupo.Id, partida.GrupoId);
        Assert.Equal(2, cenario.Partidas.Partidas.Count);
    }

    [Fact]
    public async Task CriarComResultadoAsync_DuplicidadeComAtletasInvertidosNaMesmaDupla_BloqueiaRegistro()
    {
        var cenario = Cenario.Criar(publico: true);
        await CriarPartidaAsync(cenario, cenario.CriarDto(cenario.Grupo.Id));
        var dtoInvertido = cenario.CriarDto(cenario.Grupo.Id) with
        {
            DuplaAAtleta1Id = cenario.Atletas[1].Id,
            DuplaAAtleta2Id = cenario.Atletas[0].Id,
            DuplaBAtleta1Id = cenario.Atletas[3].Id,
            DuplaBAtleta2Id = cenario.Atletas[2].Id
        };

        var resultado = await cenario.Servico.CriarComResultadoAsync(dtoInvertido);

        Assert.Equal(StatusCriacaoPartida.RequerConfirmacaoDuplicidade, resultado.Status);
        Assert.Null(resultado.Partida);
        Assert.Equal("Já existe uma partida registrada hoje com os mesmos atletas e o mesmo placar.", resultado.Mensagem);
        Assert.Single(cenario.Partidas.Partidas);
    }

    [Fact]
    public async Task CriarComResultadoAsync_PartidaParecidaComAtletaDiferente_RegistraNormalmente()
    {
        var cenario = Cenario.Criar(publico: true);
        await CriarPartidaAsync(cenario, cenario.CriarDto(cenario.Grupo.Id));
        var atletaDiferente = cenario.AdicionarAtleta("Eduardo Ramos");
        var dto = cenario.CriarDto(cenario.Grupo.Id) with
        {
            DuplaBAtleta2Id = atletaDiferente.Id
        };

        var partida = await CriarPartidaAsync(cenario, dto);

        Assert.Equal(cenario.Grupo.Id, partida.GrupoId);
        Assert.Equal(2, cenario.Partidas.Partidas.Count);
        Assert.Contains(cenario.GruposAtletas.Vinculos, vinculo => vinculo.AtletaId == atletaDiferente.Id);
    }

    [Fact]
    public async Task CriarComResultadoAsync_DuplasTrocandoDeLadoComPlacarInvertido_BloqueiaDuplicidade()
    {
        var cenario = Cenario.Criar(publico: true);
        var dto = cenario.CriarDto(cenario.Grupo.Id);
        await CriarPartidaAsync(cenario, dto);

        var resultado = await cenario.Servico.CriarComResultadoAsync(cenario.InverterLados(dto));

        Assert.Equal(StatusCriacaoPartida.RequerConfirmacaoDuplicidade, resultado.Status);
        Assert.Null(resultado.Partida);
        Assert.Equal("Já existe uma partida registrada hoje com os mesmos atletas e o mesmo placar.", resultado.Mensagem);
        Assert.Single(cenario.Partidas.Partidas);
    }

    [Fact]
    public async Task CriarComResultadoAsync_DuplasTrocandoDeLadoComPlacarInvertidoEConfirmacao_Salva()
    {
        var cenario = Cenario.Criar(publico: true);
        var dto = cenario.CriarDto(cenario.Grupo.Id);
        await CriarPartidaAsync(cenario, dto);

        var partida = await CriarPartidaAsync(cenario, cenario.InverterLados(dto) with { ConfirmarDuplicidade = true });

        Assert.Equal(cenario.Grupo.Id, partida.GrupoId);
        Assert.Equal(2, cenario.Partidas.Partidas.Count);
        Assert.Equal(5, cenario.GruposAtletas.Vinculos.Select(x => x.AtletaId).Distinct().Count());
        Assert.Equal(5, cenario.GruposAtletas.Vinculos.Count);
    }

    [Fact]
    public async Task CriarComResultadoAsync_DuplasTrocandoDeLadoComPlacarInvertidoEPermitirDuplicidade_Salva()
    {
        var cenario = Cenario.Criar(publico: true);
        var dto = cenario.CriarDto(cenario.Grupo.Id);
        await CriarPartidaAsync(cenario, dto);

        var partida = await CriarPartidaAsync(cenario, cenario.InverterLados(dto) with { PermitirDuplicidade = true });

        Assert.Equal(cenario.Grupo.Id, partida.GrupoId);
        Assert.Equal(2, cenario.Partidas.Partidas.Count);
    }

    [Fact]
    public async Task CriarComResultadoAsync_DuplasTrocandoDeLadoComPlacarDiferente_NaoBloqueiaDuplicidade()
    {
        var cenario = Cenario.Criar(publico: true);
        var dto = cenario.CriarDto(cenario.Grupo.Id);
        await CriarPartidaAsync(cenario, dto);
        var dtoLadosInvertidosComPlacarDiferente = cenario.InverterLados(dto) with
        {
            PlacarDuplaA = dto.PlacarDuplaB + 1,
            PlacarDuplaB = dto.PlacarDuplaA
        };

        var partida = await CriarPartidaAsync(cenario, dtoLadosInvertidosComPlacarDiferente);

        Assert.Equal(cenario.Grupo.Id, partida.GrupoId);
        Assert.Equal(2, cenario.Partidas.Partidas.Count);
    }

    [Fact]
    public async Task CriarComResultadoAsync_MesmaPartidaEmOutroGrupo_NaoBloqueiaDuplicidade()
    {
        var cenario = Cenario.Criar(publico: false);
        cenario.AdicionarMembro(cenario.AtletaUsuario, cenario.GrupoAlternativo);
        var dtoGrupoA = cenario.CriarDto(cenario.Grupo.Id);
        var dtoGrupoB = cenario.CriarDto(cenario.GrupoAlternativo.Id) with
        {
            DataPartida = dtoGrupoA.DataPartida
        };

        await CriarPartidaAsync(cenario, dtoGrupoA);
        var partidaGrupoB = await CriarPartidaAsync(cenario, dtoGrupoB);

        Assert.Equal(cenario.GrupoAlternativo.Id, partidaGrupoB.GrupoId);
        Assert.Equal(2, cenario.Partidas.Partidas.Count);
    }

    [Fact]
    public async Task CriarComResultadoAsync_ApenasResultadoDuplicado_RequerConfirmacaoDuplicidade()
    {
        var cenario = Cenario.Criar(publico: true);
        var dto = cenario.CriarDtoApenasResultado(cenario.Grupo.Id);
        await CriarPartidaAsync(cenario, dto);

        var resultado = await cenario.Servico.CriarComResultadoAsync(dto);

        Assert.Equal(StatusCriacaoPartida.RequerConfirmacaoDuplicidade, resultado.Status);
        Assert.Null(resultado.Partida);
        Assert.Equal("Já existe uma partida registrada hoje com os mesmos atletas e o mesmo placar.", resultado.Mensagem);
        Assert.Single(cenario.Partidas.Partidas);
    }

    [Fact]
    public async Task CriarComResultadoAsync_ApenasResultadoComDuplasInvertidas_RequerConfirmacaoDuplicidade()
    {
        var cenario = Cenario.Criar(publico: true);
        var dto = cenario.CriarDtoApenasResultado(cenario.Grupo.Id);
        await CriarPartidaAsync(cenario, dto);

        var resultado = await cenario.Servico.CriarComResultadoAsync(cenario.InverterLados(dto));

        Assert.Equal(StatusCriacaoPartida.RequerConfirmacaoDuplicidade, resultado.Status);
        Assert.Null(resultado.Partida);
        Assert.Equal("Já existe uma partida registrada hoje com os mesmos atletas e o mesmo placar.", resultado.Mensagem);
        Assert.Single(cenario.Partidas.Partidas);
    }

    [Fact]
    public async Task CriarComResultadoAsync_ApenasResultadoDuplicadoComConfirmacao_Salva()
    {
        var cenario = Cenario.Criar(publico: true);
        var dto = cenario.CriarDtoApenasResultado(cenario.Grupo.Id);
        await CriarPartidaAsync(cenario, dto);

        var partida = await CriarPartidaAsync(cenario, dto with { ConfirmarDuplicidade = true });

        Assert.Equal(cenario.Grupo.Id, partida.GrupoId);
        Assert.Equal(2, cenario.Partidas.Partidas.Count);
    }

    [Fact]
    public async Task CriarComResultadoAsync_ApenasResultadoComVencedorDiferente_NaoBloqueiaDuplicidade()
    {
        var cenario = Cenario.Criar(publico: true);
        var dto = cenario.CriarDtoApenasResultado(cenario.Grupo.Id);
        await CriarPartidaAsync(cenario, dto);

        var partida = await CriarPartidaAsync(cenario, dto with { DuplaVencedora = 2 });

        Assert.Equal(cenario.Grupo.Id, partida.GrupoId);
        Assert.Equal(2, cenario.Partidas.Partidas.Count);
    }

    [Fact]
    public async Task CriarComResultadoAsync_ApenasResultado_PermiteRegistrarSemPlacar()
    {
        var cenario = Cenario.Criar(publico: true);
        var dto = cenario.CriarDto(cenario.Grupo.Id) with
        {
            PlacarDuplaA = null,
            PlacarDuplaB = null,
            DuplaVencedora = 1,
            TipoRegistroResultado = TipoRegistroResultado.ApenasResultado
        };

        var partida = await CriarPartidaAsync(cenario, dto);

        Assert.Equal(StatusPartida.Encerrada, partida.Status);
        Assert.Null(partida.PlacarDuplaA);
        Assert.Null(partida.PlacarDuplaB);
        Assert.Equal(1, partida.DuplaVencedora);
        Assert.Equal(TipoRegistroResultado.ApenasResultado, partida.TipoRegistroResultado);
        Assert.False(partida.PossuiPlacarDetalhado);
    }

    [Fact]
    public async Task CriarComResultadoAsync_ApenasResultado_ExigeDuplaVencedora()
    {
        var cenario = Cenario.Criar(publico: true);
        var dto = cenario.CriarDto(cenario.Grupo.Id) with
        {
            PlacarDuplaA = null,
            PlacarDuplaB = null,
            DuplaVencedora = null,
            TipoRegistroResultado = TipoRegistroResultado.ApenasResultado
        };

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            CriarPartidaAsync(cenario, dto));

        Assert.Equal("Informe qual dupla venceu a partida.", excecao.Message);
        Assert.Empty(cenario.Partidas.Partidas);
    }

    [Fact]
    public async Task CriarComResultadoAsync_ApenasResultado_DuplaVencedoraInvalida_BloqueiaRegistro()
    {
        var cenario = Cenario.Criar(publico: true);
        var dto = cenario.CriarDto(cenario.Grupo.Id) with
        {
            PlacarDuplaA = null,
            PlacarDuplaB = null,
            DuplaVencedora = 3,
            TipoRegistroResultado = TipoRegistroResultado.ApenasResultado
        };

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            CriarPartidaAsync(cenario, dto));

        Assert.Equal("Informe qual dupla venceu a partida.", excecao.Message);
        Assert.Empty(cenario.Partidas.Partidas);
    }

    [Fact]
    public async Task CriarComResultadoAsync_PlacarDetalhadoEmpatado_BloqueiaRegistro()
    {
        var cenario = Cenario.Criar(publico: true);
        var dto = cenario.CriarDto(cenario.Grupo.Id) with
        {
            PlacarDuplaA = 21,
            PlacarDuplaB = 21,
            TipoRegistroResultado = TipoRegistroResultado.PlacarDetalhado
        };

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            CriarPartidaAsync(cenario, dto));

        Assert.Equal("A partida não pode terminar empatada.", excecao.Message);
        Assert.Empty(cenario.Partidas.Partidas);
    }

    [Fact]
    public async Task CriarComResultadoAsync_PlacarDetalhadoSemPontosMinimos_BloqueiaRegistro()
    {
        var cenario = Cenario.Criar(publico: true);
        var dto = cenario.CriarDto(cenario.Grupo.Id) with
        {
            PlacarDuplaA = Competicao.PontosMinimosPartidaPadrao - 1,
            PlacarDuplaB = Competicao.PontosMinimosPartidaPadrao - Competicao.DiferencaMinimaPartidaPadrao,
            TipoRegistroResultado = TipoRegistroResultado.PlacarDetalhado
        };

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            CriarPartidaAsync(cenario, dto));

        Assert.Equal($"A dupla vencedora deve alcançar no mínimo {Competicao.PontosMinimosPartidaPadrao} pontos.", excecao.Message);
        Assert.Empty(cenario.Partidas.Partidas);
    }

    [Fact]
    public async Task CriarComResultadoAsync_PlacarDetalhadoSemDiferencaMinima_BloqueiaRegistro()
    {
        var cenario = Cenario.Criar(publico: true);
        var dto = cenario.CriarDto(cenario.Grupo.Id) with
        {
            PlacarDuplaA = Competicao.PontosMinimosPartidaPadrao,
            PlacarDuplaB = Competicao.PontosMinimosPartidaPadrao - 1,
            TipoRegistroResultado = TipoRegistroResultado.PlacarDetalhado
        };

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            CriarPartidaAsync(cenario, dto));

        Assert.Equal($"A partida deve terminar com diferença mínima de {Competicao.DiferencaMinimaPartidaPadrao} pontos.", excecao.Message);
        Assert.Empty(cenario.Partidas.Partidas);
    }

    [Fact]
    public async Task CriarComResultadoAsync_CompeticaoComRegraCustomizada_UsaPontosMinimos()
    {
        var cenario = Cenario.Criar(publico: true);
        var categoria = await cenario.CriarCategoriaCampeonatoAsync(RegraCustomizada(pontosMinimosPartida: 25));
        var dto = cenario.CriarDtoCompeticao(categoria) with
        {
            PlacarDuplaA = 24,
            PlacarDuplaB = 20
        };

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            CriarPartidaAsync(cenario, dto));

        Assert.Equal("A dupla vencedora deve alcançar no mínimo 25 pontos.", excecao.Message);
        Assert.Empty(cenario.Partidas.Partidas);
    }

    [Fact]
    public async Task CriarComResultadoAsync_CompeticaoComRegraCustomizada_UsaDiferencaMinima()
    {
        var cenario = Cenario.Criar(publico: true);
        var categoria = await cenario.CriarCategoriaCampeonatoAsync(RegraCustomizada(diferencaMinimaPartida: 5));
        var dto = cenario.CriarDtoCompeticao(categoria) with
        {
            PlacarDuplaA = 21,
            PlacarDuplaB = 18
        };

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            CriarPartidaAsync(cenario, dto));

        Assert.Equal("A partida deve terminar com diferença mínima de 5 pontos.", excecao.Message);
        Assert.Empty(cenario.Partidas.Partidas);
    }

    [Fact]
    public async Task CriarComResultadoAsync_CompeticaoComRegraCustomizada_RespeitaPermiteEmpate()
    {
        var cenario = Cenario.Criar(publico: true);
        var categoria = await cenario.CriarCategoriaCampeonatoAsync(RegraCustomizada(permiteEmpate: true));
        var dto = cenario.CriarDtoCompeticao(categoria) with
        {
            PlacarDuplaA = 20,
            PlacarDuplaB = 20
        };

        var partida = await CriarPartidaAsync(cenario, dto);

        Assert.Equal(categoria.Id, partida.CategoriaCompeticaoId);
        Assert.Equal(20, partida.PlacarDuplaA);
        Assert.Equal(20, partida.PlacarDuplaB);
        Assert.Null(partida.DuplaVencedoraId);
        Assert.Null(partida.DuplaVencedora);
    }

    [Fact]
    public async Task CriarComResultadoAsync_CompeticaoSemRegraCustomizada_UsaRegraPadrao()
    {
        var cenario = Cenario.Criar(publico: true);
        var categoria = await cenario.CriarCategoriaCampeonatoAsync();
        var dto = cenario.CriarDtoCompeticao(categoria) with
        {
            PlacarDuplaA = Competicao.PontosMinimosPartidaPadrao - 1,
            PlacarDuplaB = Competicao.PontosMinimosPartidaPadrao - 3
        };

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            CriarPartidaAsync(cenario, dto));

        Assert.Equal($"A dupla vencedora deve alcançar no mínimo {Competicao.PontosMinimosPartidaPadrao} pontos.", excecao.Message);
        Assert.Empty(cenario.Partidas.Partidas);
    }

    [Fact]
    public async Task AtualizarBasicaAsync_CriadorDaPartidaPodeEditar()
    {
        var cenario = Cenario.Criar(publico: true);
        var partidaCriada = await CriarPartidaAsync(cenario, cenario.CriarDto(cenario.Grupo.Id));
        cenario.Pendencias.PartidasInicializadas.Clear();

        var partida = await cenario.Servico.AtualizarBasicaAsync(
            partidaCriada.Id,
            cenario.AtualizarBasicaDto(19, 21, duplaVencedora: 2));

        Assert.Equal(19, partida.PlacarDuplaA);
        Assert.Equal(21, partida.PlacarDuplaB);
        Assert.Equal(2, partida.DuplaVencedora);
        Assert.Equal(partidaCriada.Id, cenario.Pendencias.PartidasInicializadas.Single().PartidaId);
    }

    [Fact]
    public async Task AtualizarBasicaAsync_AdministradorPodeEditarPartidaDeTerceiro()
    {
        var cenario = Cenario.Criar(publico: true);
        var partidaCriada = await CriarPartidaAsync(cenario, cenario.CriarDto(cenario.Grupo.Id));
        cenario.Pendencias.PartidasInicializadas.Clear();
        cenario.Autorizacao.UsuarioAtual = new Usuario
        {
            Nome = "Administrador",
            Email = "admin@teste.com",
            Perfil = PerfilUsuario.Administrador
        };

        var partida = await cenario.Servico.AtualizarBasicaAsync(
            partidaCriada.Id,
            cenario.AtualizarBasicaDto(21, 19, duplaVencedora: 1));

        Assert.Equal(21, partida.PlacarDuplaA);
        Assert.Equal(19, partida.PlacarDuplaB);
        Assert.Equal(partidaCriada.Id, cenario.Pendencias.PartidasInicializadas.Single().PartidaId);
    }

    [Fact]
    public async Task AtualizarBasicaAsync_TerceiroNaoPodeEditar()
    {
        var cenario = Cenario.Criar(publico: true);
        var partidaCriada = await CriarPartidaAsync(cenario, cenario.CriarDto(cenario.Grupo.Id));
        cenario.Pendencias.PartidasInicializadas.Clear();
        cenario.Autorizacao.UsuarioAtual = new Usuario
        {
            Nome = "Terceiro",
            Email = "terceiro@teste.com",
            Perfil = PerfilUsuario.Atleta
        };

        var excecao = await Assert.ThrowsAsync<AcessoNegadoException>(() =>
            cenario.Servico.AtualizarBasicaAsync(
                partidaCriada.Id,
                cenario.AtualizarBasicaDto(21, 19, duplaVencedora: 1)));

        Assert.Equal("Você só pode editar partidas registradas por você.", excecao.Message);
        Assert.Empty(cenario.Pendencias.PartidasInicializadas);
    }

    [Fact]
    public async Task AtualizarBasicaAsync_SemGrupoNoPayloadPreservaGrupoStatusDataContextoERecompoePendencias()
    {
        var cenario = Cenario.Criar(publico: true);
        var dataPartida = new DateTime(2026, 6, 1, 18, 30, 0, DateTimeKind.Utc);
        var partidaCriada = await CriarPartidaAsync(
            cenario,
            cenario.CriarDto(cenario.Grupo.Id) with
            {
                DataPartida = dataPartida,
                Observacoes = "observacao original"
            });
        var partidaOriginal = cenario.Partidas.Partidas.Single(x => x.Id == partidaCriada.Id);
        var grupoOriginalId = partidaOriginal.GrupoId;
        var statusOriginal = partidaOriginal.Status;
        var categoriaOriginalId = partidaOriginal.CategoriaCompeticaoId;
        cenario.Pendencias.PartidasInicializadas.Clear();

        var partida = await cenario.Servico.AtualizarBasicaAsync(
            partidaCriada.Id,
            cenario.AtualizarBasicaDto(22, 20, duplaVencedora: 1));

        Assert.Equal(grupoOriginalId, partida.GrupoId);
        Assert.Equal(statusOriginal, partida.Status);
        Assert.Equal(categoriaOriginalId, partida.CategoriaCompeticaoId);
        Assert.Equal(dataPartida, partida.DataPartida);
        Assert.Equal("observacao original", partida.Observacoes);
        var inicializacao = cenario.Pendencias.PartidasInicializadas.Single();
        Assert.Equal(partidaCriada.Id, inicializacao.PartidaId);
        Assert.Equal(partidaOriginal.CriadoPorUsuarioId, inicializacao.UsuarioRegistradorId);
    }

    [Fact]
    public async Task AtualizarBasicaAsync_AlteraParaOutroGrupoPermitido()
    {
        var cenario = Cenario.Criar(publico: true);
        var partidaCriada = await CriarPartidaAsync(cenario, cenario.CriarDto(cenario.Grupo.Id));
        cenario.AdicionarMembro(cenario.AtletaUsuario, cenario.GrupoAlternativo);
        cenario.Pendencias.PartidasInicializadas.Clear();

        var partida = await cenario.Servico.AtualizarBasicaAsync(
            partidaCriada.Id,
            cenario.AtualizarBasicaDto(22, 20, duplaVencedora: 1) with
            {
                GrupoId = cenario.GrupoAlternativo.Id
            });

        Assert.Equal(cenario.GrupoAlternativo.Id, partida.GrupoId);
        Assert.All(cenario.Atletas, atleta =>
            Assert.Contains(cenario.GruposAtletas.Vinculos, vinculo =>
                vinculo.GrupoId == cenario.GrupoAlternativo.Id && vinculo.AtletaId == atleta.Id));
        Assert.Equal(partidaCriada.Id, cenario.Pendencias.PartidasInicializadas.Single().PartidaId);
    }

    [Fact]
    public async Task AtualizarBasicaAsync_GrupoPrivadoSemPermissaoBloqueiaAlteracao()
    {
        var cenario = Cenario.Criar(publico: false);
        cenario.GrupoAlternativo.Publico = false;
        var partidaCriada = await CriarPartidaAsync(cenario, cenario.CriarDto(cenario.Grupo.Id));
        cenario.Pendencias.PartidasInicializadas.Clear();

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.AtualizarBasicaAsync(
                partidaCriada.Id,
                cenario.AtualizarBasicaDto(22, 20, duplaVencedora: 1) with
                {
                    GrupoId = cenario.GrupoAlternativo.Id
                }));

        Assert.Equal("Você precisa fazer parte deste grupo para registrar partidas nele.", excecao.Message);
        Assert.Equal(cenario.Grupo.Id, cenario.Partidas.Partidas.Single(x => x.Id == partidaCriada.Id).GrupoId);
        Assert.Empty(cenario.Pendencias.PartidasInicializadas);
    }

    [Fact]
    public async Task AtualizarBasicaAsync_PartidaAgendadaPreservaStatusELimpaResultado()
    {
        var cenario = Cenario.Criar(publico: true);
        var partidaCriada = await CriarPartidaAsync(
            cenario,
            cenario.CriarDto(cenario.Grupo.Id) with
            {
                Status = StatusPartida.Agendada,
                PlacarDuplaA = null,
                PlacarDuplaB = null,
                DuplaVencedora = null,
                TipoRegistroResultado = null
            });
        cenario.Pendencias.PartidasInicializadas.Clear();

        var partida = await cenario.Servico.AtualizarBasicaAsync(
            partidaCriada.Id,
            cenario.AtualizarBasicaDto(21, 18, duplaVencedora: 1));

        Assert.Equal(StatusPartida.Agendada, partida.Status);
        Assert.Null(partida.PlacarDuplaA);
        Assert.Null(partida.PlacarDuplaB);
        Assert.Null(partida.DuplaVencedoraId);
        Assert.Equal(TipoRegistroResultado.PlacarDetalhado, partida.TipoRegistroResultado);
    }

    [Fact]
    public async Task AtualizarBasicaAsync_RespeitaRegrasDePlacarExistentes()
    {
        var cenario = Cenario.Criar(publico: true);
        var partidaCriada = await CriarPartidaAsync(cenario, cenario.CriarDto(cenario.Grupo.Id));
        cenario.Pendencias.PartidasInicializadas.Clear();

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.AtualizarBasicaAsync(
                partidaCriada.Id,
                cenario.AtualizarBasicaDto(
                    Competicao.PontosMinimosPartidaPadrao,
                    Competicao.PontosMinimosPartidaPadrao - 1,
                    duplaVencedora: 1)));

        Assert.Equal($"A partida deve terminar com diferença mínima de {Competicao.DiferencaMinimaPartidaPadrao} pontos.", excecao.Message);
        Assert.Empty(cenario.Pendencias.PartidasInicializadas);
    }

    [Fact]
    public async Task AtualizarBasicaAsync_NaoAcionaServicoDeRanking()
    {
        var cenario = Cenario.Criar(publico: true);
        var partidaCriada = await CriarPartidaAsync(cenario, cenario.CriarDto(cenario.Grupo.Id));
        cenario.Pendencias.PartidasInicializadas.Clear();

        await cenario.Servico.AtualizarBasicaAsync(
            partidaCriada.Id,
            cenario.AtualizarBasicaDto(21, 17, duplaVencedora: 1));

        Assert.Equal(0, cenario.Ranking.ConsultasPorGrupo);
    }

    [Fact]
    public async Task ListarMinhasAsync_IncluiPartidasDisputadasENaoApenasRegistradas()
    {
        var cenario = Cenario.Criar(publico: true);
        cenario.Atletas.Add(cenario.AtletaUsuario);
        await CriarPartidaAsync(cenario, cenario.CriarDto(cenario.Grupo.Id));
        var dtoComUsuarioAtleta = cenario.CriarDto(cenario.Grupo.Id) with
        {
            DuplaAAtleta1Id = cenario.AtletaUsuario.Id,
            ConfirmarDuplicidade = true
        };
        var partidaDisputada = await CriarPartidaAsync(cenario, dtoComUsuarioAtleta);

        var partidas = await cenario.Servico.ListarMinhasAsync();

        var partida = Assert.Single(partidas);
        Assert.Equal(partidaDisputada.Id, partida.Id);
        Assert.Contains(cenario.AtletaUsuario.Id, new[]
        {
            partida.DuplaAAtleta1Id,
            partida.DuplaAAtleta2Id,
            partida.DuplaBAtleta1Id,
            partida.DuplaBAtleta2Id
        });
    }

    [Fact]
    public async Task ListarRegistradasPorMimAsync_IncluiCriadasPeloUsuarioMesmoSemJogarENaoIncluiTerceiros()
    {
        var cenario = Cenario.Criar(publico: true);
        var partidaRegistradaPeloUsuario = await CriarPartidaAsync(cenario, cenario.CriarDto(cenario.Grupo.Id));
        var partidaDeTerceiro = CriarPartidaManual(
            cenario.Grupo,
            Guid.NewGuid(),
            cenario.Atletas[0],
            cenario.Atletas[1],
            cenario.Atletas[2],
            cenario.Atletas[3]);
        cenario.Partidas.Partidas.Add(partidaDeTerceiro);

        var partidas = await cenario.Servico.ListarRegistradasPorMimAsync();

        var partida = Assert.Single(partidas);
        Assert.Equal(partidaRegistradaPeloUsuario.Id, partida.Id);
        Assert.DoesNotContain(cenario.AtletaUsuario.Id, new[]
        {
            partida.DuplaAAtleta1Id,
            partida.DuplaAAtleta2Id,
            partida.DuplaBAtleta1Id,
            partida.DuplaBAtleta2Id
        });
    }

    [Fact]
    public async Task VerificarDuplicidadeAsync_NormalizaCaixaAcentosEspacosENomeOuApelido()
    {
        var cenario = Cenario.Criar(publico: true);
        cenario.Atletas[0].Nome = "Álan Silva";
        cenario.Atletas[0].Apelido = "Canhoto";
        var partidaCriada = await CriarPartidaAsync(cenario, cenario.CriarDto(cenario.Grupo.Id));

        var resultado = await cenario.Servico.VerificarDuplicidadeAsync(new VerificarDuplicidadePartidaDto(
            new VerificarDuplicidadePartidaDuplaDto("  canhoto  ", "BRUNO SOUZA", 21),
            new VerificarDuplicidadePartidaDuplaDto("carlos lima", "anda costa", 18),
            partidaCriada.DataPartida));

        Assert.True(resultado.ExisteDuplicidade);
        Assert.Equal(partidaCriada.Id, resultado.PartidaId);
    }

    [Fact]
    public async Task VerificarDuplicidadeAsync_DuplasInvertidasComPlacarInvertido_IdentificaDuplicidade()
    {
        var cenario = Cenario.Criar(publico: true);
        var partidaCriada = await CriarPartidaAsync(cenario, cenario.CriarDto(cenario.Grupo.Id));

        var resultado = await cenario.Servico.VerificarDuplicidadeAsync(new VerificarDuplicidadePartidaDto(
            new VerificarDuplicidadePartidaDuplaDto("Carlos Lima", "Anda Costa", 18),
            new VerificarDuplicidadePartidaDuplaDto("Alan Silva", "Bruno Souza", 21),
            partidaCriada.DataPartida));

        Assert.True(resultado.ExisteDuplicidade);
        Assert.Equal(partidaCriada.Id, resultado.PartidaId);
    }

    [Fact]
    public async Task VerificarDuplicidadeAsync_MesmosAtletasEmDataDiferente_NaoIdentificaDuplicidade()
    {
        var cenario = Cenario.Criar(publico: true);
        var partidaCriada = await CriarPartidaAsync(cenario, cenario.CriarDto(cenario.Grupo.Id));

        var resultado = await cenario.Servico.VerificarDuplicidadeAsync(new VerificarDuplicidadePartidaDto(
            new VerificarDuplicidadePartidaDuplaDto("Alan Silva", "Bruno Souza", 21),
            new VerificarDuplicidadePartidaDuplaDto("Carlos Lima", "Anda Costa", 18),
            partidaCriada.DataPartida!.Value.AddDays(1)));

        Assert.False(resultado.ExisteDuplicidade);
        Assert.Null(resultado.PartidaId);
    }

    [Fact]
    public void CriarPartidaDto_AceitaTipoRegistroResultadoComoTexto()
    {
        var opcoes = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        opcoes.Converters.Add(new TipoRegistroResultadoJsonConverter());

        var dto = JsonSerializer.Deserialize<CriarPartidaDto>(
            """
            {
              "competicaoId": null,
              "grupoId": "1cc97d33-8fec-49ef-afd4-599a5ae9745c",
              "categoriaCompeticaoId": null,
              "duplaAAtleta1Id": "b7c07a7d-e248-460c-b8fe-0198812f94a5",
              "duplaAAtleta2Id": "3048af30-806e-40d6-ab88-3d721f692e64",
              "duplaBAtleta1Id": "7c8e8e8d-9a3e-4d5a-8508-ecd92a9a9478",
              "duplaBAtleta2Id": "9d1324bf-8f2b-4dd5-a7c4-51f256842714",
              "status": 2,
              "placarDuplaA": null,
              "placarDuplaB": null,
              "duplaVencedora": 1,
              "tipoRegistroResultado": "ApenasResultado",
              "dataPartida": "2026-06-05T15:24:31.220Z",
              "confirmarDuplicidade": false
            }
            """,
            opcoes);

        Assert.NotNull(dto);
        Assert.Equal(TipoRegistroResultado.ApenasResultado, dto.TipoRegistroResultado);
        Assert.Null(dto.PlacarDuplaA);
        Assert.Null(dto.PlacarDuplaB);
        Assert.Equal(1, dto.DuplaVencedora);
    }

    private static async Task<PartidaDto> CriarPartidaAsync(Cenario cenario, CriarPartidaDto dto)
    {
        var resultado = await cenario.Servico.CriarComResultadoAsync(dto);

        Assert.Equal(StatusCriacaoPartida.Criada, resultado.Status);
        Assert.NotNull(resultado.Partida);

        return resultado.Partida;
    }

    private static Partida CriarPartidaManual(
        Grupo grupo,
        Guid usuarioCriadorId,
        Atleta duplaAAtleta1,
        Atleta duplaAAtleta2,
        Atleta duplaBAtleta1,
        Atleta duplaBAtleta2)
    {
        var duplaA = new Dupla
        {
            Nome = $"{duplaAAtleta1.Nome} / {duplaAAtleta2.Nome}",
            Atleta1Id = duplaAAtleta1.Id,
            Atleta1 = duplaAAtleta1,
            Atleta2Id = duplaAAtleta2.Id,
            Atleta2 = duplaAAtleta2
        };
        var duplaB = new Dupla
        {
            Nome = $"{duplaBAtleta1.Nome} / {duplaBAtleta2.Nome}",
            Atleta1Id = duplaBAtleta1.Id,
            Atleta1 = duplaBAtleta1,
            Atleta2Id = duplaBAtleta2.Id,
            Atleta2 = duplaBAtleta2
        };

        return new Partida
        {
            GrupoId = grupo.Id,
            Grupo = grupo,
            CriadoPorUsuarioId = usuarioCriadorId,
            DuplaAId = duplaA.Id,
            DuplaA = duplaA,
            DuplaBId = duplaB.Id,
            DuplaB = duplaB,
            DuplaVencedoraId = duplaA.Id,
            DuplaVencedora = duplaA,
            PlacarDuplaA = 21,
            PlacarDuplaB = 18,
            Status = StatusPartida.Encerrada,
            StatusAprovacao = StatusAprovacaoPartida.Aprovada,
            TipoRegistroResultado = TipoRegistroResultado.PlacarDetalhado,
            DataPartida = DateTime.UtcNow
        };
    }

    private static RegraCompeticao RegraCustomizada(
        int pontosMinimosPartida = 18,
        int diferencaMinimaPartida = 2,
        bool permiteEmpate = false)
        => new()
        {
            Nome = $"Regra customizada {Guid.NewGuid():N}",
            PontosMinimosPartida = pontosMinimosPartida,
            DiferencaMinimaPartida = diferencaMinimaPartida,
            PermiteEmpate = permiteEmpate,
            PontosVitoria = 3m,
            PontosDerrota = 0m,
            PontosParticipacao = 0m,
            PontosPrimeiroLugar = 0m,
            PontosSegundoLugar = 0m,
            PontosTerceiroLugar = 0m
        };

    private sealed class Cenario
    {
        private Cenario(bool publico, bool usuarioMembro)
        {
            Usuario = new Usuario
            {
                Nome = "Usuário Teste",
                Email = "usuario@teste.com",
                Perfil = PerfilUsuario.Atleta
            };
            Grupo = new Grupo { Nome = "AD7", Publico = publico, DataInicio = DateTime.UtcNow };
            GrupoAlternativo = new Grupo { Nome = "Arena 2", Publico = publico, DataInicio = DateTime.UtcNow };
            GrupoGeral = new Grupo { Nome = "Geral", Publico = true, DataInicio = DateTime.UtcNow };
            AtletaUsuario = new Atleta { Nome = "Usuário Teste" };
            Usuario.AtletaId = AtletaUsuario.Id;
            Atletas =
            [
                new Atleta { Nome = "Alan Silva" },
                new Atleta { Nome = "Bruno Souza" },
                new Atleta { Nome = "Carlos Lima" },
                new Atleta { Nome = "Anda Costa" }
            ];

            GruposAtletas = new GrupoAtletaRepositorioMemoria();
            if (usuarioMembro)
            {
                AdicionarMembro(AtletaUsuario);
            }

            Partidas = new PartidaRepositorioMemoria();
            Categorias = new CategoriaCompeticaoRepositorioMemoria();
            Inscricoes = new InscricaoCampeonatoRepositorioMemoria();
            Resolvedor = new ResolvedorAtletaDuplaMemoria(Atletas, GruposAtletas);
            Pendencias = new PendenciaServicoStub();
            Ranking = new RankingServicoStub();
            Autorizacao = new AutorizacaoUsuarioServicoStub(Usuario);

            Servico = new PartidaServico(
                Partidas,
                Categorias,
                new GrupoRepositorioStub([Grupo, GrupoAlternativo]),
                GruposAtletas,
                new GrupoPadraoServicoStub([Grupo, GrupoAlternativo], GrupoGeral),
                new DuplaRepositorioStub(),
                Inscricoes,
                new UnidadeTrabalhoStub(),
                Autorizacao,
                Resolvedor,
                Pendencias,
                Ranking,
                new MidiaPartidaServiceStub());
        }

        public Usuario Usuario { get; }
        public Grupo Grupo { get; }
        public Grupo GrupoAlternativo { get; }
        public Grupo GrupoGeral { get; }
        public Atleta AtletaUsuario { get; }
        public List<Atleta> Atletas { get; }
        public PartidaRepositorioMemoria Partidas { get; }
        public GrupoAtletaRepositorioMemoria GruposAtletas { get; }
        public CategoriaCompeticaoRepositorioMemoria Categorias { get; }
        public InscricaoCampeonatoRepositorioMemoria Inscricoes { get; }
        public ResolvedorAtletaDuplaMemoria Resolvedor { get; }
        public PendenciaServicoStub Pendencias { get; }
        public RankingServicoStub Ranking { get; }
        public AutorizacaoUsuarioServicoStub Autorizacao { get; }
        public PartidaServico Servico { get; }

        public static Cenario Criar(bool publico, bool usuarioMembro = true) => new(publico, usuarioMembro);

        public void AdicionarMembro(Atleta atleta, Grupo? grupo = null)
        {
            var grupoEfetivo = grupo ?? Grupo;
            GruposAtletas.Vinculos.Add(new GrupoAtleta { GrupoId = grupoEfetivo.Id, AtletaId = atleta.Id, Atleta = atleta });
        }

        public Atleta AdicionarAtleta(string nome)
        {
            var atleta = new Atleta { Nome = nome };
            Atletas.Add(atleta);
            return atleta;
        }

        public async Task<CategoriaCompeticao> CriarCategoriaCampeonatoAsync(RegraCompeticao? regraCompeticao = null)
        {
            Usuario.Perfil = PerfilUsuario.Organizador;
            var competicao = new Competicao
            {
                Nome = "Circuito QNF",
                Tipo = TipoCompeticao.Campeonato,
                DataInicio = DateTime.UtcNow.Date,
                UsuarioOrganizadorId = Usuario.Id,
                RegraCompeticaoId = regraCompeticao?.Id,
                RegraCompeticao = regraCompeticao
            };
            var categoria = new CategoriaCompeticao
            {
                Nome = "Open",
                CompeticaoId = competicao.Id,
                Competicao = competicao,
                Genero = GeneroCategoria.Misto,
                Nivel = NivelCategoria.Livre
            };
            categoria.AprovarTabelaJogos(Usuario.Id, DateTime.UtcNow);
            competicao.Categorias.Add(categoria);
            Categorias.Categorias.Add(categoria);

            var duplaA = await Resolvedor.ObterOuCriarDuplaAsync(Atletas[0], Atletas[1]);
            var duplaB = await Resolvedor.ObterOuCriarDuplaAsync(Atletas[2], Atletas[3]);
            Inscricoes.AdicionarAtiva(competicao, categoria, duplaA);
            Inscricoes.AdicionarAtiva(competicao, categoria, duplaB);
            return categoria;
        }

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
                DuplaVencedora: null,
                TipoRegistroResultado: TipoRegistroResultado.PlacarDetalhado,
                DataPartida: DateTime.UtcNow,
                Observacoes: null);

        public CriarPartidaDto CriarDtoCompeticao(CategoriaCompeticao categoria)
            => CriarDto(grupoId: null) with
            {
                CompeticaoId = categoria.CompeticaoId,
                GrupoId = null,
                CategoriaCompeticaoId = categoria.Id,
                FaseCampeonato = "Fase classificatória"
            };

        public CriarPartidaDto CriarDtoApenasResultado(Guid? grupoId)
            => CriarDto(grupoId) with
            {
                PlacarDuplaA = null,
                PlacarDuplaB = null,
                DuplaVencedora = 1,
                TipoRegistroResultado = TipoRegistroResultado.ApenasResultado
            };

        public AtualizarPartidaBasicaDto AtualizarBasicaDto(int? placarDuplaA, int? placarDuplaB, int? duplaVencedora)
            => new(
                GrupoId: null,
                DuplaAAtleta1Id: Atletas[0].Id,
                DuplaAAtleta1Nome: null,
                DuplaAAtleta2Id: Atletas[1].Id,
                DuplaAAtleta2Nome: null,
                DuplaBAtleta1Id: Atletas[2].Id,
                DuplaBAtleta1Nome: null,
                DuplaBAtleta2Id: Atletas[3].Id,
                DuplaBAtleta2Nome: null,
                PlacarDuplaA: placarDuplaA,
                PlacarDuplaB: placarDuplaB,
                DuplaVencedora: duplaVencedora,
                TipoRegistroResultado: placarDuplaA.HasValue || placarDuplaB.HasValue
                    ? TipoRegistroResultado.PlacarDetalhado
                    : TipoRegistroResultado.ApenasResultado);

        public CriarPartidaDto InverterLados(CriarPartidaDto dto)
            => dto with
            {
                DuplaAAtleta1Id = dto.DuplaBAtleta1Id,
                DuplaAAtleta1Nome = dto.DuplaBAtleta1Nome,
                DuplaAAtleta2Id = dto.DuplaBAtleta2Id,
                DuplaAAtleta2Nome = dto.DuplaBAtleta2Nome,
                DuplaBAtleta1Id = dto.DuplaAAtleta1Id,
                DuplaBAtleta1Nome = dto.DuplaAAtleta1Nome,
                DuplaBAtleta2Id = dto.DuplaAAtleta2Id,
                DuplaBAtleta2Nome = dto.DuplaAAtleta2Nome,
                PlacarDuplaA = dto.PlacarDuplaB,
                PlacarDuplaB = dto.PlacarDuplaA,
                DuplaVencedora = dto.DuplaVencedora switch
                {
                    1 => 2,
                    2 => 1,
                    _ => dto.DuplaVencedora
                }
            };
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

    public sealed class PartidaRepositorioMemoria : IPartidaRepositorio
    {
        public List<Partida> Partidas { get; } = [];

        public Task<IReadOnlyList<Partida>> ListarPorDiaAsync(DateTime inicioUtc, DateTime fimUtc, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Partida>>(
                Partidas
                    .Where(x => (x.DataPartida ?? x.DataCriacao) >= inicioUtc && (x.DataPartida ?? x.DataCriacao) < fimUtc)
                    .ToList());

        public Task<Partida?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Partidas.FirstOrDefault(x => x.Id == id));

        public Task AdicionarAsync(Partida partida, CancellationToken cancellationToken = default)
        {
            Partidas.Add(partida);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Partida>> ListarPorCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<int> ContarRegistradasAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<IReadOnlyList<Partida>> ListarPorGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>(Partidas.Where(x => x.GrupoId == grupoId).ToList());
        public Task<IReadOnlyList<Partida>> ListarPorCategoriaAsync(Guid categoriaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Partida>>(Partidas.Where(x =>
                x.DuplaA?.Atleta1Id == atletaId ||
                x.DuplaA?.Atleta2Id == atletaId ||
                x.DuplaB?.Atleta1Id == atletaId ||
                x.DuplaB?.Atleta2Id == atletaId).ToList());
        public Task<IReadOnlyList<Partida>> ListarPorUsuarioCriadorAsync(Guid usuarioId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>(Partidas.Where(x => x.CriadoPorUsuarioId == usuarioId).ToList());
        public Task<IReadOnlyList<Partida>> ListarAdministracaoAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>(Partidas);
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
        public void Remover(Partida partida) => Partidas.Remove(partida);
    }

    private sealed class GrupoPadraoServicoStub(IReadOnlyList<Grupo> grupos, Grupo grupoGeral) : IGrupoPadraoServico
    {
        public string NomeGrupoGeral => "Geral";
        public Task<Grupo> ObterOuCriarGrupoGeralAsync(CancellationToken cancellationToken = default) => Task.FromResult(grupoGeral);
        public Task<Grupo> ResolverGrupoRegistroPartidaAsync(Guid? grupoId, string? nomeNovoGrupo, CancellationToken cancellationToken = default)
            => Task.FromResult(grupoId.HasValue ? grupos.First(x => x.Id == grupoId.Value) : grupoGeral);
        public Task ValidarNomeDisponivelOuAcessivelAsync(string nome, Guid? grupoIgnoradoId = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class GrupoRepositorioStub(IReadOnlyList<Grupo> grupos) : IGrupoRepositorio
    {
        public Task<Grupo?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Grupo?>(grupos.FirstOrDefault(x => x.Id == id));
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
        public Usuario UsuarioAtual { get; set; } = usuario;

        public Task<Usuario?> ObterUsuarioAtualAsync(CancellationToken cancellationToken = default) => Task.FromResult<Usuario?>(UsuarioAtual);
        public Task<Usuario> ObterUsuarioAtualObrigatorioAsync(CancellationToken cancellationToken = default) => Task.FromResult(UsuarioAtual);
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

    private sealed class CategoriaCompeticaoRepositorioMemoria : ICategoriaCompeticaoRepositorio
    {
        public List<CategoriaCompeticao> Categorias { get; } = [];

        public Task<IReadOnlyList<CategoriaCompeticao>> ListarPorCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<CategoriaCompeticao>>(Categorias.Where(x => x.CompeticaoId == competicaoId).ToList());

        public Task<IReadOnlyList<CategoriaCompeticao>> ListarDisponiveisParaVinculoAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<CategoriaCompeticao>>(Categorias);

        public Task<CategoriaCompeticao?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Categorias.FirstOrDefault(x => x.Id == id));

        public Task AdicionarAsync(CategoriaCompeticao categoria, CancellationToken cancellationToken = default)
        {
            Categorias.Add(categoria);
            return Task.CompletedTask;
        }

        public void Atualizar(CategoriaCompeticao categoria) { }
        public void Remover(CategoriaCompeticao categoria) => Categorias.Remove(categoria);
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

    private sealed class InscricaoCampeonatoRepositorioMemoria : IInscricaoCampeonatoRepositorio
    {
        public List<InscricaoCampeonato> Inscricoes { get; } = [];

        public void AdicionarAtiva(Competicao competicao, CategoriaCompeticao categoria, Dupla dupla)
        {
            Inscricoes.Add(new InscricaoCampeonato
            {
                CompeticaoId = competicao.Id,
                Competicao = competicao,
                CategoriaCompeticaoId = categoria.Id,
                CategoriaCompeticao = categoria,
                DuplaId = dupla.Id,
                Dupla = dupla,
                Status = StatusInscricaoCampeonato.Ativa
            });
        }

        public Task<IReadOnlyList<InscricaoCampeonato>> ListarPorCampeonatoAsync(Guid campeonatoId, Guid? categoriaId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<InscricaoCampeonato>>(
                Inscricoes
                    .Where(x => x.CompeticaoId == campeonatoId && (!categoriaId.HasValue || x.CategoriaCompeticaoId == categoriaId.Value))
                    .ToList());

        public Task<int> ContarPorCategoriaAsync(Guid categoriaId, Guid? ignorarInscricaoId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(Inscricoes.Count(x =>
                x.CategoriaCompeticaoId == categoriaId &&
                (!ignorarInscricaoId.HasValue || x.Id != ignorarInscricaoId.Value)));

        public Task<InscricaoCampeonato?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Inscricoes.FirstOrDefault(x => x.Id == id));

        public Task<InscricaoCampeonato?> ObterDuplicadaAsync(Guid categoriaId, Guid duplaId, CancellationToken cancellationToken = default)
            => Task.FromResult(Inscricoes.FirstOrDefault(x => x.CategoriaCompeticaoId == categoriaId && x.DuplaId == duplaId));

        public Task<IReadOnlyList<InscricaoCampeonato>> ListarPorDuplasParaAtualizacaoAsync(IReadOnlyCollection<Guid> duplaIds, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<InscricaoCampeonato>>(Inscricoes.Where(x => duplaIds.Contains(x.DuplaId)).ToList());

        public Task AdicionarAsync(InscricaoCampeonato inscricao, CancellationToken cancellationToken = default)
        {
            Inscricoes.Add(inscricao);
            return Task.CompletedTask;
        }

        public void Atualizar(InscricaoCampeonato inscricao) { }
        public void Remover(InscricaoCampeonato inscricao) => Inscricoes.Remove(inscricao);
    }

    private sealed class PendenciaServicoStub : IPendenciaServico
    {
        public List<(Guid PartidaId, Guid UsuarioRegistradorId)> PartidasInicializadas { get; } = [];

        public Task InicializarFluxoPartidaAsync(Partida partida, Guid usuarioRegistradorId, CancellationToken cancellationToken = default)
        {
            PartidasInicializadas.Add((partida.Id, usuarioRegistradorId));
            return Task.CompletedTask;
        }

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
        public int ConsultasPorGrupo { get; private set; }

        public Task<IReadOnlyList<RankingCategoriaDto>> ListarAtletasPorGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default)
        {
            ConsultasPorGrupo++;
            return Task.FromResult<IReadOnlyList<RankingCategoriaDto>>([]);
        }

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
