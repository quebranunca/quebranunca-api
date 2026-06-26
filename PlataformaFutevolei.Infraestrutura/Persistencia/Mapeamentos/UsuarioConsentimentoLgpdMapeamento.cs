using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlataformaFutevolei.Dominio.Entidades;

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Mapeamentos;

public class UsuarioConsentimentoLgpdMapeamento : IEntityTypeConfiguration<UsuarioConsentimentoLgpd>
{
    public void Configure(EntityTypeBuilder<UsuarioConsentimentoLgpd> builder)
    {
        builder.ToTable("usuarios_consentimentos_lgpd");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.UsuarioId)
            .HasColumnName("usuario_id")
            .IsRequired();

        builder.Property(x => x.VersaoTermosUso)
            .HasColumnName("versao_termos_uso")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.VersaoPoliticaPrivacidade)
            .HasColumnName("versao_politica_privacidade")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.AceitouPoliticaPrivacidade)
            .HasColumnName("aceitou_politica_privacidade")
            .IsRequired();

        builder.Property(x => x.AceitouTermosUso)
            .HasColumnName("aceitou_termos_uso")
            .IsRequired();

        builder.Property(x => x.AceitouUsoLocalizacao)
            .HasColumnName("aceitou_uso_localizacao")
            .IsRequired();

        builder.Property(x => x.AceitouUsoImagem)
            .HasColumnName("aceitou_uso_imagem")
            .IsRequired();

        builder.Property(x => x.AceitoEm)
            .HasColumnName("aceito_em_utc")
            .IsRequired();

        builder.Property(x => x.DeclarouMaioridadeEmUtc)
            .HasColumnName("declarou_maioridade_em_utc");

        builder.Property(x => x.AceitouMarketing)
            .HasColumnName("aceitou_marketing")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.ConsentimentoMarketingEmUtc)
            .HasColumnName("consentimento_marketing_em_utc");

        builder.Property(x => x.Origem)
            .HasColumnName("origem")
            .HasMaxLength(80);

        builder.Property(x => x.IpAddress)
            .HasColumnName("ip_address")
            .HasMaxLength(64);

        builder.Property(x => x.UserAgent)
            .HasColumnName("user_agent")
            .HasMaxLength(512);

        builder.Property(x => x.DataCriacao)
            .HasColumnName("data_criacao")
            .IsRequired();

        builder.Property(x => x.DataAtualizacao)
            .HasColumnName("data_atualizacao")
            .IsRequired();

        builder.HasIndex(x => new { x.UsuarioId, x.VersaoPoliticaPrivacidade });
        builder.HasIndex(x => new { x.UsuarioId, x.VersaoTermosUso });

        builder.HasOne(x => x.Usuario)
            .WithMany(x => x.ConsentimentosLgpd)
            .HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
