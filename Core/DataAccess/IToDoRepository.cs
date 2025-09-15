namespace ProjectDz;

public interface IToDoRepository
{
    Task<IReadOnlyList<ToDoItem>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    //Возвращает ToDoItem для UserId со статусом Active
    Task<IReadOnlyList<ToDoItem>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<ToDoItem?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(ToDoItem item, CancellationToken cancellationToken);
    Task UpdateAsync(ToDoItem item, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
    //Проверяет есть ли задача с таким именем у пользователя
    Task<bool> ExistsByNameAsync(Guid userId, string name, CancellationToken cancellationToken);
    //Возвращает количество активных задач у пользователя
    Task<int> CountActiveAsync(Guid userId, CancellationToken cancellationToken);
}