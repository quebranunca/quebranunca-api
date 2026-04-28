using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlataformaFutevolei.Dominio.Entidades;

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Mapeamentos;

public class GrupoAtletaMapeamento : IEntityTypeConfiguration<GrupoAtleta>
{
    public void Configure(EntityTypeBuilder<GrupoAtleta> builder)
    {
        builder.ToTable("grupos_atletas");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CompeticaoId).HasColumnName("competicao_id").IsRequired();
        builder.Property(x => x.AtletaId).HasColumnName("atleta_id").IsRequired();
        builder.Property(x => x.DataCriacao).HasColumnName("data_criacao").IsRequired();
        builder.Property(x => x.DataAtualizacao).HasColumnName("data_atualizacao").IsRequired();

        builder.HasOne(x => x.Competicao)
            .WithMany(x => x.GrupoAtletas)
            .HasForeignKey(x => x.CompeticaoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Atleta)
            .WithMany(x => x.Grupos)
            .HasForeignKey(x => x.AtletaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.CompeticaoId);
        builder.HasIndex(x => x.AtletaId);
        builder.HasIndex(x => new { x.CompeticaoId, x.AtletaId }).IsUnique();
    }
}
