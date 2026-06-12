using PlataformaFutevolei.Dominio.Entidades;
using Xunit;

namespace PlataformaFutevolei.Aplicacao.Tests;

public class ConviteCadastroTests
{
    private static readonly DateTime Agora = new(2026, 6, 12, 15, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void FoiUtilizado_SemUso_RetornaFalse()
    {
        var convite = CriarConvite();

        Assert.False(convite.FoiUtilizado());
    }

    [Fact]
    public void FoiUtilizado_ComUso_RetornaTrue()
    {
        var convite = CriarConvite();
        convite.UsadoEmUtc = Agora;

        Assert.True(convite.FoiUtilizado());
    }

    [Fact]
    public void EstaExpirado_ComDataAtualAposExpiracao_RetornaTrue()
    {
        var convite = CriarConvite(expiraEmUtc: Agora.AddMinutes(-1));

        Assert.True(convite.EstaExpirado(Agora));
    }

    [Fact]
    public void EstaExpirado_ComDataAtualAntesDaExpiracao_RetornaFalse()
    {
        var convite = CriarConvite(expiraEmUtc: Agora.AddMinutes(1));

        Assert.False(convite.EstaExpirado(Agora));
    }

    [Fact]
    public void PodeSerUsado_AtivoNaoExpiradoENaoUtilizado_RetornaTrue()
    {
        var convite = CriarConvite();

        Assert.True(convite.PodeSerUsado(Agora));
    }

    [Fact]
    public void PodeSerUsado_Inativo_RetornaFalse()
    {
        var convite = CriarConvite();
        convite.Ativo = false;

        Assert.False(convite.PodeSerUsado(Agora));
    }

    [Fact]
    public void PodeSerUsado_Expirado_RetornaFalse()
    {
        var convite = CriarConvite(expiraEmUtc: Agora.AddSeconds(-1));

        Assert.False(convite.PodeSerUsado(Agora));
    }

    [Fact]
    public void PodeSerUsado_Utilizado_RetornaFalse()
    {
        var convite = CriarConvite();
        convite.UsadoEmUtc = Agora;

        Assert.False(convite.PodeSerUsado(Agora));
    }

    [Theory]
    [InlineData("Ativo")]
    [InlineData("Expirado")]
    [InlineData("Cancelado")]
    [InlineData("Usado")]
    public void ObterSituacao_ComEstadosDoConvite_RetornaSituacaoAtual(string situacaoEsperada)
    {
        var convite = situacaoEsperada switch
        {
            "Expirado" => CriarConvite(expiraEmUtc: Agora.AddSeconds(-1)),
            "Cancelado" => CriarConvite(ativo: false),
            "Usado" => CriarConvite(usadoEmUtc: Agora.AddMinutes(-5)),
            _ => CriarConvite()
        };

        Assert.Equal(situacaoEsperada, convite.ObterSituacao(Agora));
    }

    [Fact]
    public void ObterSituacao_UtilizadoEInativo_PriorizaUsado()
    {
        var convite = CriarConvite(ativo: false, usadoEmUtc: Agora.AddMinutes(-5));

        Assert.Equal("Usado", convite.ObterSituacao(Agora));
    }

    [Theory]
    [InlineData(null, null, null, "Pendente")]
    [InlineData("2026-06-12T15:00:00Z", null, "Falha SMTP", "Falhou")]
    [InlineData("2026-06-12T15:00:00Z", "2026-06-12T15:00:00Z", null, "Enviado")]
    public void ObterSituacaoEnvioEmail_ComEstadosDeEnvio_RetornaSituacaoAtual(
        string? tentativa,
        string? enviado,
        string? erro,
        string esperado)
    {
        var convite = CriarConvite();
        convite.UltimaTentativaEnvioEmailEmUtc = ConverterData(tentativa);
        convite.EmailEnviadoEmUtc = ConverterData(enviado);
        convite.ErroEnvioEmail = erro;

        Assert.Equal(esperado, convite.ObterSituacaoEnvioEmail());
    }

    [Theory]
    [InlineData(null, null, null, "Pendente")]
    [InlineData("2026-06-12T15:00:00Z", null, "Falha WhatsApp", "Falhou")]
    [InlineData("2026-06-12T15:00:00Z", "2026-06-12T15:00:00Z", null, "Enviado")]
    public void ObterSituacaoEnvioWhatsapp_ComEstadosDeEnvio_RetornaSituacaoAtual(
        string? tentativa,
        string? enviado,
        string? erro,
        string esperado)
    {
        var convite = CriarConvite();
        convite.UltimaTentativaEnvioWhatsappEmUtc = ConverterData(tentativa);
        convite.WhatsappEnviadoEmUtc = ConverterData(enviado);
        convite.ErroEnvioWhatsapp = erro;

        Assert.Equal(esperado, convite.ObterSituacaoEnvioWhatsapp());
    }

    [Fact]
    public void MarcarComoUtilizado_ComData_PreencheUsoELimpaCodigo()
    {
        var convite = CriarConvite();
        convite.DefinirCodigoConvite("123-456", "hash");

        convite.MarcarComoUtilizado(Agora);

        Assert.Equal(Agora, convite.UsadoEmUtc);
        Assert.Null(convite.CodigoConvite);
        Assert.Equal("hash", convite.CodigoConviteHash);
        Assert.True(convite.Ativo);
    }

    [Fact]
    public void Desativar_QuandoChamado_MarcaComoInativo()
    {
        var convite = CriarConvite();

        convite.Desativar();

        Assert.False(convite.Ativo);
    }

    [Fact]
    public void DefinirCodigoConvite_ComCodigoEHash_SalvaValores()
    {
        var convite = CriarConvite();

        convite.DefinirCodigoConvite("123-456", "hash-normalizado");

        Assert.Equal("123-456", convite.CodigoConvite);
        Assert.Equal("hash-normalizado", convite.CodigoConviteHash);
    }

    [Fact]
    public void RegistrarEnvioEmailComSucesso_ComErroAnterior_PreencheDatasELimpaErro()
    {
        var convite = CriarConvite();
        convite.ErroEnvioEmail = "erro anterior";

        convite.RegistrarEnvioEmailComSucesso(Agora);

        Assert.Equal(Agora, convite.UltimaTentativaEnvioEmailEmUtc);
        Assert.Equal(Agora, convite.EmailEnviadoEmUtc);
        Assert.Null(convite.ErroEnvioEmail);
    }

    [Theory]
    [InlineData(" Falha SMTP ", "Falha SMTP")]
    [InlineData(null, "Falha ao enviar o e-mail do convite.")]
    [InlineData("   ", "Falha ao enviar o e-mail do convite.")]
    public void RegistrarFalhaEnvioEmail_ComMensagem_PreencheTentativaEErro(string? erro, string esperado)
    {
        var convite = CriarConvite();
        convite.EmailEnviadoEmUtc = Agora.AddMinutes(-10);

        convite.RegistrarFalhaEnvioEmail(erro, Agora);

        Assert.Equal(Agora, convite.UltimaTentativaEnvioEmailEmUtc);
        Assert.Null(convite.EmailEnviadoEmUtc);
        Assert.Equal(esperado, convite.ErroEnvioEmail);
    }

    [Fact]
    public void RegistrarEnvioWhatsappComSucesso_ComErroAnterior_PreencheDatasELimpaErro()
    {
        var convite = CriarConvite();
        convite.ErroEnvioWhatsapp = "erro anterior";

        convite.RegistrarEnvioWhatsappComSucesso(Agora);

        Assert.Equal(Agora, convite.UltimaTentativaEnvioWhatsappEmUtc);
        Assert.Equal(Agora, convite.WhatsappEnviadoEmUtc);
        Assert.Null(convite.ErroEnvioWhatsapp);
    }

    [Theory]
    [InlineData(" Falha no provedor ", "Falha no provedor")]
    [InlineData(null, "Falha ao enviar o WhatsApp do convite.")]
    [InlineData("   ", "Falha ao enviar o WhatsApp do convite.")]
    public void RegistrarFalhaEnvioWhatsapp_ComMensagem_PreencheTentativaEErro(string? erro, string esperado)
    {
        var convite = CriarConvite();
        convite.WhatsappEnviadoEmUtc = Agora.AddMinutes(-10);

        convite.RegistrarFalhaEnvioWhatsapp(erro, Agora);

        Assert.Equal(Agora, convite.UltimaTentativaEnvioWhatsappEmUtc);
        Assert.Null(convite.WhatsappEnviadoEmUtc);
        Assert.Equal(esperado, convite.ErroEnvioWhatsapp);
    }

    private static ConviteCadastro CriarConvite(
        DateTime? expiraEmUtc = null,
        bool ativo = true,
        DateTime? usadoEmUtc = null)
        => new()
        {
            Email = "atleta@example.com",
            IdentificadorPublico = Guid.NewGuid().ToString("N"),
            ExpiraEmUtc = expiraEmUtc ?? Agora.AddHours(1),
            Ativo = ativo,
            UsadoEmUtc = usadoEmUtc
        };

    private static DateTime? ConverterData(string? valor)
        => valor is null ? null : DateTime.Parse(valor).ToUniversalTime();
}
