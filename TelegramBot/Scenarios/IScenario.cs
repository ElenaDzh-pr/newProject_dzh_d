using Telegram.Bot;
using Telegram.Bot.Types;

namespace ProjectDz;

public interface IScenario
{
    //Проверяет, может ли текущий сценарий обрабатывать указанный тип сценария.
    //Используется для определения подходящего обработчика в системе сценариев.
    bool CanHandle(ScenarioContext.ScenarioType scenario);
    
    //Обрабатывает входящее сообщение от пользователя в рамках текущего сценария.
    //Включает основную бизнес-логику
    Task<ScenarioResult> HandleMessageAsync(ITelegramBotClient bot, ScenarioContext context, Update update, CancellationToken ct);
}