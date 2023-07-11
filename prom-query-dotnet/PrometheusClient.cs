using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using PrometheusQuerySdk.Models;
using PrometheusQuerySdk.Internal;
using System.Collections.Immutable;

namespace PrometheusQuerySdk;
public class PrometheusClient : IPrometheusClient {
  private const String BaseUrlPath = "/api/v1";
  private const String QueryUrlPath = "/query";
  private const String QueryRangeUrlPath = "/query_range";
  private const String LabelsUrlPath = "/labels";
  private const String LabelValuesUrlPath = "/label";
  private const String SeriesUrlPath = "/series";
  private const String FormUrlEncodedMediaType = "application/x-www-form-urlencoded";

  private readonly Func<HttpClient> _clientFactory;

  private static readonly JsonSerializerOptions SerializerOptions = new() {
    Converters = { new JsonStringEnumConverter(), new ArrayTupleConverterFactory() },
    PropertyNameCaseInsensitive = true
  };

  public PrometheusClient(Func<HttpClient> clientFactory) {
    this._clientFactory = clientFactory;
  }

  public async Task<ResponseEnvelope<QueryResults>> QueryAsync(
    String query,
    DateTime timestamp,
    TimeSpan? timeout = null,
    CancellationToken cancellationToken = default) {

    var parameters = new UriQueryStringParameterCollection {
      { "query", query },
      { "time", timestamp },
    };

    if (timeout.HasValue) {
      parameters.Add(key: "timeout", PrometheusClient.ToPrometheusDuration(timeout.Value));
    }

    using var client = this._clientFactory();
    using var response = await client.GetAsync(
      $"{PrometheusClient.BaseUrlPath}{PrometheusClient.QueryUrlPath}?{parameters}",
      cancellationToken
    );

    return await PrometheusClient.HandleQueryResponse(response, cancellationToken);
  }

  public async Task<ResponseEnvelope<QueryResults>> QueryPostAsync(
    QueryPostRequest request,
    CancellationToken cancellationToken = default) {

    var content = new UriQueryStringParameterCollection {
      { "query", request.Query },
      { "time", request.Timestamp },
    };
    if (request.Timeout.HasValue) {
      content.Add(key: "timeout", PrometheusClient.ToPrometheusDuration(request.Timeout.Value));
    }

    using var client = this._clientFactory();
    using var response = await client.PostAsync(
      $"{PrometheusClient.BaseUrlPath}{PrometheusClient.QueryUrlPath}",
      new StringContent(
        content.ToString(),
        Encoding.UTF8,
        PrometheusClient.FormUrlEncodedMediaType),
      cancellationToken
    );

    return await PrometheusClient.HandleQueryResponse(response, cancellationToken);
  }

  private static async Task<ResponseEnvelope<QueryResults>> HandleQueryResponse(
    HttpResponseMessage response, CancellationToken ct) {
    // if (successful response)
    //   Deserialize ResponseEnvelope<QueryResults>
    if (response.IsSuccessStatusCode) {
      var responseBody =
        await response.Content.ReadFromJsonAsync<ResponseEnvelope<QueryResults>>(
          PrometheusClient.SerializerOptions,
          ct
        );

      return responseBody ?? throw new InvalidOperationException("Prometheus API returned null response to query.");
    } else {
      // Non success error code? throw exception
      //   if there is a body w/ ErrorType/Error, include that in the exception message (TODO: special exception type?)
      if (response.Content.Headers.Contains("content-type")) {
        var responseBody =
          await response.Content.ReadFromJsonAsync<ResponseEnvelope<QueryResults>>(
            PrometheusClient.SerializerOptions,
            ct
          );

        throw new InvalidOperationException(
          $"Prometheus returned non success status code ({response.StatusCode}) from query operation. Error Type: {responseBody?.ErrorType}, Detail: {responseBody?.Error}"
        );
      } else {
        throw new InvalidOperationException(
          $"Prometheus returned non success status code ({response.StatusCode}) from query operation."
        );
      }
    }
  }

