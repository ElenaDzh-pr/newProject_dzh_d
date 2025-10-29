using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProjectDz;

public class FileToDoListRepository : IToDoListRepository
{
    private readonly string _baseFolder;
    private readonly JsonSerializerOptions _jsonOptions;

    public FileToDoListRepository(string baseFolder)
    {
        _baseFolder = baseFolder;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };
        
        Directory.CreateDirectory(_baseFolder);
    }
    public async Task<ToDoList?> Get(Guid id, CancellationToken ct)
    {
        var filePath = GetFilePath(id);
        return await ReadListFromFileAsync(filePath, ct);
    }

    public async Task<IReadOnlyList<ToDoList>> GetByUserId(Guid userId, CancellationToken ct)
    {
        var userFolder = GetUserFolder(userId);
        if (!Directory.Exists(userFolder))
            return new List<ToDoList>().AsReadOnly();

        var lists = new List<ToDoList>();
        foreach (var file in Directory.GetFiles(userFolder, "*.json"))
        {
            var list = await ReadListFromFileAsync(file, ct);
            if (list != null)
                lists.Add(list);
        }

        return lists.AsReadOnly();
    }

    public async Task Add(ToDoList list, CancellationToken ct)
    {
        var userFolder = GetUserFolder(list.User.UserId);
        Directory.CreateDirectory(userFolder);
        
        var filePath = GetFilePath(list.Id);
        await WriteListToFileAsync(filePath, list, ct);
    }

    public Task Delete(Guid id, CancellationToken ct)
    {
        var filePath = GetFilePath(id);
        if (File.Exists(filePath))
            File.Delete(filePath);
        
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsByName(Guid userId, string name, CancellationToken ct)
    {
        var lists = await GetByUserId(userId, ct);
        return lists.Any(list => 
            string.Equals(list.Name, name, StringComparison.OrdinalIgnoreCase));
    }
    
    private string GetUserFolder(Guid userId) => Path.Combine(_baseFolder, userId.ToString());
    private string GetFilePath(Guid listId) => Path.Combine(_baseFolder, $"{listId}.json");

    private async Task<ToDoList?> ReadListFromFileAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            await using var fileStream = File.OpenRead(filePath);
            return await JsonSerializer.DeserializeAsync<ToDoList>(fileStream, _jsonOptions, cancellationToken);
        }
        catch
        {
            return null;
        }
    }
    
    private async Task WriteListToFileAsync(string filePath, ToDoList list, CancellationToken cancellationToken)
    {
        await using var fileStream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(fileStream, list, _jsonOptions, cancellationToken);
    }
}