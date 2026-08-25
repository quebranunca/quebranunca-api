namespace PlataformaFutevolei.Dominio.Entidades;

public class ControleRateLimit
{
    public string Chave { get; set; } = string.Empty;
    public DateTime JanelaInicioUtc { get; set; }
    public int Contador { get; set; }
    public DateTime ExpiraEmUtc { get; set; }
}
