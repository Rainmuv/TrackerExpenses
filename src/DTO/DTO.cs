namespace DTO
{
    public record CreateUserRequest(string UserName, string Password);
    public record CreateExpenseRequest(int Amount, string Category, string CreatedAt);
}