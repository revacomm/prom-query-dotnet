namespace prom_query_tests;

using PrometheusQuerySdk;
using PrometheusQuerySdk.Models;

public class UnitTest1 {
  [Fact]
  public async void QueryAsync() {
    using var httpClient = CreateHttpClient();
    var rwClient = new PrometheusRemoteProtocolClient(httpClient);
    using var source = new CancellationTokenSource();
    var token = source.Token;
    var queryClient = new PrometheusClient(CreateHttpClient);
    var timestamp = DateTime.UtcNow;
    var data = new QueryPostRequest("test",timestamp,null);
    var result = await queryClient.QueryPostAsync(data,token);
    Assert.Equals(data.Timestamp,result.Data.Result[0].Value);
  }

  private static HttpClient CreateHttpClient() {
    var httpClient = new HttpClient();
    httpClient.BaseAddress = new Uri("http://localhost:9091/");
    return httpClient;
  }
}
