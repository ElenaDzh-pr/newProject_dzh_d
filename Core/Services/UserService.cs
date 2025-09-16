namespace ProjectDz;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }
    
    public ToDoUser RegisterUser(long telegramUserId, string telegramUserName)
    {
        var user = new ToDoUser(telegramUserId, telegramUserName);
        _userRepository.AddAsync(user);
            
        return user;
    }

    public ToDoUser? GetUser(long telegramUserId)
    {
        return _userRepository.GetUserByTelegramUserIdAsync(telegramUserId, CancellationToken.None);
    }
}