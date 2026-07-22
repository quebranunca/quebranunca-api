using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Aplicacao.Utilitarios;

public readonly record struct ProjecaoPontuacaoBeneficio(
    int SaldoAtual,
    int TotalAcumulado,
    int TotalResgatado);

public static class ProjecaoPontuacaoBeneficioCalculador
{
    public static ProjecaoPontuacaoBeneficio Calcular(IEnumerable<ExtratoPontuacaoBeneficio> extratos)
    {
        var projecao = new ProjecaoPontuacaoBeneficio();
        foreach (var extrato in extratos.OrderBy(x => x.DataCriacao).ThenBy(x => x.Id))
        {
            projecao = Aplicar(projecao, extrato.Pontos, extrato.TipoEvento);
        }

        return projecao;
    }

    public static ProjecaoPontuacaoBeneficio Aplicar(
        ProjecaoPontuacaoBeneficio atual,
        int pontos,
        TipoEventoPontuacaoBeneficio tipoEvento)
    {
        var totalAcumulado = atual.TotalAcumulado;
        var totalResgatado = atual.TotalResgatado;

        if (tipoEvento == TipoEventoPontuacaoBeneficio.EstornoPartida)
        {
            totalAcumulado = Math.Max(0, totalAcumulado - Math.Abs(pontos));
        }
        else if (pontos > 0 && tipoEvento != TipoEventoPontuacaoBeneficio.EstornoResgate)
        {
            totalAcumulado += pontos;
        }

        if (tipoEvento == TipoEventoPontuacaoBeneficio.ResgateBeneficio)
        {
            totalResgatado += Math.Abs(pontos);
        }
        else if (tipoEvento == TipoEventoPontuacaoBeneficio.EstornoResgate)
        {
            totalResgatado = Math.Max(0, totalResgatado - Math.Abs(pontos));
        }

        return new ProjecaoPontuacaoBeneficio(
            atual.SaldoAtual + pontos,
            Math.Max(0, totalAcumulado),
            Math.Max(0, totalResgatado));
    }
}
