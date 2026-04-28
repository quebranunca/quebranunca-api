using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Aplicacao.Mapeadores;
using PlataformaFutevolei.Aplicacao.Utilitarios;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Aplicacao.Servicos;

public class FormatoCampeonatoServico(
    IFormatoCampeonatoRepositorio formatoRepositorio,
    IUnidadeTrabalho unidadeTrabalho
) : IFormatoCampeonatoServico
{
    public async Task<IReadOnlyList<FormatoCampeonatoDto>> ListarAsync(CancellationToken cancellationToken = default)
    {
        await GarantirFormatosPadraoAsync(cancellationToken);
        var formatos = await formatoRepositorio.ListarAsync(cancellationToken);
        return formatos
            .Select(x => x.ParaDto())
            .OrderByDescending(x => x.EhPadrao)
            .ThenBy(x => x.Nome)
            .ToList();
    }

    public async Task<FormatoCampeonatoDto> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await GarantirFormatosPadraoAsync(cancellationToken);
        var formato = await formatoRepositorio.ObterPorIdAsync(id, cancellationToken);
        if (formato is null)
        {
            throw new EntidadeNaoEncontradaException("Formato de campeonato não encontrado.");
        }

        return formato.ParaDto();
    }

    public async Task<FormatoCampeonatoDto> CriarAsync(CriarFormatoCampeonatoDto dto, CancellationToken cancellationToken = default)
    {
        await GarantirFormatosPadraoAsync(cancellationToken);
        var nome = await ValidarNomeAsync(dto.Nome, null, cancellationToken);
        var campos = ValidarCampos(dto.TipoFormato, dto.QuantidadeGrupos, dto.ClassificadosPorGrupo, dto.GeraMataMataAposGrupos,
            dto.TurnoEVolta, dto.TipoChave, dto.QuantidadeDerrotasParaEliminacao, dto.PermiteCabecaDeChave, dto.DisputaTerceiroLugar);

        var formato = new FormatoCampeonato
        {
            Nome = nome,
            Descricao = NormalizarDescricao(dto.Descricao),
            TipoFormato = dto.TipoFormato,
            Ativo = dto.Ativo,
            QuantidadeGrupos = campos.QuantidadeGrupos,
            ClassificadosPorGrupo = campos.ClassificadosPorGrupo,
            GeraMataMataAposGrupos = campos.GeraMataMataAposGrupos,
            TurnoEVolta = campos.TurnoEVolta,
            TipoChave = campos.TipoChave,
            QuantidadeDerrotasParaEliminacao = campos.QuantidadeDerrotasParaEliminacao,
            PermiteCabecaDeChave = campos.PermiteCabecaDeChave,
            DisputaTerceiroLugar = campos.DisputaTerceiroLugar
        };

        await formatoRepositorio.AdicionarAsync(formato, cancellationToken);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
        return formato.ParaDto();
    }

    public async Task<FormatoCampeonatoDto> AtualizarAsync(Guid id, AtualizarFormatoCampeonatoDto dto, CancellationToken cancellationToken = default)
    {
        await GarantirFormatosPadraoAsync(cancellationToken);
        var formato = await formatoRepositorio.ObterPorIdAsync(id, cancellationToken);
        if (formato is null)
        {
            throw new EntidadeNaoEncontradaException("Formato de campeonato não encontrado.");
        }

        if (FormatosCampeonatoPadrao.EhPadrao(formato.Nome))
        {
            throw new RegraNegocioException("Formatos padrão não podem ser alterados.");
        }

        var nome = await ValidarNomeAsync(dto.Nome, id, cancellationToken);
        var campos = ValidarCampos(dto.TipoFormato, dto.QuantidadeGrupos, dto.ClassificadosPorGrupo, dto.GeraMataMataAposGrupos,
            dto.TurnoEVolta, dto.TipoChave, dto.QuantidadeDerrotasParaEliminacao, dto.PermiteCabecaDeChave, dto.DisputaTerceiroLugar);

        formato.Nome = nome;
        formato.Descricao = NormalizarDescricao(dto.Descricao);
        formato.TipoFormato = dto.TipoFormato;
        formato.Ativo = dto.Ativo;
        formato.QuantidadeGrupos = campos.QuantidadeGrupos;
        formato.ClassificadosPorGrupo = campos.ClassificadosPorGrupo;
        formato.GeraMataMataAposGrupos = campos.GeraMataMataAposGrupos;
        formato.TurnoEVolta = campos.TurnoEVolta;
        formato.TipoChave = campos.TipoChave;
        formato.QuantidadeDerrotasParaEliminacao = campos.QuantidadeDerrotasParaEliminacao;
        formato.PermiteCabecaDeChave = campos.PermiteCabecaDeChave;
        formato.DisputaTerceiroLugar = campos.DisputaTerceiroLugar;
        formato.AtualizarDataModificacao();

        formatoRepositorio.Atualizar(formato);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
        return formato.ParaDto();
    }

    public async Task RemoverAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await GarantirFormatosPadraoAsync(cancellationToken);
        var formato = await formatoRepositorio.ObterPorIdAsync(id, cancellationToken);
        if (formato is null)
        {
            throw new EntidadeNaoEncontradaException("Formato de campeonato não encontrado.");
        }

        if (FormatosCampeonatoPadrao.EhPadrao(formato.Nome))
        {
            throw new RegraNegocioException("Formatos padrão não podem ser excluídos.");
        }

        formatoRepositorio.Remover(formato);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
    }

    private async Task GarantirFormatosPadraoAsync(CancellationToken cancellationToken)
    {
        var adicionouFormato = false;

        foreach (var definicao in FormatosCampeonatoPadrao.Lista)
        {
            var formatoExistente = await formatoRepositorio.ObterPorNomeAsync(definicao.Nome, cancellationToken);
            if (formatoExistente is not null)
            {
                continue;
            }

            await formatoRepositorio.AdicionarAsync(new FormatoCampeonato
            {
                Nome = definicao.Nome,
                Descricao = definicao.Descricao,
                TipoFormato = definicao.TipoFormato,
                Ativo = definicao.Ativo,
                QuantidadeGrupos = definicao.QuantidadeGrupos,
                ClassificadosPorGrupo = definicao.ClassificadosPorGrupo,
                GeraMataMataAposGrupos = definicao.GeraMataMataAposGrupos,
                TurnoEVolta = definicao.TurnoEVolta,
                TipoChave = definicao.TipoChave,
                QuantidadeDerrotasParaEliminacao = definicao.QuantidadeDerrotasParaEliminacao,
                PermiteCabecaDeChave = definicao.PermiteCabecaDeChave,
                DisputaTerceiroLugar = definicao.DisputaTerceiroLugar
            }, cancellationToken);

            adicionouFormato = true;
        }

        if (adicionouFormato)
        {
            await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
        }
    }

    private async Task<string> ValidarNomeAsync(string nome, Guid? idAtual, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new RegraNegocioException("Nome do formato é obrigatório.");
        }

        var nomeNormalizado = nome.Trim();
        var existente = await formatoRepositorio.ObterPorNomeAsync(nomeNormalizado, cancellationToken);
        if (existente is not null && existente.Id != idAtual)
        {
            throw new RegraNegocioException("Já existe um formato cadastrado com este nome.");
        }

        return nomeNormalizado;
    }

    private static CamposFormatoCampeonato ValidarCampos(
        TipoFormatoCampeonato tipoFormato,
        int? quantidadeGrupos,
        int? classificadosPorGrupo,
        bool geraMataMataAposGrupos,
        bool turnoEVolta,
        string? tipoChave,
        int? quantidadeDerrotasParaEliminacao,
        bool permiteCabecaDeChave,
        bool disputaTerceiroLugar)
    {
        return tipoFormato switch
        {
            TipoFormatoCampeonato.PontosCorridos => new CamposFormatoCampeonato(
                null,
                null,
                false,
                turnoEVolta,
                null,
                null,
                false,
                false),
            TipoFormatoCampeonato.FaseDeGrupos => ValidarFaseDeGrupos(
                quantidadeGrupos,
                classificadosPorGrupo,
                geraMataMataAposGrupos,
                turnoEVolta),
            TipoFormatoCampeonato.Chave => ValidarChave(
                tipoChave,
                quantidadeDerrotasParaEliminacao,
                permiteCabecaDeChave,
                disputaTerceiroLugar),
            _ => throw new RegraNegocioException("Tipo de formato inválido.")
        };
    }

    private static CamposFormatoCampeonato ValidarFaseDeGrupos(
        int? quantidadeGrupos,
        int? classificadosPorGrupo,
        bool geraMataMataAposGrupos,
        bool turnoEVolta)
    {
        if (!quantidadeGrupos.HasValue || quantidadeGrupos.Value <= 0)
        {
            throw new RegraNegocioException("Quantidade de grupos deve ser maior que zero para fase de grupos.");
        }

        if (geraMataMataAposGrupos)
        {
            if (!classificadosPorGrupo.HasValue || classificadosPorGrupo.Value <= 0)
            {
                throw new RegraNegocioException("Classificados por grupo deve ser maior que zero quando houver mata-mata após grupos.");
            }
        }
        else
        {
            classificadosPorGrupo = null;
        }

        return new CamposFormatoCampeonato(
            quantidadeGrupos.Value,
            classificadosPorGrupo,
            geraMataMataAposGrupos,
            turnoEVolta,
            null,
            null,
            false,
            false);
    }

    private static CamposFormatoCampeonato ValidarChave(
        string? tipoChave,
        int? quantidadeDerrotasParaEliminacao,
        bool permiteCabecaDeChave,
        bool disputaTerceiroLugar)
    {
        if (string.IsNullOrWhiteSpace(tipoChave))
        {
            throw new RegraNegocioException("Tipo da chave é obrigatório para formato em chave.");
        }

        if (!quantidadeDerrotasParaEliminacao.HasValue || (quantidadeDerrotasParaEliminacao.Value != 1 && quantidadeDerrotasParaEliminacao.Value != 2))
        {
            throw new RegraNegocioException("Quantidade de derrotas para eliminação deve ser 1 ou 2.");
        }

        return new CamposFormatoCampeonato(
            null,
            null,
            false,
            false,
            tipoChave.Trim(),
            quantidadeDerrotasParaEliminacao.Value,
            permiteCabecaDeChave,
            disputaTerceiroLugar);
    }

    private static string? NormalizarDescricao(string? descricao)
        => string.IsNullOrWhiteSpace(descricao) ? null : descricao.Trim();

    private sealed record CamposFormatoCampeonato(
        int? QuantidadeGrupos,
        int? ClassificadosPorGrupo,
        bool GeraMataMataAposGrupos,
        bool TurnoEVolta,
        string? TipoChave,
        int? QuantidadeDerrotasParaEliminacao,
        bool PermiteCabecaDeChave,
        bool DisputaTerceiroLugar);
}
