namespace Extensions
{
    using System.Security.Claims;
    public static class HttpContextExtension
    {
        public static bool TryGetUserId(this HttpContext context, out int userId)
        {
            return int.TryParse(context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out userId);
        }
    }
}