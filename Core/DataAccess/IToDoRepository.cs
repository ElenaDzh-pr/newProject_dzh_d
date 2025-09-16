namespace ProjectDz;

public interface IToDoRepository
{
    IReadOnlyList<ToDoItem> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    //Возвращает ToDoItem для UserId со статусом Active
    IReadOnlyList<ToDoItem> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<ToDoItem?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(ToDoItem item, CancellationToken cancellationToken);
    Task UpdateAsync(Task<ToDoItem?> item, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
    //Проверяет есть ли задача с таким именем у пользователя
    bool ExistsByNameAsync(Guid userId, string name, CancellationToken cancellationToken);
    //Возвращает количество активных задач у пользователя
    bool CountActiveAsync(Guid userId, CancellationToken cancellationToken);
    Task<ToDoItem> FindAsync(Guid userUserId, Func<object, object> func);
}