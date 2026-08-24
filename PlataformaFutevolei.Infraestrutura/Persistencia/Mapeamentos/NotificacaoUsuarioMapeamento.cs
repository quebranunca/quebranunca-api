using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Mapeamentos;

public class NotificacaoUsuarioMapeamento : IEntityTypeConfiguration<NotificacaoUsuario>
{
    public void Configure(EntityTypeBuilder<NotificacaoUsuario> builder)
    {
        builder.ToTable("notificacoes_usuarios");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.UsuarioId).HasColumnName("usuario_id").IsRequired();
        builder.Property(x => x.Origem).HasColumnName("origem").HasMaxLength(80).IsRequired();
        builder.Property(x => x.ChaveIdempotencia).HasColumnName("chave_idempotencia").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Tipo).HasColumnName("tipo").HasConversion<int>().IsRequired();
        builder.Property(x => x.Prioridade).HasColumnName("prioridade").HasConversion<int>().IsRequired();
        builder.Property(x => x.Titulo).HasColumnName("titulo").HasMaxLength(160).IsRequired();
        builder.Property(x => x.Mensagem).HasColumnName("mensagem").HasMaxLength(1000).IsRequired();
        builder.Property(x => x.LinkAcao).HasColumnName("link_acao").HasMaxLength(300);
        builder.Property(x => x.TextoAcao).HasColumnName("texto_acao").HasMaxLength(80);
        builder.Property(x => x.ReferenciaTipo).HasColumnName("referencia_tipo").HasMaxLength(80);
        builder.Property(x => x.ReferenciaId).HasColumnName("referencia_id").HasMaxLength(100);
        builder.Property(x => x.LidaEmUtc).HasColumnName("lida_em_utc");
        builder.Property(x => x.ArquivadaEmUtc).HasColumnName("arquivada_em_utc");
        builder.Property(x => x.DataCriacao).HasColumnName("data_criacao").IsRequired();
        builder.Property(x => x.DataAtualizacao).HasColumnName("data_atualizacao").IsRequired();

        builder.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.UsuarioId, x.Origem, x.ChaveIdempotencia }).IsUnique();
        builder.HasIndex(x => new { x.UsuarioId, x.LidaEmUtc, x.ArquivadaEmUtc });
    }
}
