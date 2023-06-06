using PrometheusQuerySdk.Models;

namespace PrometheusQuerySdk;
public interface IPrometheusClient {
  Task<ResponseEnvelope<QueryResults>> QueryAsync(
    String query,
    DateTime timestamp,
    TimeSpan? timeout = null,
    CancellationToken cancellationToken = default);

  Task<ResponseEnvelope<QueryResults>> QueryPostAsync(
    QueryPostRequest request,
    CancellationToken cancellationToken = default);

  Task<ResponseEnvelope<QueryResults>> QueryRangeAsync(
    String query,
    DateTime start,
    DateTime end,
    TimeSpan step,
    TimeSpan? timeout = null,
    CancellationToken cancellationToken = default);

  Task<ResponseEnvelope<QueryResults>> QueryRangePostAsync(
    QueryRangePostRequest request,
    CancellationToken cancellationToken = default);
}
