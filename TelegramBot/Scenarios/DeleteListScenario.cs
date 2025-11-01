using ProjectDz.Dto;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace ProjectDz;

public class DeleteListScenario : IScenario
{
    private readonly IUserService _userService;
    private readonly IToDoListService _toDoListService;
    private readonly IToDoService _toDoService;
    
    public DeleteListScenario(IUserService userService, IToDoListService toDoListService, IToDoService toDoService)
    {
        _userService = userService;
        _toDoListService = toDoListService;
        _toDoService = toDoService;
    }
    
    public bool CanHandle(ScenarioContext.ScenarioType scenario)
    {
        return scenario == ScenarioContext.ScenarioType.DeleteList;
    }

    public async Task<ScenarioResult> HandleMessageAsync(ITelegramBotClient bot, ScenarioContext context, Update update, CancellationToken ct)
    {
        switch (context.CurrentStep)
        {
            case null:
                var user = await _userService.GetUserAsync(context.UserId, ct);
                context.Data["user"] = user;
                
                var lists = await _toDoListService.GetUserLists(user.UserId, ct);
                var keyboard = BuildListsKeyboard(lists);

                await bot.SendMessage(
                    chatId: update.Message.Chat.Id,
                    text: "Выберете список для удаления:",
                    replyMarkup: keyboard,
                    cancellationToken: ct);
                
                context.CurrentStep = "Approve";

                return ScenarioResult.Transition;

            case "Approve":
                var listCallbackDto = ToDoListCallbackDto.FromString(update.CallbackQuery.Data);
                var list = await _toDoListService.Get(listCallbackDto.ToDoListId.Value, ct);
                context.Data["list"] = list;

                var approveKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("✅Да", "yes"),
                        InlineKeyboardButton.WithCallbackData("❌Нет", "no")
                    }
                });

                await bot.SendMessage(
                    chatId: update.CallbackQuery.Message.Chat.Id,
                    text: $"Подтверждаете удаление списка {list.Name} и всех его задач",
                    replyMarkup: approveKeyboard,
                    cancellationToken: ct);
                
                context.CurrentStep = "Delete";

                return ScenarioResult.Transition;

            case "Delete":
                if (update.CallbackQuery.Data == "yes")
                {
                    var savedUser = (ToDoUser)context.Data["user"];
                    var savedList = (ToDoList)context.Data["list"];
                    
                    var tasks = await _toDoService.GetByUserIdAndList(savedUser.UserId, savedList.Id, ct);
                    foreach (var task in tasks)
                    {
                        await _toDoService.DeleteAsync(task.Id);
                    }
                    
                    await _toDoListService.Delete(savedList.Id, ct);
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
    
    private InlineKeyboardMarkup BuildListsKeyboard(IReadOnlyList<ToDoList> lists)
    {
        var buttons = new List<InlineKeyboardButton[]>();
        
        foreach (var list in lists)
        {
            var callbackData = new ToDoListCallbackDto 
            { 
                Action = "deletelist", 
                ToDoListId = list.Id 
            }.ToString();
            
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(list.Name, callbackData)
            });
        }
        
        return new InlineKeyboardMarkup(buttons);
    }
}