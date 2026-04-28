using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PlataformaFutevolei.Infraestrutura.Persistencia;

#nullable disable

namespace PlataformaFutevolei.Infraestrutura.Persistencia.Migracoes
{
    [DbContext(typeof(PlataformaFutevoleiDbContext))]
    [Migration("20260323183000_NormalizarRegistrosAtleta")]
    public partial class NormalizarRegistrosAtleta : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                with atletas_normalizados as (
                    select
                        id,
                        trim(regexp_replace(nome, '\s+', ' ', 'g')) as nome_limpo,
                        case
                            when apelido is null then null
                            when btrim(apelido) = '' then null
                            when lower(btrim(apelido)) in ('não informado', 'nao informado', 'null', 'n/a', '-') then null
                            else trim(regexp_replace(apelido, '\s+', ' ', 'g'))
                        end as apelido_limpo
                    from atletas
                ),
                nomes_finais as (
                    select
                        id,
                        case
                            when apelido_limpo is null then nome_limpo
                            when position(lower(apelido_limpo) in lower(nome_limpo)) > 0 then nome_limpo
                            when array_length(regexp_split_to_array(nome_limpo, '\s+'), 1) >= 4 then nome_limpo
                            else trim(regexp_replace(nome_limpo || ' ' || apelido_limpo, '\s+', ' ', 'g'))
                        end as nome_final
                    from atletas_normalizados
                )
                update atletas atleta
                set
                    nome = nomes_finais.nome_final,
                    apelido = case
                        when array_length(regexp_split_to_array(nomes_finais.nome_final, '\s+'), 1) <= 1
                            then (regexp_split_to_array(nomes_finais.nome_final, '\s+'))[1]
                        else
                            (regexp_split_to_array(nomes_finais.nome_final, '\s+'))[1]
                            || ' ' ||
                            (regexp_split_to_array(nomes_finais.nome_final, '\s+'))[
                                array_length(regexp_split_to_array(nomes_finais.nome_final, '\s+'), 1)
                            ]
                    end,
                    data_atualizacao = timezone('utc', now())
                from nomes_finais
                where atleta.id = nomes_finais.id;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
