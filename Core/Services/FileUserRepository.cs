using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProjectDz;

public class FileUserRepository: IUserRepository
{
    private readonly string _baseFolder;
    private readonly JsonSerializerOptions _jsonOptions;

    public FileUserRepository(string baseFolder)
    {
        _baseFolder = baseFolder;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };
        
        Directory.CreateDirectory(_baseFolder);
    }
    public async Task<ToDoUser?> GetUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var filePath = GetFilePath(userId);
        return await ReadFromFileAsync(filePath, cancellationToken);
    }

    public async Task<ToDoUser?> GetUserByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(_baseFolder))
            return null;

        foreach (var file in Directory.GetFiles(_baseFolder, "*.json"))
        {
            var user = await ReadFromFileAsync(file, cancellationToken);
            if (user != null && user.TelegramUserId == telegramUserId)
                return user;
        }

        return null;
    }

    public async Task AddAsync(ToDoUser user, CancellationToken cancellationToken)
    {
        var filePath = GetFilePath(user.UserId);
        await WriteToFileAsync(filePath, user, cancellationToken);
    }
    
    private string GetFilePath(Guid userId) => Path.Combine(_baseFolder, $"{userId}.json");

    private async Task<ToDoUser?> ReadFromFileAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            await using var fileStream = File.OpenRead(filePath);
            return await JsonSerializer.DeserializeAsync<ToDoUser>(fileStream, _jsonOptions, cancellationToken);
        }
        catch
        {
            return null;
        }
    }
    
    private async Task WriteToFileAsync(string filePath, ToDoUser user, CancellationToken cancellationToken)
    {
        await using var fileStream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(fileStream, user, _jsonOptions, cancellationToken);
    }
}