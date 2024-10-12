namespace LoyaltyApi.Tests.Pacts;

public class ProviderState
{
    public string State { get; set; } = string.Empty;
    public Dictionary<string, object> Params { get; set; } = new Dictionary<string, object>();
}