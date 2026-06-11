using Microsoft.EntityFrameworkCore;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;
using PlataformaFutevolei.Infraestrutura.Persistencia;

namespace PlataformaFutevolei.Infraestrutura.Repositorios;

public class ConsolidacaoAtletaRepositorio(PlataformaFutevoleiDbContext dbContext) : IConsolidacaoAtletaRepositorio
{
    public async Task<IDictionary<Guid, ConsolidacaoAtletaMetricasDto>> ObterMetricasAsync(
        IEnumerable<Guid> atletaIds,
        CancellationToken cancellationToken = default)
    {
        var ids = atletaIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, ConsolidacaoAtletaMetricasDto>();
        }

        var duplas = await dbContext.Duplas
            .AsNoTracking()
            .Where(x => ids.Contains(x.Atleta1Id) || ids.Contains(x.Atleta2Id))
            .Select(x => new
            {
                x.Id,
                x.Atleta1Id,
                x.Atleta2Id
            })
            .ToListAsync(cancellationToken);
        var duplaIds = duplas.Select(x => x.Id).ToList();
        var partidasPorDupla = await dbContext.Partidas
            .AsNoTracking()
            .Where(x =>
                (x.DuplaAId.HasValue && duplaIds.Contains(x.DuplaAId.Value)) ||
                (x.DuplaBId.HasValue && duplaIds.Contains(x.DuplaBId.Value)))
            .Select(x => new
            {
                x.Id,
                x.DuplaAId,
                x.DuplaBId
            })
            .ToListAsync(cancellationToken);

        var grupos = await dbContext.GruposAtletas
            .AsNoTracking()
            .Where(x => ids.Contains(x.AtletaId))
            .GroupBy(x => x.AtletaId)
            .Select(x => new { AtletaId = x.Key, Total = x.Count() })
            .ToDictionaryAsync(x => x.AtletaId, x => x.Total, cancellationToken);
        var aprovacoes = await dbContext.PartidasAprovacoes
            .AsNoTracking()
            .Where(x => ids.Contains(x.AtletaId))
            .GroupBy(x => x.AtletaId)
            .Select(x => new { AtletaId = x.Key, Total = x.Count() })
            .ToDictionaryAsync(x => x.AtletaId, x => x.Total, cancellationToken);
        var pendencias = await dbContext.PendenciasUsuarios
            .AsNoTracking()
            .Where(x => x.AtletaId.HasValue && ids.Contains(x.AtletaId.Value))
            .GroupBy(x => x.AtletaId!.Value)
            .Select(x => new { AtletaId = x.Key, Total = x.Count() })
            .ToDictionaryAsync(x => x.AtletaId, x => x.Total, cancellationToken);
        var convites = await dbContext.ConvitesCadastro
            .AsNoTracking()
            .Where(x => x.AtletaId.HasValue && ids.Contains(x.AtletaId.Value))
            .GroupBy(x => x.AtletaId!.Value)
            .Select(x => new { AtletaId = x.Key, Total = x.Count() })
            .ToDictionaryAsync(x => x.AtletaId, x => x.Total, cancellationToken);
        var medidas = await dbContext.AtletasMedidas
            .AsNoTracking()
            .Where(x => ids.Contains(x.AtletaId))
            .GroupBy(x => x.AtletaId)
            .Select(x => new { AtletaId = x.Key, Total = x.Count() })
            .ToDictionaryAsync(x => x.AtletaId, x => x.Total, cancellationToken);
        var usuarios = await dbContext.Usuarios
            .AsNoTracking()
            .Where(x => x.AtletaId.HasValue && ids.Contains(x.AtletaId.Value))
            .Select(x => x.AtletaId!.Value)
            .ToListAsync(cancellationToken);
        var atletas = await dbContext.Atletas
            .AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .Select(x => new { x.Id, x.DataCriacao })
            .ToListAsync(cancellationToken);

