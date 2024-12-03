using System;
using System.IO;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using File = System.IO.File;


public static class Commands
{
    public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Type != Telegram.Bot.Types.Enums.UpdateType.Message || update.Message?.Text == null)
            return;

        var chatId = update.Message.Chat.Id;
        var username = update.Message.From?.Username;

        if (!Auth.IsAuthorized(username))
        {
            await botClient.SendTextMessageAsync(chatId, "У вас нет доступа к этому боту.");
            return;
        }

        var role = Auth.GetUserRole(username);
        var messageText = update.Message.Text;

        if (messageText.StartsWith("/createVpnConfig"))
        {
            if (!Auth.HasAccessToCommand(username, "/createVpnConfig"))
            {
                await botClient.SendTextMessageAsync(chatId, "У вас нет прав для выполнения этой команды.");
                return;
            }

            var parts = messageText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
            {
                await botClient.SendTextMessageAsync(chatId, "Использование: /createVpnConfig {name}");
                return;
            }

            string name = parts[1];
            await CreateVpnConfig(botClient, chatId, name);
            return;
        }

        // Остальные команды...
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

    private static async Task CreateVpnConfig(ITelegramBotClient botClient, long chatId, string name)
    {
        string scriptPath = "/root/openvpn-install.sh";
        string configPath = $"/root/{name}.ovpn";
        string guidePath = "/root/ovpn-stunnel-guide.zip";

        try
        {
            // Запускаем скрипт
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"echo '1\n{name}\n' | {scriptPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            process.WaitForExit();

            if (!File.Exists(configPath))
            {
                await botClient.SendTextMessageAsync(chatId, $"Ошибка: файл {configPath} не найден. Возможно, скрипт завершился с ошибкой.");
                return;
            }

            // Отправляем файл и архив
            using (var configStream = File.OpenRead(configPath))
            using (var guideStream = File.OpenRead(guidePath))
            {
                await botClient.SendTextMessageAsync(chatId, "Вот ваш конфиг. Инструкция по настройке и установке находится в архиве. Доступно для Windows и Android. С вас 5к зелени.");

                var configFile = InputFile.FromStream(configStream, $"{name}.ovpn");
                var guideFile = InputFile.FromStream(guideStream, "ovpn-stunnel-guide.zip");

                await botClient.SendDocumentAsync(chatId, configFile);
                await botClient.SendDocumentAsync(chatId, guideFile);
            }
        }
        catch (Exception ex)
        {
            await botClient.SendTextMessageAsync(chatId, $"Ошибка при выполнении команды: {ex.Message}");
        }
    }
}
