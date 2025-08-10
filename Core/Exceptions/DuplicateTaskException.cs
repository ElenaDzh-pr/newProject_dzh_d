namespace ProjectDz;

public class DuplicateTaskException : Exception{
    public DuplicateTaskException() : base() { }
    public DuplicateTaskException(string task) 
        : base($"Задача {task} уже существует") { }
}