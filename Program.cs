using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Config;
using Data;
using Services;
using Endpoints;


var builder = WebApplication.CreateBuilder(args);
var connection = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("SettingProgram"));
builder.Services.AddDbContext<ApplicationContext>(options => options.UseSqlServer(connection));
builder.Services.AddAuthorization();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer((options) =>
{
    var jwtKey = builder.Configuration["SettingProgram:JWTSettings:KEY"];
    if(jwtKey == null) throw new Exception("Error with authorization"); 
    options.TokenValidationParameters = new TokenValidationParameters
    {
       ValidateIssuer = true,
       ValidIssuer = builder.Configuration["SettingProgram:JWTSettings:Issuer"],
       ValidateAudience = true,
       ValidAudience = builder.Configuration["SettingProgram:JWTSettings:Audience"],
       ValidateLifetime = true,
       ValidateIssuerSigningKey = true,
       IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

var app = builder.Build();  

app.UseAuthentication();
app.UseAuthorization();

if(!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(async app => app.Run(async context =>
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

        context.Response.StatusCode = 500;
        logger.LogError("Error! 500");
        await context.Response.WriteAsync("Error!");
    }));
}

app.UseStatusCodePages(async context =>
{
   context.HttpContext.Response.ContentType = "application/json";
   await context.HttpContext.Response.WriteAsync(
        $"{{\"error\": \"Запрос завершился с кодом {context.HttpContext.Response.StatusCode}\"}}"
    );
});

app.MapAuthEndpoints();
app.MapExpenseEndpoints();

app.Map("/", (IOptions<AppSettings> conf) =>
{
    var op = conf.Value;
    return op;
} );

app.Run();