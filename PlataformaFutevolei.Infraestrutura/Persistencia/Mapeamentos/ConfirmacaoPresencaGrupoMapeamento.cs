using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlataformaFutevolei.Dominio.Entidades;

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Mapeamentos;

public class ConfirmacaoPresencaGrupoMapeamento : IEntityTypeConfiguration<ConfirmacaoPresencaGrupo>
{
    public void Configure(EntityTypeBuilder<ConfirmacaoPresencaGrupo> builder)
    {
        builder.ToTable("confirmacoes_presenca_grupos");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.EncontroGrupoId).HasColumnName("encontro_grupo_id").IsRequired();
        builder.Property(x => x.AtletaId).HasColumnName("atleta_id").IsRequired();
        builder.Property(x => x.CodigoAcesso).HasColumnName("codigo_acesso").HasMaxLength(64).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<int>().IsRequired();
        builder.Property(x => x.ExpiraEmUtc).HasColumnName("expira_em_utc").IsRequired();
        builder.Property(x => x.RespondidaEmUtc).HasColumnName("respondida_em_utc");
        builder.Property(x => x.TentativasEnvioWhatsapp).HasColumnName("tentativas_envio_whatsapp").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.UltimaTentativaEnvioWhatsappEmUtc).HasColumnName("ultima_tentativa_envio_whatsapp_em_utc");
        builder.Property(x => x.WhatsappEnviadoEmUtc).HasColumnName("whatsapp_enviado_em_utc");
        builder.Property(x => x.WhatsappMensagemId).HasColumnName("whatsapp_mensagem_id").HasMaxLength(200);
        builder.Property(x => x.ErroEnvioWhatsapp).HasColumnName("erro_envio_whatsapp").HasMaxLength(500);
        builder.Property(x => x.DataCriacao).HasColumnName("data_criacao").IsRequired();
        builder.Property(x => x.DataAtualizacao).HasColumnName("data_atualizacao").IsRequired();

        builder.HasOne(x => x.EncontroGrupo)
            .WithMany(x => x.Confirmacoes)
            .HasForeignKey(x => x.EncontroGrupoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Atleta)
            .WithMany(x => x.ConfirmacoesPresencaGrupo)
            .HasForeignKey(x => x.AtletaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.CodigoAcesso).IsUnique();
        builder.HasIndex(x => new { x.EncontroGrupoId, x.AtletaId }).IsUnique();
        builder.HasIndex(x => new { x.EncontroGrupoId, x.Status });
    }
}
