namespace Services
{
    using Models;
    using Data;
    public interface IExpenseService
    {
        List<Expense> GetAllForUser(int userId);
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
        public List<Expense> GetAllForUser(int userId) => context.expenses.Where(x => x.UserId == userId).ToList();
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



}