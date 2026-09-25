namespace Data
{
    using Models;
    public class ApplicationContext: DbContext
    {
        public DbSet<Expense> expenses {get; set;} = null!;
        public DbSet<User> users {get; set;} = null!;
        public ApplicationContext(DbContextOptions options) : base(options)
        {
        }
    }

}