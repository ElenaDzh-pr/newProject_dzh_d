namespace ProjectDz;

public class TaskCountLimitException : Exception{
    public TaskCountLimitException() : base() { }
    public TaskCountLimitException(int maxTaskLimit) 
        : base($"Превышено максимальное количество задач: {maxTaskLimit}") { }
}