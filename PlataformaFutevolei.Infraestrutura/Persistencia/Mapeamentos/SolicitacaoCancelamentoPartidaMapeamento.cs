using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Mapeamentos;

public class SolicitacaoCancelamentoPartidaMapeamento : IEntityTypeConfiguration<SolicitacaoCancelamentoPartida>
{
    public void Configure(EntityTypeBuilder<SolicitacaoCancelamentoPartida> builder)
    {
        builder.ToTable("solicitacoes_cancelamento_partidas");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.PartidaId).HasColumnName("partida_id").IsRequired();
        builder.Property(x => x.SolicitadaPorUsuarioId).HasColumnName("solicitada_por_usuario_id").IsRequired();
        builder.Property(x => x.SolicitadaEm).HasColumnName("solicitada_em").IsRequired();
        builder.Property(x => x.DuplaSolicitanteId).HasColumnName("dupla_solicitante_id").IsRequired();
        builder.Property(x => x.DuplaAdversariaId).HasColumnName("dupla_adversaria_id").IsRequired();
        builder.Property(x => x.Motivo).HasColumnName("motivo").HasConversion<int>().IsRequired();
        builder.Property(x => x.Observacao).HasColumnName("observacao").HasMaxLength(200);
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<int>().HasDefaultValue(StatusSolicitacaoCancelamentoPartida.Pendente).IsRequired();
        builder.Property(x => x.RespondidaPorUsuarioId).HasColumnName("respondida_por_usuario_id");
        builder.Property(x => x.RespondidaEm).HasColumnName("respondida_em");
        builder.Property(x => x.CanceladaPeloSolicitanteEm).HasColumnName("cancelada_pelo_solicitante_em");
        builder.Property(x => x.DataCriacao).HasColumnName("data_criacao").IsRequired();
        builder.Property(x => x.DataAtualizacao).HasColumnName("data_atualizacao").IsRequired();

        builder.HasOne(x => x.Partida)
            .WithMany(x => x.SolicitacoesCancelamento)
            .HasForeignKey(x => x.PartidaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.SolicitadaPorUsuario)
            .WithMany()
            .HasForeignKey(x => x.SolicitadaPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.RespondidaPorUsuario)
            .WithMany()
            .HasForeignKey(x => x.RespondidaPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.DuplaSolicitante)
            .WithMany()
            .HasForeignKey(x => x.DuplaSolicitanteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.DuplaAdversaria)
            .WithMany()
            .HasForeignKey(x => x.DuplaAdversariaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.PartidaId);
        builder.HasIndex(x => x.SolicitadaPorUsuarioId);
        builder.HasIndex(x => x.RespondidaPorUsuarioId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.DuplaSolicitanteId);
        builder.HasIndex(x => x.DuplaAdversariaId);
        builder.HasIndex(x => x.PartidaId)
            .IsUnique()
            .HasDatabaseName("ix_solicitacoes_cancelamento_partidas_partida_pendente")
            .HasFilter("status = 1");
    }
}
