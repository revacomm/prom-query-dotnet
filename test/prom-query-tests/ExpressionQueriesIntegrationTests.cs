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

  // Test Query Post Async
  [Fact]
  public async void TestQueryPostAsync() {
    await this.QueryTestHelper((queryClient, query, timestamp) => {
      var latestDataQuery = new QueryPostRequest(query, timestamp, null);
      return queryClient.QueryPostAsync(latestDataQuery, CancellationToken.None);
    });
  }

  // Test Query Async
  [Fact]
  public async void TestQueryAsync() {
    await this.QueryTestHelper((queryClient, query, timestamp) => {
      return queryClient.QueryAsync(query, timestamp);
    });
  }

  // Test Query Range Post Async
  [Fact]
  public async void TestQueryRangePostAsync() {
    await this.QueryRangeTestHelper((queryClient, query, start, end, step) => {
      var earliestRangeDataQuery = new QueryRangePostRequest(query, start, end, step, null);
      return queryClient.QueryRangePostAsync(earliestRangeDataQuery, CancellationToken.None);
    });
  }

  // Test Query Range Async
  [Fact]
  public async void TestQueryRangeAsync() {
    await this.QueryRangeTestHelper((queryClient, query, start, end, step) => {
      return queryClient.QueryRangeAsync(query, start, end, step, null);
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
    output.WriteLine(metricName);
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

  private async Task QueryRangeTestHelper(Func<PrometheusClient, String, DateTime, DateTime, TimeSpan, Task<ResponseEnvelope<QueryResults>>> runQueryRange){
    using var source = new CancellationTokenSource();
    var token = source.Token;
    var seed = DateTime.UtcNow;
    var lastSampleTime = seed.Subtract(TimeSpan.FromMinutes(1));

    // push test data
    var samples = CreateTestData(lastSampleTime, 4);

    // get data for sample out of range
    var firstSample = samples.First();
    var firstSampleTime = ConvertSampleTimestamp(firstSample.Timestamp);

    // get data for second to last sample
    var sample2 = samples[samples.Count() - 2];
    var sample2RangeValue = sample2.Value;

    // get data for last sample
    var lastsample = samples.Last<Sample>();
    var lastsampleRangeValue = lastsample.Value;
    var lastsampleRangeTimestamp = ConvertSampleTimestamp(lastsample.Timestamp);

    // create timespan
    TimeSpan step = TimeSpan.FromMinutes(1);

    // push test data, save random metric name
    var metricName = await PushTestData(samples, token);

    // initialize PrometheusClient
    var queryClient = new PrometheusClient(CreateHttpClient);

    // Q1: Out of range test EARLY
    var queryEarlyOutOfRangeResult = await runQueryRange(
      queryClient,
      $"{metricName}",
      firstSampleTime.AddMinutes(-4),
      firstSampleTime.AddMinutes(-3),
      step
    );
    Assert.NotNull(queryEarlyOutOfRangeResult.Data); // Tests if the query out of range result data is present
    Assert.Empty(queryEarlyOutOfRangeResult.Data.Result); // Tests if the query out of range result is empty

    // Q2: Out of range test LATE
    // Although we tested a range that was just outside the set range, we still received a result
    // we suspect that this is Prometheus' defaulting function, however, we are not sure
    // to solve this we had to set the range much further outside the set range
    // to receive the correct test result
    var queryLateOutOfRangeResult = await runQueryRange(
      queryClient,
      $"{metricName}",
      lastSampleTime.AddMinutes(10), // Additional time was required to properly run test
      lastSampleTime.AddMinutes(15), // Additional time was required to properly run test
      step
    );
    Assert.NotNull(queryLateOutOfRangeResult.Data); // Tests if the query out of range result data is present
    Assert.Empty(queryLateOutOfRangeResult.Data.Result); // Tests if the query out of range result is empty

    // Q3: Middle range test
      var middleQueryRangeResult = await runQueryRange(
        queryClient,
        $"{metricName}",
        firstSampleTime.AddMinutes(3).AddMilliseconds(1),
        lastSampleTime.AddMilliseconds(-5),
        step
      );
      Assert.NotNull(middleQueryRangeResult.Data); // Tests if result is present
      var middleQueryRangeresultData = Assert.Single(middleQueryRangeResult.Data.Result);
      Assert.NotNull(middleQueryRangeresultData);
      Assert.True(middleQueryRangeresultData.Labels.TryGetValue("__name__", out var middleQueryLabelValue));
      Assert.Equal(metricName, middleQueryLabelValue); // Tests if both names are equal
      Assert.NotNull(middleQueryRangeresultData.Values);
      var middleRangeResult = Assert.Single(middleQueryRangeresultData.Values); // Tests if result data has a value
      Assert.True(Double.TryParse(middleRangeResult.Value, out var middleQueryRangeMetricValue));
      Assert.Equal(sample2RangeValue, middleQueryRangeMetricValue); // Tests if our randomly generated sample 3 Value equals Prom's value

    // Q4: Edge range test *range over last time sample*
      var edgeQueryRangeResult = await runQueryRange(
        queryClient,
        $"{metricName}",
        firstSampleTime.AddMinutes(4).AddMilliseconds(1),
        lastSampleTime.AddMilliseconds(10),
        step
      );
      Assert.NotNull(edgeQueryRangeResult.Data); // Tests if result is present
      var edgeQueryRangeresultData = Assert.Single(edgeQueryRangeResult.Data.Result);
      Assert.NotNull(edgeQueryRangeresultData);
      Assert.True(edgeQueryRangeresultData.Labels.TryGetValue("__name__", out var edgeQueryLabelValue));
      Assert.Equal(metricName, edgeQueryLabelValue); // Tests if both names are equal
      Assert.NotNull(edgeQueryRangeresultData.Values);
      var edgeRangeResult = Assert.Single(edgeQueryRangeresultData.Values); // Tests if result data has a value
      Assert.True(Double.TryParse(edgeRangeResult.Value, out var edgeQueryRangeMetricValue));
      Assert.Equal(lastsampleRangeValue, edgeQueryRangeMetricValue); // Tests if our randomly generated sample 3 Value equals Prom's value

    // Full Range Test
    var fullRangeQueryResult = await runQueryRange(
      queryClient,
      $"{metricName}",
      firstSampleTime,
      lastSampleTime.AddMilliseconds(1),
      step
    );
    Assert.NotNull(fullRangeQueryResult.Data); // Tests if full range query result is present
    var fullRangeQueryResultCollection = Assert.Single(fullRangeQueryResult.Data.Result);
    Assert.True(fullRangeQueryResultCollection.Labels.TryGetValue("__name__", out var fullRangeLabelValue));
    Assert.Equal(metricName, fullRangeLabelValue); // Tests if both names are equal

    var queryResultArr = fullRangeQueryResultCollection.Values;

    Assert.NotNull(queryResultArr);
    Assert.Equal(samples.Length, queryResultArr.Count); // Checks if the number of samples is equal to the number of queries
    for(int i = 0; i == samples.Length; i++)
    {
      var currSampleEl = samples[i];
      var currResultEl = queryResultArr[i];
      // tests if timestamps are equal
      Assert.Equal(currSampleEl.Timestamp * 1000, currResultEl.Timestamp);
      // test if the sample values and query values are equal
      Assert.True(Double.TryParse(currResultEl.Value, out var queryVal));
      Assert.Equal(currSampleEl.Value, queryVal);
    }
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
