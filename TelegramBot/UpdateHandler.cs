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
                ? _userService.GetUser(update.Message.From.Id) 
                : null;
            
            if (update.Message.Text?.ToLower() == "/start")
            {
                RegisterUser(botClient, update.Message.Chat, update);
                return;
            }
            
            ShowCurrentMenu(botClient, update.Message.Chat, currentUser);
            
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
                AddTask(botClient, update.Message.Chat, taskName, currentUser);
            }
            else if (update.Message.Text?.ToLower().StartsWith("/removetask") == true)
            {
                var taskNumStr = update.Message.Text.Substring("/removetask".Length).Trim();
                RemoveTask(botClient, update.Message.Chat, taskNumStr, currentUser);
            }
            else if (update.Message.Text?.ToLower().StartsWith("/completetask") == true)
            {
                var taskIdStr = update.Message.Text.Substring("/completetask".Length).Trim();
                CompleteTask(botClient, update.Message.Chat, taskIdStr);
            }
            else if (update.Message.Text?.ToLower().StartsWith("/find") == true)
            {
                var taskName = update.Message.Text.Substring("/find".Length).Trim();
                FindTask(botClient, update.Message.Chat, currentUser, taskName);
            }
            else
            {

                switch (update.Message.Text.ToLower())
                {
                    case "/start":
                        RegisterUser(botClient, update.Message.Chat, update);
                        break;
                    case "/help":
                        Help(botClient, update.Message.Chat);
                        break;
                    case "/info":
                        Info(botClient, update.Message.Chat, currentUser);
                        break;
                    case "/showtasks":
                        ShowTasks(botClient, update.Message.Chat, currentUser);
                        break;
                    case "/showalltasks":
                        ShowAllTasks(botClient, update.Message.Chat, currentUser);
                        break;
                    case "/report":
                        ShowReport(botClient, update.Message.Chat, currentUser);
                        break;
                    case "/exit":
                        Exit(botClient, update.Message.Chat, currentUser);
                        return;
                    default: await botClient.SendMessage(update.Message.Chat, "Неизвестная команда", 
                        cancellationToken); break;
                }
            }
            ShowCurrentMenu(botClient, update.Message.Chat, currentUser);
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
    
    void RegisterUser(ITelegramBotClient botClient, Chat chat, Update update)
    {
        if (update?.Message?.From == null)
            throw new ArgumentException("Invalid Update object");

        var from = update.Message.From;
        var user = _userService.GetUser(from.Id);
        
        if (user != null)
        {
            botClient.SendMessage(chat, $"Вы уже зарегистрированы: {user.TelegramUserName}", CancellationToken.None);
            return;
        }
        
        string userName = !string.IsNullOrEmpty(from.Username) 
            ? from.Username 
            : $"User-{from.Id}";

        user = _userService.RegisterUser(from.Id, userName);
        
        botClient.SendMessage(chat,$"Добро пожаловать, {user.TelegramUserName}!", CancellationToken.None);
        botClient.SendMessage(chat,$"Ваш ID: {user.UserId}", CancellationToken.None);
        botClient.SendMessage(chat,$"Дата регистрации: {user.RegisteredAt}", CancellationToken.None);
    }
    
    static void Help(ITelegramBotClient botClient, Chat chat)
    {
        botClient.SendMessage(chat,"Описание доступных команд:", CancellationToken.None);
        botClient.SendMessage(chat,"/start - начало работы с ботом, ввод имени", CancellationToken.None);
        botClient.SendMessage(chat,"/info - получить информацию о боте", CancellationToken.None);
        botClient.SendMessage(chat,"/addtask - добавить задачу в список задач", CancellationToken.None);
        botClient.SendMessage(chat,"/showtasks - показать список текущих задач", CancellationToken.None);
        botClient.SendMessage(chat,"/showalltasks - показать список всех задач", CancellationToken.None);
        botClient.SendMessage(chat,"/removetask - удалить задачу из текущего списка", CancellationToken.None);
        botClient.SendMessage(chat,"/completetask - отметить задачу как завершенную", CancellationToken.None);
        botClient.SendMessage(chat,"/report - показать статистику по задачам", CancellationToken.None);
        botClient.SendMessage(chat,"/find - найти задачу по названию", CancellationToken.None);
    }

    void Info(ITelegramBotClient botClient, Chat chat, ToDoUser user)
    {
        botClient.SendMessage(chat, string.IsNullOrWhiteSpace(user.TelegramUserName)
            ? "Версия бота 1.0, дата создания 25.05.2025"
            : $"{user.TelegramUserName}, версия бота 1.0, дата создания 25.05.2025", CancellationToken.None);
    }
    
    void AddTask(ITelegramBotClient botClient, Chat chat, string taskName, ToDoUser user)
    {
        if (string.IsNullOrWhiteSpace(taskName))
        {
            botClient.SendMessage(chat,$"{user.TelegramUserName}, описание задачи не может быть пустым", 
                CancellationToken.None);
            return;
        }

        var newTask = _toDoService.Add(user, taskName);
        
        botClient.SendMessage(chat,$"{user.TelegramUserName}, задача добавлена!", CancellationToken.None);
    }
    
    void ShowTasks(ITelegramBotClient botClient, Chat chat, ToDoUser user)
    {
        var tasks = _toDoService.GetActiveByUserId(user.UserId);
        if (tasks.Count == 0)
        {
            botClient.SendMessage(chat,$"{user.TelegramUserName}, список задач пуст", CancellationToken.None);
            return;
        }
        else
        {
            botClient.SendMessage(chat,"\nТекущий список задач:", CancellationToken.None);
            for (int i = 0; i < tasks.Count; i++)
            {
                var task = tasks[i];
                if (task.State == ToDoItem.ToDoItemState.Active)
                {
                    botClient.SendMessage(chat,$"{task.Name} - {task.CreatedAt.ToLocalTime()} - " +
                                               $"{task.Id}", CancellationToken.None);
                }
            }
        }
    }
    
    void FindTask(ITelegramBotClient botClient, Chat chat, ToDoUser user, string taskName)
    {
        if (string.IsNullOrWhiteSpace(taskName))
        {
            botClient.SendMessage(chat, "Укажите текст для поиска: /find Текст", CancellationToken.None);
            return;
        }
        
        var tasks = _toDoService.Find(user, taskName);
        if (tasks.Count == 0)
        {
            botClient.SendMessage(chat, $"{user.TelegramUserName}, задачи начинающиеся на '{taskName}' не найдены", 
                CancellationToken.None);
            return;
        }
        
        botClient.SendMessage(chat, $"Найденные задачи ({taskName}):", CancellationToken.None);
        
        foreach (var task in tasks)
        {
            botClient.SendMessage(chat, 
                $"{task.Name} - {task.CreatedAt.ToLocalTime()} - {task.Id}", CancellationToken.None);
        }
    }
    
    void ShowAllTasks(ITelegramBotClient botClient, Chat chat, ToDoUser user)
    {
        var tasks = _toDoService.GetActiveByUserId(user.UserId);
        if (tasks.Count == 0)
        {
            botClient.SendMessage(chat,$"{user.TelegramUserName}, список задач пуст", CancellationToken.None);
            return;
        }
        else
        {
            botClient.SendMessage(chat,"\nСписок всех задач:", CancellationToken.None);
            for (int i = 0; i < tasks.Count; i++)
            {
                var task = tasks[i];
                if (task.State == ToDoItem.ToDoItemState.Active || task.State == ToDoItem.ToDoItemState.Completed)
                {
                    botClient.SendMessage(chat,
                        $"({task.State}) {task.Name} - {task.CreatedAt.ToLocalTime()} - {task.Id}", 
                            CancellationToken.None);
                }
            }
        }
    }
    
    void RemoveTask(ITelegramBotClient botClient, Chat chat,  string taskNumStr, ToDoUser user)
    {
        var tasks = _toDoService.GetActiveByUserId(user.UserId);
        if (tasks.Count == 0)
        {
            botClient.SendMessage(chat,$"{user.TelegramUserName}, список задач пуст. Нечего удалять.", 
                CancellationToken.None);
            return;
        }
        
        if (!int.TryParse(taskNumStr, out int taskNumber) || taskNumber < 1 || taskNumber > tasks.Count)
        {
            botClient.SendMessage(chat,$"{user.TelegramUserName}, ошибка, " +
                                       $"введите корректный номер от 1 до {tasks.Count}!", CancellationToken.None);
            return;
        }

        _toDoService.Delete(tasks[taskNumber - 1].Id);
        botClient.SendMessage(chat,$"Задача удалена. Осталось задач: {tasks.Count}", CancellationToken.None);
    }
    
    void CompleteTask(ITelegramBotClient botClient, Chat chat, string taskIdStr)
    {
        if (!Guid.TryParse(taskIdStr, out Guid taskId))
        {
            botClient.SendMessage(chat,"Неверный формат Id. Пример: 73c7940a-ca8c-4327-8a15-9119bffd1d5e", 
                CancellationToken.None);
            return;
        }
        
        _toDoService.MarkCompleted(taskId);
        botClient.SendMessage(chat, $"Задача '{taskIdStr}' отмечена завершенной", CancellationToken.None);
    }

    public static void Exit(ITelegramBotClient botClient, Chat chat, ToDoUser user)
    {
        botClient.SendMessage(chat,string.IsNullOrEmpty(user.TelegramUserName)
            ? "До свидания!"
            : $"{user.TelegramUserName}, до свидания!", CancellationToken.None);
    }
    
    static void ShowCurrentMenu(ITelegramBotClient botClient, Chat chat, ToDoUser user)
    {
        string greeting = user == null 
            ? "Добро пожаловать в бота\nДоступные команды:" 
            : $"{user.TelegramUserName}, доступные команды";
        
        botClient.SendMessage(chat, greeting, CancellationToken.None);

        if (user == null)
        {
            botClient.SendMessage(chat,"/start - начать работу", CancellationToken.None);
        }

        else
        {
            botClient.SendMessage(chat,"/addtask - добавить задачу", CancellationToken.None);
            botClient.SendMessage(chat,"/showtasks - показать список текущих задач", CancellationToken.None);
            botClient.SendMessage(chat,"/showalltasks - показать список всех задач", CancellationToken.None);
            botClient.SendMessage(chat,"/removetask - удалить задачу", CancellationToken.None);
            botClient.SendMessage(chat,"/completetask - завершить задачу", CancellationToken.None);
            botClient.SendMessage(chat,"/report - показать статистику по задачам", CancellationToken.None);
            botClient.SendMessage(chat,"/find - найти задачу по названию", CancellationToken.None);
        }

        botClient.SendMessage(chat,"/help - справка", CancellationToken.None);
        botClient.SendMessage(chat,"/info - информация о боте", CancellationToken.None);
        botClient.SendMessage(chat,"/exit - выход", CancellationToken.None);
        botClient.SendMessage(chat,"Введите команду: ", CancellationToken.None);
    }
    
    private void ShowReport(ITelegramBotClient botClient, Chat chat, ToDoUser user)
    {
        var (total, completed, active, generatedAt) = _toDoReportService.GetUserStats(user.UserId);
        botClient.SendMessage(chat, 
            $"Статистика по задачам на {generatedAt:dd.MM.yyyy HH:mm:ss}\n" +
            $"Всего: {total}; Завершенных: {completed}; Активных: {active};", CancellationToken.None);
    }
    
    public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Error: {exception.Message}");
        return Task.CompletedTask;
    }
    
}