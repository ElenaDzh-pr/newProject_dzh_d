using Otus.ToDoList.ConsoleBot;
using Otus.ToDoList.ConsoleBot.Types;
using ProjectDz;

//var handler = new UpdateHandler();
//var botClient = new ConsoleBotClient();
//botClient.StartReceiving(handler);

public class UpdateHandler : IUpdateHandler
{
    
    static string? name = null;
    static ToDoUser? currentUser;
    static List<ToDoItem> tasks = new List<ToDoItem>();
    static int maxTaskLimit = 0;
    static int maxTaskLength = 0;
    static int maxLengthTask = 100;
    static int minLengthTask = 1;
    private static bool settingsInitialized = false;
    private readonly IUserService _userService;
    private readonly IToDoService _toDoService;

    public UpdateHandler(IUserService userService, IToDoService toDoService)
    {
        _userService = userService;
        _toDoService = toDoService;
    }
    
    public void HandleUpdateAsync(ITelegramBotClient botClient, Update update)
    {
        try
        {
            if (update.Message.Text?.ToLower() == "/start")
            {
                RegisterUser(botClient, update.Message.Chat, update);
                return;
            }
            
            if (!settingsInitialized)
            {
                InitializeSettings(botClient, update.Message.Chat);
                settingsInitialized = true;
                ShowCurrentMenu(botClient, update.Message.Chat);
                return;
            }
            
            if (currentUser == null)
            {
                botClient.SendMessage(update.Message.Chat, 
                    "Сначала зарегистрируйтесь через /start\n" +
                    "Доступные команды: /help, /info");
                return;
            }
            
            if (update.Message.Text?.ToLower().StartsWith("/addtask") == true)
            {
                var taskName = update.Message.Text.Substring("/addtask".Length).Trim();
                AddTask(botClient, update.Message.Chat, taskName);
            }
            else if (update.Message.Text?.ToLower().StartsWith("/removetask") == true)
            {
                var taskNumStr = update.Message.Text.Substring("/removetask".Length).Trim();
                RemoveTask(botClient, update.Message.Chat, taskNumStr);
            }
            else if (update.Message.Text?.ToLower().StartsWith("/completetask") == true)
            {
                var taskIdStr = update.Message.Text.Substring("/completetask".Length).Trim();
                CompleteTask(botClient, update.Message.Chat, taskIdStr);
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
                        Info(botClient, update.Message.Chat);
                        break;
                    // case "/addtask":
                    //     AddTask(botClient, update.Message.Chat);
                    //     break;
                    case "/showtasks":
                        ShowTasks(botClient, update.Message.Chat);
                        break;
                    case "/showalltasks":
                        ShowAllTasks(botClient, update.Message.Chat);
                        break;
                    // case "/removetask":
                    //     RemoveTask(botClient, update.Message.Chat);
                    //     break;
                    // case "/completetask":
                    //     CompleteTask(botClient, update.Message.Chat);
                    //     break;
                    case "/exit":
                        Exit(botClient, update.Message.Chat);
                        return;
                    default: botClient.SendMessage(update.Message.Chat, "Неизвестная команда"); break;
                }
            }
            ShowCurrentMenu(botClient, update.Message.Chat);
        }
        
