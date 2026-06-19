using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;

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

        builder.HasData(
            new
            {
                Id = Guid.Parse("11111111-1111-4111-8111-111111111111"),
                Titulo = "10% OFF na loja",
                Descricao = "Solicite um cupom manual de desconto para usar na loja QuebraNunca.",
                Tipo = TipoBeneficioPontuacao.DescontoLoja,
                PontosNecessarios = 600,
                Ativo = true,
                QuantidadeDisponivel = (int?)null,
                ImagemUrl = (string?)null,
                Ordem = 1,
                Destaque = true,
                DataCriacao = DataSeed,
                DataAtualizacao = DataSeed
            },
            new
            {
                Id = Guid.Parse("22222222-2222-4222-8222-222222222222"),
                Titulo = "Boné QuebraNunca",
                Descricao = "Brinde sujeito a disponibilidade e aprovação manual.",
                Tipo = TipoBeneficioPontuacao.Brinde,
                PontosNecessarios = 1200,
                Ativo = true,
                QuantidadeDisponivel = (int?)null,
                ImagemUrl = (string?)null,
                Ordem = 2,
                Destaque = false,
                DataCriacao = DataSeed,
                DataAtualizacao = DataSeed
            },
            new
            {
                Id = Guid.Parse("33333333-3333-4333-8333-333333333333"),
                Titulo = "Garrafa QN",
                Descricao = "Brinde sujeito a disponibilidade e aprovação manual.",
                Tipo = TipoBeneficioPontuacao.Brinde,
                PontosNecessarios = 1200,
                Ativo = true,
                QuantidadeDisponivel = (int?)null,
                ImagemUrl = (string?)null,
                Ordem = 3,
                Destaque = false,
                DataCriacao = DataSeed,
                DataAtualizacao = DataSeed
            },
            new
            {
                Id = Guid.Parse("44444444-4444-4444-8444-444444444444"),
                Titulo = "Camiseta Drop Especial",
                Descricao = "Produto sujeito a estoque e aprovação manual.",
                Tipo = TipoBeneficioPontuacao.Produto,
                PontosNecessarios = 1800,
                Ativo = true,
                QuantidadeDisponivel = (int?)null,
                ImagemUrl = (string?)null,
                Ordem = 4,
                Destaque = false,
                DataCriacao = DataSeed,
                DataAtualizacao = DataSeed
            },
            new
            {
                Id = Guid.Parse("55555555-5555-4555-8555-555555555555"),
                Titulo = "Aula com parceiro",
                Descricao = "Experiência agendada manualmente pela equipe QuebraNunca.",
                Tipo = TipoBeneficioPontuacao.Experiencia,
                PontosNecessarios = 2000,
                Ativo = true,
                QuantidadeDisponivel = (int?)null,
                ImagemUrl = (string?)null,
                Ordem = 5,
                Destaque = false,
                DataCriacao = DataSeed,
                DataAtualizacao = DataSeed
            });
    }
}
