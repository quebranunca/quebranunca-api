using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Mapeamentos;

public class ResgateBeneficioPontuacaoMapeamento : IEntityTypeConfiguration<ResgateBeneficioPontuacao>
{
    public void Configure(EntityTypeBuilder<ResgateBeneficioPontuacao> builder)
    {
        builder.ToTable("resgates_beneficios_pontuacao", tabela =>
        {
            tabela.HasCheckConstraint("ck_resgates_beneficios_pontuacao_pontos_positivos", "\"pontos_utilizados\" > 0");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.AtletaId).HasColumnName("atleta_id").IsRequired();
        builder.Property(x => x.BeneficioId).HasColumnName("beneficio_id").IsRequired();
        builder.Property(x => x.PontosUtilizados).HasColumnName("pontos_utilizados").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<int>().HasDefaultValue(StatusResgateBeneficioPontuacao.Solicitado).IsRequired();
        builder.Property(x => x.CodigoCupom).HasColumnName("codigo_cupom").HasMaxLength(80);
        builder.Property(x => x.ObservacaoAtleta).HasColumnName("observacao_atleta").HasMaxLength(500);
        builder.Property(x => x.ObservacaoAdmin).HasColumnName("observacao_admin").HasMaxLength(500);
        builder.Property(x => x.SolicitadoEm).HasColumnName("solicitado_em").IsRequired();
        builder.Property(x => x.AprovadoEm).HasColumnName("aprovado_em");
        builder.Property(x => x.RejeitadoEm).HasColumnName("rejeitado_em");
        builder.Property(x => x.CanceladoEm).HasColumnName("cancelado_em");
        builder.Property(x => x.UtilizadoEm).HasColumnName("utilizado_em");
        builder.Property(x => x.AtualizadoPorUsuarioId).HasColumnName("atualizado_por_usuario_id");
        builder.Property(x => x.DataCriacao).HasColumnName("data_criacao").IsRequired();
        builder.Property(x => x.DataAtualizacao).HasColumnName("data_atualizacao").IsRequired();

        builder.HasOne(x => x.Atleta)
            .WithMany()
            .HasForeignKey(x => x.AtletaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Beneficio)
            .WithMany(x => x.Resgates)
            .HasForeignKey(x => x.BeneficioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AtualizadoPorUsuario)
            .WithMany()
            .HasForeignKey(x => x.AtualizadoPorUsuarioId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.AtletaId);
        builder.HasIndex(x => x.BeneficioId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.SolicitadoEm);
        builder.HasIndex(x => new { x.AtletaId, x.BeneficioId, x.Status });
    }
}
