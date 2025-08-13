namespace ProjectDz;

public class ToDoService  : IToDoService
{
    private readonly IToDoRepository _toDoRepository;
    private readonly List<ToDoItem> _items = new List<ToDoItem>();
    private readonly int _maxTaskLimit;
    private readonly int _maxTaskLength;
    
    public ToDoService(IToDoRepository toDoRepository, int maxTaskLimit, int maxTaskLength)
    {
        _maxTaskLimit = maxTaskLimit;
        _maxTaskLength = maxTaskLength;
        _toDoRepository = toDoRepository;
    }
    
    public IReadOnlyList<ToDoItem> GetAllByUserId(Guid userId)
    {
        return _toDoRepository.GetAllByUserId(userId);
    }

    public IReadOnlyList<ToDoItem> GetActiveByUserId(Guid userId)
    {
        return _toDoRepository.GetActiveByUserId(userId);
    }

    public ToDoItem Add(ToDoUser user, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Описание задачи не может быть пустым");
        }
        if (name.Length > _maxTaskLength)
        {
            throw new TaskLengthLimitException(name.Length, _maxTaskLength);
        }
        if (_toDoRepository.CountActive(user.UserId) >= _maxTaskLimit)
        {
            throw new TaskCountLimitException(_maxTaskLimit);
        }
        if (_toDoRepository.ExistsByName(user.UserId, name))
        {
            throw new DuplicateTaskException(name);
        }
        
        var newTask = new ToDoItem(user, name);
        _toDoRepository.Add(newTask);
        return newTask;
    }

    public void MarkCompleted(Guid id)
    {
        var task = _toDoRepository.Get(id);
        if (task == null)
        {
            throw new KeyNotFoundException("Задача с указанным ID не найдена");
        }
        
        if (task.State == ToDoItem.ToDoItemState.Completed)
        {
            throw new InvalidOperationException("Задача уже завершена");
        }
        
        task.State = ToDoItem.ToDoItemState.Completed;
        task.StateChangedAt = DateTime.UtcNow;
        _toDoRepository.Update(task);
    }

    public void Delete(Guid id)
    {
        var task = _toDoRepository.Get(id);
        if (task == null)
        {
            throw new KeyNotFoundException("Задача с указанным ID не найдена");
        }
        
        _toDoRepository.Delete(id);
    }

    public IReadOnlyList<ToDoItem> Find(ToDoUser user, string namePrefix)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));
    
        if (string.IsNullOrWhiteSpace(namePrefix))
            return new List<ToDoItem>().AsReadOnly();

        return _toDoRepository.Find(user.UserId, item => 
            item.Name.StartsWith(namePrefix, StringComparison.OrdinalIgnoreCase));
    }
}