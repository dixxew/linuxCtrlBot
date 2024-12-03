using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

class Program
{
    private static TelegramBotClient _botClient;

    static async Task Main(string[] args)
    {
        string botToken = "7879607825:AAGGXGU5RHm7U5QKSfHxqx-UuJMILZF_GuQ";
        _botClient = new TelegramBotClient(botToken);

        Auth.LoadUserRoles();

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
}
