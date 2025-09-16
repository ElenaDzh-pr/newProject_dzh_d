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
        return _toDoRepository.GetAllByUserIdAsync(userId, CancellationToken.None);
    }

    public IReadOnlyList<ToDoItem> GetActiveByUserId(Guid userId)
    {
        return _toDoRepository.GetActiveByUserIdAsync(userId, CancellationToken.None);
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

        if (_toDoRepository.CountActiveAsync(user.UserId, CancellationToken.None) >= _maxTaskLimit)
        {
            throw new TaskCountLimitException(_maxTaskLimit);
        }
        if (_toDoRepository.ExistsByNameAsync(user.UserId, name, CancellationToken.None);
        {
            throw new DuplicateTaskException(name);
        }
        
        var newTask = new ToDoItem(user, name);
        _toDoRepository.AddAsync(newTask, CancellationToken.None);
        return newTask;
    }

    public void MarkCompleted(Guid id)
    {
        Task<ToDoItem?> task = _toDoRepository.GetAsync(id, CancellationToken.None);
        if (task == null)
        {
            throw new KeyNotFoundException("Задача с указанным ID не найдена");
        }
        
        if (task.Result.State == ToDoItem.ToDoItemState.Completed)
        {
            throw new InvalidOperationException("Задача уже завершена");
        }
        
        task.Result.State = ToDoItem.ToDoItemState.Completed;
        task.Result.StateChangedAt = DateTime.UtcNow;
        _toDoRepository.UpdateAsync(task, CancellationToken.None);
    }

    public void Delete(Guid id)
    {
        var task = _toDoRepository.GetAsync(id, CancellationToken.None);
        if (task == null)
        {
            throw new KeyNotFoundException("Задача с указанным ID не найдена");
        }
        
        _toDoRepository.DeleteAsync(id, CancellationToken.None);
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