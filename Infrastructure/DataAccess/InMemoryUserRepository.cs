namespace ProjectDz;

public class InMemoryUserRepository: IUserRepository
{
    private readonly List<ToDoUser> _users = new();

    public async Task<ToDoUser?> GetUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        await Task.CompletedTask; // Для асинхронного контекста
        return _users.FirstOrDefault(u => u.UserId == userId);
    }

    public async Task<ToDoUser?> GetUserByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken)
    {
        await Task.CompletedTask; // Для асинхронного контекста
        return _users.FirstOrDefault(u => u.TelegramUserId == telegramUserId);
    }

    public async Task AddAsync(ToDoUser user, CancellationToken cancellationToken)
    {
        await Task.CompletedTask; // Для асинхронного контекста
        _users.Add(user);
    }
}