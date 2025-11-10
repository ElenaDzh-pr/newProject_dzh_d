using ProjectDz.Dto;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace ProjectDz;

public class AddTaskScenario : IScenario
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

    public async Task<ScenarioResult> HandleMessageAsync(ITelegramBotClient bot, ScenarioContext context, Update update, CancellationToken ct)
    {
        if (update.CallbackQuery != null)
        {
            await bot.AnswerCallbackQuery(update.CallbackQuery.Id, cancellationToken: ct);
            
            var callbackDto = ToDoListCallbackDto.FromString(update.CallbackQuery.Data);
            if (callbackDto.Action == "select")
            {
                context.Data["selectedListId"] = callbackDto.ToDoListId;
                context.CurrentStep = "Deadline";
                
                await bot.SendMessage(
                    chatId: update.CallbackQuery.Message.Chat.Id,
                    text: "Введите дедлайн в формате dd.MM.yyyy",
                    cancellationToken: ct);
                
                return ScenarioResult.Transition;
            }
            return ScenarioResult.Transition;
        }

        switch (context.CurrentStep)
        {
            case null:
                var user = await _userService.GetUserAsync(context.UserId, ct);
                context.Data["user"] = user;

                await bot.SendMessage(
                    chatId: update.Message.Chat.Id,
                    text: "Введите название задачи:",
                    cancellationToken: ct);

                context.CurrentStep = "Name";
                return ScenarioResult.Transition;

            case "Name":
                context.Data["taskName"] = update.Message.Text;
                
                var keyboard = BuildListsKeyboard();

                await bot.SendMessage(
                    chatId: update.Message.Chat.Id,
                    text: "Выберите список:",
                    replyMarkup: keyboard,
                    cancellationToken: ct);

                context.CurrentStep = "SelectList";
                return ScenarioResult.Transition;

            case "Deadline":
                var deadlineInput = update.Message.Text;

                if (!DateTime.TryParseExact(deadlineInput, "dd.MM.yyyy",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out DateTime deadline))
                {
                    await bot.SendMessage(
                        chatId: update.Message.Chat.Id,
                        text: "Неверный формат даты. Введите дату в формате dd.MM.yyyy",
                        cancellationToken: ct);
                    return ScenarioResult.Transition;
                }

                var savedUser = (ToDoUser)context.Data["user"];
                var taskName = (string)context.Data["taskName"];
                var selectedListId = context.Data.ContainsKey("selectedListId")
                    ? (Guid?)context.Data["selectedListId"]
                    : null;

                await _toDoService.AddAsync(savedUser, taskName, deadline,
                    null);

                await bot.SendMessage(
                    chatId: update.Message.Chat.Id,
                    text: $"Задача \"{taskName}\" добавлена!",
                    cancellationToken: ct);

                return ScenarioResult.Completed;

            default:
                return ScenarioResult.Completed;
        }
    }
    
    private InlineKeyboardMarkup BuildListsKeyboard()
    {
        var buttons = new List<InlineKeyboardButton[]>();
        
        var noListCallback = new PagedListCallbackDto 
        { 
            Action = "show", 
            ToDoListId = null,
            Page = 0
        }.ToString();
        
        buttons.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData("📌 Без списка", noListCallback)
        });
        
        var completedCallback = new PagedListCallbackDto
        { 
            Action = "show_completed", 
            ToDoListId = null,
            Page = 0
        }.ToString();
    
        buttons.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData("☑️Посмотреть выполненные", completedCallback)
        });
    
        buttons.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData("🆕 Добавить", "addlist"),
            InlineKeyboardButton.WithCallbackData("❌ Удалить", "deletelist")
        });

        return new InlineKeyboardMarkup(buttons);
    }
}