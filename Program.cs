using Otus.ToDoList.ConsoleBot;
using Otus.ToDoList.ConsoleBot.Types;

namespace ProjectDz;

class Program
{
    static int maxTaskLimit = 0;
    static int maxTaskLength = 0;

    static void Main(string[] args)
    {
        int maxLimit;
        do {
            Console.WriteLine("Введите максимальное количество задач (1-100):");
        } while (!int.TryParse(Console.ReadLine(), out maxLimit) || maxLimit < 1 || maxLimit > 100);

        int maxLength;
        do {
            Console.WriteLine("Введите максимальную длину задачи (1-100):");
        } while (!int.TryParse(Console.ReadLine(), out maxLength) || maxLength < 1 || maxLength > 100);
        
        var userService = new UserService();
        var botClient = new ConsoleBotClient();
        var toDoService = new ToDoService(maxLimit, maxLength);
        var handler = new UpdateHandler(userService, toDoService);
        botClient.StartReceiving(handler);
            
        while (true)
        {
            try
            {
                var command = Console.ReadLine();
            }
            
            catch (Exception ex)
            {
                Console.WriteLine("Произошла непредвиденная ошибка:");
                Console.WriteLine($"Тип: {ex.GetType()}");
                Console.WriteLine($"Сообщение: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                Console.WriteLine(ex.InnerException != null 
                    ? $"InnerException: {ex.InnerException.GetType()}: {ex.InnerException.Message}" 
                    : "InnerException: null");
            }
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