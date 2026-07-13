using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlataformaFutevolei.Dominio.Entidades;

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Mapeamentos;

public class HistoricoPartidaMapeamento : IEntityTypeConfiguration<HistoricoPartida>
{
    public void Configure(EntityTypeBuilder<HistoricoPartida> builder)
    {
        builder.ToTable("historicos_partidas");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.PartidaIdOriginal).HasColumnName("partida_id_original").IsRequired();
        builder.Property(x => x.Acao).HasColumnName("acao").HasMaxLength(80).IsRequired();
        builder.Property(x => x.UsuarioResponsavelId).HasColumnName("usuario_responsavel_id").IsRequired();
        builder.Property(x => x.DataHoraUtc).HasColumnName("data_hora_utc").IsRequired();
        builder.Property(x => x.Motivo).HasColumnName("motivo").HasMaxLength(500);
        builder.Property(x => x.SnapshotJson).HasColumnName("snapshot_json").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.CorrelationId).HasColumnName("correlation_id").HasMaxLength(120);
        builder.Property(x => x.DataCriacao).HasColumnName("data_criacao").IsRequired();
        builder.Property(x => x.DataAtualizacao).HasColumnName("data_atualizacao").IsRequired();

        builder.HasIndex(x => x.PartidaIdOriginal);
        builder.HasIndex(x => x.UsuarioResponsavelId);
        builder.HasIndex(x => x.Acao);
        builder.HasIndex(x => x.DataHoraUtc);
    }
}
