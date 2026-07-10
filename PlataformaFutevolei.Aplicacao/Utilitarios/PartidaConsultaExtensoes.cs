using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Aplicacao.Utilitarios;

public static class PartidaConsultaExtensoes
{
    public static bool EsportivamenteValida(this Partida partida)
        => partida.Status == StatusPartida.Encerrada &&
           partida.StatusAprovacao != StatusAprovacaoPartida.Contestada &&
           !partida.Cancelada &&
           partida.ExcluidaDefinitivamenteEm is null;

    public static IQueryable<Partida> SomenteEsportivamenteValidas(this IQueryable<Partida> consulta)
        => consulta
            .Where(x => x.Status == StatusPartida.Encerrada)
            .Where(x => x.StatusAprovacao != StatusAprovacaoPartida.Contestada)
            .Where(x => !x.Cancelada)
            .Where(x => x.ExcluidaDefinitivamenteEm == null);

    public static IQueryable<Partida> SemExclusaoAdministrativa(this IQueryable<Partida> consulta)
        => consulta.Where(x => x.ExcluidaDefinitivamenteEm == null);
}
