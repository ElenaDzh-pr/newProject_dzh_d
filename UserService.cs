namespace ProjectDz;

public class UserService : IUserService
{
    private readonly Dictionary<long, ToDoUser> _users = new();
    
    public ToDoUser RegisterUser(long telegramUserId, string telegramUserName)
    {
        var user = new ToDoUser(telegramUserId, telegramUserName);
        _users.Add(telegramUserId, user);
            
        return user;
    }

    public ToDoUser? GetUser(long telegramUserId)
    {
        _users.TryGetValue(telegramUserId, out var user);
        return user;
    }
}