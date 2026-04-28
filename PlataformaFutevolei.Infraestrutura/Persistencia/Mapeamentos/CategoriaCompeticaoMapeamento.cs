using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlataformaFutevolei.Dominio.Entidades;

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Mapeamentos;

public class CategoriaCompeticaoMapeamento : IEntityTypeConfiguration<CategoriaCompeticao>
{
    public void Configure(EntityTypeBuilder<CategoriaCompeticao> builder)
    {
        builder.ToTable("categorias_competicao");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CompeticaoId).HasColumnName("competicao_id").IsRequired();
        builder.Property(x => x.FormatoCampeonatoId).HasColumnName("formato_campeonato_id");
        builder.Property(x => x.TabelaJogosAprovadaPorUsuarioId).HasColumnName("tabela_jogos_aprovada_por_usuario_id");
        builder.Property(x => x.TabelaJogosAprovadaEmUtc).HasColumnName("tabela_jogos_aprovada_em_utc");
        builder.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(150).IsRequired();
        builder.Property(x => x.Genero).HasColumnName("genero").IsRequired();
        builder.Property(x => x.Nivel).HasColumnName("nivel").IsRequired();
        builder.Property(x => x.PesoRanking).HasColumnName("peso_ranking").HasPrecision(10, 2).HasDefaultValue(1m).IsRequired();
        builder.Property(x => x.QuantidadeMaximaDuplas).HasColumnName("quantidade_maxima_duplas");
        builder.Property(x => x.InscricoesEncerradas).HasColumnName("inscricoes_encerradas").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.DataCriacao).HasColumnName("data_criacao").IsRequired();
        builder.Property(x => x.DataAtualizacao).HasColumnName("data_atualizacao").IsRequired();

        builder.HasOne(x => x.Competicao)
            .WithMany(x => x.Categorias)
            .HasForeignKey(x => x.CompeticaoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.FormatoCampeonato)
            .WithMany()
            .HasForeignKey(x => x.FormatoCampeonatoId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.CompeticaoId);
        builder.HasIndex(x => x.FormatoCampeonatoId);
        builder.HasIndex(x => x.TabelaJogosAprovadaPorUsuarioId);
    }
}
