namespace MarketLink.Services
{
    public class JwtSettings
    {
        public string Key { get; set; } = "";
        public string Issuer { get; set; } = "";
        public string Audience { get; set; } = "";
        public int ExpiryMinutes { get; set; }
        public int RefreshTokenDays { get; set; }
    }
}