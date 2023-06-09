using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PrometheusQuerySdk.Internal;

internal class ArrayTupleConverter<TTuple> : JsonConverter<TTuple> where TTuple : ITuple {
  public override TTuple Read(
    ref Utf8JsonReader reader,
    Type typeToConvert,
    JsonSerializerOptions options) {

    Debug.Assert(reader.TokenType == JsonTokenType.StartArray);
    var readStartArray = reader.Read();
    Debug.Assert(readStartArray);

    var tupleArgumentTypes = typeToConvert.GetGenericArguments();
    var arguments = new List<Object?>();
    for (var i = 0;
      (i < tupleArgumentTypes.Length) && (reader.TokenType != JsonTokenType.EndArray);
      i++, reader.Read()) {

      arguments.Add(JsonSerializer.Deserialize(ref reader, tupleArgumentTypes[i], options));
    }

    if (arguments.Count != tupleArgumentTypes.Length) {
      throw new JsonException(
        $"Unexpected number of tuple values. Expected {tupleArgumentTypes.Length}, Actual: {arguments.Count}"
      );
    }

    if (reader.TokenType != JsonTokenType.EndArray) {
      throw new JsonException(
        $"Found more than the expected number of tuple values. Expected: {tupleArgumentTypes.Length}"
      );
    }

    // new ValueTuple<DateTime, Double>(DateTime.Now, 123.2);
    return (TTuple)TupleCreator.CreateTuple(
      typeToConvert,
      arguments.ToArray()
    );
  }

  public override void Write(
    Utf8JsonWriter writer,
    TTuple value,
    JsonSerializerOptions options) {

    if (value is ITuple tuple) {
      writer.WriteStartArray();

      var tupleTypeArguments = value.GetType().GetGenericArguments();

      for (var i = 0; i < tuple.Length; i++) {
        JsonSerializer.Serialize(
          writer,
          value[i],
          tupleTypeArguments[i],
          options
        );
      }

      writer.WriteEndArray();
    } else {
      throw new ArgumentException("The specified value is not the expected type.", nameof(value));
    }
  }

  private static class TupleCreator {
    public static Object CreateTuple(Type tupleType, Object?[] arguments) {
      var types = tupleType.GetGenericArguments();
      if (tupleType.IsClass) {
        return CreateTupleMethods[types.Length - 1].MakeGenericMethod(types).Invoke(obj: null, arguments)!;
      } else {
        return CreateValueTupleMethods[types.Length - 1].MakeGenericMethod(types).Invoke(obj: null, arguments)!;
      }
    }

    // ReSharper disable once StaticMemberInGenericType
    private static readonly MethodInfo[] CreateTupleMethods = new[] {
      new Func<Object, Tuple<Object>>(CreateTuple).Method.GetGenericMethodDefinition(),
      new Func<Object, Object, Tuple<Object, Object>>(CreateTuple).Method.GetGenericMethodDefinition(),
      new Func<Object, Object, Object, Tuple<Object, Object, Object>>(CreateTuple).Method.GetGenericMethodDefinition(),
      new Func<Object, Object, Object, Object, Tuple<Object, Object, Object, Object>>(CreateTuple).Method.GetGenericMethodDefinition(),
      new Func<Object, Object, Object, Object, Object, Tuple<Object, Object, Object, Object, Object>>(CreateTuple).Method.GetGenericMethodDefinition(),
      new Func<Object, Object, Object, Object, Object, Object, Tuple<Object, Object, Object, Object, Object, Object>>(CreateTuple).Method.GetGenericMethodDefinition(),
      new Func<Object, Object, Object, Object, Object, Object, Object, Tuple<Object, Object, Object, Object, Object, Object, Object>>(CreateTuple).Method.GetGenericMethodDefinition(),
    };

    // ReSharper disable once StaticMemberInGenericType
    private static readonly MethodInfo[] CreateValueTupleMethods = new[] {
      new Func<Object, ValueTuple<Object>>(CreateValueTuple).Method.GetGenericMethodDefinition(),
      new Func<Object, Object, ValueTuple<Object, Object>>(CreateValueTuple).Method.GetGenericMethodDefinition(),
      new Func<Object, Object, Object, ValueTuple<Object, Object, Object>>(CreateValueTuple).Method.GetGenericMethodDefinition(),
      new Func<Object, Object, Object, Object, ValueTuple<Object, Object, Object, Object>>(CreateValueTuple).Method.GetGenericMethodDefinition(),
      new Func<Object, Object, Object, Object, Object, ValueTuple<Object, Object, Object, Object, Object>>(CreateValueTuple).Method.GetGenericMethodDefinition(),
      new Func<Object, Object, Object, Object, Object, Object, ValueTuple<Object, Object, Object, Object, Object, Object>>(CreateValueTuple).Method.GetGenericMethodDefinition(),
      new Func<Object, Object, Object, Object, Object, Object, Object, ValueTuple<Object, Object, Object, Object, Object, Object, Object>>(CreateValueTuple).Method.GetGenericMethodDefinition(),
    };

