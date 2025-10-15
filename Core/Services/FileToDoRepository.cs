using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProjectDz;

public class FileToDoRepository:  IToDoRepository
{
    private readonly string _baseFolder;
    private readonly string _indexFile;
    private readonly JsonSerializerOptions _jsonOptions;
    
    public FileToDoRepository(string baseFolder)
    {
        _baseFolder = baseFolder;
        _indexFile = Path.Combine(_baseFolder, "index.json");
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };
        
        Directory.CreateDirectory(_baseFolder);
    }
    public async Task<IReadOnlyList<ToDoItem>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var userFolder = GetUserFolder(userId);
        if (!Directory.Exists(userFolder))
            return new List<ToDoItem>().AsReadOnly();

        var tasks = new List<ToDoItem>();
        foreach (var file in Directory.GetFiles(userFolder, "*.json"))
        {
            var item = await ReadFromFileAsync(file, cancellationToken);
            if (item != null)
                tasks.Add(item);
        }

        return tasks.AsReadOnly();
    }

    public async Task<IReadOnlyList<ToDoItem>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var allItems = await GetAllByUserIdAsync(userId, cancellationToken);
        return allItems.Where(item => item.State == ToDoItem.ToDoItemState.Active).ToList().AsReadOnly();
    }

    public async Task<ToDoItem?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var index = await ReadIndexAsync(cancellationToken);
        if (index.TryGetValue(id, out var userId))
        {
            var filePath = GetFilePath(userId, id);
            if (File.Exists(filePath))
                return await ReadFromFileAsync(filePath, cancellationToken);
        }
        return null;
    }

    public async Task AddAsync(ToDoItem item, CancellationToken cancellationToken)
    {
        var userFolder = GetUserFolder(item.User.UserId);
        Directory.CreateDirectory(userFolder);
        
        var filePath = GetFilePath(item.User.UserId, item.Id);
        await WriteToFileAsync(filePath, item, cancellationToken);
        
        var index = await ReadIndexAsync(cancellationToken);
        index[item.Id] = item.User.UserId;
        await WriteIndexAsync(index, cancellationToken);
    }

    public async Task UpdateAsync(ToDoItem item, CancellationToken cancellationToken)
    {
        var filePath = GetFilePath(item.User.UserId, item.Id);
        if (File.Exists(filePath))
            await WriteToFileAsync(filePath, item, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var index = await ReadIndexAsync(cancellationToken);
        if (index.TryGetValue(id, out var userId))
        {
            var filePath = GetFilePath(userId, id);
            if (File.Exists(filePath))
                File.Delete(filePath);
            
            index.Remove(id);
            await WriteIndexAsync(index, cancellationToken);
        }
    }

    public async Task<bool> ExistsByNameAsync(Guid userId, string name, CancellationToken cancellationToken)
    {
        var items = await GetAllByUserIdAsync(userId, cancellationToken);
        return items.Any(item => 
            string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<int> CountActiveAsync(Guid userId, CancellationToken cancellationToken)
    {
        var items = await GetActiveByUserIdAsync(userId, cancellationToken);
        return items.Count;
    }

    public async Task<IReadOnlyList<ToDoItem>> FindAsync(Guid userId, Func<ToDoItem, bool> predicate, CancellationToken cancellationToken)
    {
        var items = await GetAllByUserIdAsync(userId, cancellationToken);
        return items.Where(predicate).ToList().AsReadOnly();
    }
    
    private string GetUserFolder(Guid userId) => Path.Combine(_baseFolder, userId.ToString());
    private string GetFilePath(Guid userId, Guid itemId) => Path.Combine(GetUserFolder(userId), $"{itemId}.json");

    private async Task<ToDoItem?> ReadFromFileAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            await using var fileStream = File.OpenRead(filePath);
            return await JsonSerializer.DeserializeAsync<ToDoItem>(fileStream, _jsonOptions, cancellationToken);
        }
        catch
        {
            return null;
        }
    }
    
    private async Task WriteToFileAsync(string filePath, ToDoItem item, CancellationToken cancellationToken)
    {
        await using var fileStream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(fileStream, item, _jsonOptions, cancellationToken);
    }
    
    private async Task<Dictionary<Guid, Guid>> ReadIndexAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_indexFile))
        {
            return await RebuildIndexAsync(cancellationToken);
        }

        try
        {
            await using var fileStream = File.OpenRead(_indexFile);
            return await JsonSerializer.DeserializeAsync<Dictionary<Guid, Guid>>(fileStream, _jsonOptions, cancellationToken) 
                   ?? new Dictionary<Guid, Guid>();
        }
        catch
        {
            return await RebuildIndexAsync(cancellationToken);
        }
    }
    
    private async Task WriteIndexAsync(Dictionary<Guid, Guid> index, CancellationToken cancellationToken)
    {
        await using var fileStream = File.Create(_indexFile);
        await JsonSerializer.SerializeAsync(fileStream, index, _jsonOptions, cancellationToken);
    }
    
    private async Task<Dictionary<Guid, Guid>> RebuildIndexAsync(CancellationToken cancellationToken)
    {
        var index = new Dictionary<Guid, Guid>();
        
        if (!Directory.Exists(_baseFolder))
            return index;

        foreach (var userFolder in Directory.GetDirectories(_baseFolder))
        {
            if (Guid.TryParse(Path.GetFileName(userFolder), out var userId))
            {
                foreach (var file in Directory.GetFiles(userFolder, "*.json"))
                {
                    if (Guid.TryParse(Path.GetFileNameWithoutExtension(file), out var itemId))
                    {
                        index[itemId] = userId;
                    }
                }
            }
        }
        await WriteIndexAsync(index, cancellationToken);
        return index;
    }
}