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

        // Обработка админ-команды добавления пользователя
        if (messageText.StartsWith("/adduser"))
        {
            if (role != "admin")
            {
                await botClient.SendTextMessageAsync(chatId, "У вас нет прав для выполнения этой команды.");
                return;
            }

            var parts = messageText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3)
            {
                await botClient.SendTextMessageAsync(chatId, "Использование: /adduser <username> <role>");
                return;
            }

            string newUsername = parts[1];
            string newRole = parts[2];

            if (Auth.AddUser(newUsername, newRole))
            {
                await botClient.SendTextMessageAsync(chatId, $"Пользователь {newUsername} добавлен с ролью {newRole}.");
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId, "Ошибка добавления пользователя. Возможно, он уже существует или роль неверна.");
            }

            return;
        }

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

    private static async Task CreateVpnConfig(ITelegramBotClient botClient, long chatId, string name)
    {
        string scriptPath = "/root/openvpn-install.sh";
        string configPath = $"/root/{name}.ovpn";
        string guidePath = "/root/ovpn-stunnel-guide.zip";

        try
        {
            Console.WriteLine($"[INFO] Начинаю выполнение скрипта для клиента {name}");

            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{scriptPath}\"",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();

            Console.WriteLine("[INFO] Скрипт запущен. Ожидаю запроса 'Option:'...");

            bool optionRequested = false;

            // Читаем вывод в реальном времени
            var outputTask = Task.Run(async () =>
            {
                string line;
                while ((line = await process.StandardOutput.ReadLineAsync()) != null)
                {
                    Console.WriteLine($"[OUTPUT] {line}");
                    if (line.Contains("Option:"))
                    {
                        optionRequested = true;
                    }
                }
            });

            var errorTask = Task.Run(async () =>
            {
                string line;
                while ((line = await process.StandardError.ReadLineAsync()) != null)
                {
                    Console.WriteLine($"[ERROR] {line}");
                }
            });

            // Ждём появления текста "Option:"
            while (!optionRequested)
            {
                await Task.Delay(100); // Небольшая пауза для проверки
            }

            Console.WriteLine("[INFO] Запрос 'Option:' найден. Передаю выбор...");

            // Передаём данные в скрипт
            using (var writer = process.StandardInput)
            {
                if (writer.BaseStream.CanWrite)
                {
                    await writer.WriteLineAsync("1"); // Выбираем "Add a new client"
                    Console.WriteLine("[INFO] Передал выбор: Add a new client");

                    await writer.WriteLineAsync(name); // Вводим имя клиента
                    Console.WriteLine($"[INFO] Передал имя клиента: {name}");
                }
            }

            Console.WriteLine("[INFO] Ожидаю завершения работы скрипта...");
            await Task.WhenAll(outputTask, errorTask);
            process.WaitForExit();

            Console.WriteLine("[INFO] Скрипт завершён.");

            if (!File.Exists(configPath))
            {
                Console.WriteLine($"[ERROR] Файл {configPath} не найден.");
                await botClient.SendTextMessageAsync(chatId, $"Ошибка: файл {configPath} не найден. Возможно, скрипт завершился с ошибкой.");
                return;
            }

            Console.WriteLine($"[INFO] Файл {configPath} найден. Отправляю пользователю...");

            using (var configStream = File.OpenRead(configPath))
            using (var guideStream = File.OpenRead(guidePath))
            {
                await botClient.SendTextMessageAsync(chatId, "Вот ваш конфиг. Инструкция по настройке и установке находится в архиве. Доступно для Windows и Android. С вас 5к зелени.");

                var configFile = InputFile.FromStream(configStream, $"{name}.ovpn");
                var guideFile = InputFile.FromStream(guideStream, "ovpn-stunnel-guide.zip");

                await botClient.SendDocumentAsync(chatId, configFile);
                await botClient.SendDocumentAsync(chatId, guideFile);
            }

            Console.WriteLine("[INFO] Файлы успешно отправлены пользователю.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Ошибка при выполнении команды: {ex.Message}");
            await botClient.SendTextMessageAsync(chatId, $"Ошибка при выполнении команды: {ex.Message}");
        }
    }
}
