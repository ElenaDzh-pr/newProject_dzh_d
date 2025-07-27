namespace ProjectDz;

public class ToDoService  : IToDoService
{
    private readonly Dictionary<Guid, List<ToDoItem>> _userTasks = new();
    static List<ToDoItem> tasks = new List<ToDoItem>();
    private int _maxTaskLimit;
    private int _maxTaskLength;
    
    public ToDoService(int maxTaskLimit, int maxTaskLength)
    {
        _maxTaskLimit = maxTaskLimit;
        _maxTaskLength = maxTaskLength;
    }
    
    public IReadOnlyList<ToDoItem> GetAllByUserId(Guid userId)
    {
        return _userTasks.TryGetValue(userId, out var tasks) 
            ? tasks.AsReadOnly() 
            : new List<ToDoItem>().AsReadOnly();
    }

    public IReadOnlyList<ToDoItem> GetActiveByUserId(Guid userId)
    {
        return GetAllByUserId(userId)
            .Where(t => t.State == ToDoItem.ToDoItemState.Active)
            .ToList()
            .AsReadOnly();
    }

    public ToDoItem Add(ToDoUser user, string name)
    {
        //var tasks = _userTasks[user.UserId];
        
        if (!_userTasks.TryGetValue(user.UserId, out var tasks))
        {
            tasks = new List<ToDoItem>();
            _userTasks[user.UserId] = tasks;
        }
        
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Описание задачи не может быть пустым");
        }
        if (name.Length > _maxTaskLength)
        {
            throw new TaskLengthLimitException(name.Length, _maxTaskLength);
        }
        if (tasks.Count >= _maxTaskLimit)
        {
            throw new TaskCountLimitException(_maxTaskLimit);
        }
        if (tasks.Any(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new DuplicateTaskException(name);
        }
        
        
        
        var newTask = new ToDoItem(user, name);
        tasks.Add(newTask);
        return newTask;
    }

    public void MarkCompleted(Guid id)
    {
        var task = tasks.FirstOrDefault(t => t.Id == id)
                   ?? throw new KeyNotFoundException("Задача с указанным ID не найдена");
        if (task.State == ToDoItem.ToDoItemState.Completed)
        {
            throw new InvalidOperationException("Задача уже завершена");
        }
        
        task.State = ToDoItem.ToDoItemState.Completed;
        task.StateChangedAt = DateTime.UtcNow;
    }

    public void Delete(Guid id)
    {
        var task = tasks.FirstOrDefault(t => t.Id == id)
                   ?? throw new KeyNotFoundException("Задача с указанным ID не найдена");
        
        tasks.Remove(task);
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
}