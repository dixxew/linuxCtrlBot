public static class CommandsRouter
{
    public static string ParseCommand(string input)
    {
        if (input.StartsWith("/createvpnconfig"))
            return $"echo '1\n{input.Split(' ')[1]}' | /root/openvpn-install.sh";

        return input.ToLower() switch
        {
            "/ipa" => "ip a",
            "/speedtest" => "speedtest",
            "/ls" => "ls /root/",
            _ => null
        };
    }

    public static CommandType GetCommandType(string input)
    {
        // Команда для администраторов
        if (input.ToLower() == "/adduser")
            return CommandType.AdminCommand;

        // Команда для создания конфигурации, доступная всем
        if (input.ToLower().StartsWith("/createvpnconfig"))
            return CommandType.UserCommand;

        // Остальные пользовательские команды
        return input.ToLower() switch
        {
            "/ipa" => CommandType.UserCommand,
            "/speedtest" => CommandType.UserCommand,
            "/ls" => CommandType.UserCommand,
            _ => CommandType.Unknown
        };
    }

}

public enum CommandType
{
    UserCommand,
    AdminCommand,
    Unknown
}
