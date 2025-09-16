namespace ProjectDz;

public interface IUserRepository
{
    Task<ToDoUser?> GetUserAsync(Guid userId, CancellationToken cancellationToken);
    ToDoUser? GetUserByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken);
    Task AddAsync(ToDoUser user);
}