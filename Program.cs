using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IExpenseService, ExpenseService>();
var app = builder.Build();



app.MapGet("/GetAll", (IExpenseService exp) => Results.Ok(exp.GetAll()));
app.MapGet("/GetById/{id:int:min(1)}", (IExpenseService exp, int id) => exp.GetById(id) is Expense ex ? Results.Ok(ex) : Results.NotFound());
app.MapPost("/Create", (IExpenseService exp, CreateExpenseRequest body) =>
{
    var res = exp.Create(new Expense(body.Amount, body.Category));
    return Results.Created($"/GetById/{res.Id}", res);
});
app.MapDelete("/Delete/{id:int:min(1)}", (IExpenseService exp, int id) => exp.Delete(id) ? Results.NoContent() : Results.NotFound());
app.Map("/", () => "Hi!");
app.Run();

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
    public int amount {get; set;}
    public string? category {get; set;}
    public DateTime CreatedAt {get; set;}
    public string? About { get; set; }
    public Expense(int amount, string category)
    {

        this.amount = amount;
        this.category = category;
        CreatedAt = DateTime.UtcNow;
    }
}