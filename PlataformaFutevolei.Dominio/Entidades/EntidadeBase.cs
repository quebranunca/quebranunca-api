namespace PlataformaFutevolei.Dominio.Entidades;

public abstract class EntidadeBase
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime DataCriacao { get; protected set; } = DateTime.UtcNow;
    public DateTime DataAtualizacao { get; protected set; } = DateTime.UtcNow;

    public void AtualizarDataModificacao()
    {
        DataAtualizacao = DateTime.UtcNow;
    }
}
