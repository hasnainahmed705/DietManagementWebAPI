using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

public class JwtValidationMiddleware
{
    private readonly RequestDelegate _next;

    public JwtValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

        if (!string.IsNullOrEmpty(token))
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(context.RequestServices.GetRequiredService<IConfiguration>()["Jwt:Key"] ??
                    "MySuperSecretKey1234567890_AtLeast32CharsLong");

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                // Token valid hai
                var jwtToken = (JwtSecurityToken)validatedToken;
                context.User = new ClaimsPrincipal(new ClaimsIdentity(jwtToken.Claims));
            }
            catch
            {
                // Invalid token - 401 return karega automatically by [Authorize]
            }
        }

        await _next(context);
    }
}