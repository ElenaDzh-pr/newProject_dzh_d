namespace ProjectDz;

public class TaskLengthLimitException : Exception{
    public TaskLengthLimitException() : base() { }
    public TaskLengthLimitException(int taskLength, int maxTaskLength) 
        : base($"Длина задачи {taskLength} превышает максимально допустимое значение {maxTaskLength}") { }
}