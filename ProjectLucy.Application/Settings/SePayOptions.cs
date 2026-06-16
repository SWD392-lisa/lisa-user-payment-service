namespace ProjectLucy.Application.Settings;

public class SePayOptions
{
    public const string SectionName = "SePay";

    public string MerchantId { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public bool IsSandbox { get; set; } = true;
    public string SuccessUrl { get; set; } = string.Empty;
    public string ErrorUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
}
