using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlataformaFutevolei.Dominio.Entidades;

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Mapeamentos;

public class LigaMapeamento : IEntityTypeConfiguration<Liga>
{
    public void Configure(EntityTypeBuilder<Liga> builder)
    {
        builder.ToTable("ligas");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Descricao).HasColumnName("descricao").HasMaxLength(1000);
        builder.Property(x => x.DataCriacao).HasColumnName("data_criacao").IsRequired();
        builder.Property(x => x.DataAtualizacao).HasColumnName("data_atualizacao").IsRequired();

        builder.HasIndex(x => x.Nome).IsUnique();
    }
}
