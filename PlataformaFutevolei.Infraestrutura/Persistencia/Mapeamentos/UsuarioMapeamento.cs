using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlataformaFutevolei.Dominio.Entidades;

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Mapeamentos;

public class UsuarioMapeamento : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("usuarios");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(150).IsRequired();
        builder.Property(x => x.Email).HasColumnName("email").HasMaxLength(150).IsRequired();
        builder.Property(x => x.SenhaHash).HasColumnName("senha_hash").HasMaxLength(255).IsRequired();
        builder.Property(x => x.CodigoLoginHash)
            .HasColumnName("codigo_login_hash")
            .HasMaxLength(255);
        builder.Property(x => x.CodigoLoginExpiraEmUtc)
            .HasColumnName("codigo_login_expira_em_utc");
        builder.Property(x => x.CodigoRedefinicaoSenhaHash)
            .HasColumnName("codigo_redefinicao_senha_hash")
            .HasMaxLength(255);
        builder.Property(x => x.CodigoRedefinicaoSenhaExpiraEmUtc)
            .HasColumnName("codigo_redefinicao_senha_expira_em_utc");
        builder.Property(x => x.RefreshTokenHash)
            .HasColumnName("refresh_token_hash")
            .HasMaxLength(255);
        builder.Property(x => x.RefreshTokenExpiraEmUtc)
            .HasColumnName("refresh_token_expira_em_utc");
        builder.Property(x => x.Perfil).HasColumnName("perfil").IsRequired();
        builder.Property(x => x.AtletaId).HasColumnName("atleta_id");
        builder.Property(x => x.Ativo).HasColumnName("ativo").IsRequired();
        builder.Property(x => x.DataCriacao).HasColumnName("data_criacao").IsRequired();
        builder.Property(x => x.DataAtualizacao).HasColumnName("data_atualizacao").IsRequired();

        builder.HasIndex(x => x.Email).IsUnique();
        builder.HasIndex(x => x.AtletaId).IsUnique();

        builder.HasOne(x => x.Atleta)
            .WithOne(x => x.Usuario)
            .HasForeignKey<Usuario>(x => x.AtletaId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
