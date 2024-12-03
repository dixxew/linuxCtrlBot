using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

class Program
{
    private static TelegramBotClient _botClient;

    static async Task Main(string[] args)
    {
        string botToken = "7879607825:AAGGXGU5RHm7U5QKSfHxqx-UuJMILZF_GuQ";
        _botClient = new TelegramBotClient(botToken);

        Auth.LoadUserRoles();

        await SetBotCommands(_botClient);

        var me = await _botClient.GetMeAsync();
        Console.WriteLine($"Bot {me.Username} started");

        var cts = new CancellationTokenSource();
        _botClient.StartReceiving(
            Commands.HandleUpdateAsync,
            Commands.HandleErrorAsync,
            cancellationToken: cts.Token
        );

        Console.WriteLine("Press Enter to exit");
        Console.ReadLine();
        cts.Cancel();
    }

    private static async Task SetBotCommands(TelegramBotClient botClient)
    {
        // Команды для роли "user"
        var userCommands = new List<BotCommand>
    {
        new BotCommand { Command = "/ipa", Description = "Показать IP адреса" }, // Убрано подчёркивание
        new BotCommand { Command = "/speedtest", Description = "Тест скорости интернета" },
        new BotCommand { Command = "/ls", Description = "Показать файлы в /root/" },
        new BotCommand { Command = "/createvpnconfig", Description = "Создать VPN конфиг" } // Убраны пробелы для консистентности
    };

        // Команды для роли "admin"
        var adminCommands = new List<BotCommand>(userCommands)
    {
        new BotCommand { Command = "/adduser", Description = "Добавить нового пользователя" }
    };

        // Устанавливаем команды для всех пользователей
        await botClient.SetMyCommandsAsync(userCommands, new BotCommandScopeAllPrivateChats());

        // Устанавливаем команды для администраторов
        await botClient.SetMyCommandsAsync(adminCommands, new BotCommandScopeAllPrivateChats());
    }


}
