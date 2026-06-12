using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;
using Xunit;

namespace PlataformaFutevolei.Aplicacao.Tests;

public class PartidaDominioTests
{
    [Fact]
    public void ObterDuplaVencedoraPorPlacar_SemPlacarDetalhado_RetornaVencedoraInformada()
    {
        var duplaVencedoraId = Guid.NewGuid();
        var partida = CriarPartidaEncerrada();
        partida.TipoRegistroResultado = TipoRegistroResultado.ApenasResultado;
        partida.PlacarDuplaA = null;
        partida.PlacarDuplaB = null;
        partida.DuplaVencedoraId = duplaVencedoraId;

        Assert.Equal(duplaVencedoraId, partida.ObterDuplaVencedoraPorPlacar());
    }

    [Fact]
    public void ObterMaiorPlacar_SemPlacarDetalhado_RetornaZero()
    {
        var partida = new Partida { TipoRegistroResultado = TipoRegistroResultado.ApenasResultado };

        Assert.Equal(0, partida.ObterMaiorPlacar());
    }

    [Fact]
    public void ObterDiferencaPlacar_SemPlacarDetalhado_RetornaZero()
    {
        var partida = new Partida { TipoRegistroResultado = TipoRegistroResultado.ApenasResultado };

        Assert.Equal(0, partida.ObterDiferencaPlacar());
    }

    [Fact]
    public void CalcularPontosRankingVitoria_SemDuplaVencedora_RetornaZero()
    {
        var partida = new Partida { DuplaVencedoraId = null };

        Assert.Equal(0m, partida.CalcularPontosRankingVitoria());
    }

    [Fact]
    public void CalcularPontosRankingVitoria_ComPesoCustomizado_RetornaPontosComPeso()
    {
        var partida = new Partida
        {
            DuplaVencedoraId = Guid.NewGuid(),
            StatusAprovacao = StatusAprovacaoPartida.Aprovada
        };

        Assert.Equal(6m, partida.CalcularPontosRankingVitoria(pesoRanking: 3m));
    }

    [Fact]
    public void CalcularPontosRankingVitoria_ComPartidaDeGrupo_IgnoraPesoCustomizado()
    {
        var partida = new Partida
        {
            GrupoId = Guid.NewGuid(),
            DuplaVencedoraId = Guid.NewGuid(),
            StatusAprovacao = StatusAprovacaoPartida.Aprovada
        };

        Assert.Equal(2m, partida.CalcularPontosRankingVitoria(pesoRanking: 3m));
    }

    [Fact]
    public void CalcularBonusAprovacaoPendenteRanking_SemDuplaVencedora_RetornaZero()
    {
        var partida = new Partida
        {
            DuplaVencedoraId = null,
            StatusAprovacao = StatusAprovacaoPartida.PendenteAprovacao
        };

        Assert.Equal(0m, partida.CalcularBonusAprovacaoPendenteRanking());
    }

    [Fact]
    public void CalcularBonusAprovacaoPendenteRanking_ComPesoCustomizado_RetornaBonusComPeso()
    {
        var partida = new Partida
        {
            DuplaVencedoraId = Guid.NewGuid(),
            StatusAprovacao = StatusAprovacaoPartida.PendenteAprovacao
        };

        Assert.Equal(2.5m, partida.CalcularBonusAprovacaoPendenteRanking(pesoRanking: 2.5m));
    }

    [Fact]
    public void CalcularBonusAprovacaoPendenteRanking_ComPartidaAprovada_RetornaZero()
    {
        var partida = new Partida
        {
            DuplaVencedoraId = Guid.NewGuid(),
            StatusAprovacao = StatusAprovacaoPartida.Aprovada
        };

        Assert.Equal(0m, partida.CalcularBonusAprovacaoPendenteRanking(pesoRanking: 2.5m));
    }

    [Fact]
    public void AtualizarMidia_ComDadosValidos_PreencheUrlPublicIdETipo()
    {
        var partida = new Partida();

        partida.AtualizarMidia("https://cdn.example.com/midia.jpg", "partidas/midia", MidiaPartidaTipo.Imagem);

        Assert.Equal("https://cdn.example.com/midia.jpg", partida.MidiaUrl);
        Assert.Equal("partidas/midia", partida.MidiaPublicId);
        Assert.Equal(MidiaPartidaTipo.Imagem, partida.MidiaTipo);
    }

    [Fact]
    public void RemoverMidia_ComMidiaPreenchida_LimpaUrlPublicIdETipo()
    {
        var partida = new Partida
        {
            MidiaUrl = "https://cdn.example.com/midia.mp4",
            MidiaPublicId = "partidas/video",
            MidiaTipo = MidiaPartidaTipo.Video
        };

        partida.RemoverMidia();

        Assert.Null(partida.MidiaUrl);
        Assert.Null(partida.MidiaPublicId);
        Assert.Null(partida.MidiaTipo);
    }

    private static Partida CriarPartidaEncerrada()
        => new()
        {
            DuplaAId = Guid.NewGuid(),
            DuplaBId = Guid.NewGuid(),
            Status = StatusPartida.Encerrada
        };
}
