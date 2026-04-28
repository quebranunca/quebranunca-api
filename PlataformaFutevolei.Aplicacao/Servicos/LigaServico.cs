using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Aplicacao.Mapeadores;
using PlataformaFutevolei.Dominio.Entidades;

namespace PlataformaFutevolei.Aplicacao.Servicos;

public class LigaServico(
    ILigaRepositorio ligaRepositorio,
    IUnidadeTrabalho unidadeTrabalho
) : ILigaServico
{
    public async Task<IReadOnlyList<LigaDto>> ListarAsync(CancellationToken cancellationToken = default)
    {
        var ligas = await ligaRepositorio.ListarAsync(cancellationToken);
        return ligas.Select(x => x.ParaDto()).ToList();
    }

    public async Task<LigaDto> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var liga = await ligaRepositorio.ObterPorIdAsync(id, cancellationToken);
        if (liga is null)
        {
            throw new EntidadeNaoEncontradaException("Liga não encontrada.");
        }

        return liga.ParaDto();
    }

    public async Task<LigaDto> CriarAsync(CriarLigaDto dto, CancellationToken cancellationToken = default)
    {
        var nome = ValidarNome(dto.Nome);
        await ValidarDuplicidadeAsync(nome, cancellationToken);

        var liga = new Liga
        {
            Nome = nome,
            Descricao = dto.Descricao?.Trim()
        };

        await ligaRepositorio.AdicionarAsync(liga, cancellationToken);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
        return liga.ParaDto();
    }

    public async Task<LigaDto> AtualizarAsync(Guid id, AtualizarLigaDto dto, CancellationToken cancellationToken = default)
    {
        var liga = await ligaRepositorio.ObterPorIdAsync(id, cancellationToken);
        if (liga is null)
        {
            throw new EntidadeNaoEncontradaException("Liga não encontrada.");
        }

        var nome = ValidarNome(dto.Nome);
        var existente = await ligaRepositorio.ObterPorNomeAsync(nome, cancellationToken);
        if (existente is not null && existente.Id != liga.Id)
        {
            throw new RegraNegocioException("Já existe uma liga cadastrada com este nome.");
        }

        liga.Nome = nome;
        liga.Descricao = dto.Descricao?.Trim();
        liga.AtualizarDataModificacao();

        ligaRepositorio.Atualizar(liga);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
        return liga.ParaDto();
    }

    public async Task RemoverAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var liga = await ligaRepositorio.ObterPorIdAsync(id, cancellationToken);
        if (liga is null)
        {
            throw new EntidadeNaoEncontradaException("Liga não encontrada.");
        }

        ligaRepositorio.Remover(liga);
        await unidadeTrabalho.SalvarAlteracoesAsync(cancellationToken);
    }

    private async Task ValidarDuplicidadeAsync(string nome, CancellationToken cancellationToken)
    {
        var existente = await ligaRepositorio.ObterPorNomeAsync(nome, cancellationToken);
        if (existente is not null)
        {
            throw new RegraNegocioException("Já existe uma liga cadastrada com este nome.");
        }
    }

    private static string ValidarNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new RegraNegocioException("Nome da liga é obrigatório.");
        }

        return nome.Trim();
    }
}
