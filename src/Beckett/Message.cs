using System.Text.Json;
using Beckett.Messages;

namespace Beckett;

/// <summary>
/// Message envelope used to write messages in Beckett
/// </summary>
public readonly record struct Message
{
    private static readonly JsonDocument EmptyJsonDocument = JsonDocument.Parse("{}");

    /// <summary>
    /// Create a message using a .NET object where the type will be used to determine the message type using the
    /// configured type mapping as well as serializing it to JSON for storage. If the message type has not been mapped If metadata is supplied it will be
    /// serialized and stored along with the message
    /// </summary>
    /// <param name="message">The message to write</param>
    /// <param name="metadata">Message metadata</param>
    /// <exception cref="UnknownTypeException">The message type is not mapped</exception>
    public Message(object message, Dictionary<string, object>? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(message);

        Type = MessageTypeMap.GetName(message.GetType());
        Data = MessageSerializer.Serialize(message);
        Metadata = metadata ?? new Dictionary<string, object>();
    }

    /// <summary>
    /// Create a message using a known message type, data, and if metadata is supplied it will be serialized and stored
    /// along with the message
    /// </summary>
    /// <param name="type">Message type</param>
    /// <param name="data">Message data</param>
    /// <param name="metadata">Message metadata</param>
    public Message(string type, JsonDocument data, Dictionary<string, object>? metadata = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentNullException.ThrowIfNull(data);

        Type = type;
        Data = data;
        Metadata = metadata ?? new Dictionary<string, object>();
    }

    public string Type { get; }
    public JsonDocument Data { get; }
    public Dictionary<string, object> Metadata { get; }

    public void AddMetadata(string key, object value) => Metadata.Add(key, value);

    internal JsonDocument SerializedMetadata =>
        Metadata.Count > 0 ? MessageSerializer.Serialize(Metadata) : EmptyJsonDocument;
}
