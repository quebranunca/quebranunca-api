using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PlataformaFutevolei.Aplicacao.Interfaces.Seguranca;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Infraestrutura.Configuracoes;

namespace PlataformaFutevolei.Infraestrutura.Seguranca;

public class TokenJwtServico(IOptions<ConfiguracaoJwt> configuracaoJwt) : ITokenJwtServico
{
    public DateTime ObterExpiracaoTokenAcessoUtc(DateTime? limiteMaximoUtc = null)
    {
        var expiracao = DateTime.UtcNow.AddMinutes(ObterConfiguracao().ExpiracaoMinutos);
        if (!limiteMaximoUtc.HasValue)
        {
            return expiracao;
        }

        return expiracao <= limiteMaximoUtc.Value
            ? expiracao
            : limiteMaximoUtc.Value;
    }

    public DateTime ObterExpiracaoRefreshTokenUtc()
    {
        var configuracao = ObterConfiguracao();
        var dias = configuracao.ExpiracaoRefreshTokenDias > 0
            ? configuracao.ExpiracaoRefreshTokenDias
            : 90;

        return DateTime.UtcNow.AddDays(dias);
    }

    public string GerarToken(Usuario usuario, DateTime expiraEmUtc)
    {
        var configuracao = ObterConfiguracao();

        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuracao.Chave));
        var credenciais = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256);
        var agora = DateTime.UtcNow;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.Nome),
            new(ClaimTypes.Email, usuario.Email),
            new(ClaimTypes.Role, usuario.Perfil.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: configuracao.Emissor,
            audience: configuracao.Audiencia,
            claims: claims,
            notBefore: agora,
            expires: expiraEmUtc,
            signingCredentials: credenciais
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public Guid? ObterUsuarioIdTokenExpirado(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var configuracao = ObterConfiguracao();

        var parametros = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = true,
            ValidIssuer = configuracao.Emissor,
            ValidAudience = configuracao.Audiencia,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuracao.Chave)),
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            var manipulador = new JwtSecurityTokenHandler();
            var principal = manipulador.ValidateToken(token, parametros, out var tokenSeguranca);
            if (tokenSeguranca is not JwtSecurityToken jwt
                || !string.Equals(jwt.Header.Alg, SecurityAlgorithms.HmacSha256, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var usuarioId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return Guid.TryParse(usuarioId, out var id) ? id : null;
        }
        catch
        {
            return null;
        }
    }

    private ConfiguracaoJwt ObterConfiguracao()
    {
        var configuracao = configuracaoJwt.Value;
        if (string.IsNullOrWhiteSpace(configuracao.Chave))
        {
            throw new InvalidOperationException("A chave JWT não foi configurada.");
        }

        return configuracao;
    }
}
