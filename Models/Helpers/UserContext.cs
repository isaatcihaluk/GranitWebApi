using System.Security.Claims;

namespace GranitWebApi.Helpers
{
    public class UserContext
    {
        private readonly IHttpContextAccessor _context;

        public UserContext(IHttpContextAccessor context)
        {
            _context = context;
        }

        public int UserId =>
            int.Parse(_context.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        public string Username =>
            _context.HttpContext!.User.FindFirst(ClaimTypes.Name)!.Value;

        public string Role =>
            _context.HttpContext!.User.FindFirst(ClaimTypes.Role)!.Value;
    }
}