namespace ProjectDz;

public class ToDoUser
{
    public Guid UserId { get; }
    public string? TelegramUserName { get; }
    public DateTime RegisteredAt { get; }

    public ToDoUser(string telegramUserName)
    {
        TelegramUserName =  telegramUserName;
        UserId = Guid.NewGuid();
        RegisteredAt = DateTime.Now;
    }
}