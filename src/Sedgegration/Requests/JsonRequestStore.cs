using System.Text.Json;
using Sedgegration.Requests;
using Sedgegration.IO;

namespace Sedgegration.Requests;

public class JsonRequestStore : IRequestStore
{
    private readonly string _dir;
    private readonly DirectoryFileWriter _writer;
    private static readonly JsonSerializerOptions Opts = new() { WriteIndented = true };

    public JsonRequestStore(string dir)
    {
        _dir = dir;
        if (!Directory.Exists(_dir)) Directory.CreateDirectory(_dir);
        _writer = new DirectoryFileWriter(_dir);
    }

    public async Task SaveAsync(PersistedRequest request)
    {
        var fileName = request.Id + ".json";
        var json = JsonSerializer.Serialize(request, Opts);
        await _writer.WriteTextAsync(fileName, json);
    }

    public async Task<IReadOnlyList<PersistedRequest>> GetAllAsync()
    {
        var list = new List<PersistedRequest>();
        foreach (var f in Directory.GetFiles(_dir, "*.json"))
        {
            try
            {
                var json = await File.ReadAllTextAsync(f);
                var obj = JsonSerializer.Deserialize<PersistedRequest>(json);
                if (obj is not null) list.Add(obj);
            }
            catch { }
        }
        return list.OrderByDescending(r => r.ReceivedAt).ToList();
    }

    public async Task<PersistedRequest?> GetAsync(string id)
    {
        var path = Path.Combine(_dir, id + ".json");
        if (!File.Exists(path)) return null;
        try
        {
            var json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<PersistedRequest>(json);
        }
        catch { return null; }
    }
}
