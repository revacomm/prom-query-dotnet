namespace PrometheusQuerySdK.Models;

public record QueryRangePostRequest(
  String Query,
  DateTime Start,
  DateTime End,
  TimeSpan Step,
  TimeSpan? Timeout);