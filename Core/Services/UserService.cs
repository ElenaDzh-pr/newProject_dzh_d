namespace ProjectDz;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }
    
    public async Task<ToDoUser?> GetUserAsync(long telegramUserId, CancellationToken cancellationToken = default)
    {
        return await _userRepository.GetUserByTelegramUserIdAsync(telegramUserId, cancellationToken);
    }

    public async Task<ToDoUser> RegisterUserAsync(long telegramUserId, string userName, CancellationToken cancellationToken = default)
    {
        var existingUser = await _userRepository.GetUserByTelegramUserIdAsync(telegramUserId, cancellationToken);
        if (existingUser != null)
            return existingUser;

        var newUser = new ToDoUser(telegramUserId, userName);
        await _userRepository.AddAsync(newUser, cancellationToken);
        return newUser;
    }
}