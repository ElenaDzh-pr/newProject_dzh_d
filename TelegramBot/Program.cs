using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ProjectDz;

class Program
{
    static int maxTaskLimit = 0;
    static int maxTaskLength = 0;

    static async Task Main(string[] args)
    {
        int maxLimit;
        do {
            Console.WriteLine("Введите максимальное количество задач (1-100):");
        } while (!int.TryParse(Console.ReadLine(), out maxLimit) || maxLimit < 1 || maxLimit > 100);

        int maxLength;
        do {
            Console.WriteLine("Введите максимальную длину задачи (1-100):");
        } while (!int.TryParse(Console.ReadLine(), out maxLength) || maxLength < 1 || maxLength > 100);
        
        var userRepository = new FileUserRepository("Data/Users");
        var toDoRepository = new FileToDoRepository("Data/ToDoItems");
        var listRepository = new FileToDoListRepository("Data/ToDoLists");
        
        var userService = new UserService(userRepository);
        var toDoService = new ToDoService(toDoRepository, maxLimit, maxLength);
        var reportService = new ToDoReportService(toDoRepository);
        var listService = new ToDoListService(listRepository);
        
        var contextRepository = new InMemoryScenarioContextRepository();
        var scenarios = new List<IScenario>
        {
            new AddTaskScenario(userService, toDoService)
        };
        
        var token = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKE_EX12");
        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("Ошибка: TELEGRAM_BOT_TOKE_EX12 не установлен");
            return;
        }
        var botClient = new TelegramBotClient(token);
        
        var handler = new UpdateHandler(userService, toDoService, reportService, scenarios, contextRepository, listService);
        
        handler.OnHandleUpdateStarted += message => 
            Console.WriteLine($"Началась обработка сообщения '{message}'");
        handler.OnHandleUpdateCompleted += message => 
            Console.WriteLine($"Закончилась обработка сообщения '{message}'");
        
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = [UpdateType.Message],
            DropPendingUpdates = true
        };
        
        using var cts = new CancellationTokenSource();
        botClient.StartReceiving(handler, receiverOptions, cts.Token);

        var me = await botClient.GetMe();
        Console.WriteLine($"{me.FirstName} запущен!");
        
        await SetBotCommands(botClient, cts.Token);
        
        Console.WriteLine("Нажмите клавишу A для выхода");
        
        while (!cts.Token.IsCancellationRequested)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.A)
            {
                Console.WriteLine("\nЗавершение работы...");
                cts.Cancel();
                break;
            }
            else
            {
                Console.WriteLine($"\nБот: {me.FirstName} (@{me.Username})");
                Console.WriteLine("ID: " + me.Id);
                Console.WriteLine("Нажмите клавишу A для выхода");
            }
        }
        
        await Task.Delay(1000);
        Console.WriteLine("Бот остановлен.");
    }
    
    static async Task SetBotCommands(ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        var commands = new[]
        {
            new BotCommand { Command = "start", Description = "Начать работу с ботом" },
            new BotCommand { Command = "info", Description = "Получить информацию о боте" },
            new BotCommand { Command = "addtask", Description = "Добавить задачу в список задач" },
            new BotCommand { Command = "showtasks", Description = "Показать список текущих задач" },
            new BotCommand { Command = "showalltasks", Description = "Показать список всех задач" },
            new BotCommand { Command = "removetask", Description = "Удалить задачу из текущего списка" },
            new BotCommand { Command = "completetask", Description = "Отметить задачу как завершенную" },
            new BotCommand { Command = "report", Description = "Показать статистику по задачам" },
            new BotCommand { Command = "find", Description = "Найти задачу" },
            new BotCommand { Command = "exit", Description = "Выход" }
        };
        
        await botClient.SetMyCommands(commands, cancellationToken: cancellationToken);
    }

    public static int ParseAndValidateInt(string? str, int min, int max)
    {
        if (!int.TryParse(str, out int result) || result < min || result > max)
        {
            throw new ArgumentException($"Введите число в диапазоне от {min} до {max}");
        }
        return result;
    }

    public static void ValidateString(string? str)
    {
        if (string.IsNullOrWhiteSpace(str) || str.Trim().Length == 0)
        {
            throw new ArgumentException("Строка не может быть null, пустой или состоять только из пробелов");
        }
    }

}