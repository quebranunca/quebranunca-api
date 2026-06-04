using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlataformaFutevolei.Dominio.Entidades;

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Mapeamentos;

public class AtletaMedidasMapeamento : IEntityTypeConfiguration<AtletaMedidas>
{
    public void Configure(EntityTypeBuilder<AtletaMedidas> builder)
    {
        builder.ToTable("atletas_medidas");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.AtletaId).HasColumnName("atleta_id").IsRequired();
        builder.Property(x => x.Camiseta).HasColumnName("camiseta").HasMaxLength(10);
        builder.Property(x => x.Regata).HasColumnName("regata").HasMaxLength(10);
        builder.Property(x => x.Short).HasColumnName("short").HasMaxLength(10);
        builder.Property(x => x.Sunga).HasColumnName("sunga").HasMaxLength(10);
        builder.Property(x => x.Top).HasColumnName("top").HasMaxLength(10);
        builder.Property(x => x.Biquini).HasColumnName("biquini").HasMaxLength(10);
        builder.Property(x => x.AtualizadoEm).HasColumnName("atualizado_em").IsRequired();
        builder.Property(x => x.DataCriacao).HasColumnName("data_criacao").IsRequired();
        builder.Property(x => x.DataAtualizacao).HasColumnName("data_atualizacao").IsRequired();

        builder.HasOne(x => x.Atleta)
            .WithOne(x => x.Medidas)
            .HasForeignKey<AtletaMedidas>(x => x.AtletaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.AtletaId).IsUnique();
    }
}
