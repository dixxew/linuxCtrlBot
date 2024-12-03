using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

public static class Commands
{
    private static readonly Dictionary<long, List<string>> CommandHistory = new();

    public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Type != UpdateType.Message || update.Message?.Text == null) return;

        var chatId = update.Message.Chat.Id;
        var username = update.Message.From?.Username;

        if (!Auth.IsAuthorized(username))
        {
            await botClient.SendTextMessageAsync(chatId, "У вас нет доступа к этому боту.");
            return;
        }

        var role = Auth.GetUserRole(username);
        var messageText = update.Message.Text;

        if (!CommandHistory.ContainsKey(chatId))
            CommandHistory[chatId] = new List<string>();

        // Если это команда bash
        if (messageText.StartsWith("/"))
        {
            string bashCommand = CommandsRouter.ParseCommand(messageText);
            if (bashCommand != null)
            {
                if (Auth.HasAccessToCommand(username, messageText))
                {
                    string output = ExecuteBash.RunCommand(bashCommand);
                    await botClient.SendTextMessageAsync(chatId, $"Вывод команды {messageText}:\n{output}");
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId, "У вас нет прав для выполнения этой команды.");
                }
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId, "Неизвестная команда.");
            }
        }
        else
        {
            // Обработка обычного текста или других команд
            await botClient.SendTextMessageAsync(chatId, "Простое сообщение обработано.");
        }
    }

    public static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            Telegram.Bot.Exceptions.ApiRequestException apiRequestException =>
                $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        Console.WriteLine(errorMessage);
        return Task.CompletedTask;
    }
}
