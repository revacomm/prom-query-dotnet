using Prometheus;

public class TestHelper {
  public async Task<String> PushTestData(Sample[] samples, Google.Protobuf.Collections.RepeatedField<Label>? labels, CancellationToken token) {
    // initial setup
    using var httpClient = CreateHttpClient();
    var rwClient = new PrometheusRemoteProtocolClient(httpClient);

    // label and value
    var metricName = GetRandomValue();
    // var testLabel1 = GetRandomValue();

    var writeRequest = new WriteRequest();
    var timeSeries = new TimeSeries();
    writeRequest.Timeseries.Add(timeSeries);
    timeSeries.Labels.Add(new Label { Name = "__name__", Value = metricName });
    // timeSeries.Labels.Add(new Label { Name = "test1", Value = testLabel1 });

    if (labels != null) {
      timeSeries.Labels.AddRange(labels);
    }
    var samplesColl = new Google.Protobuf.Collections.RepeatedField<Sample>();
    timeSeries.Samples.AddRange(samples);

    // push test data to prometheus
    await rwClient.WriteAsync(writeRequest, token);
    return metricName;
  }

  // creates sample data using the end of sample range
  // and the number of samples requested to be made
  public Sample[] CreateTestData(DateTime seedTime, int numSamples) {
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

  public String GetRandomValue() {
    return $"newval{Guid.NewGuid().ToString().Replace("-", "")}";
  }
  public DateTime ConvertSampleTimestamp(long timestamp) {
    return DateTimeOffset.FromUnixTimeMilliseconds(timestamp).UtcDateTime;
  }
  public HttpClient CreateHttpClient() {
    var httpClient = new HttpClient();
    httpClient.BaseAddress = new Uri("http://localhost:9091/");
    return httpClient;
  }
}
