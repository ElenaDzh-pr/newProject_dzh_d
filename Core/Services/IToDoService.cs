namespace ProjectDz;

public interface IToDoService
{
    Task<IReadOnlyList<ToDoItem>> GetAllByUserIdAsync(Guid userId);
    //Возвращает ToDoItem для UserId со статусом Active
    Task<IReadOnlyList<ToDoItem>> GetActiveByUserIdAsync(Guid userId);
    Task<ToDoItem> AddAsync(ToDoUser user, string name, DateTime deadline, ToDoList? list);
    Task MarkCompletedAsync(Guid id);
    Task DeleteAsync(Guid id);
    Task<IReadOnlyList<ToDoItem>> FindAsync(ToDoUser user, string namePrefix);
    Task<IReadOnlyList<ToDoItem>> GetByUserIdAndList(Guid userId, Guid? listId, CancellationToken ct);
    
    Task<ToDoItem?> Get(Guid toDoItemId, CancellationToken ct);
}