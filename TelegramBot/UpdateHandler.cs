using Otus.ToDoList.ConsoleBot;
using Otus.ToDoList.ConsoleBot.Types;
using ProjectDz;
public delegate void MessageEventHandler(string message);

public class UpdateHandler : IUpdateHandler
{
    private readonly IUserService _userService;
    private readonly IToDoService _toDoService;
    private readonly IToDoReportService _toDoReportService;
    public event MessageEventHandler OnHandleUpdateStarted;
    public event MessageEventHandler OnHandleUpdateCompleted;
   
    public UpdateHandler(IUserService userService, IToDoService toDoService, IToDoReportService toDoReportService)
    {
        _userService = userService;
        _toDoService = toDoService;
        _toDoReportService = toDoReportService;
    }
    
    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, 
        CancellationToken cancellationToken)
    {
        OnHandleUpdateStarted?.Invoke(update.Message?.Text ?? "Unknown message");
        
        try
        {
            var currentUser = update.Message?.From != null 
                ? await Task.Run(() => _userService.GetUserAsync(update.Message.From.Id, cancellationToken))
                : null;
            
            if (update.Message.Text?.ToLower() == "/start")
            {
                await RegisterUserAsync(botClient, update.Message.Chat, update, cancellationToken);
                return;
            }
            
            await ShowCurrentMenuAsync(botClient, update.Message.Chat, currentUser, cancellationToken);
            
            if (currentUser == null)
            {
                await botClient.SendMessage(update.Message.Chat, 
                    "Сначала зарегистрируйтесь через /start\n" +
                    "Доступные команды: /help, /info", cancellationToken);
                return;
            }
            
            if (update.Message.Text?.ToLower().StartsWith("/addtask") == true)
            {
                var taskName = update.Message.Text.Substring("/addtask".Length).Trim();
                await AddTaskAsync(botClient, update.Message.Chat, taskName, currentUser, cancellationToken);
            }
            else if (update.Message.Text?.ToLower().StartsWith("/removetask") == true)
            {
                var taskNumStr = update.Message.Text.Substring("/removetask".Length).Trim();
                await RemoveTaskAsync(botClient, update.Message.Chat, taskNumStr, currentUser, cancellationToken);
            }
            else if (update.Message.Text?.ToLower().StartsWith("/completetask") == true)
            {
                var taskIdStr = update.Message.Text.Substring("/completetask".Length).Trim();
                await CompleteTaskAsync(botClient, update.Message.Chat, taskIdStr, cancellationToken);
            }
            else if (update.Message.Text?.ToLower().StartsWith("/find") == true)
            {
                var taskName = update.Message.Text.Substring("/find".Length).Trim();
                await FindTaskAsync(botClient, update.Message.Chat, currentUser, taskName,  cancellationToken);
            }
            else
            {
                switch (update.Message.Text.ToLower())
                {
                    case "/start":
                        await RegisterUserAsync(botClient, update.Message.Chat, update, cancellationToken);
                        break;
                    case "/help":
                        await HelpAsync(botClient, update.Message.Chat, cancellationToken);
                        break;
                    case "/info":
                        await InfoAsync(botClient, update.Message.Chat, currentUser, cancellationToken);
                        break;
                    case "/showtasks":
                        await ShowTasksAsync(botClient, update.Message.Chat, currentUser, cancellationToken);
                        break;
                    case "/showalltasks":
                        await ShowAllTasksAsync(botClient, update.Message.Chat, currentUser,  cancellationToken);
                        break;
                    case "/report":
                        await ShowReportAsync(botClient, update.Message.Chat, currentUser, cancellationToken);
                        break;
                    case "/exit":
                        await ExitAsync(botClient, update.Message.Chat, currentUser, cancellationToken);
                        return;
                    default: await botClient.SendMessage(update.Message.Chat, "Неизвестная команда", 
                        cancellationToken); break;
                }
            }
            await ShowCurrentMenuAsync(botClient, update.Message.Chat, currentUser, cancellationToken);
        }
        
        catch (ArgumentException ex)
        {
            await botClient.SendMessage(update.Message.Chat,ex.Message, cancellationToken);
        }
        catch (TaskCountLimitException ex)
        {
            await botClient.SendMessage(update.Message.Chat,ex.Message, cancellationToken);
        }
        catch (TaskLengthLimitException ex)
        {
            await botClient.SendMessage(update.Message.Chat,ex.Message, cancellationToken);
        }
        catch (DuplicateTaskException ex)
        {
            await botClient.SendMessage(update.Message.Chat,ex.Message, cancellationToken);
        }
        
        finally
        {
            OnHandleUpdateCompleted?.Invoke(update.Message?.Text ?? "Unknown message");
        }
    }
    
    async Task RegisterUserAsync(ITelegramBotClient botClient, Chat chat, Update update, 
        CancellationToken cancellationToken)
    {
        if (update?.Message?.From == null)
            throw new ArgumentException("Invalid Update object");

        var from = update.Message.From;
        var user = await _userService.GetUserAsync(from.Id, cancellationToken);
        
        if (user != null)
        {
            await botClient.SendMessage(chat, $"Вы уже зарегистрированы: {user.TelegramUserName}", cancellationToken);
            return;
        }
        
        string userName = !string.IsNullOrEmpty(from.Username) 
            ? from.Username 
            : $"User-{from.Id}";

        user = await _userService.RegisterUserAsync(from.Id, userName, cancellationToken);
        
        await botClient.SendMessage(chat,$"Добро пожаловать, {user.TelegramUserName}!", cancellationToken);
        await botClient.SendMessage(chat,$"Ваш ID: {user.UserId}", cancellationToken);
        await botClient.SendMessage(chat,$"Дата регистрации: {user.RegisteredAt}", cancellationToken);
    }
    
    static async Task HelpAsync(ITelegramBotClient botClient, Chat chat, CancellationToken cancellationToken)
    {
        await botClient.SendMessage(chat,"Описание доступных команд:", cancellationToken);
        await botClient.SendMessage(chat,"/start - начало работы с ботом, ввод имени", cancellationToken);
        await botClient.SendMessage(chat,"/info - получить информацию о боте", cancellationToken);
        await botClient.SendMessage(chat,"/addtask - добавить задачу в список задач", cancellationToken);
        await botClient.SendMessage(chat,"/showtasks - показать список текущих задач", cancellationToken);
        await botClient.SendMessage(chat,"/showalltasks - показать список всех задач", cancellationToken);
        await botClient.SendMessage(chat,"/removetask - удалить задачу из текущего списка", cancellationToken);
        await botClient.SendMessage(chat,"/completetask - отметить задачу как завершенную", cancellationToken);
        await botClient.SendMessage(chat,"/report - показать статистику по задачам", cancellationToken);
        await botClient.SendMessage(chat,"/find - найти задачу по названию", cancellationToken);
    }

    async Task InfoAsync(ITelegramBotClient botClient, Chat chat, ToDoUser user, CancellationToken cancellationToken)
    {
        await botClient.SendMessage(chat, string.IsNullOrWhiteSpace(user.TelegramUserName)
            ? "Версия бота 1.0, дата создания 25.05.2025"
            : $"{user.TelegramUserName}, версия бота 1.0, дата создания 25.05.2025", cancellationToken);
    }
    
    async Task AddTaskAsync(ITelegramBotClient botClient, Chat chat, string taskName, ToDoUser user, 
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(taskName))
        {
            await botClient.SendMessage(chat,$"{user.TelegramUserName}, описание задачи не может быть пустым", 
                cancellationToken);
            return;
        }

        var newTask = await _toDoService.AddAsync(user, taskName);
        
        await botClient.SendMessage(chat,$"{user.TelegramUserName}, задача добавлена!", cancellationToken);
    }
    
    async Task ShowTasksAsync(ITelegramBotClient botClient, Chat chat, ToDoUser user, 
        CancellationToken cancellationToken)
    {
        var tasks = await _toDoService.GetActiveByUserIdAsync(user.UserId);
        if (tasks.Count == 0)
        {
            await botClient.SendMessage(chat,$"{user.TelegramUserName}, список задач пуст", cancellationToken);
            return;
        }
        else
        {
            await botClient.SendMessage(chat,"\nТекущий список задач:", cancellationToken);
            for (int i = 0; i < tasks.Count; i++)
            {
                var task = tasks[i];
                if (task.State == ToDoItem.ToDoItemState.Active)
                {
                    await botClient.SendMessage(chat,$"{task.Name} - {task.CreatedAt.ToLocalTime()} - " +
                                                    $"{task.Id}", cancellationToken);
                }
            }
        }
    }
    
    async Task FindTaskAsync(ITelegramBotClient botClient, Chat chat, ToDoUser user, string taskName, 
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(taskName))
        {
            await botClient.SendMessage(chat, "Укажите текст для поиска: /find Текст", cancellationToken);
            return;
        }
        
        var tasks = await _toDoService.FindAsync(user, taskName);
        if (tasks.Count == 0)
        {
            await botClient.SendMessage(chat, $"{user.TelegramUserName}, задачи начинающиеся на '{taskName}' не найдены", 
                cancellationToken);
            return;
        }
        
        await botClient.SendMessage(chat, $"Найденные задачи ({taskName}):", cancellationToken);
        
        foreach (var task in tasks)
        {
            await botClient.SendMessage(chat, 
                $"{task.Name} - {task.CreatedAt.ToLocalTime()} - {task.Id}", cancellationToken);
        }
    }
    
    async Task ShowAllTasksAsync(ITelegramBotClient botClient, Chat chat, ToDoUser user, 
        CancellationToken cancellationToken)
    {
        var tasks = await _toDoService.GetActiveByUserIdAsync(user.UserId);
        if (tasks.Count == 0)
        {
            await botClient.SendMessage(chat,$"{user.TelegramUserName}, список задач пуст", cancellationToken);
            return;
        }
        else
        {
            await botClient.SendMessage(chat,"\nСписок всех задач:", cancellationToken);
            for (int i = 0; i < tasks.Count; i++)
            {
                var task = tasks[i];
                if (task.State == ToDoItem.ToDoItemState.Active || task.State == ToDoItem.ToDoItemState.Completed)
                {
                    await botClient.SendMessage(chat,
                        $"({task.State}) {task.Name} - {task.CreatedAt.ToLocalTime()} - {task.Id}", 
                            cancellationToken);
                }
            }
        }
    }
    
    async Task RemoveTaskAsync(ITelegramBotClient botClient, Chat chat, string taskNumStr, ToDoUser user, 
        CancellationToken cancellationToken)
    {
        var tasks = await _toDoService.GetActiveByUserIdAsync(user.UserId);
        if (tasks.Count == 0)
        {
            await botClient.SendMessage(chat,$"{user.TelegramUserName}, список задач пуст. Нечего удалять.", 
                cancellationToken);
            return;
        }
        
        if (!int.TryParse(taskNumStr, out int taskNumber) || taskNumber < 1 || taskNumber > tasks.Count)
        {
            await botClient.SendMessage(chat,$"{user.TelegramUserName}, ошибка, " +
                                            $"введите корректный номер от 1 до {tasks.Count}!", cancellationToken);
            return;
        }

        await _toDoService.DeleteAsync(tasks[taskNumber - 1].Id);
        await botClient.SendMessage(chat,$"Задача удалена. Осталось задач: {tasks.Count}", cancellationToken);
    }
    
    async Task CompleteTaskAsync(ITelegramBotClient botClient, Chat chat, string taskIdStr, 
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(taskIdStr, out Guid taskId))
        {
            await botClient.SendMessage(chat,"Неверный формат Id. Пример: 73c7940a-ca8c-4327-8a15-9119bffd1d5e", 
                cancellationToken);
            return;
        }
        
        await _toDoService.MarkCompletedAsync(taskId);
        await botClient.SendMessage(chat, $"Задача '{taskIdStr}' отмечена завершенной", cancellationToken);
    }

    static async Task ExitAsync(ITelegramBotClient botClient, Chat chat, ToDoUser user, 
        CancellationToken cancellationToken)
    {
        await botClient.SendMessage(chat,string.IsNullOrEmpty(user.TelegramUserName)
            ? "До свидания!"
            : $"{user.TelegramUserName}, до свидания!", cancellationToken);
    }
    
    static async Task ShowCurrentMenuAsync(ITelegramBotClient botClient, Chat chat, ToDoUser user, 
        CancellationToken cancellationToken)
    {
        string greeting = user == null 
            ? "Добро пожаловать в бота\nДоступные команды:" 
            : $"{user.TelegramUserName}, доступные команды";
        
        await botClient.SendMessage(chat, greeting, cancellationToken);

        if (user == null)
        {
            await botClient.SendMessage(chat,"/start - начать работу", cancellationToken);
        }

        else
        {
            await botClient.SendMessage(chat,"/addtask - добавить задачу", cancellationToken);
            await botClient.SendMessage(chat,"/showtasks - показать список текущих задач", cancellationToken);
            await botClient.SendMessage(chat,"/showalltasks - показать список всех задач", cancellationToken);
            await botClient.SendMessage(chat,"/removetask - удалить задачу", cancellationToken);
            await botClient.SendMessage(chat,"/completetask - завершить задачу", cancellationToken);
            await botClient.SendMessage(chat,"/report - показать статистику по задачам", cancellationToken);
            await botClient.SendMessage(chat,"/find - найти задачу по названию", cancellationToken);
        }

        await botClient.SendMessage(chat,"/help - справка", cancellationToken);
        await botClient.SendMessage(chat,"/info - информация о боте", cancellationToken);
        await botClient.SendMessage(chat,"/exit - выход", cancellationToken);
        await botClient.SendMessage(chat,"Введите команду: ", cancellationToken);
    }
    
    private async Task ShowReportAsync(ITelegramBotClient botClient, Chat chat, ToDoUser user, 
        CancellationToken cancellationToken)
    {
        var (total, completed, active, generatedAt) = await _toDoReportService.GetUserStatsAsync(user.UserId);
        await botClient.SendMessage(chat, 
            $"Статистика по задачам на {generatedAt:dd.MM.yyyy HH:mm:ss}\n" +
            $"Всего: {total}; Завершенных: {completed}; Активных: {active};", cancellationToken);
    }
    
    public async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        await Console.Out.WriteLineAsync($"Error: {exception.Message}");
        await Console.Out.WriteLineAsync($"Stack trace: {exception.StackTrace}");
    }
    
}