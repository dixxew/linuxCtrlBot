public static class CommandsRouter
{
    public static string ParseCommand(string input)
    {
        if (input.StartsWith("/createVpnConfig"))
            return $"echo '1\n{input.Split(' ')[1]}' | /root/openvpn-install.sh";

        return input.ToLower() switch
        {
            "/ip a" => "ip a",
            "/speedtest" => "speedtest-cli",
            "/ls" => "ls /root/",
            _ => null
        };
    }

    public static CommandType GetCommandType(string input)
    {
        if (input.ToLower() == "/adduser" || input.ToLower().StartsWith("/createVpnConfig"))
            return CommandType.AdminCommand;

        return input.ToLower() switch
        {
            "/ip a" => CommandType.UserCommand,
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
