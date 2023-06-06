using System.Collections.Immutable;

namespace PrometheusQuerySdk.Models;

public record QueryResults(
  QueryResultType ResultType,
  IImmutableList<ResultData> Result,
  IImmutableList<String>? Statistics);
