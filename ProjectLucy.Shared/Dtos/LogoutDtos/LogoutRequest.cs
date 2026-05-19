namespace ProjectLucy.Shared.Dtos.LogoutDtos
{
    /// <summary>
    /// Optional body for logout — server prefers reading the refresh token
    /// from the HttpOnly cookie, but accepts it here as a fallback.
    /// </summary>
    public class LogoutRequest
    {
        public string? RefreshToken { get; set; }
    }
}
