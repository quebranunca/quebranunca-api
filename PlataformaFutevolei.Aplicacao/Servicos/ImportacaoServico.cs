using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Aplicacao.Utilitarios;
using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Aplicacao.Servicos;

public class ImportacaoServico(
    IAtletaServico atletaServico,
    IDuplaServico duplaServico,
    ILigaServico ligaServico,
    IFormatoCampeonatoServico formatoCampeonatoServico,
    IRegraCompeticaoServico regraCompeticaoServico,
    ICompeticaoServico competicaoServico,
    ICategoriaCompeticaoServico categoriaCompeticaoServico,
    IInscricaoCampeonatoServico inscricaoCampeonatoServico,
    IPartidaServico partidaServico) : IImportacaoServico
{
    private const string TipoInscricoesCampeonato = "inscricoes-campeonato";
    private static readonly CultureInfo CulturaInvariante = CultureInfo.InvariantCulture;
    private static readonly CultureInfo CulturaPtBr = new("pt-BR");
    private static readonly XNamespace NamespacePlanilha = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace NamespaceRelacionamentosDocumento = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace NamespaceRelacionamentosPacote = "http://schemas.openxmlformats.org/package/2006/relationships";

    public async Task<ImportacaoResultadoDto> ImportarAsync(
        string tipo,
        Stream arquivoStream,
        string? nomeArquivo,
        Guid? campeonatoId,
        CancellationToken cancellationToken = default)
    {
        var tipoNormalizado = NormalizarTipo(tipo);

        if (tipoNormalizado == TipoInscricoesCampeonato)
        {
            return await ImportarInscricoesCampeonatoAsync(
                arquivoStream,
                nomeArquivo,
                campeonatoId,
                cancellationToken);
        }

        var linhas = await LerLinhasAsync(arquivoStream);

        if (linhas.Count == 0)
        {
            throw new RegraNegocioException("O arquivo CSV está vazio.");
        }

        if (linhas.Count == 1)
        {
            throw new RegraNegocioException("O arquivo CSV precisa ter cabeçalho e ao menos uma linha de dados.");
        }

        var separador = DetectarSeparador(linhas[0].Conteudo);
        var cabecalhos = ParseCsvLine(linhas[0].Conteudo, separador)
            .Select(NormalizarCabecalho)
            .ToArray();

        if (cabecalhos.Length == 0 || cabecalhos.All(string.IsNullOrWhiteSpace))
        {
            throw new RegraNegocioException("O cabeçalho do arquivo CSV é inválido.");
        }

        var erros = new List<ImportacaoLinhaErroDto>();
        var totalLinhas = 0;
        var registrosImportados = 0;

        foreach (var linha in linhas.Skip(1))
        {
            totalLinhas++;

            try
            {
                var registro = CriarRegistro(cabecalhos, ParseCsvLine(linha.Conteudo, separador), linha.Numero);
                await ImportarLinhaAsync(tipoNormalizado, registro, cancellationToken);
                registrosImportados++;
            }
            catch (Exception ex) when (
                ex is RegraNegocioException ||
                ex is EntidadeNaoEncontradaException ||
                ex is FormatException ||
                ex is ArgumentException)
            {
                erros.Add(new ImportacaoLinhaErroDto(linha.Numero, ex.Message));
            }
        }

        return new ImportacaoResultadoDto(
            tipoNormalizado,
            totalLinhas,
            registrosImportados,
            erros.Count,
            erros);
    }

    private async Task<ImportacaoResultadoDto> ImportarInscricoesCampeonatoAsync(
        Stream arquivoStream,
        string? nomeArquivo,
        Guid? campeonatoId,
        CancellationToken cancellationToken)
    {
        if (!campeonatoId.HasValue || campeonatoId == Guid.Empty)
        {
            throw new RegraNegocioException("Selecione o campeonato antes de importar os inscritos.");
        }

        var campeonato = await competicaoServico.ObterPorIdAsync(campeonatoId.Value, cancellationToken);
        if (campeonato.Tipo != TipoCompeticao.Campeonato)
        {
            throw new RegraNegocioException("A competição selecionada para a importação não é um campeonato.");
        }

        var categorias = await categoriaCompeticaoServico.ListarPorCompeticaoAsync(campeonatoId.Value, cancellationToken);
        var categoriasPorChave = categorias.ToDictionary(
            categoria => NormalizarChaveCategoria(categoria.Nome),
            categoria => categoria,
            StringComparer.OrdinalIgnoreCase);

        var registros = await LerRegistrosInscricoesCampeonatoAsync(arquivoStream, nomeArquivo, cancellationToken);
        if (registros.Count == 0)
        {
            throw new RegraNegocioException("O arquivo não possui inscrições válidas para importar.");
        }

        var erros = new List<ImportacaoLinhaErroDto>();
        var registrosImportados = 0;

        foreach (var linha in registros)
        {
            try
            {
                await ImportarLinhaInscricaoCampeonatoAsync(
                    campeonatoId.Value,
                    linha.Registro,
                    categoriasPorChave,
                    cancellationToken);
                registrosImportados++;
            }
            catch (Exception ex) when (
                ex is RegraNegocioException ||
                ex is EntidadeNaoEncontradaException ||
                ex is FormatException ||
                ex is ArgumentException)
            {
                var mensagem = string.IsNullOrWhiteSpace(linha.Contexto)
                    ? ex.Message
                    : $"{linha.Contexto}: {ex.Message}";
                erros.Add(new ImportacaoLinhaErroDto(linha.NumeroLinha, mensagem));
            }
        }

        return new ImportacaoResultadoDto(
            TipoInscricoesCampeonato,
            registros.Count,
            registrosImportados,
            erros.Count,
            erros);
    }

    private async Task ImportarLinhaInscricaoCampeonatoAsync(
        Guid campeonatoId,
        RegistroImportacao registro,
        IDictionary<string, CategoriaCompeticaoDto> categoriasPorChave,
        CancellationToken cancellationToken)
    {
        var nomeAtleta1 = registro.ObterObrigatorio("nomeatleta1");
        var nomeAtleta2Informado = registro.ObterOpcional("nomeatleta2");
        var atleta2CadastroPendente = EhNomeParceiraAusente(nomeAtleta2Informado);
        var nomeAtleta2 = atleta2CadastroPendente
            ? CriarNomeAtletaPendente(nomeAtleta1)
            : nomeAtleta2Informado;

        var categoria = await ObterOuCriarCategoriaCampeonatoAsync(
            campeonatoId,
            registro,
            categoriasPorChave,
            cancellationToken);

        await inscricaoCampeonatoServico.CriarAsync(
            campeonatoId,
            new CriarInscricaoCampeonatoDto(
                categoria.Id,
                null,
                null,
                null,
                nomeAtleta1,
                registro.ObterOpcional("apelidoatleta1"),
                nomeAtleta2,
                registro.ObterOpcional("apelidoatleta2"),
                MontarObservacaoInscricao(registro.ObterOpcional("observacao"), atleta2CadastroPendente),
                false,
                false,
                atleta2CadastroPendente),
            cancellationToken);
    }

    private async Task<CategoriaCompeticaoDto> ObterOuCriarCategoriaCampeonatoAsync(
        Guid campeonatoId,
        RegistroImportacao registro,
        IDictionary<string, CategoriaCompeticaoDto> categoriasPorChave,
        CancellationToken cancellationToken)
    {
        var categoriaId = registro.ObterGuidOpcional("categoriaid");
        if (categoriaId.HasValue)
        {
            var categoriaExistente = await categoriaCompeticaoServico.ObterPorIdAsync(categoriaId.Value, cancellationToken);
            if (categoriaExistente.CompeticaoId != campeonatoId)
            {
                throw new RegraNegocioException("A categoria informada não pertence ao campeonato selecionado.");
            }

            categoriasPorChave[NormalizarChaveCategoria(categoriaExistente.Nome)] = categoriaExistente;
            return categoriaExistente;
        }

        var nomeCategoriaOriginal = registro.ObterObrigatorio("categoria", "nomecategoria");
        var nomeCategoria = NormalizarNomeCategoria(nomeCategoriaOriginal);
        var chaveCategoria = NormalizarChaveCategoria(nomeCategoria);

        if (categoriasPorChave.TryGetValue(chaveCategoria, out var categoria))
        {
            return categoria;
        }

        var genero = registro.ObterEnumOpcional<GeneroCategoria>("genero")
            ?? InferirGeneroCategoria(nomeCategoriaOriginal)
            ?? throw new FormatException("Informe o gênero da categoria ou crie a categoria antes da importação.");

        var nivel = registro.ObterEnumOpcional<NivelCategoria>("nivel")
            ?? InferirNivelCategoria(nomeCategoriaOriginal)
            ?? throw new FormatException("Informe o nível da categoria ou crie a categoria antes da importação.");

        categoria = await categoriaCompeticaoServico.CriarAsync(
            new CriarCategoriaCompeticaoDto(
                campeonatoId,
                null,
                nomeCategoria,
                genero,
                nivel,
                null,
                null,
                false),
            cancellationToken);

        categoriasPorChave[chaveCategoria] = categoria;
        return categoria;
    }

    private async Task<IReadOnlyList<LinhaImportacaoDetalhada>> LerRegistrosInscricoesCampeonatoAsync(
        Stream arquivoStream,
        string? nomeArquivo,
        CancellationToken cancellationToken)
    {
        if (await EhArquivoXlsxAsync(arquivoStream, nomeArquivo, cancellationToken))
        {
            return await LerRegistrosInscricoesCampeonatoXlsxAsync(arquivoStream, cancellationToken);
        }

        return await LerRegistrosInscricoesCampeonatoCsvAsync(arquivoStream);
    }

    private async Task<IReadOnlyList<LinhaImportacaoDetalhada>> LerRegistrosInscricoesCampeonatoCsvAsync(Stream arquivoStream)
    {
        var linhas = await LerLinhasAsync(arquivoStream);

        if (linhas.Count == 0)
        {
            throw new RegraNegocioException("O arquivo CSV está vazio.");
        }

        if (linhas.Count == 1)
        {
            throw new RegraNegocioException("O arquivo CSV precisa ter cabeçalho e ao menos uma linha de dados.");
        }

        var separador = DetectarSeparador(linhas[0].Conteudo);
        var cabecalhos = ParseCsvLine(linhas[0].Conteudo, separador)
            .Select(NormalizarCabecalho)
            .ToArray();

        if (cabecalhos.Length == 0 || cabecalhos.All(string.IsNullOrWhiteSpace))
        {
            throw new RegraNegocioException("O cabeçalho do arquivo CSV é inválido.");
        }

        return linhas
            .Skip(1)
            .Select(linha => new LinhaImportacaoDetalhada(
                linha.Numero,
                null,
                CriarRegistro(cabecalhos, ParseCsvLine(linha.Conteudo, separador), linha.Numero)))
            .ToList();
    }

    private async Task<IReadOnlyList<LinhaImportacaoDetalhada>> LerRegistrosInscricoesCampeonatoXlsxAsync(
        Stream arquivoStream,
        CancellationToken cancellationToken)
    {
        arquivoStream.Position = 0;
        using var memoria = new MemoryStream();
        await arquivoStream.CopyToAsync(memoria, cancellationToken);
        memoria.Position = 0;

        using var zip = new ZipArchive(memoria, ZipArchiveMode.Read, leaveOpen: false);
        var sharedStrings = LerSharedStrings(zip);
        var abas = LerAbasPlanilha(zip).ToList();

        var abaVendas = abas.FirstOrDefault(aba => NormalizarCabecalho(aba.Nome) == "vendas");
        if (abaVendas is null)
        {
            throw new RegraNegocioException("A planilha XLSX precisa ter a aba Vendas para vincular os inscritos às categorias.");
        }

        var vendasPorVoucher = LerVendasPorVoucher(zip, abaVendas, sharedStrings);
        var linhasBrutas = abas
            .Where(aba => aba != abaVendas)
            .SelectMany(aba => LerInscricoesBrutasDaAba(zip, aba, sharedStrings))
            .ToList();

        var nomesConhecidos = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var linha in linhasBrutas)
        {
            var nomeCompletoCompradora = ObterNomeCompletoCompradora(linha, vendasPorVoucher);
            if (!string.IsNullOrWhiteSpace(nomeCompletoCompradora))
            {
                RegistrarAliasNome(nomesConhecidos, nomeCompletoCompradora, nomeCompletoCompradora);
                RegistrarAliasNome(nomesConhecidos, linha.NomeAtleta1Informado, nomeCompletoCompradora);
            }
        }

        var registros = new List<LinhaImportacaoDetalhada>();
        foreach (var linha in linhasBrutas)
        {
            var venda = vendasPorVoucher.GetValueOrDefault(linha.Voucher);
            var categoriaOriginal = venda?.Atividade ?? linha.NomeAba;
            var nomeCategoria = NormalizarNomeCategoria(categoriaOriginal);
            var nomeAtleta1 = ObterNomeCompletoCompradora(linha, vendasPorVoucher);
            var nomeAtleta2 = ResolverNomeAtletaParceira(linha.NomeAtleta2Informado, nomesConhecidos);

            var dados = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["categoria"] = nomeCategoria,
                ["genero"] = ((int)GeneroCategoria.Feminino).ToString(CulturaInvariante),
                ["nivel"] = ((int)(InferirNivelCategoria(categoriaOriginal) ?? NivelCategoria.Iniciante)).ToString(CulturaInvariante),
                ["nomeatleta1"] = nomeAtleta1,
                ["nomeatleta2"] = nomeAtleta2,
                ["observacao"] = $"Voucher {linha.Voucher}"
            };

            registros.Add(new LinhaImportacaoDetalhada(
                linha.NumeroLinha,
                $"Aba {linha.NomeAba}",
                new RegistroImportacao(dados)));
        }

        return registros;
    }

    private static string? ObterNomeCompletoCompradora(
        LinhaInscricaoCampeonatoBruta linha,
        IReadOnlyDictionary<string, VendaPlanilha> vendasPorVoucher)
    {
        if (vendasPorVoucher.TryGetValue(linha.Voucher, out var venda))
        {
            var nomeCompleto = NormalizadorNomeAtleta.NormalizarTexto($"{venda.Nome} {venda.Sobrenome}");
            if (!string.IsNullOrWhiteSpace(nomeCompleto))
            {
                return nomeCompleto;
            }
        }

        return NormalizadorNomeAtleta.NormalizarTexto(linha.NomeAtleta1Informado);
    }

    private static string? ResolverNomeAtletaParceira(
        string? nomeInformado,
        IReadOnlyDictionary<string, string> nomesConhecidos)
    {
        var nomeNormalizado = NormalizadorNomeAtleta.NormalizarTexto(nomeInformado);
        if (string.IsNullOrWhiteSpace(nomeNormalizado))
        {
            return null;
        }

        if (EhNomeParceiraAusente(nomeNormalizado))
        {
            return null;
        }

        var chave = NormalizarChavePessoa(nomeNormalizado);
        return nomesConhecidos.GetValueOrDefault(chave, nomeNormalizado);
    }

    private static void RegistrarAliasNome(IDictionary<string, string> nomesConhecidos, string? alias, string nomeCompleto)
    {
        var chave = NormalizarChavePessoa(alias);
        if (string.IsNullOrWhiteSpace(chave))
        {
            return;
        }

        nomesConhecidos[chave] = nomeCompleto;
    }

    private static string CriarNomeAtletaPendente(string nomeAtleta1)
    {
        return $"Dupla da {NormalizadorNomeAtleta.NormalizarTexto(nomeAtleta1)}";
    }

    private static string? MontarObservacaoInscricao(string? observacaoAtual, bool atleta2CadastroPendente)
    {
        var observacao = NormalizadorNomeAtleta.NormalizarTexto(observacaoAtual);
        if (!atleta2CadastroPendente)
        {
            return observacao;
        }

        return string.IsNullOrWhiteSpace(observacao)
            ? "Parceira com cadastro pendente."
            : $"{observacao} | Parceira com cadastro pendente.";
    }

    private static IReadOnlyDictionary<string, VendaPlanilha> LerVendasPorVoucher(
        ZipArchive zip,
        AbaPlanilha abaVendas,
        IReadOnlyList<string> sharedStrings)
    {
        var linhas = LerLinhasAba(zip, abaVendas.Caminho, sharedStrings);
        if (linhas.Count <= 1)
        {
            throw new RegraNegocioException("A aba Vendas não possui dados suficientes para importar.");
        }

        var indices = CriarMapaCabecalhos(linhas[0].Valores);
        return linhas
            .Skip(1)
            .Where(linha => !string.IsNullOrWhiteSpace(ObterValorCabecalho(linha.Valores, indices, "voucher")))
            .Select(linha => new VendaPlanilha(
                ObterValorCabecalho(linha.Valores, indices, "voucher")!,
                ObterValorCabecalho(linha.Valores, indices, "atividade"),
                ObterValorCabecalho(linha.Valores, indices, "nome"),
                ObterValorCabecalho(linha.Valores, indices, "sobrenome")))
            .ToDictionary(venda => venda.Voucher, StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<LinhaInscricaoCampeonatoBruta> LerInscricoesBrutasDaAba(
        ZipArchive zip,
        AbaPlanilha aba,
        IReadOnlyList<string> sharedStrings)
    {
        var linhas = LerLinhasAba(zip, aba.Caminho, sharedStrings);
        if (linhas.Count <= 1)
        {
            yield break;
        }

        var indices = CriarMapaCabecalhos(linhas[0].Valores);
        foreach (var linha in linhas.Skip(1))
        {
            var voucher = ObterValorCabecalho(linha.Valores, indices, "voucher");
            var nomeAtleta1 = ObterValorCabecalho(linha.Valores, indices, "seunome");
            var nomeAtleta2 = ObterValorCabecalho(linha.Valores, indices, "nomedasuadupla");

            if (string.IsNullOrWhiteSpace(voucher) &&
                string.IsNullOrWhiteSpace(nomeAtleta1) &&
                string.IsNullOrWhiteSpace(nomeAtleta2))
            {
                continue;
            }

            yield return new LinhaInscricaoCampeonatoBruta(
                aba.Nome,
                linha.NumeroLinha,
                voucher ?? string.Empty,
                nomeAtleta1,
                nomeAtleta2);
        }
    }

    private async Task<bool> EhArquivoXlsxAsync(
        Stream arquivoStream,
        string? nomeArquivo,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(nomeArquivo) &&
            Path.GetExtension(nomeArquivo).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        arquivoStream.Position = 0;
        var cabecalho = new byte[4];
        var bytesLidos = await arquivoStream.ReadAsync(cabecalho, cancellationToken);
        arquivoStream.Position = 0;

        return bytesLidos == 4 &&
               cabecalho[0] == 0x50 &&
               cabecalho[1] == 0x4B &&
               cabecalho[2] == 0x03 &&
               cabecalho[3] == 0x04;
    }

    private async Task ImportarLinhaAsync(string tipo, RegistroImportacao registro, CancellationToken cancellationToken)
    {
        switch (tipo)
        {
            case "atletas":
                await atletaServico.CriarAsync(
                    new CriarAtletaDto(
                        registro.ObterObrigatorio("nome"),
                        registro.ObterOpcional("apelido"),
                        registro.ObterOpcional("telefone"),
                        registro.ObterOpcional("email"),
                        registro.ObterOpcional("instagram"),
                        registro.ObterOpcional("cpf"),
                        registro.ObterOpcional("bairro"),
                        registro.ObterOpcional("cidade"),
                        registro.ObterOpcional("estado"),
                        registro.ObterBoolOpcional("cadastropendente") ?? false,
                        registro.ObterEnumOpcional<NivelAtleta>("nivel"),
                        registro.ObterEnumOpcional<LadoAtleta>("lado") ?? LadoAtleta.Ambos,
                        registro.ObterDateTimeOpcional("datanascimento")),
                    cancellationToken);
                break;

            case "duplas":
                await duplaServico.CriarAsync(
                    new CriarDuplaDto(
                        registro.ObterOpcional("nome"),
                        registro.ObterGuidObrigatorio("atleta1id"),
                        registro.ObterGuidObrigatorio("atleta2id")),
                    cancellationToken);
                break;

            case "ligas":
                await ligaServico.CriarAsync(
                    new CriarLigaDto(
                        registro.ObterObrigatorio("nome"),
                        registro.ObterOpcional("descricao")),
                    cancellationToken);
                break;

            case "formatos-campeonato":
                await formatoCampeonatoServico.CriarAsync(
                    new CriarFormatoCampeonatoDto(
                        registro.ObterObrigatorio("nome"),
                        registro.ObterOpcional("descricao"),
                        registro.ObterEnum<TipoFormatoCampeonato>("tipoformato"),
                        registro.ObterBoolObrigatorio("ativo"),
                        registro.ObterIntOpcional("quantidadegrupos"),
                        registro.ObterIntOpcional("classificadosporgrupo"),
                        registro.ObterBoolObrigatorio("geramatamataaposgrupos"),
                        registro.ObterBoolObrigatorio("turnoevolta"),
                        registro.ObterOpcional("tipochave"),
                        registro.ObterIntOpcional("quantidadederrotasparaeliminacao"),
                        registro.ObterBoolObrigatorio("permitecabecadechave"),
                        registro.ObterBoolObrigatorio("disputaterceirolugar")),
                    cancellationToken);
                break;

            case "regras-competicao":
                await regraCompeticaoServico.CriarAsync(
                    new CriarRegraCompeticaoDto(
                        registro.ObterObrigatorio("nome"),
                        registro.ObterOpcional("descricao"),
                        registro.ObterIntObrigatorio("pontosminimospartida"),
                        registro.ObterIntObrigatorio("diferencaminimapartida"),
                        registro.ObterBoolObrigatorio("permiteempate"),
                        registro.ObterDecimalObrigatorio("pontosvitoria"),
                        registro.ObterDecimalObrigatorio("pontosderrota"),
                        registro.ObterDecimalObrigatorio("pontosparticipacao"),
                        registro.ObterDecimalObrigatorio("pontosprimeirolugar"),
                        registro.ObterDecimalObrigatorio("pontossegundolugar"),
                        registro.ObterDecimalObrigatorio("pontosterceirolugar")),
                    cancellationToken);
                break;

            case "competicoes":
                await competicaoServico.CriarAsync(
                    new CriarCompeticaoDto(
                        registro.ObterObrigatorio("nome"),
                        registro.ObterEnum<TipoCompeticao>("tipo"),
                        registro.ObterOpcional("descricao"),
                        registro.ObterOpcional("link"),
                        registro.ObterDateTimeObrigatorio("datainicio"),
                        registro.ObterDateTimeOpcional("datafim"),
                        registro.ObterGuidOpcional("ligaid"),
                        registro.ObterGuidOpcional("localid"),
                        registro.ObterGuidOpcional("formatocampeonatoid"),
                        registro.ObterGuidOpcional("regracompeticaoid"),
                        registro.ObterBoolOpcional("inscricoesabertas"),
                        registro.ObterBoolOpcional("possuifinalreset")),
                    cancellationToken);
                break;

            case "categorias":
                await categoriaCompeticaoServico.CriarAsync(
                    new CriarCategoriaCompeticaoDto(
                        registro.ObterGuidObrigatorio("competicaoid"),
                        registro.ObterGuidOpcional("formatocampeonatoid"),
                        registro.ObterObrigatorio("nome"),
                        registro.ObterEnum<GeneroCategoria>("genero"),
                        registro.ObterEnum<NivelCategoria>("nivel"),
                        registro.ObterDecimalOpcional("pesoranking"),
                        registro.ObterIntOpcional("quantidademaximaduplas"),
                        registro.ObterBoolOpcional("inscricoesencerradas") ?? false),
                    cancellationToken);
                break;

            case "inscricoes":
                await inscricaoCampeonatoServico.CriarAsync(
                    registro.ObterGuidObrigatorio("campeonatoid"),
                    new CriarInscricaoCampeonatoDto(
                        registro.ObterGuidObrigatorio("categoriaid", "categoriacompeticaoid"),
                        registro.ObterGuidOpcional("duplaid"),
                        registro.ObterGuidOpcional("atleta1id"),
                        registro.ObterGuidOpcional("atleta2id"),
                        null,
                        null,
                        null,
                        null,
                        registro.ObterOpcional("observacao"),
                        registro.ObterBoolOpcional("pago") ?? false),
                    cancellationToken);
                break;

            case "partidas":
                await partidaServico.CriarAsync(
                    new CriarPartidaDto(
                        null,
                        null,
                        registro.ObterGuidObrigatorio("categoriacompeticaoid", "categoriaid"),
                        registro.ObterGuidObrigatorio("duplaaid"),
                        registro.ObterGuidObrigatorio("duplabid"),
                        null,
                        null,
                        null,
                        null,
                        null,
                        null,
                        null,
                        null,
                        registro.ObterOpcional("fasecampeonato"),
                        StatusPartida.Encerrada,
                        registro.ObterIntObrigatorio("placarduplaa"),
                        registro.ObterIntObrigatorio("placarduplab"),
                        registro.ObterDateTimeObrigatorio("datapartida"),
                        registro.ObterOpcional("observacoes", "observacao")),
                    cancellationToken);
                break;

            default:
                throw new RegraNegocioException("Tipo de importação não suportado.");
        }
    }

    private static string NormalizarTipo(string tipo)
    {
        var tipoNormalizado = (tipo ?? string.Empty).Trim().ToLowerInvariant();

        return tipoNormalizado switch
        {
            "atletas" => tipoNormalizado,
            "duplas" => tipoNormalizado,
            "ligas" => tipoNormalizado,
            "formatos-campeonato" => tipoNormalizado,
            "regras-competicao" => tipoNormalizado,
            "competicoes" => tipoNormalizado,
            "categorias" => tipoNormalizado,
            "inscricoes" => tipoNormalizado,
            TipoInscricoesCampeonato => tipoNormalizado,
            "partidas" => tipoNormalizado,
            _ => throw new RegraNegocioException("Tipo de importação inválido.")
        };
    }

    private static async Task<IReadOnlyList<LinhaCsv>> LerLinhasAsync(Stream arquivoStream)
    {
        arquivoStream.Position = 0;

        using var leitor = new StreamReader(arquivoStream, Encoding.UTF8, true, leaveOpen: true);
        var linhas = new List<LinhaCsv>();
        var numeroLinha = 0;

        while (true)
        {
            var conteudo = await leitor.ReadLineAsync();
            if (conteudo is null)
            {
                break;
            }

            numeroLinha++;
            if (string.IsNullOrWhiteSpace(conteudo))
            {
                continue;
            }

            linhas.Add(new LinhaCsv(numeroLinha, conteudo));
        }

        return linhas;
    }

    private static char DetectarSeparador(string cabecalho)
    {
        return cabecalho.Count(caractere => caractere == ';') > cabecalho.Count(caractere => caractere == ',')
            ? ';'
            : ',';
    }

    private static IReadOnlyList<string> ParseCsvLine(string linha, char separador)
    {
        var valores = new List<string>();
        var atual = new StringBuilder();
        var entreAspas = false;

        for (var indice = 0; indice < linha.Length; indice++)
        {
            var caractere = linha[indice];

            if (caractere == '"')
            {
                if (entreAspas && indice + 1 < linha.Length && linha[indice + 1] == '"')
                {
                    atual.Append('"');
                    indice++;
                    continue;
                }

                entreAspas = !entreAspas;
                continue;
            }

            if (caractere == separador && !entreAspas)
            {
                valores.Add(atual.ToString().Trim());
                atual.Clear();
                continue;
            }

            atual.Append(caractere);
        }

        if (entreAspas)
        {
            throw new FormatException("Linha CSV com aspas inválidas.");
        }

        valores.Add(atual.ToString().Trim());
        return valores;
    }

    private static RegistroImportacao CriarRegistro(IReadOnlyList<string> cabecalhos, IReadOnlyList<string> valores, int numeroLinha)
    {
        if (valores.Count > cabecalhos.Count)
        {
            throw new FormatException($"A linha {numeroLinha} possui mais colunas do que o cabeçalho.");
        }

        var dados = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        for (var indice = 0; indice < cabecalhos.Count; indice++)
        {
            if (string.IsNullOrWhiteSpace(cabecalhos[indice]))
            {
                continue;
            }

            dados[cabecalhos[indice]] = indice < valores.Count ? LimparValor(valores[indice]) : null;
        }

        return new RegistroImportacao(dados);
    }

    private static string NormalizarCabecalho(string cabecalho)
    {
        var texto = (cabecalho ?? string.Empty)
            .Trim()
            .Trim('\uFEFF')
            .ToLowerInvariant();

        var builder = new StringBuilder(texto.Length);
        foreach (var caractere in texto)
        {
            if (char.IsLetterOrDigit(caractere))
            {
                builder.Append(caractere);
            }
        }

        return builder.ToString();
    }

    private static string? LimparValor(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        var valorLimpo = valor.Trim();
        var valorNormalizado = valorLimpo.ToLowerInvariant();

        if (valorNormalizado is "não informado" or "nao informado" or "null" or "n/a" or "-")
        {
            return null;
        }

        return valorLimpo;
    }

    private static string NormalizarNomeCategoria(string? nomeCategoria)
    {
        var texto = NormalizadorNomeAtleta.NormalizarTexto(nomeCategoria);
        if (string.IsNullOrWhiteSpace(texto))
        {
            return string.Empty;
        }

        return CulturaPtBr.TextInfo.ToTitleCase(texto.ToLower(CulturaPtBr));
    }

    private static string NormalizarChaveCategoria(string? nomeCategoria)
    {
        return RemoverAcentos(nomeCategoria)
            .ToLowerInvariant()
            .Replace(" ", string.Empty)
            .Replace("-", string.Empty)
            .Replace("/", string.Empty)
            .Replace("(", string.Empty)
            .Replace(")", string.Empty)
            .Replace("+", string.Empty);
    }

    private static string NormalizarChavePessoa(string? nome)
    {
        return RemoverAcentos(NormalizadorNomeAtleta.NormalizarTexto(nome))
            .ToLowerInvariant();
    }

    private static string RemoverAcentos(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return string.Empty;
        }

        var textoNormalizado = valor.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(textoNormalizado.Length);

        foreach (var caractere in textoNormalizado)
        {
            var categoria = CharUnicodeInfo.GetUnicodeCategory(caractere);
            if (categoria != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(caractere);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static GeneroCategoria? InferirGeneroCategoria(string nomeCategoria)
    {
        var chave = NormalizarChaveCategoria(nomeCategoria);

        if (chave.Contains("misto", StringComparison.Ordinal))
        {
            return GeneroCategoria.Misto;
        }

        if (chave.Contains("masculino", StringComparison.Ordinal) || chave.Contains("masc", StringComparison.Ordinal))
        {
            return GeneroCategoria.Masculino;
        }

        if (chave.Contains("feminino", StringComparison.Ordinal) || chave.Contains("fem", StringComparison.Ordinal))
        {
            return GeneroCategoria.Feminino;
        }

        return null;
    }

    private static NivelCategoria? InferirNivelCategoria(string nomeCategoria)
    {
        var chave = NormalizarChaveCategoria(nomeCategoria);

        if (chave.Contains("profissional", StringComparison.Ordinal))
        {
            return NivelCategoria.Profissional;
        }

        if (chave.Contains("amador", StringComparison.Ordinal))
        {
            return NivelCategoria.Amador;
        }

        if (chave.Contains("intermediario", StringComparison.Ordinal))
        {
            return NivelCategoria.Intermediario;
        }

        if (chave.Contains("livre", StringComparison.Ordinal))
        {
            return NivelCategoria.Livre;
        }

        if (chave.Contains("iniciante", StringComparison.Ordinal))
        {
            return NivelCategoria.Iniciante;
        }

        if (chave.Contains("estreante", StringComparison.Ordinal))
        {
            return NivelCategoria.Estreante;
        }

        return null;
    }

    private static bool EhNomeParceiraAusente(string? nome)
    {
        var chave = NormalizarChavePessoa(nome);
        return string.IsNullOrWhiteSpace(chave) ||
               chave is "-" or "." or "dupla" or "nao tenho" or "sem nome" ||
               chave.Contains("ainda nao tenho", StringComparison.Ordinal);
    }

    private static IReadOnlyList<string> LerSharedStrings(ZipArchive zip)
    {
        var entrada = zip.GetEntry("xl/sharedStrings.xml");
        if (entrada is null)
        {
            return [];
        }

        using var stream = entrada.Open();
        var documento = XDocument.Load(stream);

        return documento
            .Descendants(NamespacePlanilha + "si")
            .Select(item => string.Concat(item.Descendants(NamespacePlanilha + "t").Select(texto => texto.Value)))
            .ToList();
    }

    private static IEnumerable<AbaPlanilha> LerAbasPlanilha(ZipArchive zip)
    {
        var workbookEntry = zip.GetEntry("xl/workbook.xml")
            ?? throw new RegraNegocioException("Arquivo XLSX inválido: workbook.xml não encontrado.");
        var relsEntry = zip.GetEntry("xl/_rels/workbook.xml.rels")
            ?? throw new RegraNegocioException("Arquivo XLSX inválido: relacionamentos do workbook não encontrados.");

        using var workbookStream = workbookEntry.Open();
        using var relsStream = relsEntry.Open();
        var workbook = XDocument.Load(workbookStream);
        var relacionamentos = XDocument.Load(relsStream);

        var caminhosPorRelacionamento = relacionamentos
            .Descendants(NamespaceRelacionamentosPacote + "Relationship")
            .Where(item => item.Attribute("Id") is not null && item.Attribute("Target") is not null)
            .ToDictionary(
                item => item.Attribute("Id")!.Value,
                item => NormalizarCaminhoEntry(item.Attribute("Target")!.Value),
                StringComparer.OrdinalIgnoreCase);

        foreach (var aba in workbook.Descendants(NamespacePlanilha + "sheet"))
        {
            var relacionamentoId = aba.Attribute(NamespaceRelacionamentosDocumento + "id")?.Value;
            if (string.IsNullOrWhiteSpace(relacionamentoId) ||
                !caminhosPorRelacionamento.TryGetValue(relacionamentoId, out var caminho))
            {
                continue;
            }

            yield return new AbaPlanilha(aba.Attribute("name")?.Value ?? "Aba", caminho);
        }
    }

    private static string NormalizarCaminhoEntry(string caminho)
    {
        var caminhoNormalizado = caminho.Replace('\\', '/');
        return caminhoNormalizado.StartsWith("xl/", StringComparison.OrdinalIgnoreCase)
            ? caminhoNormalizado
            : $"xl/{caminhoNormalizado.TrimStart('/')}";
    }

    private static IReadOnlyList<LinhaPlanilha> LerLinhasAba(
        ZipArchive zip,
        string caminhoEntrada,
        IReadOnlyList<string> sharedStrings)
    {
        var entrada = zip.GetEntry(caminhoEntrada)
            ?? throw new RegraNegocioException($"Arquivo XLSX inválido: aba {caminhoEntrada} não encontrada.");

        using var stream = entrada.Open();
        var documento = XDocument.Load(stream);
        var linhas = new List<LinhaPlanilha>();

        foreach (var linha in documento.Descendants(NamespacePlanilha + "row"))
        {
            var valores = new List<string?>();

            foreach (var celula in linha.Elements(NamespacePlanilha + "c"))
            {
                var referencia = celula.Attribute("r")?.Value;
                var indiceColuna = referencia is null ? valores.Count : ObterIndiceColuna(referencia);

                while (valores.Count < indiceColuna)
                {
                    valores.Add(null);
                }

                var valor = ObterValorCelula(celula, sharedStrings);
                if (valores.Count == indiceColuna)
                {
                    valores.Add(valor);
                }
                else
                {
                    valores[indiceColuna] = valor;
                }
            }

            var numeroLinha = int.TryParse(linha.Attribute("r")?.Value, out var numero)
                ? numero
                : linhas.Count + 1;
            linhas.Add(new LinhaPlanilha(numeroLinha, valores));
        }

        return linhas;
    }

    private static int ObterIndiceColuna(string referenciaCelula)
    {
        var letras = new string(referenciaCelula.TakeWhile(char.IsLetter).ToArray());
        var indice = 0;

        foreach (var letra in letras.ToUpperInvariant())
        {
            indice = (indice * 26) + (letra - 'A' + 1);
        }

        return Math.Max(0, indice - 1);
    }

    private static string? ObterValorCelula(XElement celula, IReadOnlyList<string> sharedStrings)
    {
        var tipo = celula.Attribute("t")?.Value;

        if (string.Equals(tipo, "inlineStr", StringComparison.OrdinalIgnoreCase))
        {
            return LimparValor(string.Concat(celula.Descendants(NamespacePlanilha + "t").Select(item => item.Value)));
        }

        var valor = celula.Element(NamespacePlanilha + "v")?.Value;
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        if (string.Equals(tipo, "s", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(valor, NumberStyles.Integer, CulturaInvariante, out var indiceSharedString) &&
            indiceSharedString >= 0 &&
            indiceSharedString < sharedStrings.Count)
        {
            return LimparValor(sharedStrings[indiceSharedString]);
        }

        return LimparValor(valor);
    }

    private static Dictionary<string, int> CriarMapaCabecalhos(IReadOnlyList<string?> cabecalhos)
    {
        var mapa = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (var indice = 0; indice < cabecalhos.Count; indice++)
        {
            var cabecalho = NormalizarCabecalho(cabecalhos[indice] ?? string.Empty);
            if (string.IsNullOrWhiteSpace(cabecalho))
            {
                continue;
            }

            mapa[cabecalho] = indice;
        }

        return mapa;
    }

    private static string? ObterValorCabecalho(
        IReadOnlyList<string?> valores,
        IReadOnlyDictionary<string, int> indices,
        params string[] chaves)
    {
        foreach (var chave in chaves)
        {
            var chaveNormalizada = NormalizarCabecalho(chave);
            if (indices.TryGetValue(chaveNormalizada, out var indice) &&
                indice >= 0 &&
                indice < valores.Count)
            {
                return LimparValor(valores[indice]);
            }
        }

        return null;
    }

    private sealed class RegistroImportacao(IReadOnlyDictionary<string, string?> dados)
    {
        public string ObterObrigatorio(params string[] chaves)
        {
            var valor = ObterOpcional(chaves);
            if (string.IsNullOrWhiteSpace(valor))
            {
                throw new FormatException($"Campo obrigatório ausente: {chaves[0]}.");
            }

            return valor;
        }

        public string? ObterOpcional(params string[] chaves)
        {
            foreach (var chave in chaves)
            {
                if (dados.TryGetValue(chave, out var valor) && !string.IsNullOrWhiteSpace(valor))
                {
                    return valor;
                }
            }

            return null;
        }

        public Guid ObterGuidObrigatorio(params string[] chaves)
        {
            var valor = ObterObrigatorio(chaves);
            if (Guid.TryParse(valor, out var guid))
            {
                return guid;
            }

            throw new FormatException($"Valor inválido para GUID em {chaves[0]}.");
        }

        public Guid? ObterGuidOpcional(params string[] chaves)
        {
            var valor = ObterOpcional(chaves);
            if (string.IsNullOrWhiteSpace(valor))
            {
                return null;
            }

            if (Guid.TryParse(valor, out var guid))
            {
                return guid;
            }

            throw new FormatException($"Valor inválido para GUID em {chaves[0]}.");
        }

        public int ObterIntObrigatorio(params string[] chaves)
        {
            var valor = ObterObrigatorio(chaves);
            if (int.TryParse(valor, NumberStyles.Integer, CulturaInvariante, out var numero))
            {
                return numero;
            }

            throw new FormatException($"Valor inválido para número inteiro em {chaves[0]}.");
        }

        public int? ObterIntOpcional(params string[] chaves)
        {
            var valor = ObterOpcional(chaves);
            if (string.IsNullOrWhiteSpace(valor))
            {
                return null;
            }

            if (int.TryParse(valor, NumberStyles.Integer, CulturaInvariante, out var numero))
            {
                return numero;
            }

            throw new FormatException($"Valor inválido para número inteiro em {chaves[0]}.");
        }

        public decimal ObterDecimalObrigatorio(params string[] chaves)
        {
            var valor = ObterObrigatorio(chaves);
            if (TryParseDecimal(valor, out var numero))
            {
                return numero;
            }

            throw new FormatException($"Valor inválido para número decimal em {chaves[0]}.");
        }

        public decimal? ObterDecimalOpcional(params string[] chaves)
        {
            var valor = ObterOpcional(chaves);
            if (string.IsNullOrWhiteSpace(valor))
            {
                return null;
            }

            if (TryParseDecimal(valor, out var numero))
            {
                return numero;
            }

            throw new FormatException($"Valor inválido para número decimal em {chaves[0]}.");
        }

        public bool ObterBoolObrigatorio(params string[] chaves)
        {
            var valor = ObterObrigatorio(chaves);
            if (TryParseBool(valor, out var booleano))
            {
                return booleano;
            }

            throw new FormatException($"Valor inválido para booleano em {chaves[0]}.");
        }

        public bool? ObterBoolOpcional(params string[] chaves)
        {
            var valor = ObterOpcional(chaves);
            if (string.IsNullOrWhiteSpace(valor))
            {
                return null;
            }

            if (TryParseBool(valor, out var booleano))
            {
                return booleano;
            }

            throw new FormatException($"Valor inválido para booleano em {chaves[0]}.");
        }

        public DateTime ObterDateTimeObrigatorio(params string[] chaves)
        {
            var valor = ObterObrigatorio(chaves);
            if (TryParseDateTime(valor, out var data))
            {
                return data;
            }

            throw new FormatException($"Valor inválido para data em {chaves[0]}.");
        }

        public DateTime? ObterDateTimeOpcional(params string[] chaves)
        {
            var valor = ObterOpcional(chaves);
            if (string.IsNullOrWhiteSpace(valor))
            {
                return null;
            }

            if (TryParseDateTime(valor, out var data))
            {
                return data;
            }

            throw new FormatException($"Valor inválido para data em {chaves[0]}.");
        }

        public TEnum ObterEnum<TEnum>(params string[] chaves) where TEnum : struct, Enum
        {
            var valor = ObterObrigatorio(chaves);
            return ConverterEnum<TEnum>(valor, chaves[0]);
        }

        public TEnum? ObterEnumOpcional<TEnum>(params string[] chaves) where TEnum : struct, Enum
        {
            var valor = ObterOpcional(chaves);
            if (string.IsNullOrWhiteSpace(valor))
            {
                return null;
            }

            return ConverterEnum<TEnum>(valor, chaves[0]);
        }

        private static TEnum ConverterEnum<TEnum>(string valor, string nomeCampo) where TEnum : struct, Enum
        {
            if (int.TryParse(valor, NumberStyles.Integer, CulturaInvariante, out var numero))
            {
                var enumPorNumero = (TEnum)Enum.ToObject(typeof(TEnum), numero);
                if (Enum.IsDefined(typeof(TEnum), enumPorNumero))
                {
                    return enumPorNumero;
                }
            }

            if (Enum.TryParse<TEnum>(valor, true, out var enumPorNome) && Enum.IsDefined(typeof(TEnum), enumPorNome))
            {
                return enumPorNome;
            }

            throw new FormatException($"Valor inválido para {typeof(TEnum).Name} em {nomeCampo}.");
        }

        private static bool TryParseBool(string valor, out bool resultado)
        {
            if (bool.TryParse(valor, out resultado))
            {
                return true;
            }

            switch (valor.Trim().ToLowerInvariant())
            {
                case "1":
                case "sim":
                case "s":
                    resultado = true;
                    return true;
                case "0":
                case "nao":
                case "não":
                case "n":
                    resultado = false;
                    return true;
                default:
                    resultado = false;
                    return false;
            }
        }

        private static bool TryParseDecimal(string valor, out decimal resultado)
        {
            return decimal.TryParse(valor, NumberStyles.Number, CulturaInvariante, out resultado)
                || decimal.TryParse(valor, NumberStyles.Number, CulturaPtBr, out resultado);
        }

        private static bool TryParseDateTime(string valor, out DateTime resultado)
        {
            return DateTime.TryParse(valor, CulturaInvariante, DateTimeStyles.RoundtripKind, out resultado)
                || DateTime.TryParse(valor, CulturaPtBr, DateTimeStyles.AssumeLocal, out resultado);
        }
    }

    private sealed record LinhaCsv(int Numero, string Conteudo);

    private sealed record LinhaImportacaoDetalhada(int NumeroLinha, string? Contexto, RegistroImportacao Registro);

    private sealed record AbaPlanilha(string Nome, string Caminho);

    private sealed record LinhaPlanilha(int NumeroLinha, IReadOnlyList<string?> Valores);

    private sealed record VendaPlanilha(string Voucher, string? Atividade, string? Nome, string? Sobrenome);

    private sealed record LinhaInscricaoCampeonatoBruta(
        string NomeAba,
        int NumeroLinha,
        string Voucher,
        string? NomeAtleta1Informado,
        string? NomeAtleta2Informado);
}
