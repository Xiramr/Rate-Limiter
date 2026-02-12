using System.Text.Json.Serialization;

namespace RateLimiter.Reader.Transport;

public sealed record UserRequest(
    [property: JsonPropertyName("user_id")] int UserId, 
    [property: JsonPropertyName("endpoint")] string Endpoint
);