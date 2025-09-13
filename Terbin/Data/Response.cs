using Newtonsoft.Json;

namespace Terbin.Data;

class Response
{
    public required StatusResponse Status;
    public object? Content;

    public string Json()
    {
        return JsonConvert.SerializeObject(this);
    }
}
public enum StatusCode
{
    OK = 200,
    BAD_REQUEST = 400,
    INTERNAL_SERVER_ERROR = 500,
}
public class StatusResponse
{
    public required int Code; // Usamos códigos HTTP
    public string? Message;
}