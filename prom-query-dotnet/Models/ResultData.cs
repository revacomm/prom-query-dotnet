using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace PrometheusQuerySdK.Models;

public record ResultData(
  [property:JsonPropertyName("metric")]
  IImmutableDictionary<String, String> Labels,
  (Decimal Timestamp, String Value)? Value,
  IImmutableList<(Decimal Timestamp, String Value)>? Values);