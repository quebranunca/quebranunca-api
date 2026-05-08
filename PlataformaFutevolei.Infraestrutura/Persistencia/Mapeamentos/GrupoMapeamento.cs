using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlataformaFutevolei.Dominio.Entidades;

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Mapeamentos;

public class GrupoMapeamento : IEntityTypeConfiguration<Grupo>
{
    public void Configure(EntityTypeBuilder<Grupo> builder)
    {
        builder.ToTable("grupos");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Descricao).HasColumnName("descricao").HasMaxLength(1000);
        builder.Property(x => x.Link).HasColumnName("link").HasMaxLength(500);
        builder.Property(x => x.DataInicio).HasColumnName("data_inicio").IsRequired();
        builder.Property(x => x.DataFim).HasColumnName("data_fim");
        builder.Property(x => x.LocalId).HasColumnName("local_id");
        builder.Property(x => x.UsuarioOrganizadorId).HasColumnName("usuario_organizador_id");
        builder.Property(x => x.DataCriacao).HasColumnName("data_criacao").IsRequired();
        builder.Property(x => x.DataAtualizacao).HasColumnName("data_atualizacao").IsRequired();

        builder.HasOne(x => x.Local)
            .WithMany(x => x.Grupos)
            .HasForeignKey(x => x.LocalId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.UsuarioOrganizador)
            .WithMany()
            .HasForeignKey(x => x.UsuarioOrganizadorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.LocalId);
        builder.HasIndex(x => x.UsuarioOrganizadorId);
    }
}
