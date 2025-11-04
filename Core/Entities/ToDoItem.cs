namespace ProjectDz;

public class ToDoItem
{
    public enum ToDoItemState
    {
        Active,
        Completed
    }
    
    public Guid Id { get; set; }
    public ToDoUser User { get; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; }
    public ToDoItemState State { get; set; }
    public DateTime? StateChangedAt { get; set; }
    public DateTime Deadline { get; set; }
    public ToDoList? List { get; set; }

    public ToDoItem(ToDoUser user, string name, DateTime deadline, ToDoList? list)
    {
        User = user;
        Name = name;
        Id = Guid.NewGuid();
        CreatedAt = DateTime.Now;
        State = ToDoItemState.Active;
        StateChangedAt = null;
        Deadline = deadline;
        List = list;
    }
}