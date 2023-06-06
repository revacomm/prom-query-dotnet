using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.Json;

namespace PrometheusQuerySdk.Internal;

internal class UriQueryStringParameterCollection : IEnumerable<KeyValuePair<String, Object>>, ILookup<String, Object> {
  private readonly JsonSerializerOptions _options;
  private readonly IDictionary<String, List<Object>> _values = new Dictionary<String, List<Object>>();

  public Int32 Count => this._values.Values.Sum(v => v.Count);

  public IEnumerable<Object> this[String key] =>
    this._values.TryGetValue(key, out var val) ?
      val :
      Enumerable.Empty<Object>();

  public UriQueryStringParameterCollection() {
    this._options = new JsonSerializerOptions();
  }

  public UriQueryStringParameterCollection(JsonSerializerOptions options) {
    this._options = options;
  }

  public static implicit operator String(UriQueryStringParameterCollection collection) {
    return collection.ToString();
  }

  public Boolean Contains(String key) {
    return this._values.ContainsKey(key);
  }

  public void Add(String key, Object value) {
    if (this._values.TryGetValue(key, out var existing)) {
      existing.Add(value);
    } else {
      this._values.Add(key, new List<Object> { value });
    }
  }

  public override String ToString() {
    // encode each key and value as url_encode(key)=url_encode(as_string(value))
    // join results key1=value1&key2=value2&...

    return String.Join(
      separator: "&",
      this._values.SelectMany(kvp => kvp.Value.Select(val => (kvp.Key, val)))
        .Select(kvp => $"{WebUtility.UrlEncode(kvp.Key)}={WebUtility.UrlEncode(this.AsString(kvp.val))}")
    );
  }

  private String AsString(Object obj) {
    // obj = "as\"df"
    // json(obj) => "\"asdf\""
    // foo="asdf"
    // foo=asdf
    //
    // obj = DateTime.UtcNow
    // json(obj) => "\"2022-11-03T....\""

    var serialized = JsonSerializer.Serialize(obj, obj.GetType(), this._options);
    if (serialized.StartsWith('"')) {
      // If the object serialized as a JSON string (for example obj was a String or a DateTime or a
      // TimeSpan), then unquote the string for inclusion in the query string
      var unquoted = JsonSerializer.Deserialize<String>(serialized);
      Debug.Assert(unquoted != null);
      return unquoted;
    } else {
      return serialized;
    }
  }

  IEnumerator<IGrouping<String, Object>> IEnumerable<IGrouping<String, Object>>.GetEnumerator() {
    return this._values.Select(kvp => new Grouping(kvp.Key, kvp.Value))
      .Cast<IGrouping<String, Object>>()
      .GetEnumerator();
  }

  IEnumerator<KeyValuePair<String, Object>> IEnumerable<KeyValuePair<String, Object>>.GetEnumerator() {
    return this._values.SelectMany(kvp => kvp.Value.Select(val => new KeyValuePair<String, Object>(kvp.Key, val)))
      .GetEnumerator();
  }

  IEnumerator IEnumerable.GetEnumerator() {
    return ((IEnumerable<KeyValuePair<String, Object>>)this).GetEnumerator();
  }

  private class Grouping : IGrouping<String, Object> {
    private readonly IEnumerable<Object> _values;

    public String Key { get; }

    public Grouping(String key, IEnumerable<Object> values) {
      this._values = values;
      this.Key = key;
    }

    public IEnumerator<Object> GetEnumerator() {
      return this._values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
      return this.GetEnumerator();
    }

  }
}
