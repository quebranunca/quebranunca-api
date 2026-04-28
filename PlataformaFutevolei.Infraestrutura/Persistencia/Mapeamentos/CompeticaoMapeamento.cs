using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlataformaFutevolei.Dominio.Entidades;

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Mapeamentos;

public class CompeticaoMapeamento : IEntityTypeConfiguration<Competicao>
{
    public void Configure(EntityTypeBuilder<Competicao> builder)
    {
        builder.ToTable("competicoes");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Tipo).HasColumnName("tipo").IsRequired();
        builder.Property(x => x.Descricao).HasColumnName("descricao").HasMaxLength(1000);
        builder.Property(x => x.Link).HasColumnName("link").HasMaxLength(500);
        builder.Property(x => x.DataInicio).HasColumnName("data_inicio").IsRequired();
        builder.Property(x => x.DataFim).HasColumnName("data_fim");
        builder.Property(x => x.LigaId).HasColumnName("liga_id");
        builder.Property(x => x.LocalId).HasColumnName("local_id");
        builder.Property(x => x.FormatoCampeonatoId).HasColumnName("formato_campeonato_id");
        builder.Property(x => x.RegraCompeticaoId).HasColumnName("regra_competicao_id");
        builder.Property(x => x.UsuarioOrganizadorId).HasColumnName("usuario_organizador_id");
        builder.Property(x => x.ContaRankingLiga).HasColumnName("conta_ranking_liga").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.InscricoesAbertas).HasColumnName("inscricoes_abertas").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.PossuiFinalReset).HasColumnName("possui_final_reset").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.DataCriacao).HasColumnName("data_criacao").IsRequired();
        builder.Property(x => x.DataAtualizacao).HasColumnName("data_atualizacao").IsRequired();

        builder.HasOne(x => x.Liga)
            .WithMany(x => x.Competicoes)
            .HasForeignKey(x => x.LigaId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Local)
            .WithMany(x => x.Competicoes)
            .HasForeignKey(x => x.LocalId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.FormatoCampeonato)
            .WithMany()
            .HasForeignKey(x => x.FormatoCampeonatoId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.RegraCompeticao)
            .WithMany(x => x.Competicoes)
            .HasForeignKey(x => x.RegraCompeticaoId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.UsuarioOrganizador)
            .WithMany()
            .HasForeignKey(x => x.UsuarioOrganizadorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.LigaId);
        builder.HasIndex(x => x.LocalId);
        builder.HasIndex(x => x.FormatoCampeonatoId);
        builder.HasIndex(x => x.RegraCompeticaoId);
        builder.HasIndex(x => x.UsuarioOrganizadorId);
    }
}
