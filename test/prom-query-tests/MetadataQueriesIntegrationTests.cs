using Prometheus;
using Xunit.Abstractions;

namespace prom_query_tests;

using System.Collections.Immutable;
using PrometheusQuerySdk;
using PrometheusQuerySdk.Models;


public class MetadataQueriesIntegrationTests {
  private readonly ITestOutputHelper output;
  private readonly TestHelper testHelper;
  public MetadataQueriesIntegrationTests(ITestOutputHelper outputHelper) {
    this.output = outputHelper;
    this.testHelper = new TestHelper();
  }

  // Test Labels Async
  [Fact]
  public async void TestLabelsAsync() {
    await this.LabelsTestHelper((queryClient, labels, token) => {
      return queryClient.LabelsAsync(labels, null, null, token);
    });
  }

  // Test Labels Post Async
  [Fact]
  public async void TestLabelsPostAsync() {
    await this.LabelsTestHelper((queryClient, labels, token) => {
      return queryClient.LabelsPostAsync(labels, token);
    });
  }

  // Test Labels Range Async
  [Fact]
  public async void TestLabelsRangeAsync() {
    await this.LabelsRangeTestHelper((queryClient, labels, start, end, token) => {
      return queryClient.LabelsAsync(labels, start, end, token);
    });
  }

   // Test Labels Range Post Async
  // [Fact]
  // public async void TestLabelsRangePostAsync() {
  //   await this.LabelsRangeTestHelper((queryClient, labels, start, end, token) => {
  //     return queryClient.LabelsRangePostAsync(labels, start, end, token);
  //   });
  // }

  private async Task LabelsTestHelper(Func<PrometheusClient, String[]?, CancellationToken, Task<ResponseEnvelope<IImmutableList<String>>>> runLabelQuery) {
    using var source = new CancellationTokenSource();
    var token = source.Token;
    var queryClient = new PrometheusClient(testHelper.CreateHttpClient);
    var seed = DateTime.UtcNow;
    var lastSampleTime = seed.Subtract(TimeSpan.FromMinutes(1));

    // create timseries samples
    var samples = testHelper.CreateTestData(lastSampleTime, 4);

    // create test labels
    var testLabel1 = CreateTestLabel();
    var testLabel2 = CreateTestLabel();
    var testLabel3 = CreateTestLabel();

    // push test data in 2 rounds, 1st with labels 1 & 2 and the second with
    // label 3
    var labelArg1 = new Google.Protobuf.Collections.RepeatedField<Label>();
    labelArg1.AddRange(new[] {testLabel1, testLabel2});

    var labelArg2 = new Google.Protobuf.Collections.RepeatedField<Label>();
    labelArg2.AddRange(new[] {testLabel3});

    var query1MetricName = await testHelper.PushTestData(samples, labelArg1, token);
    var query2MetricName = await testHelper.PushTestData(samples, labelArg2, token);

    // Only test labels 1 and 2 should be present
    var labelQueryResult = await runLabelQuery(
      queryClient,
      new String[] {query1MetricName},
      token
    );
    Assert.NotNull(labelQueryResult.Data);
    Assert.Contains(testLabel1.Name,labelQueryResult.Data);
    Assert.Contains(testLabel2.Name, labelQueryResult.Data);
    Assert.DoesNotContain(testLabel3.Name, labelQueryResult.Data);
    foreach (var label in labelQueryResult.Data) {
       output.WriteLine(label);
    }

    // Only test label 3 should be present
    var labelQueryResult2 = await runLabelQuery(
      queryClient,
      new String[] {query2MetricName},
      token
    );
    Assert.NotNull(labelQueryResult2.Data);
    Assert.DoesNotContain(testLabel1.Name,labelQueryResult2.Data);
    Assert.DoesNotContain(testLabel2.Name, labelQueryResult2.Data);
    Assert.Contains(testLabel3.Name, labelQueryResult2.Data);
    foreach (var label in labelQueryResult2.Data) {
       output.WriteLine(label);
    }

    // All test labels should be present
    var labelQueryResult3 = await runLabelQuery(
      queryClient,
      new String[] {query1MetricName, query2MetricName},
      token
    );
    Assert.NotNull(labelQueryResult3.Data);
    Assert.Contains(testLabel1.Name,labelQueryResult3.Data);
    Assert.Contains(testLabel2.Name, labelQueryResult3.Data);
    Assert.Contains(testLabel3.Name, labelQueryResult3.Data);
  }

  private async Task LabelsRangeTestHelper(Func<PrometheusClient, String[]?, DateTime?, DateTime?, CancellationToken, Task<ResponseEnvelope<IImmutableList<String>>>> runLabelRangeQuery) {
    using var source = new CancellationTokenSource();
    var token = source.Token;
    var queryClient = new PrometheusClient(testHelper.CreateHttpClient);
    var seed = DateTime.UtcNow;
    var lastSampleTime = seed.Subtract(TimeSpan.FromMinutes(10));

    // create timseries samples
    var samples = testHelper.CreateTestData(lastSampleTime, 4);
    var offsetSamples = testHelper.CreateTestData(seed, 4);

    // get data for sample out of range
    var firstSample = samples.First();
    var firstSampleTime = testHelper.ConvertSampleTimestamp(firstSample.Timestamp);

    // create test labels
    var testLabel1 = CreateTestLabel();
    var testLabel2 = CreateTestLabel();
    var testLabel3 = CreateTestLabel();

    var labelArg1 = new Google.Protobuf.Collections.RepeatedField<Label>();
    labelArg1.AddRange(new[] {testLabel1});

    var labelArg2 = new Google.Protobuf.Collections.RepeatedField<Label>();
    labelArg2.AddRange(new[] {testLabel2});

    var query1MetricName = await testHelper.PushTestData(samples, labelArg1, token);

    var query2MetricName = await testHelper.PushTestData(offsetSamples, labelArg2, token);

    // First label test
    var firstlabelQueryRangeResult = await runLabelRangeQuery(
      queryClient,
      null,
      //new String[] {query1MetricName},
      seed.AddSeconds(-30),
      seed.AddMilliseconds(-1),
      token
    );

    Assert.NotNull(firstlabelQueryRangeResult.Data);
    Assert.Contains(testLabel1.Name,firstlabelQueryRangeResult.Data);
    output.WriteLine("Test Label 1: " + testLabel1.Name);
    // Assert.Contains(testLabel2.Name, labelQueryRangeResult.Data);
    // Assert.DoesNotContain(testLabel3.Name, firstlabelQueryRangeResult.Data);
    foreach (var label in firstlabelQueryRangeResult.Data) {
         output.WriteLine(label);
    }
  }

  // Creates random test label
  private Label CreateTestLabel() {
    return new Label {
      Name = testHelper.GetRandomValue(),
      Value = testHelper.GetRandomValue(),
    };
  }
}
