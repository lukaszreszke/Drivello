using System.Net;
using System.Text.Json;

namespace Drivello.Services;

public record PointsResponse(int Points);

public class LoyaltyServiceClient : ILoyaltyService
{
    private readonly HttpClient _httpClient;
    private readonly string _loyaltyApiBaseUrl;

    public LoyaltyServiceClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _loyaltyApiBaseUrl = configuration["LoyaltyApiBaseUrl"];
    }

    public async Task<PointsResponse> GetLoyaltyPoints(int userId)
    {
        var response = await _httpClient.GetAsync($"{_loyaltyApiBaseUrl}/api/loyalty/{userId}");
        var content = await response.Content.ReadAsStringAsync();
        
        if(response.StatusCode == HttpStatusCode.NotFound)
        {
            return new PointsResponse(0);
        }
        
        return JsonSerializer.Deserialize<PointsResponse>(content);
    }

    public async Task<PointsResponse> EarnLoyaltyPoints(int userId, int points)
    {
        var response = await _httpClient.PostAsJsonAsync($"{_loyaltyApiBaseUrl}/api/loyalty/earn_points", new
        {
            UserId = userId,
            Points = points
        });
        
        return await response.Content.ReadFromJsonAsync<PointsResponse>();
    }

    public async Task<IList<PointsResponse>> GetPointsForAllUsers()
    {
        var response = await _httpClient.GetAsync($"{_loyaltyApiBaseUrl}/api/loyalty/all");
        var content = await response.Content.ReadAsStringAsync();
        
        return JsonSerializer.Deserialize<IList<PointsResponse>>(content);
    }
}

public interface ILoyaltyService
{
    Task<PointsResponse> GetLoyaltyPoints(int userId);
}