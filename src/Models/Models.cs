namespace Models
{
    public class User
    {
        public int Id {get; set;}
        public string UserName {get; set;} = "";
        public string PasswordHash {get; set;} ="";
        public User(string UserName, string PasswordHash)
        {
            this.UserName = UserName;
            this.PasswordHash = PasswordHash;
        }
    }

    public class Expense
    {
        public int UserId {get; set;} 
        public int Id {get; set;} 
        public int Amount {get; set;}
        public string Category {get; set;}
        public DateTime CreatedAt {get; set;}
        public Expense(int UserId, int Amount, string Category, DateTime CreatedAt)
        {
            this.UserId = UserId;
            this.Amount = Amount;
            this.Category = Category;
            this.CreatedAt = CreatedAt;
        }
    }

}