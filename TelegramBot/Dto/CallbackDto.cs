namespace ProjectDz.Dto;

public class CallbackDto
{
    public string Action { get; set; } = string.Empty;

    public static CallbackDto FromString(string input)
    {
        if (string.IsNullOrEmpty(input))
            return new CallbackDto { Action = string.Empty };
        
        if (!input.Contains('|'))
            return new CallbackDto { Action = input };
        
        var parts = input.Split('|', 2);
        return new CallbackDto { Action = parts[0] };
    }

    public override string ToString()
    {
        return Action;
    }
}