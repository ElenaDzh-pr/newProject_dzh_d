using System.Globalization;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace ProjectDz;

public class AddTaskScenario: IScenario
{
    private readonly IUserService _userService;
    private readonly IToDoService _toDoService;
    public AddTaskScenario(IUserService userService, IToDoService toDoService)
    {
        _userService = userService;
        _toDoService = toDoService;
    }
    public bool CanHandle(ScenarioContext.ScenarioType scenario)
    {
        return scenario == ScenarioContext.ScenarioType.AddTask;
    }

    private ReplyKeyboardMarkup GetCancelKeyboard()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton[] { "/cancel" }
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = true
        };
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
                    text: "Введите название задачи:",
                    replyMarkup: GetCancelKeyboard(),
                    cancellationToken: ct);
                
                context.CurrentStep = "Name";
                
                return ScenarioResult.Transition;
            
            case "Name":
                context.Data["taskName"] = update.Message.Text;
                
                await bot.SendMessage(
                    chatId: update.Message.Chat.Id,
                    text: "Введите дедлайн в формате dd.MM.yyyy",
                    replyMarkup: GetCancelKeyboard(),
                    cancellationToken: ct);
                
                context.CurrentStep = "Deadline";
                
                return ScenarioResult.Transition;
            
            case "Deadline":
                var deadlineInput = update.Message.Text;
                
                if (!DateTime.TryParseExact(deadlineInput, "dd.MM.yyyy", 
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime deadline))
                {
                    await bot.SendMessage(
                        chatId: update.Message.Chat.Id,
                        text: "Неверный формат даты. Введите дату в формате dd.MM.yyyy (например, 12.12.2025):",
                        replyMarkup: GetCancelKeyboard(),
                        cancellationToken: ct);
                    
                    return ScenarioResult.Transition;
                }

                var savedUser = (ToDoUser)context.Data["user"];
                var taskName = (string)context.Data["taskName"];
                
                var newTask = await _toDoService.AddAsync(savedUser, taskName, deadline, null);
                
                await bot.SendMessage(
                    chatId: update.Message.Chat.Id,
                    text: $"Задача \"{taskName}\" добавлена! Дедлайн: {deadline:dd.MM.yyyy}",
                    cancellationToken: ct);
                
                return ScenarioResult.Completed;

            default:
                return ScenarioResult.Completed;
        }
    }
}