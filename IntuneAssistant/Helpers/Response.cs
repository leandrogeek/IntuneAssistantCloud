using System.Text.Json;

namespace IntuneAssistant.Helpers;

public sealed record GraphValueResponse<T>
{
        public IEnumerable<T>? Value { get; init; }
}

public static class CustomJsonOptions
{
        public static JsonSerializerOptions Default()
        {
                return new JsonSerializerOptions
                {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        PropertyNameCaseInsensitive = true,
                };
        }
}