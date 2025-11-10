using ProjectDz.Dto;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace ProjectDz;

public class DeleteTaskScenario : IScenario
{
    private readonly IToDoService _toDoService;
    
    public DeleteTaskScenario(IToDoService toDoService)
    {
         _toDoService = toDoService; 
    }
    public bool CanHandle(ScenarioContext.ScenarioType scenario)
    {
        return scenario == ScenarioContext.ScenarioType.DeleteTask;
    }

    public async Task<ScenarioResult> HandleMessageAsync(ITelegramBotClient bot, ScenarioContext context, Update update, CancellationToken ct)
    {
        switch (context.CurrentStep)
        {
            case null:
                var taskCallbackDto = ToDoItemCallbackDto.FromString(update.CallbackQuery.Data);
                var task = await _toDoService.Get(taskCallbackDto.ToDoItemId, ct);
                context.Data["task"] = task;
                
                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("✅Да", "yes"),
                        InlineKeyboardButton.WithCallbackData("❌Нет", "no")
                    }
                });
                
                await bot.SendMessage(
                    chatId: update.CallbackQuery.Message.Chat.Id,
                    text: $"Подтверждаете удаление задачи \"{task.Name}\"?",
                    replyMarkup: keyboard,
                    cancellationToken: ct);
                
                context.CurrentStep = "Confirm";
                return ScenarioResult.Transition;
            
            case "Confirm":
                if (update.CallbackQuery.Data == "yes")
                {
                    var savedTask = (ToDoItem)context.Data["task"];
                    await _toDoService.DeleteAsync(savedTask.Id);
                    
                    await bot.SendMessage(
                        chatId: update.CallbackQuery.Message.Chat.Id,
                        text: "Задача удалена!",
                        cancellationToken: ct);
                }
                else if (update.CallbackQuery.Data == "no")
                {
                    await bot.SendMessage(
                        chatId: update.CallbackQuery.Message.Chat.Id,
                        text: "Удаление отменено",
                        cancellationToken: ct);
                }
                
                return ScenarioResult.Completed;
            
            default:
                return ScenarioResult.Completed;
        }
    }
}