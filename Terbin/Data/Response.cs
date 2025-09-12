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

public class StatusResponse
{
    public required int Code; // Usamos códigos HTTP
    public string? Message;

    public static StatusResponse OK = new StatusResponse()
    {
        Code = 200,
        Message = "ok"
    };
}