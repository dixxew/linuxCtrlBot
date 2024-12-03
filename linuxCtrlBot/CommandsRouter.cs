public static class CommandsRouter
{
    public static string ParseCommand(string input)
    {
        if (input.StartsWith("/createVpnConfig"))
            return "createVpnConfig";

        return input.ToLower() switch
        {
            "/ip a" => MapCommand(UserCommands.IpA),
            "/speedtest" => MapCommand(UserCommands.Speedtest),
            "/ls" => MapCommand(UserCommands.Ls),
            "/adduser" => MapCommand(AdminCommands.AddUser),
            _ => null
        };
    }

    private static string MapCommand(UserCommands command) =>
        command switch
        {
            UserCommands.IpA => "ip a",
            UserCommands.Speedtest => "speedtest-cli",
            UserCommands.Ls => "ls /root/",
            
            _ => null
        };

    private static string MapCommand(AdminCommands command) =>
        command switch
        {
            AdminCommands.AddUser => null, // Нет привязки к bash-команде, обработка на уровне логики
            _ => null
        };
}
public enum UserCommands
{
    IpA,
    Speedtest,
    Ls
}

public enum AdminCommands
{
    AddUser
}