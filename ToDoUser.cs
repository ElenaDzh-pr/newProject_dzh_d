namespace ProjectDz;

public class ToDoUser
{
    public Guid UserId { get; }
    public string? TelegramUserName { get; }
    public DateTime RegisteredAt { get; }
    public long TelegramUserId { get; }

    public ToDoUser( long telegramUserId, string telegramUserName)
    {
        TelegramUserName =  telegramUserName;
        TelegramUserId = telegramUserId;
        UserId = Guid.NewGuid();
        RegisteredAt = DateTime.Now;
    }
}