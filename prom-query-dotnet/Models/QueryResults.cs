using System.Collections.Immutable;

namespace PrometheusQuerySdK.Models;

public record QueryResults(
  QueryResultType ResultType,
  IImmutableList<ResultData> Result,
  IImmutableList<String>? Statistics);