using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;

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


app.Environment.EnvironmentName = "Production";

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


app.MapPost("/Register", (CreateUserRequest body, ApplicationContext context) =>
{
    if(context.users.FirstOrDefault(c => c.UserName == body.UserName) == null)
    {
        context.users.Add(new User(body.UserName, body.Password));
        context.SaveChanges();
        return Results.NoContent();
    }
    return Results.Conflict();
    
});

app.MapPost("/Login", (CreateUserRequest body, ApplicationContext context, IOptions<AppSettings> settings) =>
{
    User? user = context.users.FirstOrDefault(c => c.UserName == body.UserName);
    if(user is null) return Results.Unauthorized();
    if(BCrypt.Net.BCrypt.Verify(body.Password, user.PasswordHash)) return Results.Unauthorized();

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

app.MapGet("/GetAll", [Authorize] (IExpenseService exp, string? category,int? sum, DateTime? dateFirst, DateTime? dateLast, string? sortSetting, bool? descending, HttpContext httpcontext) =>
{
    IEnumerable<Expense> res = exp.GetAll();
    if(int.TryParse(httpcontext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int resId))
    {
        res = res.Where(x => x.UserId ==resId);
    }
    if(category != null)
    {
        res = res.Where(x => x.Category.Contains(category));
    }
    if(dateFirst != null)
    {
        res = res.Where(x => x.CreatedAt.Date >= dateFirst);
    }
    if(dateLast != null)
    {
        res = res.Where(x => x.CreatedAt.Date <= dateLast);
    }
    if(sum != null)
    {
        res = res.Where(x => x.Amount == sum);
    }
    if(sortSetting != null)
    {
        if(sortSetting == "amount")
        {
            res = descending??false ? res.OrderByDescending(n => n.Amount) : res.OrderBy(n => n.Amount);
        } else if(sortSetting == "date")
        {
            res = descending??false ? res.OrderByDescending(n => n.CreatedAt) : res.OrderBy(n => n.CreatedAt);
        }
    }
    return  Results.Ok(res);
});
app.MapGet("/GetById/{id:int:min(1)}", [Authorize] (IExpenseService exp, int id, ILogger<Program> logger, HttpContext httpcontext) => {
    if(exp.GetById(id) is Expense ex)
    {   
        int.TryParse(httpcontext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int resId);
        if(ex.UserId == resId)
        {
            logger.LogInformation("Найден эелемент с айди {id}", id);
            return Results.Ok(ex);
        } 
    }
    logger.LogWarning("Элемент не был найден {id}", id);
    return Results.NotFound();
    });

app.MapPost("/Create", [Authorize] (IExpenseService exp, CreateExpenseRequest body, IOptions<AppSettings> conf, ILogger<Program> logger, HttpContext httpcontext) =>
{
    var ruls = conf.Value;
    if(!ruls.Categories.Any(c => c.Equals(body.Category, StringComparison.OrdinalIgnoreCase)))
    {
        logger.LogWarning("Не удалось создать потому что-Категория '{body.Category}' не разрешена", body.Category);
        return Results.BadRequest($"Категория '{body.Category}' не разрешена");
    }
    if(body.Amount < 0)
    {
        logger.LogWarning("Не удалось создать потому что-Сумма '{body.Amount}' не разрешена", body.Amount);
        return Results.BadRequest($"Сумма '{body.Amount}' не разрешена"); 
    }
    if(!DateTime.TryParse(body.CreatedAt, out var result) || result >= DateTime.UtcNow.Date)
    {
        logger.LogWarning("Не удалось создать потому что-Дата '{body.CreatedAt}' не разрешена", body.CreatedAt);
        return Results.BadRequest($"Дата '{body.CreatedAt}' не разрешена"); 
    }
    if(!int.TryParse(httpcontext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int resId)) return Results.Unauthorized();
    var res = exp.Create(new Expense(resId,body.Amount, body.Category, DateTime.Parse(body.CreatedAt)));
    logger.LogInformation("Создан эелемент с айди {res.Id}", res.Id);
    return Results.Created($"/GetById/{res.Id}", res);
});
app.MapDelete("/Delete/{id:int:min(1)}", [Authorize] (IExpenseService exp, int id, ILogger<Program> logger, HttpContext httpcontext) => {
    if(exp.GetById(id) is Expense ex)
    {   
        int.TryParse(httpcontext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int resId);
        if(ex.UserId == resId)
        {
            if(exp.Delete(id))
            {
                logger.LogInformation("Удалён эелемент с айди {id}", id);
                return Results.NoContent();
            } 
        } 
    }
    logger.LogWarning("Элемент {id} не был найден при удалении", id);
    return Results.NotFound();
    });
app.Map("/", (IOptions<AppSettings> conf) =>
{
    var op = conf.Value;
    return op;
} );
app.Run();


public class ApplicationContext: DbContext
{
    public DbSet<Expense> expenses {get; set;} = null!;
    public DbSet<User> users {get; set;} = null!;
    public ApplicationContext(DbContextOptions options) : base(options)
    {
        Database.EnsureCreated();
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Expense>().HasData();
    }
}

public class AppSettings
{
    public string Currency {get; set;} ="";
    public string[] Categories {get; set;} = [];
    public JWT JWTSettings {get; set;} = new();
}
public class JWT
{
    public string Issuer {get; set;} ="";
    public string Audience {get; set;} ="";
    public string KEY {get; set;} ="";
}
public record CreateUserRequest(string UserName, string Password);
public record CreateExpenseRequest(int Amount, string Category, string CreatedAt);
public interface IExpenseService
{
    List<Expense> GetAll();
    Expense? GetById(int id);
    Expense Create(Expense expense);
    bool Delete(int id);
}

public class ExpenseService : IExpenseService
{
    private readonly ApplicationContext context;
    public ExpenseService(ApplicationContext context)
    {
        this.context = context;
    }
    public List<Expense> GetAll() => context.expenses.ToList();
    public Expense? GetById(int Id) => context.expenses.FirstOrDefault(e => e.Id == Id);
    public Expense Create(Expense expense)
    {
        context.expenses.Add(expense);
        context.SaveChanges();
        return expense;
    }
    public bool Delete(int Id)  {
        var res = context.expenses.FirstOrDefault(e => e.Id == Id);
        if(res != null)
        {
            context.expenses.Remove(res);
            context.SaveChanges();
            return true;
        }
        return false;
    }
}

public class User
{
    public int Id {get; set;}
    public string UserName {get; set;} = "";
    public string PasswordHash {get; set;} ="";
    public User(string UserName, string PasswordHash)
    {
        this.UserName = UserName;
        this.PasswordHash = BCrypt.Net.BCrypt.HashPassword(PasswordHash);
    }
}

public class Expense
{
    public int UserId {get; set;} 
    public int Id {get; set;} 
    public int Amount {get; set;}
    public string Category {get; set;}
    public DateTime CreatedAt {get; set;}
    public string About { get; set; } ="";
    public Expense(int UserId, int Amount, string Category, DateTime CreatedAt)
    {
        this.UserId = UserId;
        this.Amount = Amount;
        this.Category = Category;
        this.CreatedAt = CreatedAt;
    }
}