using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlataformaFutevolei.Dominio.Entidades;

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Mapeamentos;

public sealed class SessaoUsuarioMapeamento : IEntityTypeConfiguration<SessaoUsuario>
{
    public void Configure(EntityTypeBuilder<SessaoUsuario> builder)
    {
        builder.ToTable("usuarios_sessoes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.UsuarioId).HasColumnName("usuario_id").IsRequired();
        builder.Property(x => x.RefreshTokenHash).HasColumnName("refresh_token_hash").HasMaxLength(255).IsRequired();
        builder.Property(x => x.RefreshTokenHash).IsConcurrencyToken();
        builder.Property(x => x.ExpiraEmUtc).HasColumnName("expira_em_utc").IsRequired();
        builder.Property(x => x.UltimoUsoEmUtc).HasColumnName("ultimo_uso_em_utc");
        builder.Property(x => x.RevogadaEmUtc).HasColumnName("revogada_em_utc");
        builder.Property(x => x.UserAgent).HasColumnName("user_agent").HasMaxLength(500);
        builder.Property(x => x.IpAddress).HasColumnName("ip_address").HasMaxLength(64);
        builder.Property(x => x.DataCriacao).HasColumnName("data_criacao").IsRequired();
        builder.Property(x => x.DataAtualizacao).HasColumnName("data_atualizacao").IsRequired();
        builder.HasIndex(x => x.UsuarioId);
        builder.HasIndex(x => new { x.UsuarioId, x.RevogadaEmUtc, x.ExpiraEmUtc });
        builder.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
    }
}
