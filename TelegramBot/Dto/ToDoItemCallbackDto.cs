namespace ProjectDz.Dto;

public class ToDoItemCallbackDto : CallbackDto
{
    public Guid ToDoItemId { get; set; }
    
    public static new ToDoItemCallbackDto FromString(string input)
    {
        if (string.IsNullOrEmpty(input))
            return new ToDoItemCallbackDto { Action = string.Empty };

        var parts = input.Split('|');
            
        var dto = new ToDoItemCallbackDto
        {
            Action = parts.Length > 0 ? parts[0] : string.Empty
        };
        
        if (parts.Length > 1 && !string.IsNullOrEmpty(parts[1]) && 
            Guid.TryParse(parts[1], out Guid itemId))
        {
            dto.ToDoItemId =  itemId;
        }

        return dto;
    }
    
    public override string ToString()
    {
        return $"{base.ToString()}|{ToDoItemId}";
    }
}