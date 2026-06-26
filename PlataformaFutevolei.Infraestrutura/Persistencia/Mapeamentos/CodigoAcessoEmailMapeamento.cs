using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlataformaFutevolei.Dominio.Entidades;

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Mapeamentos;

public class CodigoAcessoEmailMapeamento : IEntityTypeConfiguration<CodigoAcessoEmail>
{
    public void Configure(EntityTypeBuilder<CodigoAcessoEmail> builder)
    {
        builder.ToTable("codigos_acesso_email");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.EmailNormalizado)
            .HasColumnName("email_normalizado")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.CodigoHash)
            .HasColumnName("codigo_hash")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Finalidade)
            .HasColumnName("finalidade")
            .IsRequired();

        builder.Property(x => x.ExpiraEmUtc)
            .HasColumnName("expira_em_utc")
            .IsRequired();

        builder.Property(x => x.Tentativas)
            .HasColumnName("tentativas")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.ConsumidoEmUtc)
            .HasColumnName("consumido_em_utc");

        builder.Property(x => x.UltimoEnvioEmUtc)
            .HasColumnName("ultimo_envio_em_utc")
            .IsRequired();

        builder.Property(x => x.CadastroTokenHash)
            .HasColumnName("cadastro_token_hash")
            .HasMaxLength(64);

        builder.Property(x => x.CadastroTokenExpiraEmUtc)
            .HasColumnName("cadastro_token_expira_em_utc");

        builder.Property(x => x.DataCriacao)
            .HasColumnName("data_criacao")
            .IsRequired();

        builder.Property(x => x.DataAtualizacao)
            .HasColumnName("data_atualizacao")
            .IsRequired();

        builder.HasIndex(x => new { x.EmailNormalizado, x.Finalidade, x.ConsumidoEmUtc });
        builder.HasIndex(x => x.ExpiraEmUtc);
        builder.HasIndex(x => x.CadastroTokenHash);
    }
}
