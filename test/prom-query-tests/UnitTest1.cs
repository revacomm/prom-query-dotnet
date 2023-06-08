using Prometheus;
using Xunit.Abstractions;

namespace prom_query_tests;

using PrometheusQuerySdk;
using PrometheusQuerySdk.Models;

public class UnitTest1 {
  private readonly ITestOutputHelper output;

  public UnitTest1(ITestOutputHelper outputHelper) {
    this.output = outputHelper;
  }
  [Fact]
  public async void QueryAsync() {
    // initial setup
    using var httpClient = CreateHttpClient();
    using var source = new CancellationTokenSource();
    var token = source.Token;
    var rwClient = new PrometheusRemoteProtocolClient(httpClient);

    // timestamp configuration
    var timestamp = DateTime.UtcNow;
    DateTimeOffset dto = new DateTimeOffset(timestamp);

    // label and value
    var query = "testData";
    var value = 1.0;

    // push test data to prometheus
    var writeRequest = new WriteRequest {
      Timeseries = {
        new TimeSeries {
          Labels = {
            new Label { Name = query }
          },
          Samples = {
            new Sample {
              Timestamp = dto.ToUnixTimeMilliseconds(),
              Value = value
            }
          }
        }
      }
    };
    await rwClient.WriteAsync(writeRequest, token);

    // query test data
    var queryClient = new PrometheusClient(CreateHttpClient);

    var data = new QueryPostRequest("test",timestamp,null);
    var result = await queryClient.QueryPostAsync(data,token);
    var resultLabel = result.Data.Result[0].Labels.FirstOrDefault().Key;
    this.output.WriteLine(resultLabel);
    Assert.Equal(query,resultLabel);
  }

  private static HttpClient CreateHttpClient() {
    var httpClient = new HttpClient();
    httpClient.BaseAddress = new Uri("http://localhost:9091/");
    return httpClient;
  }
}
