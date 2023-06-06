namespace PrometheusQuerySdK.Models;

public record ResponseEnvelope<TData>(
  ResponseStatus Status,
  TData? Data,
  String? ErrorType,
  String? Error,
  String[]? Warnings);