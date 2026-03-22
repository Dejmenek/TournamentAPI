namespace TournamentAPI.Extensions;

public static class HttpResponseExtensions
{
    public static void AppendRefreshTokenCookie(this HttpResponse response, string token, DateTime expiry)
    {
        response.Cookies.Append(
            "refreshToken",
            token,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = expiry
            });
    }
}
