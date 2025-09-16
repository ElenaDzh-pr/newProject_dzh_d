namespace ProjectDz;

public class InMemoryToDoRepository:  IToDoRepository
{
    private readonly List<ToDoItem> _items = new List<ToDoItem>();
    
    public async IReadOnlyList<ToDoItem> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return _items.Where(item => item.User.UserId == userId).ToList().AsReadOnly();
    }

    public async IReadOnlyList<ToDoItem> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return _items.Where(item => 
            item.User.UserId == userId &&
            item.State == ToDoItem.ToDoItemState.Active)
            .ToList().AsReadOnly();
    }

    public async Task<ToDoItem?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return _items.FirstOrDefault(item => item.Id == id);
    }

    public async Task AddAsync(ToDoItem item, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        _items.Add(item);
    }

    public async Task UpdateAsync(Task<ToDoItem?> item, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        var existingItem = await GetAsync(item.Id, cancellationToken);
        if (existingItem != null)
        {
            existingItem.Name = item.Name;
            existingItem.State = item.State;
            existingItem.StateChangedAt = item.StateChangedAt;
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        var item = await GetAsync(id, cancellationToken);
        if (item != null)
        {
            _items.Remove(item);
        }
    }

    public async bool ExistsByNameAsync(Guid userId, string name, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return _items.Any(item => 
            item.User.UserId == userId && 
            string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<int> CountActiveAsync(Guid userId, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return _items.Count(item => 
            item.User.UserId == userId &&
            item.State == ToDoItem.ToDoItemState.Active);
    }

    public async Task<IReadOnlyList<ToDoItem>> FindAsync(Guid userId, Func<ToDoItem, bool> predicate, 
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return _items
            .Where(item => item.User.UserId == userId)
            .Where(predicate)
            .ToList()
            .AsReadOnly();
    }
}