namespace ProjectDz;

public class ToDoListService : IToDoListService
{
    private readonly IToDoListRepository _listRepository;
    private const int MaxListNameLength = 10;

    public ToDoListService(IToDoListRepository listRepository)
    {
        _listRepository = listRepository;
    }

    public async Task<ToDoList> Add(ToDoUser user, string name, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Название списка не может быть пустым");
        }

        if (name.Length > MaxListNameLength)
        {
            throw new ArgumentException($"Название списка не может быть больше {MaxListNameLength} символов. Текущая длина: {name.Length}");
        }
        
        var exists = await _listRepository.ExistsByName(user.UserId, name, ct);
        if (exists)
        {
            throw new InvalidOperationException($"Список с названием '{name}' уже существует");
        }
        
        var list = new ToDoList(name, user);
        await _listRepository.Add(list, ct);
        
        return list;
    }

    public async Task<ToDoList?> Get(Guid id, CancellationToken ct)
    {
        return await _listRepository.Get(id, ct);
    }

    public async Task Delete(Guid id, CancellationToken ct)
    {
        await _listRepository.Delete(id, ct);
    }

    public async Task<IReadOnlyList<ToDoList>> GetUserLists(Guid userId, CancellationToken ct)
    {
        return await _listRepository.GetByUserId(userId, ct);
    }
}