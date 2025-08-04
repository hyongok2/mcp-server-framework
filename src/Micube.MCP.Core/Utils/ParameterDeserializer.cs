using System;
using System.Text.Json;
using Newtonsoft.Json;

namespace Micube.MCP.Core.Utils;

public static class ParameterDeserializer
{
    public static T? DeserializeParams<T>(object? paramsObj) where T : class
    {
        if (paramsObj == null) return null;

        try
        {
            return paramsObj switch
            {
                // 이미 원하는 타입인 경우
                T directType => directType,
                
                // JSON 문자열인 경우
                string jsonStr when !string.IsNullOrWhiteSpace(jsonStr) => 
                    JsonConvert.DeserializeObject<T>(jsonStr),
                
                // JObject (Newtonsoft.Json)
                Newtonsoft.Json.Linq.JObject jObj => jObj.ToObject<T>(),
                
                // JToken (Newtonsoft.Json의 상위 타입)
                Newtonsoft.Json.Linq.JToken jToken => jToken.ToObject<T>(),
                
                // JsonElement (System.Text.Json)
                JsonElement jsonElement => JsonConvert.DeserializeObject<T>(jsonElement.GetRawText()),
                
                // Dictionary<string, object> - 일반적인 경우
                Dictionary<string, object> dict => JsonConvert.DeserializeObject<T>(
                    JsonConvert.SerializeObject(dict)),
                
                // 기타 모든 객체 - 재직렬화 방식
                _ => JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(paramsObj))
            };
        }
        catch (Exception ex)
        {
            throw new Newtonsoft.Json.JsonException($"Failed to deserialize parameters to {typeof(T).Name}: {ex.Message}", ex);
        }
    }
}