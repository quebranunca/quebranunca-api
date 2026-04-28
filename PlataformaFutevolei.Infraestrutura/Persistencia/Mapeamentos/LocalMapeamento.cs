using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlataformaFutevolei.Dominio.Entidades;

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Mapeamentos;

public class LocalMapeamento : IEntityTypeConfiguration<Local>
{
    public void Configure(EntityTypeBuilder<Local> builder)
    {
        builder.ToTable("locais");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Tipo).HasColumnName("tipo").IsRequired();
        builder.Property(x => x.QuantidadeQuadras).HasColumnName("quantidade_quadras").IsRequired();
        builder.Property(x => x.UsuarioCriadorId).HasColumnName("usuario_criador_id");
        builder.Property(x => x.DataCriacao).HasColumnName("data_criacao").IsRequired();
        builder.Property(x => x.DataAtualizacao).HasColumnName("data_atualizacao").IsRequired();

        builder.HasOne(x => x.UsuarioCriador)
            .WithMany()
            .HasForeignKey(x => x.UsuarioCriadorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.Nome).IsUnique();
        builder.HasIndex(x => x.UsuarioCriadorId);
    }
}
