using Otus.ToDoList.ConsoleBot;
using Otus.ToDoList.ConsoleBot.Types;

namespace ProjectDz;

class Program
{
    //static string? name = null;
    //static ToDoUser? currentUser;
    //static List<ToDoItem> tasks = new List<ToDoItem>();
    static int maxTaskLimit = 0;
    static int maxTaskLength = 0;
    //static int maxLengthTask = 100;
    //static int minLengthTask = 1;

    static void Main(string[] args)
    {
        var userService = new UserService();
        var toDoService = new ToDoService(maxTaskLimit, maxTaskLength);
        var handler = new UpdateHandler(userService, toDoService);
        var botClient = new ConsoleBotClient();
        botClient.StartReceiving(handler);
            
        while (true)
        {
            try
            {
                var command = Console.ReadLine();
                var update = new Update
                {
                    Message = new Message
                    {
                        Text = command,
                        Chat = new Chat { Id = 1 }
                    }
                };
                handler.HandleUpdateAsync(botClient, update);
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