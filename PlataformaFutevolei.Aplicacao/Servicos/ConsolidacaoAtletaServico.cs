using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Aplicacao.Utilitarios;
using PlataformaFutevolei.Dominio.Entidades;

namespace PlataformaFutevolei.Aplicacao.Servicos;

public class ConsolidacaoAtletaServico(
    IAtletaRepositorio atletaRepositorio,
    IConsolidacaoAtletaRepositorio consolidacaoAtletaRepositorio,
    IUnidadeTrabalho unidadeTrabalho
) : IConsolidacaoAtletaServico
{
    public async Task<Atleta> ConsolidarCandidatosAsync(
        IEnumerable<Atleta?> candidatos,
        Guid? atletaVinculadoConfiavelId = null,
        string? emailNormalizado = null,
        CancellationToken cancellationToken = default)
    {
        ConsolidacaoInternaResultado? resultado = null;
        await unidadeTrabalho.ExecutarEmTransacaoAsync(async ct =>
        {
            resultado = await ConsolidarCandidatosComResultadoAsync(
                candidatos,
                atletaVinculadoConfiavelId,
                emailNormalizado,
                ct);
        }, cancellationToken);

        if (resultado is null)
        {
            throw new InvalidOperationException("A consolidação de atletas não retornou resultado.");
        }

        return resultado.AtletaVencedor;
    }

    private async Task<ConsolidacaoInternaResultado> ConsolidarCandidatosComResultadoAsync(
        IEnumerable<Atleta?> candidatos,
        Guid? atletaVinculadoConfiavelId,
        string? emailNormalizado,
        CancellationToken cancellationToken)
    {
        var candidatosValidos = candidatos
            .OfType<Atleta>()
            .DistinctBy(x => x.Id)
            .ToList();

        if (candidatosValidos.Count == 0)
        {
            throw new InvalidOperationException("Informe ao menos um atleta para consolidação.");
        }

        if (candidatosValidos.Count == 1)
        {
            if (!string.IsNullOrWhiteSpace(emailNormalizado))
            {
                candidatosValidos[0].Email = NormalizarEmail(emailNormalizado);
                candidatosValidos[0].CadastroPendente = false;
                candidatosValidos[0].AtualizarDataModificacao();
            }

            return new ConsolidacaoInternaResultado(
                candidatosValidos[0],
                [],
                new SaneamentoAtletasEmailContadoresDto(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0));
        }

        var metricas = await consolidacaoAtletaRepositorio.ObterMetricasAsync(
            candidatosValidos.Select(x => x.Id),
            cancellationToken);
        var vencedor = EscolherVencedor(candidatosValidos, metricas, atletaVinculadoConfiavelId);
        var perdedores = candidatosValidos
            .Where(x => x.Id != vencedor.Id)
            .OrderBy(x => x.DataCriacao)
            .ThenBy(x => x.Id)
            .ToList();

        var contadores = new ContadorConsolidacao();
        foreach (var perdedor in perdedores)
        {
            var migrados = await consolidacaoAtletaRepositorio.TransferirVinculosAsync(
                vencedor.Id,
                perdedor.Id,
                cancellationToken);
            contadores.Somar(migrados);
        }

        if (!string.IsNullOrWhiteSpace(emailNormalizado))
        {
            vencedor.Email = NormalizarEmail(emailNormalizado);
        }

        vencedor.CadastroPendente = false;
        vencedor.AtualizarDataModificacao();
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        var atletaAtualizado = await atletaRepositorio.ObterPorIdAsync(vencedor.Id, cancellationToken);
        return new ConsolidacaoInternaResultado(
            atletaAtualizado ?? vencedor,
            perdedores.Select(x => x.Id).ToList(),
            contadores.ParaDto());
    }

    public async Task<SaneamentoAtletasEmailResumoDto> ConsolidarDuplicadosPorEmailAsync(
        CancellationToken cancellationToken = default)
    {
        var grupos = await consolidacaoAtletaRepositorio.ListarDuplicadosPorEmailAsync(cancellationToken);
        var resultados = new List<SaneamentoAtletasEmailGrupoDto>();

        foreach (var grupo in grupos)
        {
            var email = NormalizarEmail(grupo.First().Email);
            ConsolidacaoInternaResultado? resultado = null;
            await unidadeTrabalho.ExecutarEmTransacaoAsync(async ct =>
            {
                resultado = await ConsolidarCandidatosComResultadoAsync(
                    grupo,
                    atletaVinculadoConfiavelId: null,
                    email,
                    ct);
            }, cancellationToken);

            if (resultado is null)
            {
                throw new InvalidOperationException("A consolidação de atletas não retornou resultado.");
            }

            resultados.Add(new SaneamentoAtletasEmailGrupoDto(
                email,
                resultado.AtletaVencedor.Id,
                resultado.AtletasPerdedoresIds,
                resultado.Contadores));
        }

        return new SaneamentoAtletasEmailResumoDto(
            grupos.Count,
            resultados.Count,
            resultados.Sum(x => x.AtletasDuplicadosIds.Count),
            resultados);
    }

    private static Atleta EscolherVencedor(
        IReadOnlyList<Atleta> candidatos,
        IDictionary<Guid, ConsolidacaoAtletaMetricasDto> metricas,
        Guid? atletaVinculadoConfiavelId)
    {
        // O vínculo só tem prioridade quando o fluxo chamador já validou que ele é confiável.
        // Em saneamentos automáticos e conflitos de e-mail, não passamos esse id para evitar perpetuar vínculo errado.
        return candidatos
            .OrderByDescending(x => atletaVinculadoConfiavelId.HasValue && x.Id == atletaVinculadoConfiavelId.Value)
            .ThenByDescending(x => ObterMetricas(metricas, x).TotalPartidas)
            .ThenByDescending(x => ObterMetricas(metricas, x).TotalHistorico)
            .ThenBy(x => x.DataCriacao)
            .ThenBy(x => x.Id)
            .First();
    }

    private static ConsolidacaoAtletaMetricasDto ObterMetricas(
        IDictionary<Guid, ConsolidacaoAtletaMetricasDto> metricas,
        Atleta atleta)
    {
        return metricas.TryGetValue(atleta.Id, out var metrica)
            ? metrica
            : new ConsolidacaoAtletaMetricasDto(atleta.Id, atleta.Usuario is not null, 0, 0, 0, 0, 0, 0, 0, atleta.DataCriacao);
    }

    private static string NormalizarEmail(string? email)
    {
        return NormalizadorNomeAtleta.NormalizarTexto(email).ToLowerInvariant();
    }

    private sealed class ContadorConsolidacao
    {
        private int duplasAtualizadas;
        private int duplasConsolidadas;
        private int partidasAtualizadas;
        private int inscricoesAtualizadas;
        private int inscricoesConsolidadas;
        private int gruposAtualizados;
        private int gruposConsolidados;
        private int aprovacoesAtualizadas;
        private int aprovacoesConsolidadas;
        private int pendenciasAtualizadas;
        private int convitesAtualizados;
        private int usuariosAtualizados;
        private int atletasRemovidos;

        public void Somar(SaneamentoAtletasEmailContadoresDto contadores)
        {
            duplasAtualizadas += contadores.DuplasAtualizadas;
            duplasConsolidadas += contadores.DuplasConsolidadas;
            partidasAtualizadas += contadores.PartidasAtualizadas;
            inscricoesAtualizadas += contadores.InscricoesAtualizadas;
            inscricoesConsolidadas += contadores.InscricoesConsolidadas;
            gruposAtualizados += contadores.GruposAtualizados;
            gruposConsolidados += contadores.GruposConsolidados;
            aprovacoesAtualizadas += contadores.AprovacoesAtualizadas;
            aprovacoesConsolidadas += contadores.AprovacoesConsolidadas;
            pendenciasAtualizadas += contadores.PendenciasAtualizadas;
            convitesAtualizados += contadores.ConvitesAtualizados;
            usuariosAtualizados += contadores.UsuariosAtualizados;
            atletasRemovidos += contadores.AtletasRemovidos;
        }

        public SaneamentoAtletasEmailContadoresDto ParaDto()
        {
            return new SaneamentoAtletasEmailContadoresDto(
                duplasAtualizadas,
                duplasConsolidadas,
                partidasAtualizadas,
                inscricoesAtualizadas,
                inscricoesConsolidadas,
                gruposAtualizados,
                gruposConsolidados,
                aprovacoesAtualizadas,
                aprovacoesConsolidadas,
                pendenciasAtualizadas,
                convitesAtualizados,
                usuariosAtualizados,
                atletasRemovidos);
        }
    }

    private sealed record ConsolidacaoInternaResultado(
        Atleta AtletaVencedor,
        IReadOnlyList<Guid> AtletasPerdedoresIds,
        SaneamentoAtletasEmailContadoresDto Contadores);
}
