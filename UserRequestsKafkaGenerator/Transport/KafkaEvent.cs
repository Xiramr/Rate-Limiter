using System.Text.Json.Serialization;

namespace UserRequestsKafkaGenerator.Transport;

public sealed record KafkaEvent(
    [property: JsonPropertyName("user_id")] int UserId, 
    [property: JsonPropertyName("endpoint")] string Endpoint
);