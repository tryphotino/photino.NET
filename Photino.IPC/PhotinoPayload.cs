using System.Text.Json;
using System.Text.Json.Serialization;

namespace Photino.NET.IPC;

public class PhotinoPayload<T>(string key, T? data) where T : class
{
    private static readonly JsonSerializerOptions DEFAULT_OPTIONS = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private static PhotinoPayload<T> Empty => new(string.Empty, null);

    [JsonPropertyName("key")]
    public string Key { get; init; } = key;

    [JsonPropertyName("data")]
    public T? Data { get; init; } = data;

    public static string ToJson(T payload) => JsonSerializer.Serialize(payload, DEFAULT_OPTIONS);

    public static PhotinoPayload<T>? FromJson(string json) => JsonSerializer.Deserialize<PhotinoPayload<T>>(json, DEFAULT_OPTIONS);

    public static bool TryFromJson(string json, out PhotinoPayload<T> payload)
    {
        try
        {
            payload = FromJson(json) ?? throw new InvalidOperationException();
            return true;
        }
        catch
        {
            payload = Empty;
            return false;
        }
    }
}