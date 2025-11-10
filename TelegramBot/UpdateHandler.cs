using Telegram.Bot;
using Telegram.Bot.Types;

using ProjectDz;
using ProjectDz.Dto;
using ProjectDz.Helpers;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

public delegate void MessageEventHandler(string message);

public class UpdateHandler : IUpdateHandler
{
    private readonly IUserService _userService;
    private readonly IToDoService _toDoService;
    private readonly IToDoReportService _toDoReportService;
    private readonly Dictionary<long, bool> _waitingForTaskDescription = new();
    private readonly IEnumerable<IScenario> _scenarios;
    private readonly IScenarioContextRepository _contextRepository;
    private readonly IToDoListService _toDoListService;
    public event MessageEventHandler OnHandleUpdateStarted;
    public event MessageEventHandler OnHandleUpdateCompleted;
    private static int _pageSize = 5;
   
    public UpdateHandler(
        IUserService userService, 
        IToDoService toDoService, 
        IToDoReportService toDoReportService,
        IEnumerable<IScenario> scenarios,
        IScenarioContextRepository contextRepository,
        IToDoListService toDoListService)
    {
        _userService = userService;
        _toDoService = toDoService;
        _toDoReportService = toDoReportService;
        _scenarios = scenarios;
        _contextRepository = contextRepository;
        _toDoListService = toDoListService;
    }
    
    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, 
        CancellationToken cancellationToken)
    {
        OnHandleUpdateStarted?.Invoke(update.Message?.Text ?? "Unknown message");
        
        try
        {
            if (update.CallbackQuery != null)
            {
                await OnCallbackQuery(botClient, update.CallbackQuery, cancellationToken);
                return;
            }
            
            if (update.Message.Text?.ToLower() == "/cancel")
            {
                var context = await _contextRepository.GetContext(update.Message.Chat.Id, cancellationToken);
                if (context != null)
                {
                    await _contextRepository.ResetContext(update.Message.Chat.Id, cancellationToken);
                    await botClient.SendMessage(
                        chatId: update.Message.Chat.Id,
                        text: "Сценарий отменен",
                        replyMarkup: GetMainKeyboard(),
                        cancellationToken: cancellationToken);
                }
                else
                {
                    await botClient.SendMessage(
                        chatId: update.Message.Chat.Id,
                        text: "Нет активного сценария для отмены",
                        replyMarkup: GetMainKeyboard(),
                        cancellationToken: cancellationToken);
                }
                return;
            }
            var existingContext = await _contextRepository.GetContext(update.Message.Chat.Id, cancellationToken);
            if (existingContext != null)
            {
                await ProcessScenario(botClient, existingContext, update, cancellationToken);
                return;
            }
            
            var currentUser = update.Message?.From != null 
                ? await Task.Run(() => _userService.GetUserAsync(update.Message.From.Id, cancellationToken))
                : null;
            
            if (update.Message.Text?.ToLower() == "/start")
            {
                await RegisterUserAsync(botClient, update.Message.Chat, update, cancellationToken);
                return;
            }
            
            await ShowCurrentMenuAsync(botClient, update.Message.Chat, currentUser, cancellationToken);
            
            if (currentUser == null)
            {
                await botClient.SendMessage(update.Message.Chat.Id, 
                    "Сначала зарегистрируйтесь через /start\n" +
                    "Доступные команды: /help, /info", cancellationToken: cancellationToken);
                return;
            }
            
            if (_waitingForTaskDescription.ContainsKey(update.Message.Chat.Id))
            {
                var taskName = update.Message.Text;
                await AddTaskAsync(botClient, update.Message.Chat, taskName, currentUser, cancellationToken);
                _waitingForTaskDescription.Remove(update.Message.Chat.Id);
                return;
            }
            
            else if (update.Message.Text?.ToLower() == "/show")
            {
                await ShowListsSelectionAsync(botClient, update.Message.Chat, update.Message.From.Id, cancellationToken);
                return;
            }
            
            if (update.Message.Text?.ToLower().StartsWith("/addtask") == true)
            {
                var scenarioContext = new ScenarioContext(update.Message.Chat.Id, ScenarioContext.ScenarioType.AddTask)
                {
                    UserId = update.Message.Chat.Id
                };
                
                await _contextRepository.SetContext(scenarioContext.UserId, scenarioContext, cancellationToken);
                
                await ProcessScenario(botClient, scenarioContext, update, cancellationToken);
                return;
            }
            else if (update.Message.Text?.ToLower().StartsWith("/find") == true)
            {
                var taskName = update.Message.Text.Substring("/find".Length).Trim();
                await FindTaskAsync(botClient, update.Message.Chat, currentUser, taskName,  cancellationToken);
            }
            else
            {
                switch (update.Message.Text.ToLower())
                {
                    case "/start":
                        await RegisterUserAsync(botClient, update.Message.Chat, update, cancellationToken);
                        break;
                    case "/help":
                        await HelpAsync(botClient, update.Message.Chat, cancellationToken);
                        break;
                    case "/info":
                        await InfoAsync(botClient, update.Message.Chat, currentUser, cancellationToken);
                        break;
                    case "/show":
                        await ShowTasksAsync(botClient, update.Message.Chat, currentUser, cancellationToken);
                        break;
                    case "/report":
                        await ShowReportAsync(botClient, update.Message.Chat, currentUser, cancellationToken);
                        break;
                    case "/exit":
                        await ExitAsync(botClient, update.Message.Chat, currentUser, cancellationToken);
                        return;
                    default: await botClient.SendMessage(update.Message.Chat.Id, "Неизвестная команда", 
                        cancellationToken: cancellationToken); break;
                }
            }
            await ShowCurrentMenuAsync(botClient, update.Message.Chat, currentUser, cancellationToken);
        }
        
        catch (ArgumentException ex)
        {
            await botClient.SendMessage(update.Message.Chat.Id,ex.Message, cancellationToken: cancellationToken);
        }
        catch (TaskCountLimitException ex)
        {
            await botClient.SendMessage(update.Message.Chat.Id,ex.Message, cancellationToken: cancellationToken);
        }
        catch (TaskLengthLimitException ex)
        {
            await botClient.SendMessage(update.Message.Chat.Id,ex.Message, cancellationToken: cancellationToken);
        }
        catch (DuplicateTaskException ex)
        {
            await botClient.SendMessage(update.Message.Chat.Id,ex.Message, cancellationToken: cancellationToken);
        }
        
        finally
        {
            OnHandleUpdateCompleted?.Invoke(update.Message?.Text ?? "Unknown message");
        }
    }

    public async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source,
        CancellationToken cancellationToken)
    {
        await Console.Out.WriteLineAsync($"Error ({source}): {exception.Message}");
        await Console.Out.WriteLineAsync($"Stack trace: {exception.StackTrace}");
    }

    async Task RegisterUserAsync(ITelegramBotClient botClient, Chat chat, Update update, 
        CancellationToken cancellationToken)
    {
        if (update?.Message?.From == null)
            throw new ArgumentException("Invalid Update object");

        var from = update.Message.From;
        var user = await _userService.GetUserAsync(from.Id, cancellationToken);
        
        if (user != null)
        {
            await botClient.SendMessage(chat.Id, $"Вы уже зарегистрированы: {user.TelegramUserName}", cancellationToken: cancellationToken);
            return;
        }
        
        string userName = !string.IsNullOrEmpty(from.Username) 
            ? from.Username 
            : $"User-{from.Id}";

        user = await _userService.RegisterUserAsync(from.Id, userName, cancellationToken);
        
        await botClient.SendMessage(
            chatId: chat.Id,
            text: $"Добро пожаловать, {user.TelegramUserName}!",
            replyMarkup: GetMainKeyboard(),
            cancellationToken: cancellationToken);
        await botClient.SendMessage(
            chatId: chat.Id,
            text: $"Ваш ID: {user.UserId}",
            replyMarkup: GetMainKeyboard(),
            cancellationToken: cancellationToken);
        await botClient.SendMessage(
            chatId: chat.Id,
            text: $"Дата регистрации: {user.RegisteredAt}",
            replyMarkup: GetMainKeyboard(),
            cancellationToken: cancellationToken);
    }
    
    static async Task HelpAsync(ITelegramBotClient botClient, Chat chat, CancellationToken cancellationToken)
    {
        await botClient.SendMessage(chat.Id,"Описание доступных команд:", cancellationToken: cancellationToken);
        await botClient.SendMessage(chat.Id,"/start - начало работы с ботом, ввод имени", cancellationToken: cancellationToken);
        await botClient.SendMessage(chat.Id,"/report - показать статистику по задачам", cancellationToken: cancellationToken);
        await botClient.SendMessage(chat.Id,"/cancel - отменить текущий сценарий", cancellationToken: cancellationToken);
    }

    async Task InfoAsync(ITelegramBotClient botClient, Chat chat, ToDoUser user, CancellationToken cancellationToken)
    {
        await botClient.SendMessage(chat.Id, string.IsNullOrWhiteSpace(user.TelegramUserName)
            ? "Версия бота 1.0, дата создания 25.05.2025"
            : $"{user.TelegramUserName}, версия бота 1.0, дата создания 25.05.2025", cancellationToken: cancellationToken);
    }
    
    async Task AddTaskAsync(ITelegramBotClient botClient, Chat chat, string taskName, ToDoUser user, 
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(taskName))
        {
            await botClient.SendMessage(chatId: chat.Id, $"{user.TelegramUserName}, описание задачи не может быть пустым", 
                cancellationToken: cancellationToken);
            return;
        }

        var newTask = await _toDoService.AddAsync(user, taskName, DateTime.Now.AddDays(7), null);
        
        await botClient.SendMessage(chat.Id,$"{user.TelegramUserName}, задача добавлена!", cancellationToken: cancellationToken);
    }
    
    async Task ShowTasksAsync(ITelegramBotClient botClient, Chat chat, ToDoUser user, 
        CancellationToken cancellationToken)
    {
        var tasks = await _toDoService.GetActiveByUserIdAsync(user.UserId);
        if (tasks.Count == 0)
        {
            await botClient.SendMessage(
                chatId: chat.Id,
                text: $"{user.TelegramUserName}, список задач пуст",
                cancellationToken: cancellationToken);
            return;
        }
        await botClient.SendMessage(
            chatId: chat.Id,
            text: "\nТекущий список задач:",
            cancellationToken: cancellationToken);
    
        foreach (var task in tasks.Where(t => t.State == ToDoItem.ToDoItemState.Active))
        {
            await botClient.SendMessage(
                chatId: chat.Id,
                text: $"{task.Name} - {task.CreatedAt.ToLocalTime()} - `{task.Id}`",
                parseMode: ParseMode.MarkdownV2,
                cancellationToken: cancellationToken);
        }
    }
    
    async Task FindTaskAsync(ITelegramBotClient botClient, Chat chat, ToDoUser user, string taskName, 
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(taskName))
        {
            await botClient.SendMessage(chat.Id, "Укажите текст для поиска: /find Текст", cancellationToken: cancellationToken);
            return;
        }
        
        var tasks = await _toDoService.FindAsync(user, taskName);
        if (tasks.Count == 0)
        {
            await botClient.SendMessage(chat.Id, $"{user.TelegramUserName}, задачи начинающиеся на '{taskName}' не найдены", 
                cancellationToken: cancellationToken);
            return;
        }
        
        await botClient.SendMessage(chat.Id, $"Найденные задачи ({taskName}):", cancellationToken: cancellationToken);
        
        foreach (var task in tasks)
        {
            await botClient.SendMessage(chat.Id, 
                $"{task.Name} - {task.CreatedAt.ToLocalTime()} - {task.Id}", cancellationToken: cancellationToken);
        }
    }

    static async Task ExitAsync(ITelegramBotClient botClient, Chat chat, ToDoUser user, 
        CancellationToken cancellationToken)
    {
        await botClient.SendMessage(chat.Id,string.IsNullOrEmpty(user.TelegramUserName)
            ? "До свидания!"
            : $"{user.TelegramUserName}, до свидания!", cancellationToken: cancellationToken);
    }

    async Task ShowCurrentMenuAsync(ITelegramBotClient botClient, Chat chat, ToDoUser user, 
        CancellationToken cancellationToken)
    {
        string greeting = user == null 
            ? "Добро пожаловать в бота\nДоступные команды:" 
            : $"{user.TelegramUserName}, доступные команды";
        
        var keyboard = user == null ? GetStartKeyboard() : GetMainKeyboard();
    
        await botClient.SendMessage(
            chatId: chat.Id,
            text: greeting,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);
    }
    
    private async Task ShowReportAsync(ITelegramBotClient botClient, Chat chat, ToDoUser user, 
        CancellationToken cancellationToken)
    {
        var (total, completed, active, generatedAt) = await _toDoReportService.GetUserStatsAsync(user.UserId);
        await botClient.SendMessage(chat.Id, 
            $"Статистика по задачам на {generatedAt:dd.MM.yyyy HH:mm:ss}\n" +
            $"Всего: {total}; Завершенных: {completed}; Активных: {active};", cancellationToken: cancellationToken);
    }
    
    private ReplyKeyboardMarkup GetStartKeyboard()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton[] { "/start" }
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = true
        };
    }

    private ReplyKeyboardMarkup GetMainKeyboard()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton[] { "/addtask", "/show", "/report" }
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false
        };
    }
    
    private IScenario GetScenario(ScenarioContext.ScenarioType scenario)
    {
        var foundScenario = _scenarios.FirstOrDefault(s => s.CanHandle(scenario));
        
        if (foundScenario == null)
        {
            throw new InvalidOperationException($"Сценарий {scenario} не найден");
        }
        
        return foundScenario;
    }
    
    private async Task ProcessScenario(ITelegramBotClient botClient, ScenarioContext context, Update update, CancellationToken ct)
    {
        var scenario = GetScenario(context.CurrentScenario);
        var result = await scenario.HandleMessageAsync(botClient, context, update, ct);
        
        if (result == ScenarioResult.Completed)
        {
            await _contextRepository.ResetContext(context.UserId, ct);
            await botClient.SendMessage(
                chatId: update.Message.Chat.Id,
                text: "Сценарий завершен",
                replyMarkup: GetMainKeyboard(),
                cancellationToken: ct);
        }
        else
        {
            await _contextRepository.SetContext(context.UserId, context, ct);
        }
    }
    
    private async Task ShowListsSelectionAsync(ITelegramBotClient botClient, Chat chat, long userId, CancellationToken ct)
    {
        var user = await _userService.GetUserAsync(userId, ct);
        if (user == null)
        {
            await botClient.SendMessage(chat.Id, "Сначала зарегистрируйтесь через /start", cancellationToken: ct);
            return;
        }

        var lists = await _toDoListService.GetUserLists(user.UserId, ct);
    
        var keyboard = BuildListsKeyboard(lists);
    
        await botClient.SendMessage(
            chatId: chat.Id,
            text: "Выберите список:",
            replyMarkup: keyboard,
            cancellationToken: ct);
    }
    
    private InlineKeyboardMarkup BuildListsKeyboard(IReadOnlyList<ToDoList> lists)
    {
        var buttons = new List<InlineKeyboardButton[]>();
        
        var noListCallback = new ToDoListCallbackDto 
        { 
            Action = "show", 
            ToDoListId = null 
        }.ToString();
    
        buttons.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData("📌 Без списка", noListCallback)
        });
        
        foreach (var list in lists)
        {
            var listCallback = new ToDoListCallbackDto 
            { 
                Action = "show", 
                ToDoListId = list.Id 
            }.ToString();
            
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData($"📁 {list.Name}", listCallback)
            });
        }
        
        buttons.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData("🆕 Добавить", "addlist"),
            InlineKeyboardButton.WithCallbackData("❌ Удалить", "deletelist")
        });

        return new InlineKeyboardMarkup(buttons);
    }
    
    private async Task OnCallbackQuery(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken ct)
    {
        await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
        
        var user = await _userService.GetUserAsync(callbackQuery.From.Id, ct);
        if (user == null) return;
        
        var callbackDto = CallbackDto.FromString(callbackQuery.Data);
        
        if (callbackDto.Action == "show")
        {
            var listCallbackDto = PagedListCallbackDto.FromString(callbackQuery.Data);
            var tasks = await _toDoService.GetByUserIdAndList(user.UserId, listCallbackDto.ToDoListId, ct);
            var message = "Задачи:\n";
            var callbackData = new List<KeyValuePair<string, string>>();
            
            foreach (var task in tasks)
            {
                var taskCallback = new ToDoItemCallbackDto 
                { 
                    Action = "showtask", 
                    ToDoItemId = task.Id 
                }.ToString();
                callbackData.Add(new KeyValuePair<string, string>($"{task.Name} - {task.Deadline:dd.MM.yyyy}", taskCallback));
                message += $"{task.Name} - {task.Deadline:dd.MM.yyyy}\n";
            }
        
            var keyboard = BuildPagedButtons(callbackData, listCallbackDto);
    
            await botClient.EditMessageText(
                chatId: callbackQuery.Message.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: message,
                replyMarkup: keyboard,
                cancellationToken: ct);
        }
        
        else if (callbackDto.Action == "addlist")
        {
            var context = new ScenarioContext(callbackQuery.From.Id, ScenarioContext.ScenarioType.AddList);
            await _contextRepository.SetContext(context.UserId, context, ct);
            await ProcessScenario(botClient, context, new Update { CallbackQuery = callbackQuery }, ct);
        }
        else if (callbackDto.Action == "deletelist") 
        {
            var context = new ScenarioContext(callbackQuery.From.Id, ScenarioContext.ScenarioType.DeleteList);
            await _contextRepository.SetContext(context.UserId, context, ct);
            await ProcessScenario(botClient, context, new Update { CallbackQuery = callbackQuery }, ct);
        }
        
        else if (callbackDto.Action == "showtask")
        {
            var itemCallbackDto = ToDoItemCallbackDto.FromString(callbackQuery.Data);
            
            var task = await _toDoService.Get(itemCallbackDto.ToDoItemId, ct);
            if (task == null) return;
        
            var message = $"Задача: {task.Name}\n";
            message += $"Дедлайн: {task.Deadline:dd.MM.yyyy}\n";
            message += $"Статус: {task.State}";
            
            var completeCallback = new ToDoItemCallbackDto 
            { 
                Action = "completetask", 
                ToDoItemId = task.Id 
            }.ToString();
            var deleteCallback = new ToDoItemCallbackDto 
            { 
                Action = "deletetask", 
                ToDoItemId = task.Id 
            }.ToString();
        
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("✅Выполнить", completeCallback),
                    InlineKeyboardButton.WithCallbackData("❌Удалить", deleteCallback)
                }
            });
        
            await botClient.SendMessage(
                chatId: callbackQuery.Message.Chat.Id,
                text: message,
                replyMarkup: keyboard,
                cancellationToken: ct);
        }
        
        else if (callbackDto.Action == "completetask")
        {
            var itemCallbackDto = ToDoItemCallbackDto.FromString(callbackQuery.Data);
            await _toDoService.MarkCompletedAsync(itemCallbackDto.ToDoItemId);
        
            await botClient.SendMessage(
                chatId: callbackQuery.Message.Chat.Id,
                text: "Задача выполнена!",
                cancellationToken: ct);
        }
        else if (callbackDto.Action == "deletetask")
        {
            var itemCallbackDto = ToDoItemCallbackDto.FromString(callbackQuery.Data);
            await _toDoService.DeleteAsync(itemCallbackDto.ToDoItemId);
        
            await botClient.SendMessage(
                chatId: callbackQuery.Message.Chat.Id,
                text: "Задача удалена!",
                cancellationToken: ct);
        }
        
        else if (callbackDto.Action == "show_completed")
        {
            var listCallbackDto = PagedListCallbackDto.FromString(callbackQuery.Data);
            
            var allTasks = await _toDoService.GetAllByUserIdAsync(user.UserId);
            var completedTasks = allTasks.Where(t => t.State == ToDoItem.ToDoItemState.Completed).ToList();
            
            if (completedTasks.Count == 0)
            {
                await botClient.EditMessageText(
                    chatId: callbackQuery.Message.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    text: "Задач нет",
                    cancellationToken: ct);
                return;
            }
            var message = "Выполненные задачи:\n";
            var callbackData = new List<KeyValuePair<string, string>>();
        
            foreach (var task in completedTasks)
            {
                var taskCallback = new ToDoItemCallbackDto 
                { 
                    Action = "showtask", 
                    ToDoItemId = task.Id 
                }.ToString();
                callbackData.Add(new KeyValuePair<string, string>($"{task.Name} - {task.Deadline:dd.MM.yyyy}", taskCallback));
                message += $"{task.Name} - {task.Deadline:dd.MM.yyyy}\n";
            }
        
            var keyboard = BuildPagedButtons(callbackData, listCallbackDto);
        
            await botClient.EditMessageText(
                chatId: callbackQuery.Message.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: message,
                replyMarkup: keyboard,
                cancellationToken: ct);
        }
    }

    private InlineKeyboardMarkup BuildPagedButtons(IReadOnlyList<KeyValuePair<string, string>> callbackData, PagedListCallbackDto listDto)
    {
        var totalPages = (callbackData.Count + _pageSize - 1) / _pageSize;
    
        var buttons = new List<InlineKeyboardButton[]>();
    
        var pageButtons = callbackData.GetBatchByNumber(_pageSize, listDto.Page);
        foreach (var button in pageButtons)
        {
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(button.Key, button.Value)
            });
        }
    
        var navigationButtons = new List<InlineKeyboardButton>();
    
        if (listDto.Page > 0)
        {
            var prevCallback = new PagedListCallbackDto 
            { 
                Action = listDto.Action, 
                ToDoListId = listDto.ToDoListId, 
                Page = listDto.Page - 1 
            }.ToString();
            navigationButtons.Add(InlineKeyboardButton.WithCallbackData("⬅️", prevCallback));
        }
    
        if (listDto.Page < totalPages - 1)
        {
            var nextCallback = new PagedListCallbackDto 
            { 
                Action = listDto.Action, 
                ToDoListId = listDto.ToDoListId, 
                Page = listDto.Page + 1 
            }.ToString();
            navigationButtons.Add(InlineKeyboardButton.WithCallbackData("➡️", nextCallback));
        }
    
        if (navigationButtons.Count > 0)
        {
            buttons.Add(navigationButtons.ToArray());
        }
    
        return new InlineKeyboardMarkup(buttons);
    }
    
}