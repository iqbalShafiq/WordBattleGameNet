namespace WordBattleGame
{
    public class JwtCookieMiddleware(RequestDelegate next, ILogger<JwtCookieMiddleware> logger)
    {
        private readonly RequestDelegate _next = next;
        private readonly ILogger<JwtCookieMiddleware> _logger = logger;
        private const string CookieName = "access_token";

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Cookies.TryGetValue(CookieName, out var token))
            {
                context.Request.Headers.Append("Authorization", $"Bearer {token}");
                _logger.LogInformation("Authorization header set with JWT token. {IsTrue}", context.Request.Headers.ContainsKey("Authorization"));
            }
            await _next(context);
        }
    }
}
