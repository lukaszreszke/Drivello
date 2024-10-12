namespace ConsumerTests;

public class MockHttpClientFactory
{
    public string BaseUri => "http://localhost:9876";

    public HttpClient CreateClient()
    {
        return new HttpClient { BaseAddress = new Uri(BaseUri) };
    }
}