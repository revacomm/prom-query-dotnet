using Prometheus;
using Xunit.Abstractions;

namespace prom_query_tests;

using PrometheusQuerySdk;
using PrometheusQuerySdk.Models;

public class ExpressionQueriesIntegrationTests {
  private readonly ITestOutputHelper output;

  public ExpressionQueriesIntegrationTests(ITestOutputHelper outputHelper) {
    this.output = outputHelper;
  }

  [Fact]
  public async void TestQueryPostAsync() {
    await this.QueryTestHelper((queryClient, query, timestamp) => {
      var latestDataQuery = new QueryPostRequest(query, timestamp, null);
      return queryClient.QueryPostAsync(latestDataQuery, CancellationToken.None);
    });
  }

  [Fact]
  public async void TestQueryAsync() {
    await this.QueryTestHelper((queryClient, query, timestamp) => {
      return queryClient.QueryAsync(query, timestamp);
    });
  }

  private async Task QueryTestHelper(Func<PrometheusClient, String, DateTime, Task<ResponseEnvelope<QueryResults>>> runQuery){
    using var source = new CancellationTokenSource();
    var token = source.Token;
    var seed = DateTime.UtcNow;
    var lastSampleTime = seed.Subtract(TimeSpan.FromMinutes(1));

    // push test data
    var samples = CreateTestData(lastSampleTime, 4);

    // get data for first sample (oldest/earliest)
    var sample2 = samples.First();
    var sample2Timestamp = ConvertSampleTimestamp(sample2.Timestamp);

    // get data for second to last sample (in between oldest/earliest and last/most recent)
    var sample3 = samples[samples.Count() - 2];
    var sample3Value = sample3.Value;
    var sample3Timestamp = ConvertSampleTimestamp(sample3.Timestamp);

    // get data for last sample (most recent/latest)
    var sample1 = samples.Last<Sample>();
    var sample1Value = sample1.Value;
    var sample1Timestamp = ConvertSampleTimestamp(sample1.Timestamp);
    // push test data, save random metric name
    var metricName = await PushTestData(samples, token);

    // initialize PrometheusClient
    var queryClient = new PrometheusClient(CreateHttpClient);

    // Q1: after last/most recent sample (should return last sample)
    var latestresult = await runQuery(queryClient, $"{metricName}", lastSampleTime.AddMilliseconds(1));
    Assert.NotNull(latestresult.Data); // Tests if result data is present
    var latestresultData = Assert.Single(latestresult.Data.Result);
    Assert.True(latestresultData.Labels.TryGetValue("__name__", out var sample1labelValue));
    Assert.Equal(metricName, sample1labelValue); // Tests if metric name and sample 1 label value are the same
    Assert.True(latestresultData.Value.HasValue); // Tests if result data has a value
    Assert.True(Double.TryParse(latestresultData.Value.Value.Value, out var sample1MetricValue));
    Assert.Equal(sample1Value, sample1MetricValue); // Tests if our randomly generated sample 1 Value equals Prom's value

    // Q2: before first/earliest sample (no result)
    var earliestQueryResult = await runQuery(queryClient, $"{metricName}", sample2Timestamp.AddMilliseconds(-1));
    Assert.NotNull(earliestQueryResult.Data); // Tests if the oldestquery result data is present
    Assert.Empty(earliestQueryResult.Data.Result); // Tests if the oldestquery result is empty

    // Q3: before last sample, after first/earliest sample (returns second to last item)
    var inBetweenQueryResult = await runQuery(queryClient, $"{metricName}", lastSampleTime.AddMilliseconds(-1));
    Assert.NotNull(inBetweenQueryResult.Data); // Tests if result is present
    var inBetweenQueryresultData = Assert.Single(inBetweenQueryResult.Data.Result);

    Assert.True(inBetweenQueryresultData.Labels.TryGetValue("__name__", out var sample3labelValue));
    Assert.Equal(metricName, sample3labelValue); // Tests if both names are equal
    Assert.True(inBetweenQueryresultData.Value.HasValue); // Tests if result data has a value
    Assert.True(Double.TryParse(inBetweenQueryresultData.Value.Value.Value, out var sample3MetricValue));
    Assert.Equal(sample3Value, sample3MetricValue); // Tests if our randomly generated sample 3 Value equals Prom's value
  }

  private async Task<String> PushTestData(Sample[] samples, CancellationToken token) {
    // initial setup
    using var httpClient = CreateHttpClient();
    var rwClient = new PrometheusRemoteProtocolClient(httpClient);

    // label and value
    var metricName = $"newval{Guid.NewGuid().ToString().Replace("-", "")}";

    var writeRequest = new WriteRequest();
    var timeSeries = new TimeSeries();
    writeRequest.Timeseries.Add(timeSeries);
    timeSeries.Labels.Add(new Label { Name = "__name__", Value = metricName });
    var samplesColl = new Google.Protobuf.Collections.RepeatedField<Sample>();
    timeSeries.Samples.AddRange(samples);

    // push test data to prometheus
    await rwClient.WriteAsync(writeRequest, token);
    return metricName;
  }

  // creates sample data using the end of sample range
  // and the number of samples requested to be made
  private Sample[] CreateTestData(DateTime seedTime, int numSamples) {
    // timestamp configuration
    var samples = new List<Sample>();
    DateTime start = DateTime.MaxValue;

    for (int i = 0; i <= numSamples; i++) {
      var index = numSamples - i;
      var sampleDateTime = seedTime.Subtract(TimeSpan.FromMinutes(index));

      // if oldest timestamp, save (oldest)
      if (start > sampleDateTime) {
        start = sampleDateTime;
      }

      var testSampleTimestamp = (Int64)Math.Round(sampleDateTime.Subtract(DateTime.UnixEpoch).TotalMilliseconds);
      Random rnd = new Random();
      int rng = rnd.Next();
      var value = Convert.ToDouble(rng);
      var sample = new Sample {
        Timestamp = testSampleTimestamp,
        Value = value
      };
      samples.Add(sample);
    }

    return samples.ToArray();
  }

  private DateTime ConvertSampleTimestamp(long timestamp) {
    return DateTimeOffset.FromUnixTimeMilliseconds(timestamp).UtcDateTime;
  }

  private static HttpClient CreateHttpClient() {
    var httpClient = new HttpClient();
    httpClient.BaseAddress = new Uri("http://localhost:9091/");
    return httpClient;
  }
}
