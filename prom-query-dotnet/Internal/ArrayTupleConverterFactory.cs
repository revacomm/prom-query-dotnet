using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PrometheusQuerySdk.Internal;

internal class ArrayTupleConverterFactory : JsonConverterFactory {
  public override Boolean CanConvert(Type typeToConvert) {
    return typeToConvert.GetInterfaces().Contains(typeof(ITuple));
  }

  public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options) {
    return ArrayTupleConverter.ForType(typeToConvert);
  }
}