        catch (ArgumentException ex)
        {
            botClient.SendMessage(update.Message.Chat,ex.Message);
        }
        catch (ToDoService.TaskCountLimitException ex)
        {
            botClient.SendMessage(update.Message.Chat,ex.Message);
        }
        catch (ToDoService.TaskLengthLimitException ex)
        {
            botClient.SendMessage(update.Message.Chat,ex.Message);
        }
        catch (ToDoService.DuplicateTaskException ex)
        {
            botClient.SendMessage(update.Message.Chat,ex.Message);
        }
    }

    static void InitializeSettings(ITelegramBotClient botClient, Chat chat)
    {
        while (true)
        {
            try
            {
                botClient.SendMessage(chat,"Введите максимально допустимое количество задач (от 1 до 100):");
                var input = Console.ReadLine();
                ProjectDz.Program.ValidateString(input);
                maxTaskLimit = ProjectDz.Program.ParseAndValidateInt(input, minLengthTask, maxLengthTask);
                break;
            }
            catch (ArgumentException ex)
            {
                botClient.SendMessage(chat, ex.Message);
                botClient.SendMessage(chat,"Попробуйте еще раз.\n");
            }
        }

        while (true)
        {
            try
            {
                botClient.SendMessage(chat,"Введите максимально допустимую длину задачи (от 1 до 100):");
                var input = Console.ReadLine();
                ProjectDz.Program.ValidateString(input);
                maxTaskLength = ProjectDz.Program.ParseAndValidateInt(input, minLengthTask, maxLengthTask);
                break;
            }
            catch (ArgumentException ex)
            {
                botClient.SendMessage(chat, ex.Message);
                botClient.SendMessage(chat,"Попробуйте еще раз.\n");
            }
        }
    }
    
    void RegisterUser(ITelegramBotClient botClient, Chat chat, Update update)
    {
        if (update?.Message?.From == null)
            throw new ArgumentException("Invalid Update object");

        var from = update.Message.From;
        currentUser = _userService.GetUser(from.Id);
        
        if (currentUser != null)
        {
            botClient.SendMessage(chat, $"Вы уже зарегистрированы: {currentUser.TelegramUserName}");
            return;
        }
        
        string userName = !string.IsNullOrEmpty(from.Username) 
            ? from.Username 
            : $"User-{from.Id}";

        currentUser = _userService.RegisterUser(from.Id, userName);
        
        botClient.SendMessage(chat,$"Добро пожаловать, {currentUser.TelegramUserName}!");
        botClient.SendMessage(chat,$"Ваш ID: {currentUser.UserId}");
        botClient.SendMessage(chat,$"Дата регистрации: {currentUser.RegisteredAt}");
    }
    
    static void Help(ITelegramBotClient botClient, Chat chat)
    {
        botClient.SendMessage(chat,"Описание доступных команд:");
        botClient.SendMessage(chat,"/start - начало работы с ботом, ввод имени");
        botClient.SendMessage(chat,"/info - получить информацию о боте");
        botClient.SendMessage(chat,"/addtask - добавить задачу в список задач");
        botClient.SendMessage(chat,"/showtasks - показать список текущих задач");
        botClient.SendMessage(chat,"/showalltasks - показать список всех задач");
        botClient.SendMessage(chat,"/removetask - удалить задачу из текущего списка");
        botClient.SendMessage(chat,"/completetask - отметить задачу как завершенную");
    }

    static void Info(ITelegramBotClient botClient, Chat chat)
    {
        botClient.SendMessage(chat, string.IsNullOrWhiteSpace(name)
            ? "Версия бота 1.0, дата создания 25.05.2025"
            : $"{currentUser.TelegramUserName}, версия бота 1.0, дата создания 25.05.2025");
    }
    
    void AddTask(ITelegramBotClient botClient, Chat chat, string taskName)
    {
        if (string.IsNullOrWhiteSpace(taskName))
        {
            botClient.SendMessage(chat,$"{currentUser.TelegramUserName}, описание задачи не может быть пустым");
            return;
        }

        var newTask = _toDoService.Add(currentUser, taskName);
        
        botClient.SendMessage(chat,$"{currentUser.TelegramUserName}, задача добавлена!");
    }
    
    static void ShowTasks(ITelegramBotClient botClient, Chat chat)
    {
        if (tasks.Count == 0)
        {
            botClient.SendMessage(chat,$"{currentUser.TelegramUserName}, список задач пуст");
            return;
        }
        else
        {
            botClient.SendMessage(chat,"\nТекущий список задач:");
            for (int i = 0; i < tasks.Count; i++)
            {
                var task = tasks[i];
                if (task.State == ToDoItem.ToDoItemState.Active)
                {
                    botClient.SendMessage(chat,$"{task.Name} - {task.CreatedAt.ToLocalTime()} - " +
                                               $"{task.Id}");
                }
            }
        }
    }
    
    static void ShowAllTasks(ITelegramBotClient botClient, Chat chat)
    {
        if (tasks.Count == 0)
        {
            botClient.SendMessage(chat,$"{currentUser.TelegramUserName}, список задач пуст");
            return;
        }
        else
        {
            botClient.SendMessage(chat,"\nСписок всех задач:");
            for (int i = 0; i < tasks.Count; i++)
            {
                var task = tasks[i];
                if (task.State == ToDoItem.ToDoItemState.Active || task.State == ToDoItem.ToDoItemState.Completed)
                {
                    botClient.SendMessage(chat,
                        $"({task.State}) {task.Name} - {task.CreatedAt.ToLocalTime()} - {task.Id}");
                }
            }
        }
    }
    
    void RemoveTask(ITelegramBotClient botClient, Chat chat,  string taskNumStr)
    {
        if (tasks.Count == 0)
        {
            botClient.SendMessage(chat,$"{currentUser.TelegramUserName}, список задач пуст. Нечего удалять.");
            return;
        }

        // ShowTasks(botClient, chat);
        //
        // botClient.SendMessage(chat,$"{currentUser.TelegramUserName}, введите номер задачи для удаления:");
        // var input = Console.ReadLine();
        // ProjectDz.Program.ValidateString(input);
        //
        // var i = !int.TryParse(taskNumStr, out int taskNumber);

        if (!int.TryParse(taskNumStr, out int taskNumber) || taskNumber < 1 || taskNumber > tasks.Count)
        {
            botClient.SendMessage(chat,$"{currentUser.TelegramUserName}, ошибка, " +
                                       $"введите корректный номер от 1 до {tasks.Count}!");
            return;
        }

        _toDoService.Delete(tasks[taskNumber - 1].Id);
        botClient.SendMessage(chat,$"Задача удалена. Осталось задач: {tasks.Count}");
    }
    
    void CompleteTask(ITelegramBotClient botClient, Chat chat, string taskIdStr)
    {
        if (!Guid.TryParse(taskIdStr, out Guid taskId))
        {
            botClient.SendMessage(chat,"Неверный формат Id. Пример: 73c7940a-ca8c-4327-8a15-9119bffd1d5e");
            return;
        }
        
        _toDoService.MarkCompleted(taskId);
        botClient.SendMessage(chat, $"Задача '{taskIdStr}' отмечена завершенной");
    }

    public static void Exit(ITelegramBotClient botClient, Chat chat)
    {
        botClient.SendMessage(chat,string.IsNullOrEmpty(name)
            ? "До свидания!"
            : $"{currentUser.TelegramUserName}, до свидания!");
    }
    
    static void ShowCurrentMenu(ITelegramBotClient botClient, Chat chat)
    {
        botClient.SendMessage(chat,string.IsNullOrWhiteSpace(name)
            ? "Добро пожаловать в бота\nДоступные команды:"
            : $"{name}, доступные команды");

        if (string.IsNullOrWhiteSpace(name))
        {
            botClient.SendMessage(chat,"/start - начать работу");
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            botClient.SendMessage(chat,"/addtask - добавить задачу");
            botClient.SendMessage(chat,"/showtasks - показать список текущих задач");
            botClient.SendMessage(chat,"/showalltasks - показать список всех задач");
            botClient.SendMessage(chat,"/removetask - удалить задачу");
            botClient.SendMessage(chat,"/completetask - завершить задачу");
        }

        botClient.SendMessage(chat,"/help - справка");
        botClient.SendMessage(chat,"/info - информация о боте");
        botClient.SendMessage(chat,"/exit - выход");
        botClient.SendMessage(chat,"Введите команду: ");
    }
    
}