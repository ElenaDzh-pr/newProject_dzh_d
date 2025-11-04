using Telegram.Bot;
using Telegram.Bot.Types;

namespace ProjectDz;

public class AddListScenario: IScenario
{
    private readonly IUserService _userService;
    private readonly IToDoListService _toDoListService;
    
    public AddListScenario(IUserService userService,  IToDoListService toDoListService)
    {
        _userService = userService;
        _toDoListService = toDoListService;
    }
    public bool CanHandle(ScenarioContext.ScenarioType scenario)
    {
        return scenario == ScenarioContext.ScenarioType.AddList;
    }

    public async Task<ScenarioResult> HandleMessageAsync(ITelegramBotClient bot, ScenarioContext context, Update update, CancellationToken ct)
    {
        switch (context.CurrentStep)
        {
            case null:
                var user = await _userService.GetUserAsync(context.UserId, ct);
                context.Data["user"] = user;
                
                await bot.SendMessage(
                    chatId: update.Message.Chat.Id,
                    text: "Введите название списка:",
                    cancellationToken: ct);
                
                context.CurrentStep = "Name";
                
                return ScenarioResult.Transition;
            
            case "Name":
                var savedUser = (ToDoUser)context.Data["user"];
                var listName = update.Message.Text;
                
                await _toDoListService.Add(savedUser, listName, ct);
                
                return ScenarioResult.Completed;
            
            default:
                return ScenarioResult.Completed;
        }
    }
}