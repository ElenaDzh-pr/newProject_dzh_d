namespace ProjectDz;

public class InMemoryToDoRepository:  IToDoRepository
{
    private readonly List<ToDoItem> _items = new List<ToDoItem>();
    
    public IReadOnlyList<ToDoItem> GetAllByUserId(Guid userId)
    {
        return _items.Where(item => item.User.UserId == userId).ToList().AsReadOnly();
    }

    public IReadOnlyList<ToDoItem> GetActiveByUserId(Guid userId)
    {
        return _items.Where(item => 
            item.User.UserId == userId &&
            item.State == ToDoItem.ToDoItemState.Active)
            .ToList().AsReadOnly();
    }

    public ToDoItem? Get(Guid id)
    {
        return _items.FirstOrDefault(item => item.Id == id);
    }

    public void Add(ToDoItem item)
    {
        _items.Add(item);
    }

    public void Update(ToDoItem item)
    {
        var existingItem = Get(item.Id);
        if (existingItem != null)
        {
            existingItem.Name = item.Name;
            existingItem.State = item.State;
            existingItem.StateChangedAt = item.StateChangedAt;
        }
    }

    public void Delete(Guid id)
    {
        var item = Get(id);
        if (item != null)
        {
            _items.Remove(item);
        }
    }

    public bool ExistsByName(Guid userId, string name)
    {
        return _items.Any(item => 
            item.User.UserId == userId && 
            string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public int CountActive(Guid userId)
    {
        return _items.Count(item => 
            item.User.UserId == userId &&
            item.State == ToDoItem.ToDoItemState.Active);
    }

    public IReadOnlyList<ToDoItem> Find(Guid userId, Func<ToDoItem, bool> predicate)
    {
        return _items
            .Where(item => item.User.UserId == userId)
            .Where(predicate)
            .ToList()
            .AsReadOnly();
    }
}