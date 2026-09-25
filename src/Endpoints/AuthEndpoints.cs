namespace Endpoints
{
    using DTO;
    using Data;
    using Models;
    using System.Security.Claims;
    using System.IdentityModel.Tokens.Jwt;
    using Microsoft.IdentityModel.Tokens;
    using System.Text;
    using Config;

    public static class AuthEndpoints
    {
        public static void MapAuthEndpoints(this WebApplication app)
        {
            app.MapPost("/Register", (CreateUserRequest body, ApplicationContext context) =>
            {
                if(context.users.FirstOrDefault(c => c.UserName == body.UserName) == null)
                {
                    context.users.Add(new User(body.UserName, BCrypt.Net.BCrypt.HashPassword(body.Password)));
                    context.SaveChanges();
                    return Results.Created();
                }
                return Results.Conflict();
                
            });

            app.MapPost("/Login", (CreateUserRequest body, ApplicationContext context, IOptions<AppSettings> settings) =>
            {
                User? user = context.users.FirstOrDefault(c => c.UserName == body.UserName);
                if(user is null) return Results.Unauthorized();
                var isValid = BCrypt.Net.BCrypt.Verify(body.Password, user.PasswordHash);
                if(!isValid) return Results.Unauthorized();
                var claims = new List<Claim> {new Claim(ClaimTypes.Name, user.UserName),new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())};
                var jwt = new JwtSecurityToken(
                    issuer: settings.Value.JWTSettings.Issuer,
                    audience: settings.Value.JWTSettings.Audience,
                    claims: claims,
                    expires: DateTime.UtcNow.Add(TimeSpan.FromHours(1)),
                    signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Value.JWTSettings.KEY)), SecurityAlgorithms.HmacSha256)
                );
                var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);

                var response = new
                {
                access_token = encodedJwt,
                username = body.UserName  
                };

                return Results.Json(response);
            });
        }
    }
}