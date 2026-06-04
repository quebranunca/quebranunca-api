using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;
using PlataformaFutevolei.Infraestrutura.Persistencia;

namespace PlataformaFutevolei.Infraestrutura.Servicos;

public class SaneamentoAtletasEmailServico(
    PlataformaFutevoleiDbContext dbContext,
    ILogger<SaneamentoAtletasEmailServico> logger
) : ISaneamentoAtletasEmailServico
{
    public async Task<SaneamentoAtletasEmailResumoDto> UnificarDuplicadosPorEmailAsync(
        CancellationToken cancellationToken = default)
    {
        var grupos = await LocalizarGruposDuplicadosAsync(cancellationToken);
        var resultados = new List<SaneamentoAtletasEmailGrupoDto>();

        await using var transacao = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        foreach (var grupo in grupos)
        {
            var resultado = await UnificarGrupoAsync(grupo.Email, grupo.Atletas, cancellationToken);
            resultados.Add(resultado);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transacao.CommitAsync(cancellationToken);

        return new SaneamentoAtletasEmailResumoDto(
            grupos.Count,
            resultados.Count,
            resultados.Sum(x => x.AtletasDuplicadosIds.Count),
            resultados);
    }

    private async Task<IReadOnlyList<GrupoDuplicadoEmail>> LocalizarGruposDuplicadosAsync(
        CancellationToken cancellationToken)
    {
        var atletas = await dbContext.Atletas
            .Where(x => x.Email != null && x.Email.Trim() != string.Empty)
            .OrderBy(x => x.DataCriacao)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        return atletas
            .GroupBy(x => NormalizarEmail(x.Email))
            .Where(x => !string.IsNullOrWhiteSpace(x.Key) && x.Count() > 1)
            .Select(x => new GrupoDuplicadoEmail(x.Key!, x.ToList()))
            .ToList();
    }

    private async Task<SaneamentoAtletasEmailGrupoDto> UnificarGrupoAsync(
        string email,
        IReadOnlyList<Atleta> atletas,
        CancellationToken cancellationToken)
    {
        var principal = atletas
            .OrderBy(x => x.DataCriacao)
            .ThenBy(x => x.Id)
            .First();
        var duplicados = atletas
            .Where(x => x.Id != principal.Id)
            .OrderBy(x => x.DataCriacao)
            .ThenBy(x => x.Id)
            .ToList();

        var contador = new ContadorSaneamento();
        logger.LogInformation(
            "Saneamento de atletas duplicados por e-mail iniciado. Email: {Email}. Principal: {AtletaPrincipalId}. Duplicados: {AtletasDuplicadosIds}.",
            email,
            principal.Id,
            string.Join(", ", duplicados.Select(x => x.Id)));

        foreach (var duplicado in duplicados)
        {
            await ConsolidarDuplasAsync(principal, duplicado, contador, cancellationToken);
            await MigrarGruposAsync(principal.Id, duplicado.Id, contador, cancellationToken);
            await MigrarAprovacoesAsync(principal.Id, duplicado.Id, contador, cancellationToken);
            await MigrarPendenciasAsync(principal.Id, duplicado.Id, contador, cancellationToken);
            await MigrarConvitesAsync(principal.Id, duplicado.Id, contador, cancellationToken);
            await MigrarUsuariosAsync(principal.Id, duplicado.Id, contador, cancellationToken);

            dbContext.Atletas.Remove(duplicado);
            contador.AtletasRemovidos++;
        }

        principal.Email = email;
        principal.AtualizarDataModificacao();

        var contadores = contador.ParaDto();
        logger.LogInformation(
            "Saneamento de atletas duplicados por e-mail concluído. Email: {Email}. Principal: {AtletaPrincipalId}. Duplicados: {AtletasDuplicadosIds}. DuplasAtualizadas: {DuplasAtualizadas}. DuplasConsolidadas: {DuplasConsolidadas}. PartidasAtualizadas: {PartidasAtualizadas}. InscricoesAtualizadas: {InscricoesAtualizadas}. InscricoesConsolidadas: {InscricoesConsolidadas}. GruposAtualizados: {GruposAtualizados}. GruposConsolidados: {GruposConsolidados}. AprovacoesAtualizadas: {AprovacoesAtualizadas}. AprovacoesConsolidadas: {AprovacoesConsolidadas}. PendenciasAtualizadas: {PendenciasAtualizadas}. ConvitesAtualizados: {ConvitesAtualizados}. UsuariosAtualizados: {UsuariosAtualizados}. AtletasRemovidos: {AtletasRemovidos}.",
            email,
            principal.Id,
            string.Join(", ", duplicados.Select(x => x.Id)),
            contadores.DuplasAtualizadas,
            contadores.DuplasConsolidadas,
            contadores.PartidasAtualizadas,
            contadores.InscricoesAtualizadas,
            contadores.InscricoesConsolidadas,
            contadores.GruposAtualizados,
            contadores.GruposConsolidados,
            contadores.AprovacoesAtualizadas,
            contadores.AprovacoesConsolidadas,
            contadores.PendenciasAtualizadas,
            contadores.ConvitesAtualizados,
            contadores.UsuariosAtualizados,
            contadores.AtletasRemovidos);

        return new SaneamentoAtletasEmailGrupoDto(
            email,
            principal.Id,
            duplicados.Select(x => x.Id).ToList(),
            contadores);
    }

    private async Task ConsolidarDuplasAsync(
        Atleta principal,
        Atleta duplicado,
        ContadorSaneamento contador,
        CancellationToken cancellationToken)
    {
        var duplas = await dbContext.Duplas
            .Where(x => x.Atleta1Id == duplicado.Id || x.Atleta2Id == duplicado.Id)
            .OrderBy(x => x.DataCriacao)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        foreach (var dupla in duplas)
        {
            var parceiroId = dupla.Atleta1Id == duplicado.Id ? dupla.Atleta2Id : dupla.Atleta1Id;
            if (parceiroId == principal.Id)
            {
                throw new RegraNegocioException(
                    $"Não é possível unificar atletas com e-mail {NormalizarEmail(principal.Email)} porque a dupla {dupla.Id} ficaria com o mesmo atleta duas vezes.");
            }

            var (atleta1Id, atleta2Id) = NormalizarAtletas(principal.Id, parceiroId);
            var duplaDestino = await dbContext.Duplas
                .FirstOrDefaultAsync(
                    x => x.Id != dupla.Id && x.Atleta1Id == atleta1Id && x.Atleta2Id == atleta2Id,
                    cancellationToken);

            if (duplaDestino is not null)
            {
                contador.PartidasAtualizadas += await MigrarPartidasDaDuplaAsync(
                    dupla.Id,
                    duplaDestino.Id,
                    cancellationToken);
                await MigrarInscricoesDaDuplaAsync(dupla.Id, duplaDestino.Id, contador, cancellationToken);
                dbContext.Duplas.Remove(dupla);
                contador.DuplasConsolidadas++;
                continue;
            }

            dupla.Atleta1Id = atleta1Id;
            dupla.Atleta2Id = atleta2Id;
            dupla.Nome = await MontarNomeDuplaAsync(atleta1Id, atleta2Id, cancellationToken);
            dupla.AtualizarDataModificacao();
            contador.DuplasAtualizadas++;
        }
    }

    private async Task<int> MigrarPartidasDaDuplaAsync(
        Guid duplaOrigemId,
        Guid duplaDestinoId,
        CancellationToken cancellationToken)
    {
        var partidas = await dbContext.Partidas
            .Where(x =>
                x.DuplaAId == duplaOrigemId ||
                x.DuplaBId == duplaOrigemId ||
                x.DuplaVencedoraId == duplaOrigemId)
            .ToListAsync(cancellationToken);

        foreach (var partida in partidas)
        {
            if (partida.DuplaAId == duplaOrigemId)
            {
                partida.DuplaAId = duplaDestinoId;
            }

            if (partida.DuplaBId == duplaOrigemId)
            {
                partida.DuplaBId = duplaDestinoId;
            }

            if (partida.DuplaVencedoraId == duplaOrigemId)
            {
                partida.DuplaVencedoraId = duplaDestinoId;
            }

            if (partida.DuplaAId.HasValue &&
                partida.DuplaBId.HasValue &&
                partida.DuplaAId == partida.DuplaBId)
            {
                throw new RegraNegocioException(
                    $"Não é possível unificar as duplas porque a partida {partida.Id} ficaria com a mesma dupla nos dois lados.");
            }

            partida.AtualizarDataModificacao();
        }

        return partidas.Count;
    }

    private async Task MigrarInscricoesDaDuplaAsync(
        Guid duplaOrigemId,
        Guid duplaDestinoId,
        ContadorSaneamento contador,
        CancellationToken cancellationToken)
    {
        var inscricoesOrigem = await dbContext.InscricoesCampeonato
            .Where(x => x.DuplaId == duplaOrigemId)
            .ToListAsync(cancellationToken);

        foreach (var inscricao in inscricoesOrigem)
        {
            var inscricaoDestino = await dbContext.InscricoesCampeonato
                .FirstOrDefaultAsync(
                    x => x.Id != inscricao.Id &&
                         x.CategoriaCompeticaoId == inscricao.CategoriaCompeticaoId &&
                         x.DuplaId == duplaDestinoId,
                    cancellationToken);

            if (inscricaoDestino is not null)
            {
                MesclarInscricao(inscricaoDestino, inscricao);
                dbContext.InscricoesCampeonato.Remove(inscricao);
                contador.InscricoesConsolidadas++;
                continue;
            }

            inscricao.DuplaId = duplaDestinoId;
            inscricao.AtualizarDataModificacao();
            contador.InscricoesAtualizadas++;
        }
    }

    private async Task MigrarGruposAsync(
        Guid atletaPrincipalId,
        Guid atletaDuplicadoId,
        ContadorSaneamento contador,
        CancellationToken cancellationToken)
    {
        var vinculos = await dbContext.GruposAtletas
            .Where(x => x.AtletaId == atletaDuplicadoId)
            .ToListAsync(cancellationToken);

        foreach (var vinculo in vinculos)
        {
            var existente = await dbContext.GruposAtletas
                .FirstOrDefaultAsync(
                    x => x.Id != vinculo.Id &&
                         x.GrupoId == vinculo.GrupoId &&
                         x.AtletaId == atletaPrincipalId,
                    cancellationToken);

            if (existente is not null)
            {
                dbContext.GruposAtletas.Remove(vinculo);
                contador.GruposConsolidados++;
                continue;
            }

            vinculo.AtletaId = atletaPrincipalId;
            vinculo.AtualizarDataModificacao();
            contador.GruposAtualizados++;
        }
    }

    private async Task MigrarAprovacoesAsync(
        Guid atletaPrincipalId,
        Guid atletaDuplicadoId,
        ContadorSaneamento contador,
        CancellationToken cancellationToken)
    {
        var aprovacoes = await dbContext.PartidasAprovacoes
            .Where(x => x.AtletaId == atletaDuplicadoId)
            .ToListAsync(cancellationToken);

        foreach (var aprovacao in aprovacoes)
        {
            var existente = await dbContext.PartidasAprovacoes
                .FirstOrDefaultAsync(
                    x => x.Id != aprovacao.Id &&
                         x.PartidaId == aprovacao.PartidaId &&
                         x.AtletaId == atletaPrincipalId,
                    cancellationToken);

            if (existente is not null)
            {
                MesclarAprovacao(existente, aprovacao);
                dbContext.PartidasAprovacoes.Remove(aprovacao);
                contador.AprovacoesConsolidadas++;
                continue;
            }

            aprovacao.AtletaId = atletaPrincipalId;
            aprovacao.AtualizarDataModificacao();
            contador.AprovacoesAtualizadas++;
        }
    }

    private async Task MigrarPendenciasAsync(
        Guid atletaPrincipalId,
        Guid atletaDuplicadoId,
        ContadorSaneamento contador,
        CancellationToken cancellationToken)
    {
        var pendencias = await dbContext.PendenciasUsuarios
            .Where(x => x.AtletaId == atletaDuplicadoId)
            .ToListAsync(cancellationToken);

        foreach (var pendencia in pendencias)
        {
            pendencia.AtletaId = atletaPrincipalId;
            pendencia.AtualizarDataModificacao();
        }

        contador.PendenciasAtualizadas += pendencias.Count;
    }

    private async Task MigrarConvitesAsync(
        Guid atletaPrincipalId,
        Guid atletaDuplicadoId,
        ContadorSaneamento contador,
        CancellationToken cancellationToken)
    {
        var convites = await dbContext.ConvitesCadastro
            .Where(x => x.AtletaId == atletaDuplicadoId)
            .ToListAsync(cancellationToken);

        foreach (var convite in convites)
        {
            convite.AtletaId = atletaPrincipalId;
            convite.AtualizarDataModificacao();
        }

        contador.ConvitesAtualizados += convites.Count;
    }

    private async Task MigrarUsuariosAsync(
        Guid atletaPrincipalId,
        Guid atletaDuplicadoId,
        ContadorSaneamento contador,
        CancellationToken cancellationToken)
    {
        var usuarioPrincipal = await dbContext.Usuarios
            .FirstOrDefaultAsync(x => x.AtletaId == atletaPrincipalId, cancellationToken);
        var usuariosDuplicado = await dbContext.Usuarios
            .Where(x => x.AtletaId == atletaDuplicadoId)
            .ToListAsync(cancellationToken);

        if (usuariosDuplicado.Count == 0)
        {
            return;
        }

        if (usuarioPrincipal is not null || usuariosDuplicado.Count > 1)
        {
            throw new RegraNegocioException(
                $"Não é possível unificar o atleta {atletaDuplicadoId} porque há conflito de usuários vinculados.");
        }

        var usuario = usuariosDuplicado[0];
        usuario.AtletaId = atletaPrincipalId;
        usuario.AtualizarDataModificacao();
        contador.UsuariosAtualizados++;
    }

    private async Task<string> MontarNomeDuplaAsync(
        Guid atleta1Id,
        Guid atleta2Id,
        CancellationToken cancellationToken)
    {
        var atletas = await dbContext.Atletas
            .Where(x => x.Id == atleta1Id || x.Id == atleta2Id)
            .ToDictionaryAsync(x => x.Id, x => x.Nome, cancellationToken);

        return $"{atletas[atleta1Id]} / {atletas[atleta2Id]}";
    }

    private static void MesclarInscricao(
        InscricaoCampeonato destino,
        InscricaoCampeonato origem)
    {
        destino.Pago = destino.Pago || origem.Pago;
        destino.Status = EscolherStatusInscricao(destino.Status, origem.Status);
        if (string.IsNullOrWhiteSpace(destino.Observacao) && !string.IsNullOrWhiteSpace(origem.Observacao))
        {
            destino.Observacao = origem.Observacao;
        }

        if (origem.DataInscricaoUtc < destino.DataInscricaoUtc)
        {
            destino.DataInscricaoUtc = origem.DataInscricaoUtc;
        }

        destino.AtualizarDataModificacao();
    }

    private static void MesclarAprovacao(
        PartidaAprovacao destino,
        PartidaAprovacao origem)
    {
        if (PrioridadeStatusAprovacao(origem.Status) > PrioridadeStatusAprovacao(destino.Status))
        {
            destino.Status = origem.Status;
            destino.DataResposta = origem.DataResposta;
            destino.Observacao = origem.Observacao;
        }
        else if (string.IsNullOrWhiteSpace(destino.Observacao) && !string.IsNullOrWhiteSpace(origem.Observacao))
        {
            destino.Observacao = origem.Observacao;
        }

        if (origem.DataSolicitacao < destino.DataSolicitacao)
        {
            destino.DataSolicitacao = origem.DataSolicitacao;
        }

        destino.AtualizarDataModificacao();
    }

    private static StatusInscricaoCampeonato EscolherStatusInscricao(
        StatusInscricaoCampeonato atual,
        StatusInscricaoCampeonato candidato)
    {
        if (atual == StatusInscricaoCampeonato.Ativa || candidato == StatusInscricaoCampeonato.Ativa)
        {
            return StatusInscricaoCampeonato.Ativa;
        }

        if (atual == StatusInscricaoCampeonato.PendenteAprovacao ||
            candidato == StatusInscricaoCampeonato.PendenteAprovacao)
        {
            return StatusInscricaoCampeonato.PendenteAprovacao;
        }

        return StatusInscricaoCampeonato.Cancelada;
    }

    private static int PrioridadeStatusAprovacao(StatusPartidaAprovacao status)
    {
        return status switch
        {
            StatusPartidaAprovacao.Aprovada => 3,
            StatusPartidaAprovacao.Contestada => 3,
            StatusPartidaAprovacao.Pendente => 1,
            _ => 0
        };
    }

    private static (Guid atleta1Id, Guid atleta2Id) NormalizarAtletas(Guid atleta1Id, Guid atleta2Id)
    {
        return atleta1Id.CompareTo(atleta2Id) <= 0
            ? (atleta1Id, atleta2Id)
            : (atleta2Id, atleta1Id);
    }

    private static string? NormalizarEmail(string? email)
    {
        var normalizado = email?.Trim().ToLowerInvariant();
        return string.IsNullOrWhiteSpace(normalizado) ? null : normalizado;
    }

    private sealed record GrupoDuplicadoEmail(string Email, IReadOnlyList<Atleta> Atletas);

    private sealed class ContadorSaneamento
    {
        public int DuplasAtualizadas { get; set; }
        public int DuplasConsolidadas { get; set; }
        public int PartidasAtualizadas { get; set; }
        public int InscricoesAtualizadas { get; set; }
        public int InscricoesConsolidadas { get; set; }
        public int GruposAtualizados { get; set; }
        public int GruposConsolidados { get; set; }
        public int AprovacoesAtualizadas { get; set; }
        public int AprovacoesConsolidadas { get; set; }
        public int PendenciasAtualizadas { get; set; }
        public int ConvitesAtualizados { get; set; }
        public int UsuariosAtualizados { get; set; }
        public int AtletasRemovidos { get; set; }

        public SaneamentoAtletasEmailContadoresDto ParaDto()
        {
            return new SaneamentoAtletasEmailContadoresDto(
                DuplasAtualizadas,
                DuplasConsolidadas,
                PartidasAtualizadas,
                InscricoesAtualizadas,
                InscricoesConsolidadas,
                GruposAtualizados,
                GruposConsolidados,
                AprovacoesAtualizadas,
                AprovacoesConsolidadas,
                PendenciasAtualizadas,
                ConvitesAtualizados,
                UsuariosAtualizados,
                AtletasRemovidos);
        }
    }
}
