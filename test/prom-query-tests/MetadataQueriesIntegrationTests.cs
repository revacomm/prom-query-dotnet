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
      return queryClient.LabelsPostAsync(labels, null, null, token);
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
   [Fact]
   public async void TestLabelsRangePostAsync() {
     await this.LabelsRangeTestHelper((queryClient, labels, start, end, token) => {
       return queryClient.LabelsPostAsync(labels, start, end, token);
     });
   }

  // Test Label Value Async
  [Fact]
  public async void TestLabelValueAsync() {
    await this.LabelValueTestHelper((queryClient, labelKey, labels, token) => {
      return queryClient.LabelValueAsync(labelKey, labels, null, null, token);
    });
  }

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
    Assert.Contains(testLabel1.Name,labelQueryResult.Data); // Tests if testLabel1's name is within the data
    Assert.Contains(testLabel2.Name,labelQueryResult.Data); // Tests if testLabel2's name is within the data
    Assert.DoesNotContain(testLabel3.Name,labelQueryResult.Data); // Tests if testLabel3's name is not within the data

    // Only test label 3 should be present
    var labelQueryResult2 = await runLabelQuery(
      queryClient,
      new String[] {query2MetricName},
      token
    );
    Assert.NotNull(labelQueryResult2.Data);
    Assert.DoesNotContain(testLabel1.Name,labelQueryResult2.Data); // Tests if testLabel1's name is not within the data
    Assert.DoesNotContain(testLabel2.Name, labelQueryResult2.Data); // Tests if testLabel2's name is not within the data
    Assert.Contains(testLabel3.Name, labelQueryResult2.Data); // Tests if testLabel3's name is within the data

    // All test labels should be present
    var labelQueryResult3 = await runLabelQuery(
      queryClient,
      new String[] {query1MetricName, query2MetricName},
      token
    );
    Assert.NotNull(labelQueryResult3.Data);
    Assert.Contains(testLabel1.Name,labelQueryResult3.Data); // Tests if testLabel1's name is within the data
    Assert.Contains(testLabel2.Name, labelQueryResult3.Data); // Tests if testLabel2's name is within the data
    Assert.Contains(testLabel3.Name, labelQueryResult3.Data); // Tests if testLabel3's name is within the data
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

    // create test labels
    var testLabel1 = CreateTestLabel();
    var testLabel2 = CreateTestLabel();

    var labelArg1 = new Google.Protobuf.Collections.RepeatedField<Label>();
    labelArg1.AddRange(new[] {testLabel1});

    var labelArg2 = new Google.Protobuf.Collections.RepeatedField<Label>();
    labelArg2.AddRange(new[] {testLabel2});

    var query1MetricName = await testHelper.PushTestData(samples, labelArg1, token);
    var query2MetricName = await testHelper.PushTestData(offsetSamples, labelArg2, token);

    // Out of range label test
    // Only this test is possible because datetimes appear to not affect the results
    // in any other way
    var outOfRangeLabelQueryRangeResult = await runLabelRangeQuery(
      queryClient,
      new String[] {query1MetricName, query2MetricName},
      lastSampleTime.AddHours(-4), // Time added to make start time well out of range
      lastSampleTime.AddHours(-3), // Time added to make end time well out of range
      token
    );
    Assert.NotNull(outOfRangeLabelQueryRangeResult.Data);
    Assert.DoesNotContain(testLabel1.Name, outOfRangeLabelQueryRangeResult.Data); // Tests that testLabel1's name is not within the range
    Assert.DoesNotContain(testLabel2.Name, outOfRangeLabelQueryRangeResult.Data); // Tests that testLabel2's name is not within the range
  }

  private async Task LabelValueTestHelper(Func<PrometheusClient, String, String[]?, CancellationToken, Task<ResponseEnvelope<IImmutableList<String>>>> runLabelValueQuery) {
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

    // create target label name
    var targetLabelName = testLabel1.Name;

    // push test data in 2 rounds, 1st with label 1 and the 2nd with
    // label 2
    var labelArg1 = new Google.Protobuf.Collections.RepeatedField<Label>();
    labelArg1.AddRange(new[] {testLabel1});

    var labelArg2 = new Google.Protobuf.Collections.RepeatedField<Label>();
    labelArg2.AddRange(new[] {testLabel2});

    // creates two different series the 1st with testLabel1 and 2nd with testLabel2
    var query1Series = await testHelper.PushTestData(samples, labelArg1, token);
    var query2Series = await testHelper.PushTestData(samples, labelArg2, token);

    // *Note* This test is only dealing with Label Values
    // In this test, the target name is testLabel1's name and the series is
    // query1Series which contains only testLabel 1's name and value
    // the result should contain only testLabel1's Value and not testLabel2's value
    // because only testLabel1's Value matches with the target name and is within the series
    var labelQueryResult = await runLabelValueQuery(
      queryClient,
      targetLabelName,
      new String[] {query1Series},
      token
    );
    Assert.NotNull(labelQueryResult.Data);
    Assert.Contains(testLabel1.Value,labelQueryResult.Data); // Tests if testLabel1's value is within the data
    Assert.DoesNotContain(testLabel2.Value,labelQueryResult.Data); // Tests if testLabel2's value is not within the data

    // *Note* This test is only dealing with Label Values
    // In this test, the target name is also testLabel1's name, but the series is
    // query2Series which contains only testLabel 2's name and value
    // the result should contain no values because
    // testLabel 1's value is not within the series being tested, therefore no
    // value is created for it and testLabel 2's value does not match with the target name
    var labelQueryResult2 = await runLabelValueQuery(
      queryClient,
      targetLabelName,
      new String[] {query2Series},
      token
    );
    Assert.NotNull(labelQueryResult2.Data);
    Assert.DoesNotContain(testLabel1.Value,labelQueryResult2.Data); // Tests if testLabel1's value is not within the data
    Assert.DoesNotContain(testLabel2.Value, labelQueryResult2.Data); // Tests if testLabel2's value is not within the data
  }

  // Creates random test label
  private Label CreateTestLabel() {
    return new Label {
      Name = testHelper.GetRandomValue(),
      Value = testHelper.GetRandomValue(),
    };
  }
}
