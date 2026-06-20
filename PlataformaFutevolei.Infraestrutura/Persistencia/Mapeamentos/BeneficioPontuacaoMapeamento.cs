using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlataformaFutevolei.Aplicacao.Configuracoes;
using PlataformaFutevolei.Dominio.Entidades;

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Mapeamentos;

public class BeneficioPontuacaoMapeamento : IEntityTypeConfiguration<BeneficioPontuacao>
{
    private static readonly DateTime DataSeed = new(2026, 6, 19, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<BeneficioPontuacao> builder)
    {
        builder.ToTable("beneficios_pontuacao", tabela =>
        {
            tabela.HasCheckConstraint("ck_beneficios_pontuacao_pontos_positivos", "\"pontos_necessarios\" > 0");
            tabela.HasCheckConstraint("ck_beneficios_pontuacao_quantidade_nao_negativa", "\"quantidade_disponivel\" IS NULL OR \"quantidade_disponivel\" >= 0");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Titulo).HasColumnName("titulo").HasMaxLength(120).IsRequired();
        builder.Property(x => x.Descricao).HasColumnName("descricao").HasMaxLength(500).IsRequired();
        builder.Property(x => x.Tipo).HasColumnName("tipo").HasConversion<int>().IsRequired();
        builder.Property(x => x.PontosNecessarios).HasColumnName("pontos_necessarios").IsRequired();
        builder.Property(x => x.Ativo).HasColumnName("ativo").HasDefaultValue(true).IsRequired();
        builder.Property(x => x.QuantidadeDisponivel).HasColumnName("quantidade_disponivel");
        builder.Property(x => x.ImagemUrl).HasColumnName("imagem_url").HasMaxLength(500);
        builder.Property(x => x.Ordem).HasColumnName("ordem").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.Destaque).HasColumnName("destaque").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.DataCriacao).HasColumnName("data_criacao").IsRequired();
        builder.Property(x => x.DataAtualizacao).HasColumnName("data_atualizacao").IsRequired();

        builder.HasIndex(x => x.Tipo);
        builder.HasIndex(x => x.Ativo);
        builder.HasIndex(x => x.Ordem);

        builder.HasData(PontuacaoBeneficioRegras.BeneficiosPadrao.Select(beneficio => new
        {
            beneficio.Id,
            beneficio.Titulo,
            beneficio.Descricao,
            beneficio.Tipo,
            beneficio.PontosNecessarios,
            Ativo = true,
            QuantidadeDisponivel = (int?)null,
            beneficio.ImagemUrl,
            beneficio.Ordem,
            beneficio.Destaque,
            DataCriacao = DataSeed,
            DataAtualizacao = DataSeed
        }));
    }
}
