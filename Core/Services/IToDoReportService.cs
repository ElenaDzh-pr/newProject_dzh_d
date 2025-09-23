namespace ProjectDz;

public interface IToDoReportService
{ 
    Task<(int total, int completed, int active, DateTime generatedAt)> GetUserStatsAsync(Guid userId);
}