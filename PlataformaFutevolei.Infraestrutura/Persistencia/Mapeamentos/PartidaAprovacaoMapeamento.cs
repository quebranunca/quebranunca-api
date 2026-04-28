using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Mapeamentos;

public class PartidaAprovacaoMapeamento : IEntityTypeConfiguration<PartidaAprovacao>
{
    public void Configure(EntityTypeBuilder<PartidaAprovacao> builder)
    {
        builder.ToTable("partidas_aprovacoes");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.PartidaId).HasColumnName("partida_id").IsRequired();
        builder.Property(x => x.AtletaId).HasColumnName("atleta_id").IsRequired();
        builder.Property(x => x.UsuarioId).HasColumnName("usuario_id").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<int>().HasDefaultValue(StatusPartidaAprovacao.Pendente).IsRequired();
        builder.Property(x => x.DataSolicitacao).HasColumnName("data_solicitacao").IsRequired();
        builder.Property(x => x.DataResposta).HasColumnName("data_resposta");
        builder.Property(x => x.Observacao).HasColumnName("observacao").HasMaxLength(1000);
        builder.Property(x => x.DataCriacao).HasColumnName("data_criacao").IsRequired();
        builder.Property(x => x.DataAtualizacao).HasColumnName("data_atualizacao").IsRequired();

        builder.HasOne(x => x.Partida)
            .WithMany(x => x.Aprovacoes)
            .HasForeignKey(x => x.PartidaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Atleta)
            .WithMany()
            .HasForeignKey(x => x.AtletaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Usuario)
            .WithMany()
            .HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.PartidaId);
        builder.HasIndex(x => x.AtletaId);
        builder.HasIndex(x => x.UsuarioId);
        builder.HasIndex(x => new { x.PartidaId, x.AtletaId }).IsUnique();
    }
}
