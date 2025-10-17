namespace ProjectDz;

public class InMemoryScenarioContextRepository:  IScenarioContextRepository
{
    private readonly Dictionary<long, ScenarioContext> _contexts = new Dictionary<long, ScenarioContext>();
    
    public Task<ScenarioContext?> GetContext(long userId, CancellationToken ct)
    {
        _contexts.TryGetValue(userId, out var context);
        return Task.FromResult<ScenarioContext?>(context);
    }

    public Task SetContext(long userId, ScenarioContext context, CancellationToken ct)
    {
        _contexts[userId] = context;
        return Task.CompletedTask;
    }

    public Task ResetContext(long userId, CancellationToken ct)
    {
        _contexts.Remove(userId);
        return Task.CompletedTask;
    }
}