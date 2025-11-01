namespace ProjectDz;

public class ScenarioContext
{
    public enum ScenarioType
    {
        None,
        AddTask,
        AddList,
        DeleteList
    }
    
    public long UserId { get; set; }
    public ScenarioType CurrentScenario { get; set; }
    public string? CurrentStep { get; set; }
    public Dictionary<string, object> Data  { get; set; }  = new Dictionary<string, object>();

    public ScenarioContext(long userId, ScenarioType scenario)
    {
        UserId = userId;
        CurrentScenario = scenario;
        Data = new Dictionary<string, object>();
    }
}