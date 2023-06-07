using Google.Protobuf;
using Prometheus;
using Snappy;

public class PrometheusRemoteProtocolClient : IPrometheusRemoteProtocolClient {

  private const String WriteEndpoint = "/api/v1/write";

  private readonly HttpClient _prometheusHttpClient;

  /// <summary>
  /// Creates a new <see cref="PrometheusRemoteProtocolClient"/> instance that uses the given <see cref="HttpClient"/>.
  /// </summary>
  /// <param name="prometheusHttpClient">
  ///   An <see cref="HttpClient"/> who's <c>BaseUrl</c> property points to a Prometheus instance.
  /// </param>
  public PrometheusRemoteProtocolClient(HttpClient prometheusHttpClient) =>
    this._prometheusHttpClient = prometheusHttpClient;

  /// <inheritdoc/>
  public async Task WriteAsync(
    WriteRequest writeRequest,
    CancellationToken cancellationToken = default) {

    using var response = await this._prometheusHttpClient.PostAsync(
      WriteEndpoint,
      new ByteArrayContent(SnappyCodec.Compress(writeRequest.ToByteArray())),
      cancellationToken);

    if (!response.IsSuccessStatusCode) {
      var errorMessage = $"Prometheus remote write request failed ({(Int32)response.StatusCode} {response.StatusCode})";
      var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
      if (!String.IsNullOrEmpty(responseContent)) {
        errorMessage += ": " + responseContent;
      }

      throw (Int32)response.StatusCode switch {
        >= 400 and < 500 => new InvalidOperationException(errorMessage),
        _ => new InvalidOperationException(errorMessage)
      };
    }
  }
}