        return atletas.ToDictionary(
            x => x.Id,
            x =>
            {
                var duplasAtleta = duplas
                    .Where(dupla => dupla.Atleta1Id == x.Id || dupla.Atleta2Id == x.Id)
                    .Select(dupla => dupla.Id)
                    .ToHashSet();
                var totalPartidas = partidasPorDupla
                    .Count(partida =>
                        (partida.DuplaAId.HasValue && duplasAtleta.Contains(partida.DuplaAId.Value)) ||
                        (partida.DuplaBId.HasValue && duplasAtleta.Contains(partida.DuplaBId.Value)));

                return new ConsolidacaoAtletaMetricasDto(
                    x.Id,
                    usuarios.Contains(x.Id),
                    totalPartidas,
                    duplasAtleta.Count,
                    grupos.GetValueOrDefault(x.Id),
                    aprovacoes.GetValueOrDefault(x.Id),
                    pendencias.GetValueOrDefault(x.Id),
                    convites.GetValueOrDefault(x.Id),
                    medidas.GetValueOrDefault(x.Id),
                    x.DataCriacao);
            });
    }

    public async Task<IReadOnlyList<IReadOnlyList<Atleta>>> ListarDuplicadosPorEmailAsync(
        CancellationToken cancellationToken = default)
    {
        var atletas = await dbContext.Atletas
            .Include(x => x.Usuario)
            .Where(x => x.Email != null && x.Email.Trim() != string.Empty)
            .OrderBy(x => x.DataCriacao)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        return atletas
            .GroupBy(x => NormalizarEmail(x.Email))
            .Where(x => !string.IsNullOrWhiteSpace(x.Key) && x.Count() > 1)
            .Select(x => (IReadOnlyList<Atleta>)x.ToList())
            .ToList();
    }

    public async Task<SaneamentoAtletasEmailContadoresDto> TransferirVinculosAsync(
        Guid atletaVencedorId,
        Guid atletaPerdedorId,
        CancellationToken cancellationToken = default)
    {
        if (atletaVencedorId == atletaPerdedorId)
        {
            return new SaneamentoAtletasEmailContadoresDto(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        }

        var vencedor = await dbContext.Atletas.FirstOrDefaultAsync(x => x.Id == atletaVencedorId, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Atleta vencedor não encontrado.");
        var perdedor = await dbContext.Atletas.FirstOrDefaultAsync(x => x.Id == atletaPerdedorId, cancellationToken);
        if (perdedor is null)
        {
            return new SaneamentoAtletasEmailContadoresDto(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        }

        var contador = new ContadorSaneamento();
        await ConsolidarDuplasAsync(vencedor, perdedor, contador, cancellationToken);
        await MigrarGruposAsync(vencedor.Id, perdedor.Id, contador, cancellationToken);
        await MigrarAprovacoesAsync(vencedor.Id, perdedor.Id, contador, cancellationToken);
        await MigrarPendenciasAsync(vencedor.Id, perdedor.Id, contador, cancellationToken);
        await MigrarConvitesAsync(vencedor.Id, perdedor.Id, contador, cancellationToken);
        await MigrarMedidasAsync(vencedor.Id, perdedor.Id, contador, cancellationToken);
        await MigrarUsuariosAsync(vencedor.Id, perdedor.Id, contador, cancellationToken);
        await GarantirSemReferenciasAoPerdedorAsync(perdedor.Id, cancellationToken);

        dbContext.Atletas.Remove(perdedor);
        contador.AtletasRemovidos++;
        vencedor.AtualizarDataModificacao();

        return contador.ParaDto();
    }

    private async Task ConsolidarDuplasAsync(
        Atleta vencedor,
        Atleta perdedor,
        ContadorSaneamento contador,
        CancellationToken cancellationToken)
    {
        var duplas = await dbContext.Duplas
            .Where(x => x.Atleta1Id == perdedor.Id || x.Atleta2Id == perdedor.Id)
            .OrderBy(x => x.DataCriacao)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        foreach (var dupla in duplas)
        {
            var parceiroId = dupla.Atleta1Id == perdedor.Id ? dupla.Atleta2Id : dupla.Atleta1Id;
            if (parceiroId == vencedor.Id)
            {
                throw new RegraNegocioException(
                    $"Não é possível unificar atletas porque a dupla {dupla.Id} ficaria com o mesmo atleta duas vezes.");
            }

            var (atleta1Id, atleta2Id) = NormalizarAtletas(vencedor.Id, parceiroId);
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
        Guid atletaVencedorId,
        Guid atletaPerdedorId,
        ContadorSaneamento contador,
        CancellationToken cancellationToken)
    {
        var vinculos = await dbContext.GruposAtletas
            .Where(x => x.AtletaId == atletaPerdedorId)
            .ToListAsync(cancellationToken);

        foreach (var vinculo in vinculos)
        {
            var existente = await dbContext.GruposAtletas
                .FirstOrDefaultAsync(
                    x => x.Id != vinculo.Id &&
                         x.GrupoId == vinculo.GrupoId &&
                         x.AtletaId == atletaVencedorId,
                    cancellationToken);

            if (existente is not null)
            {
                if (vinculo.DataCriacao < existente.DataCriacao)
                {
                    dbContext.Entry(existente).Property(nameof(GrupoAtleta.DataCriacao)).CurrentValue = vinculo.DataCriacao;
                    existente.AtualizarDataModificacao();
                }

                dbContext.GruposAtletas.Remove(vinculo);
                contador.GruposConsolidados++;
                continue;
            }

            vinculo.AtletaId = atletaVencedorId;
            vinculo.AtualizarDataModificacao();
            contador.GruposAtualizados++;
        }
    }

    private async Task MigrarAprovacoesAsync(
        Guid atletaVencedorId,
        Guid atletaPerdedorId,
        ContadorSaneamento contador,
        CancellationToken cancellationToken)
    {
        var aprovacoes = await dbContext.PartidasAprovacoes
            .Where(x => x.AtletaId == atletaPerdedorId)
            .ToListAsync(cancellationToken);

        foreach (var aprovacao in aprovacoes)
        {
            var existente = await dbContext.PartidasAprovacoes
                .FirstOrDefaultAsync(
                    x => x.Id != aprovacao.Id &&
                         x.PartidaId == aprovacao.PartidaId &&
                         x.AtletaId == atletaVencedorId,
                    cancellationToken);

            if (existente is not null)
            {
                MesclarAprovacao(existente, aprovacao);
                dbContext.PartidasAprovacoes.Remove(aprovacao);
                contador.AprovacoesConsolidadas++;
                continue;
            }

            aprovacao.AtletaId = atletaVencedorId;
            aprovacao.AtualizarDataModificacao();
            contador.AprovacoesAtualizadas++;
        }
    }

    private async Task MigrarPendenciasAsync(
        Guid atletaVencedorId,
        Guid atletaPerdedorId,
        ContadorSaneamento contador,
        CancellationToken cancellationToken)
    {
        var pendencias = await dbContext.PendenciasUsuarios
            .Where(x => x.AtletaId == atletaPerdedorId)
            .ToListAsync(cancellationToken);

        foreach (var pendencia in pendencias)
        {
            var existente = await dbContext.PendenciasUsuarios
                .FirstOrDefaultAsync(
                    x => x.Id != pendencia.Id &&
                         x.AtletaId == atletaVencedorId &&
                         x.UsuarioId == pendencia.UsuarioId &&
                         x.Tipo == pendencia.Tipo &&
                         x.PartidaId == pendencia.PartidaId,
                    cancellationToken);

            if (existente is not null)
            {
                MesclarPendencia(existente, pendencia);
                dbContext.PendenciasUsuarios.Remove(pendencia);
                contador.PendenciasAtualizadas++;
                continue;
            }

            pendencia.AtletaId = atletaVencedorId;
            pendencia.AtualizarDataModificacao();
            contador.PendenciasAtualizadas++;
        }
    }

    private async Task MigrarConvitesAsync(
        Guid atletaVencedorId,
        Guid atletaPerdedorId,
        ContadorSaneamento contador,
        CancellationToken cancellationToken)
    {
        var convites = await dbContext.ConvitesCadastro
            .Where(x => x.AtletaId == atletaPerdedorId)
            .ToListAsync(cancellationToken);

        foreach (var convite in convites)
        {
            var emailNormalizado = NormalizarEmail(convite.Email);
            var existente = await dbContext.ConvitesCadastro
                .FirstOrDefaultAsync(
                    x => x.Id != convite.Id &&
                         x.AtletaId == atletaVencedorId &&
                         x.PerfilDestino == convite.PerfilDestino &&
                         x.PartidaId == convite.PartidaId &&
                         x.Email.Trim().ToLower() == emailNormalizado,
                    cancellationToken);

            if (existente is not null)
            {
                MesclarConvite(existente, convite);
                dbContext.ConvitesCadastro.Remove(convite);
                contador.ConvitesAtualizados++;
                continue;
            }

            convite.AtletaId = atletaVencedorId;
            convite.AtualizarDataModificacao();
            contador.ConvitesAtualizados++;
        }
    }

    private async Task MigrarMedidasAsync(
        Guid atletaVencedorId,
        Guid atletaPerdedorId,
        ContadorSaneamento contador,
        CancellationToken cancellationToken)
    {
        var medidasOrigem = await dbContext.AtletasMedidas
            .FirstOrDefaultAsync(x => x.AtletaId == atletaPerdedorId, cancellationToken);
        if (medidasOrigem is null)
        {
            return;
        }

        var medidasDestino = await dbContext.AtletasMedidas
            .FirstOrDefaultAsync(x => x.AtletaId == atletaVencedorId, cancellationToken);
        if (medidasDestino is not null)
        {
            dbContext.AtletasMedidas.Remove(medidasOrigem);
            return;
        }

        medidasOrigem.AtletaId = atletaVencedorId;
        medidasOrigem.AtualizarDataModificacao();
    }

    private async Task MigrarUsuariosAsync(
        Guid atletaVencedorId,
        Guid atletaPerdedorId,
        ContadorSaneamento contador,
        CancellationToken cancellationToken)
    {
        var usuarioVencedor = await dbContext.Usuarios
            .FirstOrDefaultAsync(x => x.AtletaId == atletaVencedorId, cancellationToken);
        var usuariosPerdedor = await dbContext.Usuarios
            .Where(x => x.AtletaId == atletaPerdedorId)
            .ToListAsync(cancellationToken);

        if (usuariosPerdedor.Count == 0)
        {
            return;
        }

        if (usuarioVencedor is not null || usuariosPerdedor.Count > 1)
        {
            throw new RegraNegocioException(
                $"Não é possível unificar o atleta {atletaPerdedorId} porque há conflito de usuários vinculados.");
        }

        var usuario = usuariosPerdedor[0];
        usuario.AtletaId = atletaVencedorId;
        usuario.AtualizarDataModificacao();
        contador.UsuariosAtualizados++;
    }

    private async Task GarantirSemReferenciasAoPerdedorAsync(
        Guid atletaPerdedorId,
        CancellationToken cancellationToken)
    {
        var possuiReferencia =
            await ExisteReferenciaFinalAsync(
                dbContext.Usuarios.AsNoTracking().Where(x => x.AtletaId == atletaPerdedorId),
                x => x.AtletaId == atletaPerdedorId,
                cancellationToken) ||
            await ExisteReferenciaFinalAsync(
                dbContext.Duplas.AsNoTracking().Where(x => x.Atleta1Id == atletaPerdedorId || x.Atleta2Id == atletaPerdedorId),
                x => x.Atleta1Id == atletaPerdedorId || x.Atleta2Id == atletaPerdedorId,
                cancellationToken) ||
            await ExisteReferenciaFinalAsync(
                dbContext.GruposAtletas.AsNoTracking().Where(x => x.AtletaId == atletaPerdedorId),
                x => x.AtletaId == atletaPerdedorId,
                cancellationToken) ||
            await ExisteReferenciaFinalAsync(
                dbContext.PartidasAprovacoes.AsNoTracking().Where(x => x.AtletaId == atletaPerdedorId),
                x => x.AtletaId == atletaPerdedorId,
                cancellationToken) ||
            await ExisteReferenciaFinalAsync(
                dbContext.PendenciasUsuarios.AsNoTracking().Where(x => x.AtletaId == atletaPerdedorId),
                x => x.AtletaId == atletaPerdedorId,
                cancellationToken) ||
            await ExisteReferenciaFinalAsync(
                dbContext.ConvitesCadastro.AsNoTracking().Where(x => x.AtletaId == atletaPerdedorId),
                x => x.AtletaId == atletaPerdedorId,
                cancellationToken) ||
            await ExisteReferenciaFinalAsync(
                dbContext.AtletasMedidas.AsNoTracking().Where(x => x.AtletaId == atletaPerdedorId),
                x => x.AtletaId == atletaPerdedorId,
                cancellationToken);

        if (possuiReferencia)
        {
            throw new RegraNegocioException(
                $"Não é possível remover o atleta {atletaPerdedorId} porque ainda existem vínculos após a consolidação.");
        }
    }

    private async Task<bool> ExisteReferenciaFinalAsync<TEntity>(
        IQueryable<TEntity> referenciasBanco,
        Func<TEntity, bool> possuiReferencia,
        CancellationToken cancellationToken)
        where TEntity : EntidadeBase
    {
        dbContext.ChangeTracker.DetectChanges();
        var entradasRastreadas = dbContext.ChangeTracker.Entries<TEntity>().ToList();
        if (entradasRastreadas.Any(x => x.State != EntityState.Deleted && possuiReferencia(x.Entity)))
        {
            return true;
        }

        var idsRastreados = entradasRastreadas.Select(x => x.Entity.Id).ToList();
        return await referenciasBanco
            .Where(x => !idsRastreados.Contains(x.Id))
            .AnyAsync(cancellationToken);
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

    private static void MesclarInscricao(InscricaoCampeonato destino, InscricaoCampeonato origem)
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

    private static void MesclarAprovacao(PartidaAprovacao destino, PartidaAprovacao origem)
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

    private static void MesclarPendencia(PendenciaUsuario destino, PendenciaUsuario origem)
    {
        if (PrioridadeStatusPendencia(origem.Status) > PrioridadeStatusPendencia(destino.Status))
        {
            destino.Status = origem.Status;
            destino.DataConclusao = origem.DataConclusao;
        }
        else if (!destino.DataConclusao.HasValue && origem.DataConclusao.HasValue)
        {
            destino.DataConclusao = origem.DataConclusao;
        }

        if (string.IsNullOrWhiteSpace(destino.Observacao) && !string.IsNullOrWhiteSpace(origem.Observacao))
        {
            destino.Observacao = origem.Observacao;
        }

        if (origem.DataCriacao < destino.DataCriacao)
        {
            destino.AtualizarDataModificacao();
        }

        destino.AtualizarDataModificacao();
    }

    private static void MesclarConvite(ConviteCadastro destino, ConviteCadastro origem)
    {
        if (origem.UsadoEmUtc.HasValue &&
            (!destino.UsadoEmUtc.HasValue || origem.UsadoEmUtc.Value < destino.UsadoEmUtc.Value))
        {
            destino.UsadoEmUtc = origem.UsadoEmUtc;
            destino.CodigoConvite = null;
            destino.CodigoConviteHash = null;
        }

        destino.Ativo = destino.Ativo || origem.Ativo;
        if (origem.ExpiraEmUtc > destino.ExpiraEmUtc)
        {
            destino.ExpiraEmUtc = origem.ExpiraEmUtc;
        }

        destino.Telefone ??= origem.Telefone;
        destino.CanalEnvio ??= origem.CanalEnvio;
        destino.EmailEnviadoEmUtc ??= origem.EmailEnviadoEmUtc;
        destino.WhatsappEnviadoEmUtc ??= origem.WhatsappEnviadoEmUtc;
        destino.ErroEnvioEmail ??= origem.ErroEnvioEmail;
        destino.ErroEnvioWhatsapp ??= origem.ErroEnvioWhatsapp;
        destino.UltimaTentativaEnvioEmailEmUtc ??= origem.UltimaTentativaEnvioEmailEmUtc;
        destino.UltimaTentativaEnvioWhatsappEmUtc ??= origem.UltimaTentativaEnvioWhatsappEmUtc;
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

    private static int PrioridadeStatusPendencia(StatusPendenciaUsuario status)
    {
        return status switch
        {
            StatusPendenciaUsuario.Concluida => 3,
            StatusPendenciaUsuario.Cancelada => 2,
            StatusPendenciaUsuario.Pendente => 1,
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
