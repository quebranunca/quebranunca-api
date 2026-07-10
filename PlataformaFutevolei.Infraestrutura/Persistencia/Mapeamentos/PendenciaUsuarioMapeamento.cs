using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Mapeamentos;

public class PendenciaUsuarioMapeamento : IEntityTypeConfiguration<PendenciaUsuario>
{
    public void Configure(EntityTypeBuilder<PendenciaUsuario> builder)
    {
        builder.ToTable("pendencias_usuarios");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Tipo).HasColumnName("tipo").HasConversion<int>().IsRequired();
        builder.Property(x => x.UsuarioId).HasColumnName("usuario_id").IsRequired();
        builder.Property(x => x.AtletaId).HasColumnName("atleta_id");
        builder.Property(x => x.PartidaId).HasColumnName("partida_id");
        builder.Property(x => x.SolicitacaoCancelamentoPartidaId).HasColumnName("solicitacao_cancelamento_partida_id");
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<int>().HasDefaultValue(StatusPendenciaUsuario.Pendente).IsRequired();
        builder.Property(x => x.DataConclusao).HasColumnName("data_conclusao");
        builder.Property(x => x.Observacao).HasColumnName("observacao").HasMaxLength(1000);
        builder.Property(x => x.EmailInformado).HasColumnName("email_informado").HasMaxLength(150);
        builder.Property(x => x.DataCriacao).HasColumnName("data_criacao").IsRequired();
        builder.Property(x => x.DataAtualizacao).HasColumnName("data_atualizacao").IsRequired();

        builder.HasOne(x => x.Usuario)
            .WithMany()
            .HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Atleta)
            .WithMany()
            .HasForeignKey(x => x.AtletaId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Partida)
            .WithMany(x => x.Pendencias)
            .HasForeignKey(x => x.PartidaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.SolicitacaoCancelamentoPartida)
            .WithMany(x => x.Pendencias)
            .HasForeignKey(x => x.SolicitacaoCancelamentoPartidaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.UsuarioId);
        builder.HasIndex(x => x.AtletaId);
        builder.HasIndex(x => x.PartidaId);
        builder.HasIndex(x => x.SolicitacaoCancelamentoPartidaId);
        builder.HasIndex(x => new { x.UsuarioId, x.Status });
        builder.HasIndex(x => new { x.SolicitacaoCancelamentoPartidaId, x.AtletaId })
            .IsUnique()
            .HasDatabaseName("ix_pendencias_usuarios_cancelamento_atleta")
            .HasFilter("solicitacao_cancelamento_partida_id IS NOT NULL AND atleta_id IS NOT NULL");
    }
}
