using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlataformaFutevolei.Dominio.Entidades;

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Mapeamentos;

public class ArenaMapeamento : IEntityTypeConfiguration<Arena>
{
    public void Configure(EntityTypeBuilder<Arena> builder)
    {
        builder.ToTable("arenas");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Slug).HasColumnName("slug").HasMaxLength(220).IsRequired();
        builder.Property(x => x.Descricao).HasColumnName("descricao").HasMaxLength(1000);
        builder.Property(x => x.TipoArena).HasColumnName("tipo_arena").IsRequired();
        builder.Property(x => x.QuantidadeEspacos).HasColumnName("quantidade_espacos").IsRequired();
        builder.Property(x => x.Endereco).HasColumnName("endereco").HasMaxLength(500);
        builder.Property(x => x.EnderecoResumo).HasColumnName("endereco_resumo").HasMaxLength(250);
        builder.Property(x => x.Cidade).HasColumnName("cidade").HasMaxLength(100);
        builder.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(50);
        builder.Property(x => x.Latitude).HasColumnName("latitude");
        builder.Property(x => x.Longitude).HasColumnName("longitude");
        builder.Property(x => x.Whatsapp).HasColumnName("whatsapp").HasMaxLength(30);
        builder.Property(x => x.Instagram).HasColumnName("instagram").HasMaxLength(100);
        builder.Property(x => x.Site).HasColumnName("site").HasMaxLength(500);
        builder.Property(x => x.PossuiIluminacao).HasColumnName("possui_iluminacao").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.PossuiEstacionamento).HasColumnName("possui_estacionamento").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.PossuiVestiario).HasColumnName("possui_vestiario").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.PossuiDucha).HasColumnName("possui_ducha").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.PossuiBarRestaurante).HasColumnName("possui_bar_restaurante").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.PossuiLoja).HasColumnName("possui_loja").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.PossuiCobertura).HasColumnName("possui_cobertura").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.LogoUrl).HasColumnName("logo_url").HasMaxLength(500);
        builder.Property(x => x.LogoPublicId).HasColumnName("logo_public_id").HasMaxLength(255);
        builder.Property(x => x.CapaUrl).HasColumnName("capa_url").HasMaxLength(500);
        builder.Property(x => x.CapaPublicId).HasColumnName("capa_public_id").HasMaxLength(255);
        builder.Property(x => x.Publica).HasColumnName("publica").HasDefaultValue(true).IsRequired();
        builder.Property(x => x.Ativa).HasColumnName("ativa").HasDefaultValue(true).IsRequired();
        builder.Property(x => x.DataCriacao).HasColumnName("data_criacao").IsRequired();
        builder.Property(x => x.DataAtualizacao).HasColumnName("data_atualizacao").IsRequired();

        builder.HasIndex(x => x.Nome).IsUnique();
        builder.HasIndex(x => x.Slug).IsUnique();
    }
}
