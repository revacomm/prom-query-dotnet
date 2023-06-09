using Prometheus;

public interface IPrometheusRemoteProtocolClient {
  /// <summary>
  /// Sends the given request to the Prometheus remote write API.
  /// </summary>
  /// <param name="writeRequest">The Prometheus remote write request.</param>
  /// <param name="cancellationToken">The cancellation token for the async operation.</param>
  /// <exception cref="BadRequestException">If the Prometheus request is invalid.</exception>
  /// <exception cref="InternalServerErrorException">If there's any other problem calling Prometheus.</exception>
  public Task WriteAsync(WriteRequest writeRequest, CancellationToken cancellationToken);
}