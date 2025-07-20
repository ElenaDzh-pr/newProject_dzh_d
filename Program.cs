namespace ProjectDz;

class Program
{
    static string? name = null;
    static ToDoUser? currentUser;
    static List<ToDoItem> tasks = new List<ToDoItem>();
    static int maxTaskLimit = 0;
    static int maxTaskLength = 0;
    static int maxLengthTask = 100;
    static int minLengthTask = 1;

    static void Main(string[] args)
    {
        while (true)
        {
            try
            {
                Console.WriteLine("Введите максимально допустимое количество задач (от 1 до 100):");
                var input = Console.ReadLine();
                ValidateString(input);
                maxTaskLimit = ParseAndValidateInt(input, minLengthTask, maxLengthTask);
                break;
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("Попробуйте еще раз.\n");
            }
        }

        while (true)
        {
            try
            {
                Console.WriteLine("Введите максимально допустимую длину задачи (от 1 до 100):");
                var input = Console.ReadLine();
                ValidateString(input);
                maxTaskLength = ParseAndValidateInt(input, minLengthTask, maxLengthTask);
                break;
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("Попробуйте еще раз.\n");
            }
        }
            
        while (true)
        {
            try
            {
                ShowCurrentMenu();
                var command = Console.ReadLine();
                switch (command)
                {
                    case "/start": RegisterUser(); break;
                    case "/help": Help(); break;
                    case "/info": Info(); break;
                    case "/echo": EchoMessage(); break;
                    case "/addtask": AddTask(); break;
                    case "/showtasks": ShowTasks(); break;
                    case "/showalltasks": ShowAllTasks(); break;
                    case "/removetask": RemoveTask(); break;
                    case "/completetask": CompleteTask(); break;
                    case "/exit": Exit(); return;
                    default: Console.WriteLine("Неизвестная команда"); break;
                }
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (TaskCountLimitException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (TaskLengthLimitException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (DuplicateTaskException ex)
            {
                Console.WriteLine(ex.Message);
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

    static void ShowCurrentMenu()
    {
        Console.WriteLine(string.IsNullOrWhiteSpace(name)
            ? "Добро пожаловать в бота\nДоступные команды:"
            : $"{name}, доступные команды");

        if (string.IsNullOrWhiteSpace(name))
        {
            Console.WriteLine("/start - начать работу");
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            Console.WriteLine("/echo - отправить текст");
            Console.WriteLine("/addtask - добавить задачу");
            Console.WriteLine("/showtasks - показать список текущих задач");
            Console.WriteLine("/showalltasks - показать список всех задач");
            Console.WriteLine("/removetask - удалить задачу");
            Console.WriteLine("/completetask - завершить задачу");
        }

        Console.WriteLine("/help - справка");
        Console.WriteLine("/info - информация о боте");
        Console.WriteLine("/exit - выход");
        Console.Write("Введите команду: ");
    }

    static void RegisterUser()
    {
        Console.WriteLine("Пожалуйста, введите ваше имя: ");
        name = Console.ReadLine()?.Trim();
        ValidateString(name);
        currentUser = new ToDoUser(name);
        
        Console.WriteLine($"Добро пожаловать, {currentUser.TelegramUserName}!");
        Console.WriteLine($"Ваш ID: {currentUser.UserId}");
        Console.WriteLine($"Дата регистрации: {currentUser.RegisteredAt}");
    }

    static void EchoMessage()
    {
        Console.WriteLine($"{currentUser.TelegramUserName}, введите текст после команды /echo:");

        while (true)
        {
            string? input = Console.ReadLine() ?? "";
            ValidateString(input);
            string[] words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length >= 2)
            {
                string result = string.Join(' ', words.Skip(1));
                Console.WriteLine($"{result}");
                return;
            }
            else
            {
                Console.WriteLine("Введите текст через команду /echo Ваш текст");
            }
        }
    }

    static void Help()
    {
        Console.WriteLine("Описание доступных команд:");
        Console.WriteLine("/start - начало работы с ботом, ввод имени");
        Console.WriteLine("/echo - повторяет введенное сообщение и отправляет его обратно");
        Console.WriteLine("/info - получить информацию о боте");
        Console.WriteLine("/addtask - добавить задачу в список задач");
        Console.WriteLine("/showtasks - показать список текущих задач");
        Console.WriteLine("/showalltasks - показать список всех задач");
        Console.WriteLine("/removetask - удалить задачу из текущего списка");
        Console.WriteLine("/completetask - отметить задачу как завершенную");
    }

    static void Info()
    {
        Console.WriteLine(string.IsNullOrWhiteSpace(name)
            ? "Версия бота 1.0, дата создания 25.05.2025"
            : $"{currentUser.TelegramUserName}, версия бота 1.0, дата создания 25.05.2025");
    }

    static void AddTask()
    {
        if (tasks.Count >= maxTaskLimit)
        {
            throw new TaskCountLimitException(maxTaskLimit);
        }

        Console.WriteLine($"{currentUser.TelegramUserName}, пожалуйста, введите описание задачи:");

        string? taskDescription = Console.ReadLine()?.Trim();
        ValidateString(taskDescription);
        
        if (tasks.Any(t => string.Equals(t.Name, taskDescription, StringComparison.OrdinalIgnoreCase)))
        {
            throw new DuplicateTaskException(taskDescription);
        }
        
        if (string.IsNullOrWhiteSpace(taskDescription))
        {
            Console.WriteLine($"{currentUser.TelegramUserName}, описание задачи не может быть пустым");
            return;
        }
        
        if (taskDescription.Length > maxTaskLength)
        {
            throw new TaskLengthLimitException(taskDescription.Length, maxTaskLength);
        }

        var newTask = new ToDoItem(currentUser, taskDescription);
        tasks.Add(newTask);

        Console.WriteLine($"{currentUser.TelegramUserName}, задача добавлена!");
    }

    static void ShowTasks()
    {
        if (tasks.Count == 0)
        {
            Console.WriteLine($"{currentUser.TelegramUserName}, список задач пуст");
            return;
        }
        else
        {
            Console.WriteLine("\nТекущий список задач:");
            for (int i = 0; i < tasks.Count; i++)
            {
                var task = tasks[i];
                if (task.State == ToDoItem.ToDoItemState.Active)
                {
                    Console.WriteLine($"{task.Name} - {task.CreatedAt.ToLocalTime()} - {task.Id}");
                }
            }
        }
    }

    static void ShowAllTasks()
    {
        if (tasks.Count == 0)
        {
            Console.WriteLine($"{currentUser.TelegramUserName}, список задач пуст");
            return;
        }
        else
        {
            Console.WriteLine("\nСписок всех задач:");
            for (int i = 0; i < tasks.Count; i++)
            {
                var task = tasks[i];
                if (task.State == ToDoItem.ToDoItemState.Active || task.State == ToDoItem.ToDoItemState.Completed)
                {
                    Console.WriteLine($"({task.State}) {task.Name} - {task.CreatedAt.ToLocalTime()} - {task.Id}");
                }
            }
        }
    }

    static void RemoveTask()
    {
        if (tasks.Count == 0)
        {
            Console.WriteLine($"{currentUser.TelegramUserName}, список задач пуст. Нечего удалять.");
            return;
        }

        ShowTasks();

        Console.WriteLine($"{currentUser.TelegramUserName}, введите номер задачи для удаления:");
        var input = Console.ReadLine();
        ValidateString(input);
        
        var i = !int.TryParse(input, out int taskNumber);

        if (taskNumber < 1 || taskNumber > tasks.Count)
        {
            Console.WriteLine($"{currentUser.TelegramUserName}, ошибка, введите корректный номер от 1 до {tasks.Count}!");
            return;
        }

        tasks.RemoveAt(taskNumber - 1);
        Console.WriteLine($"Задача удалена. Осталось задач: {tasks.Count}");
    }

    static void CompleteTask()
    {
        Console.WriteLine("Введите Id задачи в формате 73c7940a-ca8c-4327-8a15-9119bffd1d5e:");
        var input = Console.ReadLine()?.Trim();
        if (!Guid.TryParse(input, out Guid taskId))
        {
            Console.WriteLine("Неверный формат Id. Пример: 73c7940a-ca8c-4327-8a15-9119bffd1d5e");
            return;
        }
        
        var task = tasks.FirstOrDefault(t => t.Id == taskId)
                   ?? throw new KeyNotFoundException("Задача с указанным ID не найдена");

        if (task.State == ToDoItem.ToDoItemState.Completed)
            throw new InvalidOperationException("Эта задача уже завершена");
        
        task.State = ToDoItem.ToDoItemState.Completed;
        task.StateChangedAt = DateTime.UtcNow;
        
        Console.WriteLine($"Задача '{task.Id}' отмечена завершененной");
    }

    static void Exit()
    {
        Console.WriteLine(string.IsNullOrEmpty(name)
            ? "До свидания!"
            : $"{currentUser.TelegramUserName}, до свидания!");
    }
    
    public class TaskCountLimitException : Exception{
        public TaskCountLimitException() : base() { }
        public TaskCountLimitException(int maxTaskLimit) 
            : base($"Превышено максимальное количество задач: {maxTaskLimit}") { }
    }
    
    public class TaskLengthLimitException : Exception{
        public TaskLengthLimitException() : base() { }
        public TaskLengthLimitException(int taskLength, int maxTaskLength) 
            : base($"Длина задачи {taskLength} превышает максимально допустимое значение {maxTaskLength}") { }
    }
    
    public class DuplicateTaskException : Exception{
        public DuplicateTaskException() : base() { }
        public DuplicateTaskException(string task) 
            : base($"Задача {task} уже существует") { }
    }

    static int ParseAndValidateInt(string? str, int min, int max)
    {
        if (!int.TryParse(str, out int result) || result < min || result > max)
        {
            throw new ArgumentException($"Введите число в диапазоне от {min} до {max}");
        }
        return result;
    }

    static void ValidateString(string? str)
    {
        if (string.IsNullOrWhiteSpace(str) || str.Trim().Length == 0)
        {
            throw new ArgumentException("Строка не может быть null, пустой или состоять только из пробелов");
        }
    }

}