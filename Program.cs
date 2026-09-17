using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IExpenseService, ExpenseService>();
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("SettingProgram"));
var app = builder.Build();  



app.MapGet("/GetAll", (IExpenseService exp) => Results.Ok(exp.GetAll()));
app.MapGet("/GetById/{id:int:min(1)}", (IExpenseService exp, int id) => exp.GetById(id) is Expense ex ? Results.Ok(ex) : Results.NotFound());
app.MapPost("/Create", (IExpenseService exp, CreateExpenseRequest body, IOptions<AppSettings> conf) =>
{
    if(conf.Value.Categories.Any(c => c.Equals(body.Category, StringComparison.OrdinalIgnoreCase)))
    {
        var res = exp.Create(new Expense(body.Amount, body.Category));
        return Results.Created($"/GetById/{res.Id}", res);
    }
    return Results.BadRequest($"Категория '{body.Category}' не разрешена");
});
app.MapDelete("/Delete/{id:int:min(1)}", (IExpenseService exp, int id) => exp.Delete(id) ? Results.NoContent() : Results.NotFound());
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
}
public record CreateExpenseRequest(int Amount, string Category);
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
    public string? Category {get; set;}
    public DateTime CreatedAt {get; set;}
    public string? About { get; set; }
    public Expense(int Amount, string Category)
    {

        this.Amount = Amount;
        this.Category = Category;
        CreatedAt = DateTime.UtcNow;
    }
}