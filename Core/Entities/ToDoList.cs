namespace ProjectDz;

public class ToDoList
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public ToDoUser User { get; }
    public DateTime CreatedAt { get; }
    
    public ToDoList(string name, ToDoUser user)
    {
        Id = Guid.NewGuid();
        Name = name;
        User = user;
        CreatedAt = DateTime.Now;
    }
}