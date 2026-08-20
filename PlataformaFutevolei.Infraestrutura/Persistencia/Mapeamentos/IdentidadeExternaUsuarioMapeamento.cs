using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlataformaFutevolei.Dominio.Entidades;

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Mapeamentos;

public sealed class IdentidadeExternaUsuarioMapeamento : IEntityTypeConfiguration<IdentidadeExternaUsuario>
{
    public void Configure(EntityTypeBuilder<IdentidadeExternaUsuario> builder)
    {
        builder.ToTable("usuarios_identidades_externas");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.UsuarioId).HasColumnName("usuario_id").IsRequired();
        builder.Property(x => x.Emissor).HasColumnName("emissor").HasMaxLength(500).IsRequired();
        builder.Property(x => x.Subject).HasColumnName("subject").HasMaxLength(200).IsRequired();
        builder.Property(x => x.VinculadaEmUtc).HasColumnName("vinculada_em_utc").IsRequired();
        builder.Property(x => x.UltimoLoginEmUtc).HasColumnName("ultimo_login_em_utc").IsRequired();
        builder.Property(x => x.DataCriacao).HasColumnName("data_criacao").IsRequired();
        builder.Property(x => x.DataAtualizacao).HasColumnName("data_atualizacao").IsRequired();
        builder.HasIndex(x => new { x.Emissor, x.Subject }).IsUnique();
        builder.HasIndex(x => new { x.UsuarioId, x.Emissor }).IsUnique();
        builder.HasOne(x => x.Usuario)
            .WithMany()
            .HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
