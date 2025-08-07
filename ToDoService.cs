namespace ProjectDz;

public class ToDoService  : IToDoService
{
    private readonly Dictionary<Guid, List<ToDoItem>> _userTasks = new();
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
        foreach (var userTasks in _userTasks.Values)
        {
            var task = userTasks.FirstOrDefault(t => t.Id == id);
            if (task != null)
            {
                if (task.State == ToDoItem.ToDoItemState.Completed)
                    throw new InvalidOperationException("Задача уже завершена");
                
                task.State = ToDoItem.ToDoItemState.Completed;
                task.StateChangedAt = DateTime.UtcNow;
                return;
            }
        }
        throw new KeyNotFoundException("Задача с указанным ID не найдена");
    }

    public void Delete(Guid id)
    {
        foreach (var userTasks in _userTasks.Values)
        {
            var task = userTasks.FirstOrDefault(t => t.Id == id);
            if (task != null)
            {
                userTasks.Remove(task);
                return;
            }
        }
        throw new KeyNotFoundException("Задача с указанным ID не найдена");
    }
    
}