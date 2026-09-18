using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IExpenseService, ExpenseService>();
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("SettingProgram"));
var app = builder.Build();  



app.MapGet("/GetAll", (IExpenseService exp, string? category,int? sum, DateTime? dateFirst, DateTime? dateLast, string? sortSetting, bool? descending) =>
{
    IEnumerable<Expense> res = exp.GetAll();
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
app.MapGet("/GetById/{id:int:min(1)}", (IExpenseService exp, int id, ILogger<Program> logger) => {
    if(exp.GetById(id) is Expense ex)
    {
        logger.LogInformation("Найден эелемент с айди {id}", id);
        return Results.Ok(ex);
    }
    logger.LogWarning("Элемент не был найден {id}", id);
    return Results.NotFound();
    });

app.MapPost("/Create", (IExpenseService exp, CreateExpenseRequest body, IOptions<AppSettings> conf, ILogger<Program> logger) =>
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
    var res = exp.Create(new Expense(body.Amount, body.Category, body.CreatedAt));
    logger.LogInformation("Создан эелемент с айди {res.Id}", res.Id);
    return Results.Created($"/GetById/{res.Id}", res);
});
app.MapDelete("/Delete/{id:int:min(1)}", (IExpenseService exp, int id, ILogger<Program> logger) => {
    
    if(exp.Delete(id))
    {
        logger.LogInformation("Удалён эелемент с айди {id}", id);
        return Results.NoContent();
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


public class AppSettings
{
    public string Currency {get; set;} ="";
    public string[] Categories {get; set;} = [];
    public string CreatedAt {get; set;} ="";
}
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
    private readonly List<Expense> expenses = new();
    private int nextId = 1;
    public List<Expense> GetAll() => expenses;
    public Expense? GetById(int Id) => expenses.FirstOrDefault(e => e.Id == Id);
    public Expense Create(Expense expense)
    {
        expense.Id = nextId++;
        expenses.Add(expense);
        Console.WriteLine(expenses);
        return expense;
    }
    public bool Delete(int Id) => expenses.RemoveAll(e => e.Id == Id) > 0;
}

public class Expense
{
    public int Id {get; set;} 
    public int Amount {get; set;}
    public string Category {get; set;}
    public DateTime CreatedAt {get; set;}
    public string About { get; set; } ="";
    public Expense(int Amount, string Category, string CreatedAt)
    {

        this.Amount = Amount;
        this.Category = Category;
        this.CreatedAt = DateTime.Parse(CreatedAt);
    }
}