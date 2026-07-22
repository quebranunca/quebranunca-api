using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlataformaFutevolei.Dominio.Entidades;

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Mapeamentos;

public class PontuacaoBeneficioAtletaMapeamento : IEntityTypeConfiguration<PontuacaoBeneficioAtleta>
{
    public void Configure(EntityTypeBuilder<PontuacaoBeneficioAtleta> builder)
    {
        builder.ToTable("pontuacoes_beneficios_atletas", tabela =>
        {
            tabela.HasCheckConstraint("ck_pontuacoes_beneficios_atletas_totais_nao_negativos", "\"total_acumulado\" >= 0 AND \"total_resgatado\" >= 0");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.AtletaId).HasColumnName("atleta_id").IsRequired();
        builder.Property(x => x.SaldoAtual).HasColumnName("saldo_atual").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.TotalAcumulado).HasColumnName("total_acumulado").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.TotalResgatado).HasColumnName("total_resgatado").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.DataCriacao).HasColumnName("data_criacao").IsRequired();
        builder.Property(x => x.DataAtualizacao).HasColumnName("data_atualizacao").IsRequired();

        builder.HasOne(x => x.Atleta)
            .WithMany()
            .HasForeignKey(x => x.AtletaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.AtletaId).IsUnique();
    }
}
