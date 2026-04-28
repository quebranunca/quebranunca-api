using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Mapeamentos;

public class PartidaMapeamento : IEntityTypeConfiguration<Partida>
{
    public void Configure(EntityTypeBuilder<Partida> builder)
    {
        builder.ToTable("partidas", tabela =>
        {
            tabela.HasCheckConstraint(
                "ck_partidas_duplas_diferentes",
                "\"dupla_a_id\" IS NULL OR \"dupla_b_id\" IS NULL OR \"dupla_a_id\" <> \"dupla_b_id\"");
            tabela.HasCheckConstraint(
                "ck_partidas_vencedora_valida",
                "\"dupla_vencedora_id\" IS NULL OR (\"dupla_a_id\" IS NOT NULL AND \"dupla_b_id\" IS NOT NULL AND (\"dupla_vencedora_id\" = \"dupla_a_id\" OR \"dupla_vencedora_id\" = \"dupla_b_id\"))"
            );
            tabela.HasCheckConstraint(
                "ck_partidas_placar_nao_negativo",
                "\"placar_dupla_a\" >= 0 AND \"placar_dupla_b\" >= 0"
            );
            tabela.HasCheckConstraint(
                "ck_partidas_status_e_resultado",
                "((\"status\" = 1) AND \"dupla_vencedora_id\" IS NULL AND \"placar_dupla_a\" = 0 AND \"placar_dupla_b\" = 0) OR ((\"status\" = 2) AND \"dupla_a_id\" IS NOT NULL AND \"dupla_b_id\" IS NOT NULL AND (((\"placar_dupla_a\" = \"placar_dupla_b\") AND \"dupla_vencedora_id\" IS NULL) OR ((\"placar_dupla_a\" > \"placar_dupla_b\") AND \"dupla_vencedora_id\" = \"dupla_a_id\") OR ((\"placar_dupla_b\" > \"placar_dupla_a\") AND \"dupla_vencedora_id\" = \"dupla_b_id\")))"
            );
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CategoriaCompeticaoId).HasColumnName("categoria_competicao_id").IsRequired();
        builder.Property(x => x.CriadoPorUsuarioId).HasColumnName("criado_por_usuario_id");
        builder.Property(x => x.DuplaAId).HasColumnName("dupla_a_id");
        builder.Property(x => x.DuplaBId).HasColumnName("dupla_b_id");
        builder.Property(x => x.FaseCampeonato).HasColumnName("fase_campeonato").HasMaxLength(100);
        builder.Property(x => x.LadoDaChave).HasColumnName("lado_da_chave").HasConversion<int>();
        builder.Property(x => x.Rodada).HasColumnName("rodada");
        builder.Property(x => x.PosicaoNaChave).HasColumnName("posicao_na_chave");
        builder.Property(x => x.PartidaOrigemParticipanteAId).HasColumnName("partida_origem_participante_a_id");
        builder.Property(x => x.OrigemParticipanteATipo).HasColumnName("origem_participante_a_tipo").HasConversion<int>();
        builder.Property(x => x.PartidaOrigemParticipanteBId).HasColumnName("partida_origem_participante_b_id");
        builder.Property(x => x.OrigemParticipanteBTipo).HasColumnName("origem_participante_b_tipo").HasConversion<int>();
        builder.Property(x => x.ProximaPartidaVencedorId).HasColumnName("proxima_partida_vencedor_id");
        builder.Property(x => x.ProximaPartidaPerdedorId).HasColumnName("proxima_partida_perdedor_id");
        builder.Property(x => x.SlotDestinoVencedor).HasColumnName("slot_destino_vencedor").HasConversion<int>();
        builder.Property(x => x.SlotDestinoPerdedor).HasColumnName("slot_destino_perdedor").HasConversion<int>();
        builder.Property(x => x.Ativa).HasColumnName("ativa").HasDefaultValue(true).IsRequired();
        builder.Property(x => x.EhPreliminar).HasColumnName("eh_preliminar").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.EhFinal).HasColumnName("eh_final").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.EhFinalissima).HasColumnName("eh_finalissima").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<int>().HasDefaultValue(StatusPartida.Agendada).IsRequired();
        builder.Property(x => x.StatusAprovacao).HasColumnName("status_aprovacao").HasConversion<int>().HasDefaultValue(StatusAprovacaoPartida.Aprovada).IsRequired();
        builder.Property(x => x.PlacarDuplaA).HasColumnName("placar_dupla_a").IsRequired();
        builder.Property(x => x.PlacarDuplaB).HasColumnName("placar_dupla_b").IsRequired();
        builder.Property(x => x.DuplaVencedoraId).HasColumnName("dupla_vencedora_id");
        builder.Property(x => x.DataPartida).HasColumnName("data_partida");
        builder.Property(x => x.Observacoes).HasColumnName("observacoes").HasMaxLength(1000);
        builder.Property(x => x.DataCriacao).HasColumnName("data_criacao").IsRequired();
        builder.Property(x => x.DataAtualizacao).HasColumnName("data_atualizacao").IsRequired();

        builder.HasOne(x => x.CategoriaCompeticao)
            .WithMany(x => x.Partidas)
            .HasForeignKey(x => x.CategoriaCompeticaoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.CriadoPorUsuario)
            .WithMany()
            .HasForeignKey(x => x.CriadoPorUsuarioId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.DuplaA)
            .WithMany(x => x.PartidasComoDuplaA)
            .HasForeignKey(x => x.DuplaAId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.DuplaB)
            .WithMany(x => x.PartidasComoDuplaB)
            .HasForeignKey(x => x.DuplaBId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.DuplaVencedora)
            .WithMany(x => x.PartidasVencidas)
            .HasForeignKey(x => x.DuplaVencedoraId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.CategoriaCompeticaoId);
        builder.HasIndex(x => x.CriadoPorUsuarioId);
        builder.HasIndex(x => x.DuplaAId);
        builder.HasIndex(x => x.DuplaBId);
        builder.HasIndex(x => x.DuplaVencedoraId);
        builder.HasIndex(x => x.LadoDaChave);
        builder.HasIndex(x => x.PartidaOrigemParticipanteAId);
        builder.HasIndex(x => x.PartidaOrigemParticipanteBId);
        builder.HasIndex(x => x.ProximaPartidaVencedorId);
        builder.HasIndex(x => x.ProximaPartidaPerdedorId);
        builder.HasIndex(x => x.StatusAprovacao);
        builder.HasIndex(x => new { x.CategoriaCompeticaoId, x.DataPartida });
    }
}
