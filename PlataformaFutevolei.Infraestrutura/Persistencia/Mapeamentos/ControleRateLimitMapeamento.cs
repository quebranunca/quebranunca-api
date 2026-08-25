using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlataformaFutevolei.Dominio.Entidades;

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Mapeamentos;

public sealed class ControleRateLimitMapeamento : IEntityTypeConfiguration<ControleRateLimit>
{
    public void Configure(EntityTypeBuilder<ControleRateLimit> builder)
    {
        builder.ToTable("controles_rate_limit");
        builder.HasKey(x => x.Chave);
        builder.Property(x => x.Chave).HasColumnName("chave").HasMaxLength(128);
        builder.Property(x => x.JanelaInicioUtc).HasColumnName("janela_inicio_utc").IsRequired();
        builder.Property(x => x.Contador).HasColumnName("contador").IsRequired();
        builder.Property(x => x.ExpiraEmUtc).HasColumnName("expira_em_utc").IsRequired();
        builder.HasIndex(x => x.ExpiraEmUtc);
    }
}
