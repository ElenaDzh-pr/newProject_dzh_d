namespace ProjectDz;

public class ToDoReportService: IToDoReportService
{
    private readonly IToDoRepository _toDoRepository;

    public ToDoReportService(IToDoRepository toDoRepository)
    {
        _toDoRepository = toDoRepository ?? throw new ArgumentNullException(nameof(toDoRepository));
    }

    public async Task<(int total, int completed, int active, DateTime generatedAt)> GetUserStatsAsync(Guid userId)
    {
        var allTasks = await _toDoRepository.GetAllByUserIdAsync(userId, CancellationToken.None);
        var total = allTasks.Count;
        var completed = allTasks.Count(t => t.State == ToDoItem.ToDoItemState.Completed);
        var active = allTasks.Count(t => t.State == ToDoItem.ToDoItemState.Active);
        var generatedAt = DateTime.Now;

        return (total, completed, active, generatedAt);
    }
}