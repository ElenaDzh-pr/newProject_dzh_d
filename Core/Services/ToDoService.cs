namespace ProjectDz;

public class ToDoService : IToDoService
{
    private readonly IToDoRepository _toDoRepository;
    private readonly int _maxTaskLimit;
    private readonly int _maxTaskLength;

    public ToDoService(IToDoRepository toDoRepository, int maxTaskLimit, int maxTaskLength)
    {
        _maxTaskLimit = maxTaskLimit;
        _maxTaskLength = maxTaskLength;
        _toDoRepository = toDoRepository;
    }

    public async Task<IReadOnlyList<ToDoItem>> GetAllByUserIdAsync(Guid userId)
    {
        return await _toDoRepository.GetAllByUserIdAsync(userId, CancellationToken.None);
    }

    public async Task<IReadOnlyList<ToDoItem>> GetActiveByUserIdAsync(Guid userId)
    {
        return await _toDoRepository.GetActiveByUserIdAsync(userId, CancellationToken.None);
    }

    public async Task<ToDoItem> AddAsync(ToDoUser user, string name, DateTime deadline)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Описание задачи не может быть пустым");
        }

        if (name.Length > _maxTaskLength)
        {
            throw new TaskLengthLimitException(name.Length, _maxTaskLength);
        }
        var activeCount = await _toDoRepository.CountActiveAsync(user.UserId, CancellationToken.None);

        if (activeCount >= _maxTaskLimit)
        {
            throw new TaskCountLimitException(_maxTaskLimit);
        }

        var exists = await _toDoRepository.ExistsByNameAsync(user.UserId, name, CancellationToken.None);
        if (exists)
        {
            throw new DuplicateTaskException(name);
        }

        var newTask = new ToDoItem(user, name, deadline);
        await _toDoRepository.AddAsync(newTask, CancellationToken.None);
        return newTask;
    }

    public async Task MarkCompletedAsync(Guid id)
    {
        var task = await _toDoRepository.GetAsync(id, CancellationToken.None);
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
        await _toDoRepository.UpdateAsync(task, CancellationToken.None);
    }

    public async Task DeleteAsync(Guid id)
    {
        var task = _toDoRepository.GetAsync(id, CancellationToken.None);
        if (task == null)
        {
            throw new KeyNotFoundException("Задача с указанным ID не найдена");
        }

        _toDoRepository.DeleteAsync(id, CancellationToken.None);
    }

    public async Task<IReadOnlyList<ToDoItem>> FindAsync(ToDoUser user, string namePrefix)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        if (string.IsNullOrWhiteSpace(namePrefix))
            return new List<ToDoItem>().AsReadOnly();

        return await _toDoRepository.FindAsync(user.UserId, item =>
            item.Name.StartsWith(namePrefix, StringComparison.OrdinalIgnoreCase), CancellationToken.None);
    }

}
