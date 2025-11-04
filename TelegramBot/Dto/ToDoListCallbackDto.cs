namespace ProjectDz.Dto;

public class ToDoListCallbackDto : CallbackDto
{
    public Guid? ToDoListId { get; set; }

    public static new ToDoListCallbackDto FromString(string input)
    {
        if (string.IsNullOrEmpty(input))
            return new ToDoListCallbackDto { Action = string.Empty };

        var parts = input.Split('|');
            
        var dto = new ToDoListCallbackDto
        {
            Action = parts.Length > 0 ? parts[0] : string.Empty
        };
        
        if (parts.Length > 1 && !string.IsNullOrEmpty(parts[1]) && 
            Guid.TryParse(parts[1], out Guid listId))
        {
            dto.ToDoListId = listId;
        }

        return dto;
    }

    public override string ToString()
    {
        if (ToDoListId == null)
            return $"{base.ToString()}|";
        
        return $"{base.ToString()}|{ToDoListId}";
    }
}