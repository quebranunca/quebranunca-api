using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlataformaFutevolei.Dominio.Entidades;

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Mapeamentos;

public class DuplaMapeamento : IEntityTypeConfiguration<Dupla>
{
    public void Configure(EntityTypeBuilder<Dupla> builder)
    {
        builder.ToTable("duplas", tabela =>
        {
            tabela.HasCheckConstraint("ck_duplas_atletas_diferentes", "\"atleta1_id\" <> \"atleta2_id\"");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Atleta1Id).HasColumnName("atleta1_id").IsRequired();
        builder.Property(x => x.Atleta2Id).HasColumnName("atleta2_id").IsRequired();
        builder.Property(x => x.DataCriacao).HasColumnName("data_criacao").IsRequired();
        builder.Property(x => x.DataAtualizacao).HasColumnName("data_atualizacao").IsRequired();

        builder.HasOne(x => x.Atleta1)
            .WithMany(x => x.DuplasComoAtleta1)
            .HasForeignKey(x => x.Atleta1Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Atleta2)
            .WithMany(x => x.DuplasComoAtleta2)
            .HasForeignKey(x => x.Atleta2Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.Atleta1Id);
        builder.HasIndex(x => x.Atleta2Id);
        builder.HasIndex(x => new { x.Atleta1Id, x.Atleta2Id }).IsUnique();
    }
}
