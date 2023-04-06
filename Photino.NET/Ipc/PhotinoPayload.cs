using System.Text.Json;
using System.Text.Json.Serialization;

namespace PhotinoNET.Ipc;

public class PhotinoPayload<T> where T : class
{
    private static readonly JsonSerializerOptions DEFAULT_OPTIONS = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private static PhotinoPayload<T> Empty => new(string.Empty, null);

    public PhotinoPayload(string key, T data)
    {
        Key = key;
        Data = data;
    }

    [JsonPropertyName("key")]
    public string Key { get; init; }

    [JsonPropertyName("data")]
    public T Data { get; init; }

    public static string ToJson(T payload) => JsonSerializer.Serialize(payload, DEFAULT_OPTIONS);

    public static PhotinoPayload<T> FromJson(string json) => JsonSerializer.Deserialize<PhotinoPayload<T>>(json, DEFAULT_OPTIONS);

    public static bool TryFromJson(string json, out PhotinoPayload<T> payload)
    {
        try
        {
            payload = FromJson(json);
            return true;
        }
        catch
        {
            payload = PhotinoPayload<T>.Empty;
            return false;
        }
    }
}