  private static async Task<ResponseEnvelope<IImmutableList<String>>> HandleLabelsResponse(
    HttpResponseMessage response, CancellationToken ct) {
    // if (successful response)
    //   Deserialize ResponseEnvelope<QueryResults>
    if (response.IsSuccessStatusCode) {
      var responseBody =
        await response.Content.ReadFromJsonAsync<ResponseEnvelope<IImmutableList<String>>>(
          PrometheusClient.SerializerOptions,
          ct
        );

      return responseBody ?? throw new InvalidOperationException("Prometheus API returned null response to query.");
    } else {
      // Non success error code? throw exception
      //   if there is a body w/ ErrorType/Error, include that in the exception message (TODO: special exception type?)
      if (response.Content.Headers.Contains("content-type")) {
        var responseBody =
          await response.Content.ReadFromJsonAsync<ResponseEnvelope<IImmutableList<String>>>(
            PrometheusClient.SerializerOptions,
            ct
          );

        throw new InvalidOperationException(
          $"Prometheus returned non success status code ({response.StatusCode}) from query operation. Error Type: {responseBody?.ErrorType}, Detail: {responseBody?.Error}"
        );
      } else {
        throw new InvalidOperationException(
          $"Prometheus returned non success status code ({response.StatusCode}) from query operation."
        );
      }
    }
  }

  public async Task<ResponseEnvelope<QueryResults>> QueryRangeAsync(
    String query,
    DateTime start,
    DateTime end,
    TimeSpan step,
    TimeSpan? timeout,
    CancellationToken cancellationToken = default) {

    var parameters = new UriQueryStringParameterCollection {
      { "query", query },
      { "start", start },
      { "end", end },
      { "step", step.TotalSeconds },
    };

    if (timeout.HasValue) {
      parameters.Add(key: "timeout", PrometheusClient.ToPrometheusDuration(timeout.Value));
    }

    using var client = this._clientFactory();
    using var response = await client.GetAsync(
      $"{PrometheusClient.BaseUrlPath}{PrometheusClient.QueryRangeUrlPath}?{parameters}",
      cancellationToken
    );

    return await PrometheusClient.HandleQueryResponse(response, cancellationToken);
  }

  public async Task<ResponseEnvelope<QueryResults>> QueryRangePostAsync(
    QueryRangePostRequest request,
    CancellationToken cancellationToken = default) {

    var content = new UriQueryStringParameterCollection {
      { "query", request.Query },
      { "start", request.Start },
      { "end", request.End },
      { "step", request.Step.TotalSeconds },
    };

    if (request.Timeout.HasValue) {
      content.Add(key: "timeout", PrometheusClient.ToPrometheusDuration(request.Timeout.Value));
    }

    using var client = this._clientFactory();
    using var response = await client.PostAsync(
      $"{PrometheusClient.BaseUrlPath}{PrometheusClient.QueryRangeUrlPath}",
      new StringContent(
        content.ToString(),
        Encoding.UTF8,
        PrometheusClient.FormUrlEncodedMediaType),
      cancellationToken
    );

    return await PrometheusClient.HandleQueryResponse(response, cancellationToken);
  }

  public async Task<ResponseEnvelope<IImmutableList<String>>> LabelsAsync(
    String[]? labels,
    DateTime? start,
    DateTime? end,
    CancellationToken cancellationToken = default) {
    var parameters = new UriQueryStringParameterCollection();

    if (labels != null && labels.Any()) {
      foreach (var label in labels) {
        parameters.Add(key: "match[]", label);
      }
    }

    if(start != null && end != null){
      parameters.Add(key: "start", start);
      parameters.Add(key: "end", end);
    }

    using var client = this._clientFactory();
    // var labelVal = labels[0];
    using var response = await client.GetAsync(
      // $"{PrometheusClient.BaseUrlPath}{PrometheusClient.LabelsUrlPath}?match[]={labelVal}",
      $"{PrometheusClient.BaseUrlPath}{PrometheusClient.LabelsUrlPath}" +
      (parameters.Count > 0 ? $"?{parameters}" : ""),
      cancellationToken
    );

    return await PrometheusClient.HandleLabelsResponse(response, cancellationToken);
  }

