using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlataformaFutevolei.Dominio.Entidades;

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Mapeamentos;

public class ArenaResponsavelMapeamento : IEntityTypeConfiguration<ArenaResponsavel>
{
    public void Configure(EntityTypeBuilder<ArenaResponsavel> builder)
    {
        builder.ToTable("arena_responsaveis");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ArenaId).HasColumnName("arena_id").IsRequired();
        builder.Property(x => x.UsuarioId).HasColumnName("usuario_id").IsRequired();
        builder.Property(x => x.Papel).HasColumnName("papel").IsRequired();
        builder.Property(x => x.Ativo).HasColumnName("ativo").HasDefaultValue(true).IsRequired();
        builder.Property(x => x.DataCriacao).HasColumnName("data_criacao").IsRequired();
        builder.Property(x => x.DataAtualizacao).HasColumnName("data_atualizacao").IsRequired();

        builder.HasOne(x => x.Arena)
            .WithMany(x => x.Responsaveis)
            .HasForeignKey(x => x.ArenaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Usuario)
            .WithMany(x => x.ArenasResponsaveis)
            .HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ArenaId, x.UsuarioId, x.Papel }).IsUnique();
        builder.HasIndex(x => x.UsuarioId);
    }
}
