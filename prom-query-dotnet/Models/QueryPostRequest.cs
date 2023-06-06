namespace PrometheusQuerySdK.Models;

public record QueryPostRequest(String Query, DateTime Timestamp, TimeSpan? Timeout);