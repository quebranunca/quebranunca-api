using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlataformaFutevolei.Dominio.Entidades;

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Mapeamentos;

public class ConviteCadastroMapeamento : IEntityTypeConfiguration<ConviteCadastro>
{
    public void Configure(EntityTypeBuilder<ConviteCadastro> builder)
    {
        builder.ToTable("convites_cadastro");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Email).HasColumnName("email").HasMaxLength(150).IsRequired();
        builder.Property(x => x.Telefone).HasColumnName("telefone").HasMaxLength(30);
        builder.Property(x => x.IdentificadorPublico).HasColumnName("identificador_publico").HasMaxLength(40).IsRequired();
        builder.Property(x => x.CodigoConviteHash).HasColumnName("codigo_convite_hash").HasMaxLength(64);
        builder.Property(x => x.PerfilDestino).HasColumnName("perfil_destino").IsRequired();
        builder.Property(x => x.ExpiraEmUtc).HasColumnName("expira_em_utc").IsRequired();
        builder.Property(x => x.UsadoEmUtc).HasColumnName("usado_em_utc");
        builder.Property(x => x.Ativo).HasColumnName("ativo").IsRequired();
        builder.Property(x => x.CriadoPorUsuarioId).HasColumnName("criado_por_usuario_id").IsRequired();
        builder.Property(x => x.CanalEnvio).HasColumnName("canal_envio").HasMaxLength(50);
        builder.Property(x => x.UltimaTentativaEnvioEmailEmUtc).HasColumnName("ultima_tentativa_envio_email_em_utc");
        builder.Property(x => x.EmailEnviadoEmUtc).HasColumnName("email_enviado_em_utc");
        builder.Property(x => x.ErroEnvioEmail).HasColumnName("erro_envio_email").HasMaxLength(500);
        builder.Property(x => x.UltimaTentativaEnvioWhatsappEmUtc).HasColumnName("ultima_tentativa_envio_whatsapp_em_utc");
        builder.Property(x => x.WhatsappEnviadoEmUtc).HasColumnName("whatsapp_enviado_em_utc");
        builder.Property(x => x.ErroEnvioWhatsapp).HasColumnName("erro_envio_whatsapp").HasMaxLength(500);
        builder.Property(x => x.DataCriacao).HasColumnName("data_criacao").IsRequired();
        builder.Property(x => x.DataAtualizacao).HasColumnName("data_atualizacao").IsRequired();

        builder.HasIndex(x => x.IdentificadorPublico).IsUnique();
        builder.HasIndex(x => x.Email);
        builder.HasIndex(x => new { x.Ativo, x.ExpiraEmUtc });

        builder.HasOne(x => x.CriadoPorUsuario)
            .WithMany()
            .HasForeignKey(x => x.CriadoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
