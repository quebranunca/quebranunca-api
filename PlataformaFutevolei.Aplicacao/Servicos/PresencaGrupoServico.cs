using System.Globalization;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using PlataformaFutevolei.Aplicacao.Configuracoes;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Interfaces.Seguranca;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Aplicacao.Utilitarios;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Aplicacao.Servicos;

public class PresencaGrupoServico(
    IPresencaGrupoRepositorio presencaGrupoRepositorio,
    INotificacaoUsuarioRepositorio notificacaoUsuarioRepositorio,
    IEntregaNotificacaoExternaServico entregaNotificacaoExternaServico,
    IUnidadeTrabalho unidadeTrabalho,
    IAutorizacaoUsuarioServico autorizacaoUsuarioServico,
    AgendaPresencaGrupoConfiguracao configuracao,
    ILogger<PresencaGrupoServico> logger) : IPresencaGrupoServico
{
    private const string OrigemNotificacao = "presenca-grupo";
    private const string TemplateWhatsapp = "qnf.grupo.presenca.v1";
    private static readonly TimeSpan IntervaloMinimoNovaTentativa = TimeSpan.FromHours(1);
    private const int MaximoTentativasWhatsapp = 3;

    public async Task ProcessarAgendaDoDiaAsync(
        DateTime agoraUtc,
        CancellationToken cancellationToken = default)
    {
        agoraUtc = GarantirUtc(agoraUtc);
        var fusoHorario = ObterFusoHorario();
        var agoraLocal = TimeZoneInfo.ConvertTimeFromUtc(agoraUtc, fusoHorario);
        var hoje = DateOnly.FromDateTime(agoraLocal);
        var horarioLocal = TimeOnly.FromDateTime(agoraLocal);
        var horaEnvio = ObterHoraEnvioLocal();
        var grupos = await presencaGrupoRepositorio.ListarGruposComAgendaAsync(cancellationToken);

        foreach (var grupo in grupos.Where(x => TemJogoNaData(x, hoje)))
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var encontroExistente = await presencaGrupoRepositorio.ObterEncontroAsync(
                    grupo.Id,
                    hoje,
                    cancellationToken);
                if (encontroExistente is null &&
                    grupo.HorarioFim.HasValue &&
                    horarioLocal > grupo.HorarioFim.Value)
                {
                    continue;
                }

                var encontro = await GarantirEncontroEConfirmacoesAsync(
                    grupo,
                    hoje,
                    fusoHorario,
                    cancellationToken,
                    encontroExistente);

                if (horarioLocal < horaEnvio)
                {
                    continue;
                }

                if (horarioLocal > encontro.HorarioFim)
                {
                    continue;
                }

                await CriarNotificacoesInternasAsync(encontro, cancellationToken);
                await EnviarConfirmacoesWhatsappAsync(encontro, agoraUtc, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Falha ao preparar confirmações de presença. GrupoId: {GrupoId}; DataJogo: {DataJogo}.",
                    grupo.Id,
                    hoje);
            }
        }
    }

    public async Task<PainelPresencaGrupoDto> ObterPainelAsync(
        Guid grupoId,
        CancellationToken cancellationToken = default)
    {
        await autorizacaoUsuarioServico.GarantirGestaoGrupoAsync(grupoId, cancellationToken);
        var grupo = await presencaGrupoRepositorio.ObterGrupoComAgendaAsync(grupoId, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Grupo não encontrado.");
        var fusoHorario = ObterFusoHorario();
        var agoraLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, fusoHorario);
        var hoje = DateOnly.FromDateTime(agoraLocal);
        var horarioLocal = TimeOnly.FromDateTime(agoraLocal);
        var encontro = await presencaGrupoRepositorio.ObterEncontroAsync(grupoId, hoje, cancellationToken);
        var jogoHojeTerminou = encontro is null &&
            TemJogoNaData(grupo, hoje) &&
            grupo.HorarioFim.HasValue &&
            horarioLocal > grupo.HorarioFim.Value;

        if (encontro is null && TemJogoNaData(grupo, hoje) && !jogoHojeTerminou)
        {
            encontro = await GarantirEncontroEConfirmacoesAsync(grupo, hoje, fusoHorario, cancellationToken);
        }

        return new PainelPresencaGrupoDto(
            grupo.Id,
            grupo.Nome,
            MontarAgenda(grupo),
            ObterProximaDataJogo(grupo, jogoHojeTerminou ? hoje.AddDays(1) : hoje),
            encontro is null ? null : MontarEncontro(encontro));
    }

    public async Task<ConfirmacaoPresencaGrupoPublicaDto> ConsultarAsync(
        string codigo,
        CancellationToken cancellationToken = default)
    {
        var confirmacao = await ObterConfirmacaoObrigatoriaAsync(codigo, cancellationToken);
        return MontarPublica(confirmacao, DateTime.UtcNow);
    }

    public async Task<ConfirmacaoPresencaGrupoPublicaDto> ResponderAsync(
        string codigo,
        bool vaiParticipar,
        CancellationToken cancellationToken = default)
    {
        var confirmacao = await ObterConfirmacaoObrigatoriaAsync(codigo, cancellationToken);
        var agoraUtc = DateTime.UtcNow;
        if (!PodeResponder(confirmacao, agoraUtc))
        {
            throw new RegraNegocioException("O prazo para confirmar presença neste encontro terminou.");
        }

        confirmacao.Responder(vaiParticipar, agoraUtc);
        presencaGrupoRepositorio.AtualizarConfirmacao(confirmacao);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
        return MontarPublica(confirmacao, agoraUtc);
    }

    private async Task<EncontroGrupo> GarantirEncontroEConfirmacoesAsync(
        Grupo grupo,
        DateOnly dataJogo,
        TimeZoneInfo fusoHorario,
        CancellationToken cancellationToken,
        EncontroGrupo? encontroExistente = null)
    {
        if (!grupo.HorarioInicio.HasValue || !grupo.HorarioFim.HasValue)
        {
            throw new RegraNegocioException("A agenda do grupo ainda não possui horários completos.");
        }

        var encontro = encontroExistente ??
            await presencaGrupoRepositorio.ObterEncontroAsync(grupo.Id, dataJogo, cancellationToken);
        if (encontro is null)
        {
            encontro = new EncontroGrupo
            {
                GrupoId = grupo.Id,
                DataJogo = dataJogo,
                HorarioInicio = grupo.HorarioInicio.Value,
                HorarioFim = grupo.HorarioFim.Value,
                ArenaId = grupo.ArenaId,
                LocalSnapshot = ObterNomeLocal(grupo)
            };
            await presencaGrupoRepositorio.AdicionarEncontroAsync(encontro, cancellationToken);
            await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
            encontro = await presencaGrupoRepositorio.ObterEncontroAsync(grupo.Id, dataJogo, cancellationToken)
                ?? throw new EntidadeNaoEncontradaException("Encontro do grupo não encontrado.");
        }

        var idsExistentes = encontro.Confirmacoes
            .Select(x => x.AtletaId)
            .ToHashSet();
        var expiracao = CalcularExpiracaoUtc(dataJogo, encontro.HorarioFim, fusoHorario);
        var adicionouConfirmacao = false;

        foreach (var membro in grupo.Atletas
                     .Where(x => x.Atleta is not null)
                     .DistinctBy(x => x.AtletaId))
        {
            if (idsExistentes.Contains(membro.AtletaId))
            {
                continue;
            }

            await presencaGrupoRepositorio.AdicionarConfirmacaoAsync(
                new ConfirmacaoPresencaGrupo
                {
                    EncontroGrupoId = encontro.Id,
                    AtletaId = membro.AtletaId,
                    CodigoAcesso = GerarCodigoAcesso(),
                    ExpiraEmUtc = expiracao
                },
                cancellationToken);
            adicionouConfirmacao = true;
        }

        if (adicionouConfirmacao)
        {
            await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
            encontro = await presencaGrupoRepositorio.ObterEncontroAsync(grupo.Id, dataJogo, cancellationToken)
                ?? throw new EntidadeNaoEncontradaException("Encontro do grupo não encontrado.");
        }

        return encontro;
    }

    private async Task CriarNotificacoesInternasAsync(
        EncontroGrupo encontro,
        CancellationToken cancellationToken)
    {
        var idsMembrosAtuais = ObterIdsMembrosAtuais(encontro);
        var notificacoes = encontro.Confirmacoes
            .Where(x => idsMembrosAtuais.Contains(x.AtletaId))
            .Where(x => x.Status == StatusConfirmacaoPresencaGrupo.Pendente)
            .Where(x => x.Atleta.Usuario is { Ativo: true, DadosAnonimizados: false })
            .Select(x => new NotificacaoUsuario
            {
                UsuarioId = x.Atleta.Usuario!.Id,
                Origem = OrigemNotificacao,
                ChaveIdempotencia = x.Id.ToString("N"),
                Tipo = TipoNotificacaoUsuario.AcaoNecessaria,
                Prioridade = PrioridadePendenciaUsuario.Alta,
                Titulo = $"Você vai jogar com {encontro.Grupo.Nome}?",
                Mensagem = MontarResumoEncontro(encontro),
                LinkAcao = MontarLinkConfirmacao(x.CodigoAcesso, absoluto: false),
                TextoAcao = "Confirmar presença",
                ReferenciaTipo = "encontro-grupo",
                ReferenciaId = encontro.Id.ToString()
            })
            .ToList();

        if (notificacoes.Count == 0)
        {
            return;
        }

        await notificacaoUsuarioRepositorio.AdicionarIntervaloAsync(notificacoes, cancellationToken);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
    }

    private async Task EnviarConfirmacoesWhatsappAsync(
        EncontroGrupo encontro,
        DateTime agoraUtc,
        CancellationToken cancellationToken)
    {
        var alterou = false;
        var idsMembrosAtuais = ObterIdsMembrosAtuais(encontro);
        foreach (var confirmacao in encontro.Confirmacoes
                     .Where(x => idsMembrosAtuais.Contains(x.AtletaId))
                     .Where(x => DeveTentarEnviar(x, agoraUtc)))
        {
            var telefone = confirmacao.Atleta.TelefoneNormalizado ?? confirmacao.Atleta.Telefone;
            if (string.IsNullOrWhiteSpace(telefone))
            {
                continue;
            }

            var reservouEnvio = await presencaGrupoRepositorio.TentarReservarEnvioWhatsappAsync(
                confirmacao.Id,
                agoraUtc,
                IntervaloMinimoNovaTentativa,
                MaximoTentativasWhatsapp,
                cancellationToken);
            if (!reservouEnvio)
            {
                continue;
            }

            ResultadoEntregaNotificacaoDto resultado;
            try
            {
                resultado = await entregaNotificacaoExternaServico.EnviarAsync(
                    new SolicitacaoEntregaNotificacaoDto(
                        OrigemNotificacao,
                        confirmacao.Id.ToString("N"),
                        CanalNotificacaoExterna.Whatsapp,
                        TemplateWhatsapp,
                        telefone,
                        new Dictionary<string, string>
                        {
                            ["nomeAtleta"] = ObterNomeAtleta(confirmacao.Atleta),
                            ["nomeGrupo"] = encontro.Grupo.Nome,
                            ["dataJogo"] = encontro.DataJogo.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("pt-BR")),
                            ["horarioJogo"] = $"{FormatarHorario(encontro.HorarioInicio)} às {FormatarHorario(encontro.HorarioFim)}",
                            ["localJogo"] = encontro.LocalSnapshot ?? encontro.Arena?.Nome ?? "Local a confirmar",
                            ["linkConfirmacao"] = MontarLinkConfirmacao(confirmacao.CodigoAcesso, absoluto: true)
                        }),
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Falha inesperada no envio de confirmação de presença. ConfirmacaoId: {ConfirmacaoId}.",
                    confirmacao.Id);
                resultado = new ResultadoEntregaNotificacaoDto(
                    false,
                    false,
                    "Não foi possível comunicar com o provedor de WhatsApp.",
                    null);
            }

            confirmacao.RegistrarResultadoEnvioWhatsapp(
                resultado.TentativaRealizada,
                resultado.Enviado || resultado.Aceito,
                resultado.Erro,
                resultado.IdentificadorMensagem,
                agoraUtc);
            presencaGrupoRepositorio.AtualizarConfirmacao(confirmacao);
            alterou = true;
        }

        if (alterou)
        {
            await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
        }
    }

    private async Task<ConfirmacaoPresencaGrupo> ObterConfirmacaoObrigatoriaAsync(
        string codigo,
        CancellationToken cancellationToken)
    {
        var normalizado = NormalizarCodigo(codigo);
        return await presencaGrupoRepositorio.ObterConfirmacaoPorCodigoAsync(normalizado, cancellationToken)
            ?? throw new EntidadeNaoEncontradaException("Link de confirmação inválido ou expirado.");
    }

    private static string NormalizarCodigo(string codigo)
    {
        var normalizado = codigo?.Trim() ?? string.Empty;
        if (normalizado.Length is < 32 or > 64 || normalizado.Any(x => !char.IsAsciiHexDigit(x)))
        {
            throw new EntidadeNaoEncontradaException("Link de confirmação inválido ou expirado.");
        }

        return normalizado.ToLowerInvariant();
    }

    private static bool DeveTentarEnviar(ConfirmacaoPresencaGrupo confirmacao, DateTime agoraUtc)
    {
        if (confirmacao.Status != StatusConfirmacaoPresencaGrupo.Pendente ||
            confirmacao.WhatsappEnviadoEmUtc.HasValue ||
            agoraUtc > confirmacao.ExpiraEmUtc ||
            confirmacao.TentativasEnvioWhatsapp >= MaximoTentativasWhatsapp)
        {
            return false;
        }

        return !confirmacao.UltimaTentativaEnvioWhatsappEmUtc.HasValue ||
               agoraUtc - confirmacao.UltimaTentativaEnvioWhatsappEmUtc.Value >= IntervaloMinimoNovaTentativa;
    }

    private static bool PodeResponder(ConfirmacaoPresencaGrupo confirmacao, DateTime agoraUtc)
        => confirmacao.EncontroGrupo.Grupo.Ativo &&
           confirmacao.EncontroGrupo.Grupo.Atletas.Any(x => x.AtletaId == confirmacao.AtletaId) &&
           agoraUtc <= confirmacao.ExpiraEmUtc;

    private static ConfirmacaoPresencaGrupoPublicaDto MontarPublica(
        ConfirmacaoPresencaGrupo confirmacao,
        DateTime agoraUtc)
    {
        var encontro = confirmacao.EncontroGrupo;
        return new ConfirmacaoPresencaGrupoPublicaDto(
            encontro.Grupo.Nome,
            ObterNomeAtleta(confirmacao.Atleta),
            encontro.DataJogo,
            FormatarHorario(encontro.HorarioInicio),
            FormatarHorario(encontro.HorarioFim),
            encontro.LocalSnapshot ?? encontro.Arena?.Nome,
            ObterStatus(confirmacao.Status),
            PodeResponder(confirmacao, agoraUtc),
            confirmacao.RespondidaEmUtc);
    }

    private static EncontroPresencaGrupoDto MontarEncontro(EncontroGrupo encontro)
    {
        var idsMembrosAtuais = ObterIdsMembrosAtuais(encontro);
        var membros = encontro.Confirmacoes
            .Where(x => idsMembrosAtuais.Contains(x.AtletaId))
            .OrderBy(x => OrdemStatus(x.Status))
            .ThenBy(x => ObterNomeAtleta(x.Atleta))
            .Select(x => new PresencaGrupoMembroDto(
                x.AtletaId,
                x.Atleta.Nome,
                x.Atleta.Apelido,
                FotoPerfilAtletaUtil.ObterUrlPublica(x.Atleta),
                ObterStatus(x.Status),
                x.RespondidaEmUtc,
                !string.IsNullOrWhiteSpace(x.Atleta.TelefoneNormalizado ?? x.Atleta.Telefone),
                ObterStatusEnvio(x)))
            .ToList();

        return new EncontroPresencaGrupoDto(
            encontro.Id,
            encontro.DataJogo,
            FormatarHorario(encontro.HorarioInicio),
            FormatarHorario(encontro.HorarioFim),
            encontro.LocalSnapshot ?? encontro.Arena?.Nome,
            membros.Count,
            membros.Count(x => x.Status == "Confirmada"),
            membros.Count(x => x.Status == "Não vai"),
            membros.Count(x => x.Status == "Pendente"),
            membros);
    }

    private static HashSet<Guid> ObterIdsMembrosAtuais(EncontroGrupo encontro)
        => encontro.Grupo.Atletas.Select(x => x.AtletaId).ToHashSet();

    private static AgendaGrupoDto MontarAgenda(Grupo grupo)
        => new(
            grupo.ArenaId,
            grupo.Arena?.Nome,
            grupo.LocalPrincipal,
            grupo.DiasDaSemana ?? [],
            grupo.HorarioInicio?.ToString("HH:mm"),
            grupo.HorarioFim?.ToString("HH:mm"),
            AgendaCompleta(grupo));

    private static bool AgendaCompleta(Grupo grupo)
        => grupo.DiasDaSemana is { Length: > 0 } &&
           grupo.HorarioInicio.HasValue &&
           grupo.HorarioFim.HasValue &&
           (grupo.ArenaId.HasValue || !string.IsNullOrWhiteSpace(grupo.LocalPrincipal));

    private static DateOnly? ObterProximaDataJogo(Grupo grupo, DateOnly hoje)
    {
        if (!AgendaCompleta(grupo))
        {
            return null;
        }

        for (var dias = 0; dias <= 7; dias++)
        {
            var candidata = hoje.AddDays(dias);
            if (TemJogoNaData(grupo, candidata))
            {
                return candidata;
            }
        }

        return null;
    }

    private static bool TemJogoNaData(Grupo grupo, DateOnly data)
    {
        if (!AgendaCompleta(grupo))
        {
            return false;
        }

        var dataInicio = DateOnly.FromDateTime(grupo.DataInicio);
        var dataFim = grupo.DataFim.HasValue ? DateOnly.FromDateTime(grupo.DataFim.Value) : (DateOnly?)null;
        if (data < dataInicio || (dataFim.HasValue && data > dataFim.Value))
        {
            return false;
        }

        var nomeDia = data.DayOfWeek switch
        {
            DayOfWeek.Monday => "Segunda",
            DayOfWeek.Tuesday => "Terça",
            DayOfWeek.Wednesday => "Quarta",
            DayOfWeek.Thursday => "Quinta",
            DayOfWeek.Friday => "Sexta",
            DayOfWeek.Saturday => "Sábado",
            _ => "Domingo"
        };
        return grupo.DiasDaSemana!.Contains(nomeDia, StringComparer.OrdinalIgnoreCase);
    }

    private TimeZoneInfo ObterFusoHorario()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(configuracao.FusoHorario);
        }
        catch (TimeZoneNotFoundException)
        {
            logger.LogWarning(
                "Fuso horário {FusoHorario} não encontrado; UTC será usado na agenda de grupos.",
                configuracao.FusoHorario);
            return TimeZoneInfo.Utc;
        }
        catch (InvalidTimeZoneException)
        {
            logger.LogWarning(
                "Fuso horário {FusoHorario} inválido; UTC será usado na agenda de grupos.",
                configuracao.FusoHorario);
            return TimeZoneInfo.Utc;
        }
    }

    private TimeOnly ObterHoraEnvioLocal()
        => TimeOnly.TryParse(configuracao.HoraEnvioLocal, CultureInfo.InvariantCulture, out var hora)
            ? hora
            : new TimeOnly(8, 0);

    private string MontarLinkConfirmacao(string codigo, bool absoluto)
    {
        var caminho = $"/presenca#{codigo}";
        if (!absoluto)
        {
            return caminho;
        }

        var urlBase = configuracao.UrlApp
            .Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault()
            ?.TrimEnd('/');
        return string.IsNullOrWhiteSpace(urlBase) ? caminho : $"{urlBase}{caminho}";
    }

    private static string MontarResumoEncontro(EncontroGrupo encontro)
        => $"{encontro.DataJogo:dd/MM} • {FormatarHorario(encontro.HorarioInicio)} às {FormatarHorario(encontro.HorarioFim)} • " +
           (encontro.LocalSnapshot ?? encontro.Arena?.Nome ?? "Local a confirmar");

    private static string? ObterNomeLocal(Grupo grupo)
        => grupo.Arena?.Nome ?? grupo.LocalPrincipal;

    private static string ObterNomeAtleta(Atleta atleta)
        => string.IsNullOrWhiteSpace(atleta.Apelido) ? atleta.Nome : atleta.Apelido.Trim();

    private static string ObterStatus(StatusConfirmacaoPresencaGrupo status)
        => status switch
        {
            StatusConfirmacaoPresencaGrupo.Confirmada => "Confirmada",
            StatusConfirmacaoPresencaGrupo.NaoVai => "Não vai",
            _ => "Pendente"
        };

    private static int OrdemStatus(StatusConfirmacaoPresencaGrupo status)
        => status switch
        {
            StatusConfirmacaoPresencaGrupo.Confirmada => 0,
            StatusConfirmacaoPresencaGrupo.Pendente => 1,
            _ => 2
        };

    private static string ObterStatusEnvio(ConfirmacaoPresencaGrupo confirmacao)
    {
        if (confirmacao.WhatsappEnviadoEmUtc.HasValue)
        {
            return "Enviado";
        }

        if (string.IsNullOrWhiteSpace(confirmacao.Atleta.TelefoneNormalizado ?? confirmacao.Atleta.Telefone))
        {
            return "Sem WhatsApp";
        }

        return string.IsNullOrWhiteSpace(confirmacao.ErroEnvioWhatsapp) ? "Aguardando" : "Falha";
    }

    private static string FormatarHorario(TimeOnly horario) => horario.ToString("HH:mm");

    private static string GerarCodigoAcesso()
        => Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();

    private static DateTime CalcularExpiracaoUtc(
        DateOnly dataJogo,
        TimeOnly horarioFim,
        TimeZoneInfo fusoHorario)
    {
        var fimLocal = DateTime.SpecifyKind(dataJogo.ToDateTime(horarioFim), DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(fimLocal, fusoHorario);
    }

    private static DateTime GarantirUtc(DateTime data)
        => data.Kind switch
        {
            DateTimeKind.Utc => data,
            DateTimeKind.Local => data.ToUniversalTime(),
            _ => DateTime.SpecifyKind(data, DateTimeKind.Utc)
        };
}
