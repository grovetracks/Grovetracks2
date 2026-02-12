using System.Text.Json;

namespace Grovetracks.Etl.Models;

public class QuickdrawRecord
{
    public string key_id { get; set; } = "";
    public string word { get; set; } = "";
    public string countrycode { get; set; } = "";
    public string timestamp { get; set; } = "";
    public bool recognized { get; set; }
    public JsonElement drawing { get; set; }
}
