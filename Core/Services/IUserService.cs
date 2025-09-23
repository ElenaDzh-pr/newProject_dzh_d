namespace ProjectDz;

public interface IUserService
{
    Task<ToDoUser> RegisterUserAsync(long telegramUserId, string telegramUserName, CancellationToken cancellationToken = default);
    Task<ToDoUser?> GetUserAsync(long telegramUserId, CancellationToken cancellationToken = default);
}