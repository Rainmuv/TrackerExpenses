namespace Endpoints
{
    using System.IdentityModel.Tokens.Jwt;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.IdentityModel.Tokens;
    using Microsoft.AspNetCore.Authorization;
    using Services;
    using Extensions;
    using DTO;
    using Models;
    using Config;

    public static class ExpenseEndpoints
    {
        public static void MapExpenseEndpoints(this WebApplication app)
        {
            app.MapGet("/GetAll", [Authorize] (IExpenseService exp, string? category,int? sum, DateTime? dateFirst, DateTime? dateLast, string? sortSetting, bool? descending, HttpContext httpcontext) =>
            {
                if (!httpcontext.TryGetUserId(out var resId)) return Results.Unauthorized();
                IEnumerable<Expense> res = exp.GetAllForUser(resId);
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
                    if (!httpcontext.TryGetUserId(out var resId)) return Results.Unauthorized();
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
                if(body.Amount <= 0)
                {
                    logger.LogWarning("Не удалось создать потому что-Сумма '{body.Amount}' не разрешена", body.Amount);
                    return Results.BadRequest($"Сумма '{body.Amount}' не разрешена"); 
                }
                if(!DateTime.TryParse(body.CreatedAt, out var result) || result.Date > DateTime.UtcNow.Date)
                {
                    logger.LogWarning("Не удалось создать потому что-Дата '{body.CreatedAt}' не разрешена", body.CreatedAt);
                    return Results.BadRequest($"Дата '{body.CreatedAt}' не разрешена"); 
                }
                if (!httpcontext.TryGetUserId(out var resId)) return Results.Unauthorized();
                var res = exp.Create(new Expense(resId,body.Amount, body.Category, result));
                logger.LogInformation("Создан эелемент с айди {res.Id}", res.Id);
                return Results.Created($"/GetById/{res.Id}", res);
            });
        }
    }
    
}