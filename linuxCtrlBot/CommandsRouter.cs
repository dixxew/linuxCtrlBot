public static class CommandsRouter
{
    public static string ParseCommand(string input)
    {
        return input.ToLower() switch
        {
            "/ip a" => "ip a",
            "/speedtest" => "speedtest",
            "/ls" => "ls /root/",
            _ => null
        };
    }
}
