using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Aplicacao.Utilitarios;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Aplicacao.Servicos;

public class ResolvedorAtletaDuplaServico(
    IAtletaRepositorio atletaRepositorio,
    IDuplaRepositorio duplaRepositorio,
    IGrupoAtletaRepositorio grupoAtletaRepositorio
) : IResolvedorAtletaDuplaServico
{
    public async Task<Atleta> ObterAtletaExistenteAsync(
        Guid atletaId,
        string mensagemQuandoInvalido,
        CancellationToken cancellationToken = default)
    {
        if (atletaId == Guid.Empty)
        {
            throw new RegraNegocioException(mensagemQuandoInvalido);
        }

        var atleta = await atletaRepositorio.ObterPorIdAsync(atletaId, cancellationToken);
        if (atleta is null)
        {
            throw new RegraNegocioException(mensagemQuandoInvalido);
        }

        return atleta;
    }

    public async Task<Atleta> ResolverAtletaAsync(
        Guid? atletaId,
        string? nomeInformado,
        string? apelidoInformado,
        string mensagemQuandoInvalido,
        bool cadastroPendente,
        CancellationToken cancellationToken = default)
    {
        if (atletaId.HasValue && atletaId.Value != Guid.Empty)
        {
            return await ObterAtletaExistenteAsync(atletaId.Value, mensagemQuandoInvalido, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(nomeInformado))
        {
            throw new RegraNegocioException(mensagemQuandoInvalido);
        }

        return await ObterOuCriarAtletaAsync(nomeInformado, apelidoInformado, cadastroPendente, cancellationToken);
    }

    public async Task<Atleta> ObterOuCriarAtletaAsync(
        string? nomeInformado,
        string? apelidoInformado,
        bool cadastroPendente,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nomeInformado))
        {
            throw new RegraNegocioException("Informe um nome válido para o atleta.");
        }

        var nomeBase = NormalizadorNomeAtleta.NormalizarTexto(nomeInformado);
        var complemento = NormalizadorNomeAtleta.NormalizarTexto(apelidoInformado);

        if (string.IsNullOrWhiteSpace(nomeBase))
        {
            throw new RegraNegocioException("Informe um nome válido para o atleta.");
        }

        if (string.IsNullOrWhiteSpace(complemento))
        {
            var atletaExistente = await atletaRepositorio.ObterPorNomeAsync(nomeBase, cancellationToken);
            if (atletaExistente is not null)
            {
                return atletaExistente;
            }
        }

        var (nomeFinal, apelidoFinal) = NormalizadorNomeAtleta.NormalizarNomeEApelido(nomeBase, complemento);
        var atletaComNomeFinal = await atletaRepositorio.ObterPorNomeAsync(nomeFinal, cancellationToken);
        if (atletaComNomeFinal is not null)
        {
            return atletaComNomeFinal;
        }

        var atleta = new Atleta
        {
            Nome = nomeFinal,
            Apelido = apelidoFinal,
            CadastroPendente = cadastroPendente,
            Lado = LadoAtleta.Ambos
        };

        await atletaRepositorio.AdicionarAsync(atleta, cancellationToken);
        return atleta;
    }

    public async Task<Atleta> ObterOuCriarAtletaParaUsuarioAsync(
        string nomeInformado,
        string emailInformado,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nomeInformado))
        {
            throw new RegraNegocioException("Nome do atleta é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(emailInformado))
        {
            throw new RegraNegocioException("E-mail do atleta é obrigatório.");
        }

        var emailNormalizado = NormalizadorNomeAtleta.NormalizarTexto(emailInformado).ToLowerInvariant();
        var (nomeFinal, apelidoFinal) = NormalizadorNomeAtleta.NormalizarNomeEApelido(nomeInformado, null);

        var atletaPorEmail = await ObterCandidatoUnicoPorEmailAsync(emailNormalizado, cancellationToken);
        if (atletaPorEmail is not null)
        {
            PrepararAtletaParaUsuario(atletaPorEmail, nomeFinal, apelidoFinal, emailNormalizado);
            return atletaPorEmail;
        }

        var atletaPorNome = await ObterCandidatoUnicoPorNomeAsync(nomeFinal, emailNormalizado, cancellationToken);
        if (atletaPorNome is not null)
        {
            PrepararAtletaParaUsuario(atletaPorNome, nomeFinal, apelidoFinal, emailNormalizado);
            return atletaPorNome;
        }

        var atleta = new Atleta
        {
            Nome = nomeFinal,
            Apelido = apelidoFinal,
            Email = emailNormalizado,
            CadastroPendente = false,
            Lado = LadoAtleta.Ambos
        };

        await atletaRepositorio.AdicionarAsync(atleta, cancellationToken);
        return atleta;
    }

    public async Task<Dupla> ObterOuCriarDuplaAsync(
        Atleta atleta1,
        Atleta atleta2,
        CancellationToken cancellationToken = default)
    {
        if (atleta1.Id == atleta2.Id)
        {
            throw new RegraNegocioException("Um atleta não pode formar dupla com ele mesmo.");
        }

        var (atletaNormalizado1Id, atletaNormalizado2Id) = NormalizarAtletas(atleta1.Id, atleta2.Id);
        var dupla = await duplaRepositorio.ObterPorAtletasAsync(atletaNormalizado1Id, atletaNormalizado2Id, cancellationToken);
        if (dupla is not null)
        {
            return dupla;
        }

        var atletaNormalizado1 = atleta1.Id == atletaNormalizado1Id ? atleta1 : atleta2;
        var atletaNormalizado2 = atleta2.Id == atletaNormalizado2Id ? atleta2 : atleta1;

        var novaDupla = new Dupla
        {
            Nome = $"{atletaNormalizado1.Nome} / {atletaNormalizado2.Nome}",
            Atleta1Id = atletaNormalizado1Id,
            Atleta2Id = atletaNormalizado2Id
        };

        await duplaRepositorio.AdicionarAsync(novaDupla, cancellationToken);
        return novaDupla;
    }

    public async Task<GrupoAtleta> GarantirAtletaNoGrupoAsync(
        Guid competicaoId,
        Atleta atleta,
        CancellationToken cancellationToken = default)
    {
        var grupoAtletaExistente = await grupoAtletaRepositorio.ObterPorCompeticaoEAtletaAsync(
            competicaoId,
            atleta.Id,
            cancellationToken);
        if (grupoAtletaExistente is not null)
        {
            return grupoAtletaExistente;
        }

        var grupoAtleta = new GrupoAtleta
        {
            CompeticaoId = competicaoId,
            AtletaId = atleta.Id,
            Atleta = atleta
        };

        await grupoAtletaRepositorio.AdicionarAsync(grupoAtleta, cancellationToken);
        return grupoAtleta;
    }

    private static (Guid atleta1Id, Guid atleta2Id) NormalizarAtletas(Guid atleta1Id, Guid atleta2Id)
    {
        return atleta1Id.CompareTo(atleta2Id) <= 0
            ? (atleta1Id, atleta2Id)
            : (atleta2Id, atleta1Id);
    }

    private async Task<Atleta?> ObterCandidatoUnicoPorEmailAsync(
        string emailNormalizado,
        CancellationToken cancellationToken)
    {
        var candidatos = await atletaRepositorio.ListarPorEmailAsync(emailNormalizado, cancellationToken);
        var candidatosDisponiveis = candidatos
            .Where(x => x.Usuario is null)
            .ToList();

        return candidatosDisponiveis.Count == 1
            ? candidatosDisponiveis[0]
            : null;
    }

    private async Task<Atleta?> ObterCandidatoUnicoPorNomeAsync(
        string nomeNormalizado,
        string emailNormalizado,
        CancellationToken cancellationToken)
    {
        var candidatos = await atletaRepositorio.ListarPorNomeAsync(nomeNormalizado, cancellationToken);
        var candidatosDisponiveis = candidatos
            .Where(x =>
                x.Usuario is null &&
                (string.IsNullOrWhiteSpace(x.Email) ||
                 string.Equals(x.Email, emailNormalizado, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        return candidatosDisponiveis.Count == 1
            ? candidatosDisponiveis[0]
            : null;
    }

    private static void PrepararAtletaParaUsuario(
        Atleta atleta,
        string nomeNormalizado,
        string apelidoNormalizado,
        string emailNormalizado)
    {
        atleta.Nome = nomeNormalizado;
        atleta.Apelido = apelidoNormalizado;
        atleta.Email = emailNormalizado;
        atleta.CadastroPendente = false;
        atleta.AtualizarDataModificacao();
    }
}
