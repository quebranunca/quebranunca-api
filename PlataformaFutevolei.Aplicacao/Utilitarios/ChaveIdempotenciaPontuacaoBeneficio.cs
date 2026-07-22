using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Aplicacao.Utilitarios;

public static class ChaveIdempotenciaPontuacaoBeneficio
{
    public static string NormalizarParaConsolidacao(
        string chave,
        TipoEventoPontuacaoBeneficio tipoEvento,
        Guid atletaPerdedorId,
        Guid atletaVencedorId)
    {
        if (tipoEvento is TipoEventoPontuacaoBeneficio.SaldoInicialRetroativo or
            TipoEventoPontuacaoBeneficio.EstornoPartida)
        {
            return chave;
        }

        return chave.Replace(
            atletaPerdedorId.ToString(),
            atletaVencedorId.ToString(),
            StringComparison.OrdinalIgnoreCase);
    }

    public static string MontarReferenciaEstorno(Guid partidaId, Guid extratoOriginalId)
        => $"ESTORNO_PARTIDA:{partidaId}:EXTRATO:{extratoOriginalId}";
}
