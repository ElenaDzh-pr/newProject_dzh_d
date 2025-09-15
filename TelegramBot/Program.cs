using Otus.ToDoList.ConsoleBot;
using Otus.ToDoList.ConsoleBot.Types;

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
        
        var userRepository = new InMemoryUserRepository();
        var toDoRepository = new InMemoryToDoRepository();
        
        var userService = new UserService(userRepository);
        var botClient = new ConsoleBotClient();
        var toDoService = new ToDoService(toDoRepository, maxLimit, maxLength);
        var reportService = new ToDoReportService(toDoRepository);
        var handler = new UpdateHandler(userService, toDoService, reportService);
        handler.OnHandleUpdateStarted += message => 
            Console.WriteLine($"Началась обработка сообщения '{message}'");
        handler.OnHandleUpdateCompleted += message => 
            Console.WriteLine($"Закончилась обработка сообщения '{message}'");
            
        //while (true)
        try
        {
            using var cts = new CancellationTokenSource();
            await botClient.StartReceiving(handler, cts.Token);
        
            Console.WriteLine("Бот запущен. Нажмите Enter для остановки...");
            Console.ReadLine();
        
            cts.Cancel();
        }
        finally
        {
            // отписка от событий
            handler.OnHandleUpdateStarted -= null;
            handler.OnHandleUpdateCompleted -= null;
        }
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