  public async Task<ResponseEnvelope<IImmutableList<String>>> LabelsPostAsync(
    String[]? labels,
    DateTime? start,
    DateTime? end,
    CancellationToken cancellationToken = default) {

    var parameters = new UriQueryStringParameterCollection();

    if (labels != null && labels.Any()) {
      foreach (var label in labels) {
        parameters.Add(key: "match[]", label);
      }
    }

    if(start != null && end != null){
      parameters.Add(key: "start", start);
      parameters.Add(key: "end", end);
    }

    using var client = this._clientFactory();
    // var labelVal = labels[0];
    using var response = await client.PostAsync(
      // $"{PrometheusClient.BaseUrlPath}{PrometheusClient.LabelsUrlPath}?match[]={labelVal}",
      $"{PrometheusClient.BaseUrlPath}{PrometheusClient.LabelsUrlPath}",
      new StringContent(
        parameters.ToString(),
        Encoding.UTF8,
        PrometheusClient.FormUrlEncodedMediaType),
      cancellationToken
    );

    return await PrometheusClient.HandleLabelsResponse(response, cancellationToken);
  }

  public async Task<ResponseEnvelope<IImmutableList<String>>> LabelValueAsync(
    String labelKey,
    String[]? seriesSelectors,
    DateTime? start,
    DateTime? end,
    CancellationToken cancellationToken = default) {

    var parameters = new UriQueryStringParameterCollection();

    if (seriesSelectors != null && seriesSelectors.Any()) {
      foreach (var label in seriesSelectors) {
        parameters.Add(key: "match[]", label);
      }
    }

    if(start != null && end != null){
      parameters.Add(key: "start", start);
      parameters.Add(key: "end", end);
    }

    using var client = this._clientFactory();
    // var labelVal = labels[0];
    using var response = await client.GetAsync(
      $"{PrometheusClient.BaseUrlPath}{PrometheusClient.LabelValuesUrlPath}/{labelKey}/values" +
      (parameters.Count > 0 ? $"?{parameters}" : ""),
      cancellationToken
    );

    return await PrometheusClient.HandleLabelsResponse(response, cancellationToken);
  }

  public async Task<ResponseEnvelope<IImmutableList<String>>> LabelValuePostAsync(
    String labelKey,
    String[]? seriesSelectors,
    DateTime? start,
    DateTime? end,
    CancellationToken cancellationToken = default) {

    var parameters = new UriQueryStringParameterCollection();

    if (seriesSelectors != null && seriesSelectors.Any()) {
      foreach (var label in seriesSelectors) {
        parameters.Add(key: "match[]", label);
      }
    }

    if(start != null && end != null){
      parameters.Add(key: "start", start);
      parameters.Add(key: "end", end);
    }

    using var client = this._clientFactory();
    using var response = await client.PostAsync(
      $"{PrometheusClient.BaseUrlPath}{PrometheusClient.LabelValuesUrlPath}/{labelKey}/values",
      new StringContent(
        parameters.ToString(),
        Encoding.UTF8,
        PrometheusClient.FormUrlEncodedMediaType),
      cancellationToken
    );

    return await PrometheusClient.HandleLabelsResponse(response, cancellationToken);
  }



  /// <summary>
  ///   Converts a C# <see cref="TimeSpan" /> to a
  ///   <a href="https://prometheus.io/docs/prometheus/latest/querying/basics/#time-durations">
  ///     Prometheus duration string
  ///   </a>
  ///   consisting of integer numbers time units.
  /// </summary>
  /// <remarks>
  ///   The maximum supported unit in the output is days.
  /// </remarks>
  public static String ToPrometheusDuration(TimeSpan duration) {
    var parts = new List<String>();
    if (duration.Days > 0) {
      parts.Add($"{duration.Days}d");
    }
    if (duration.Hours > 0) {
      parts.Add($"{duration.Hours}h");
    }
    if (duration.Minutes > 0) {
      parts.Add($"{duration.Minutes}m");
    }
    if (duration.Seconds > 0) {
      parts.Add($"{duration.Seconds}s");
    }
    if (duration.Milliseconds > 0) {
      parts.Add($"{duration.Milliseconds}ms");
    }

    return String.Join(separator: "", parts);
  }
}
