namespace ProjectDz.Dto;

public class PagedListCallbackDto : ToDoListCallbackDto
{
    public int Page { get; set; }

    public static new PagedListCallbackDto FromString(string input)
    {
        if (string.IsNullOrEmpty(input))
            return new PagedListCallbackDto { Action = string.Empty };

        var parts = input.Split('|');
            
        var dto = new PagedListCallbackDto
        {
            Action = parts.Length > 0 ? parts[0] : string.Empty
        };
        
        if (parts.Length > 1 && !string.IsNullOrEmpty(parts[1]) && 
            Guid.TryParse(parts[1], out Guid listId))
        {
            dto.ToDoListId = listId;
        }
        
        if (parts.Length > 2 && int.TryParse(parts[2], out int page))
        {
            dto.Page = page;
        }

        return dto;
    }

    public override string ToString()
    {
        return $"{base.ToString()}|{Page}";
    }
}