using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace MakrisPortfolio.Server.Auth
{
    public class JwtTokenService
    {
        private readonly string _issuer;
        private readonly string _audience;
        private readonly SigningCredentials _creds;

        public JwtTokenService(string issuer, string audience, SymmetricSecurityKey key)
        {
            _issuer = issuer;
            _audience = audience;
            _creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        }

        public string CreateToken(IEnumerable<Claim> claims, TimeSpan? lifetime = null)
        {
            var now = DateTime.UtcNow;
            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                notBefore: now,
                expires: now.Add(lifetime ?? TimeSpan.FromHours(12)),
                signingCredentials: _creds);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