    private static Tuple<T1> CreateTuple<T1>(T1 value1) {
      return new Tuple<T1>(value1);
    }

    private static Tuple<T1, T2> CreateTuple<T1, T2>(T1 value1, T2 value2) {
      return new Tuple<T1, T2>(value1, value2);
    }

    private static Tuple<T1, T2, T3> CreateTuple<T1, T2, T3>(T1 value1, T2 value2, T3 value3) {
      return new Tuple<T1, T2, T3>(value1, value2, value3);
    }

    private static Tuple<T1, T2, T3, T4> CreateTuple<T1, T2, T3, T4>(T1 value1, T2 value2, T3 value3, T4 value4) {
      return new Tuple<T1, T2, T3, T4>(value1, value2, value3, value4);
    }

    private static Tuple<T1, T2, T3, T4, T5> CreateTuple<T1, T2, T3, T4, T5>(
      T1 value1, T2 value2, T3 value3, T4 value4, T5 value5) {
      return new Tuple<T1, T2, T3, T4, T5>(value1, value2, value3, value4, value5);
    }

    private static Tuple<T1, T2, T3, T4, T5, T6> CreateTuple<T1, T2, T3, T4, T5, T6>(
      T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6) {
      return new Tuple<T1, T2, T3, T4, T5, T6>(value1, value2, value3, value4, value5, value6);
    }

    private static Tuple<T1, T2, T3, T4, T5, T6, T7> CreateTuple<T1, T2, T3, T4, T5, T6, T7>(
      T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7) {
      return new Tuple<T1, T2, T3, T4, T5, T6, T7>(value1, value2, value3, value4, value5, value6, value7);
    }

    private static ValueTuple<T1> CreateValueTuple<T1>(T1 value1) {
      return new ValueTuple<T1>(value1);
    }

    private static ValueTuple<T1, T2> CreateValueTuple<T1, T2>(T1 value1, T2 value2) {
      return new ValueTuple<T1, T2>(value1, value2);
    }

    private static ValueTuple<T1, T2, T3> CreateValueTuple<T1, T2, T3>(T1 value1, T2 value2, T3 value3) {
      return new ValueTuple<T1, T2, T3>(value1, value2, value3);
    }

    private static ValueTuple<T1, T2, T3, T4> CreateValueTuple<T1, T2, T3, T4>(T1 value1, T2 value2, T3 value3, T4 value4) {
      return new ValueTuple<T1, T2, T3, T4>(value1, value2, value3, value4);
    }

    private static ValueTuple<T1, T2, T3, T4, T5> CreateValueTuple<T1, T2, T3, T4, T5>(
      T1 value1, T2 value2, T3 value3, T4 value4, T5 value5) {
      return new ValueTuple<T1, T2, T3, T4, T5>(value1, value2, value3, value4, value5);
    }

    private static ValueTuple<T1, T2, T3, T4, T5, T6> CreateValueTuple<T1, T2, T3, T4, T5, T6>(
      T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6) {
      return new ValueTuple<T1, T2, T3, T4, T5, T6>(value1, value2, value3, value4, value5, value6);
    }

    private static ValueTuple<T1, T2, T3, T4, T5, T6, T7> CreateValueTuple<T1, T2, T3, T4, T5, T6, T7>(
      T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7) {
      return new ValueTuple<T1, T2, T3, T4, T5, T6, T7>(value1, value2, value3, value4, value5, value6, value7);
    }
  }
}

internal static class ArrayTupleConverter {
  public static JsonConverter ForType(Type typeToConvert) {
    return ((JsonConverter)ArrayTupleConverter.ForTypeMethod.MakeGenericMethod(typeToConvert)
      .Invoke(obj: null, Array.Empty<Object?>())!);
  }

  private static readonly MethodInfo ForTypeMethod =
    new Func<ArrayTupleConverter<ITuple>>(ArrayTupleConverter.ForType<ITuple>).Method.GetGenericMethodDefinition();

  public static ArrayTupleConverter<TTuple> ForType<TTuple>() where TTuple : ITuple {
    return new ArrayTupleConverter<TTuple>();
  }
}
