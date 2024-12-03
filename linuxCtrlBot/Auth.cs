public static class Auth
{
    private static readonly Dictionary<string, string> UserRoles = new();
    private const string RolesFilePath = "user_roles.txt";

    public static void LoadUserRoles()
    {
        if (!File.Exists(RolesFilePath))
        {
            File.WriteAllText(RolesFilePath, "# user roles file\n# Format: username role (e.g., user1 admin)\n");
            Console.WriteLine($"Roles file created at {RolesFilePath}. Add users before running.");
            Environment.Exit(0);
        }

        foreach (var line in File.ReadAllLines(RolesFilePath))
        {
            if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line)) continue;
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
                UserRoles[parts[0]] = parts[1];
        }
    }

    public static bool IsAuthorized(string username) => username != null && UserRoles.ContainsKey(username);

    public static string GetUserRole(string username) =>
        UserRoles.TryGetValue(username, out string role) ? role : "unknown";

    public static bool HasAccessToCommand(string username, string input)
    {
        string role = GetUserRole(username);

        if (ParseUserCommand(input, out var userCommand))
        {
            return role switch
            {
                "admin" => true,
                "user" => userCommand == UserCommands.IpA ||
                          userCommand == UserCommands.Ls ||
                          userCommand == UserCommands.Speedtest ||
                          input.StartsWith("/createVpnConfig"),
                _ => false
            };
        }

        if (ParseAdminCommand(input, out var adminCommand))
        {
            return role == "admin";
        }

        return false;
    }
    public static bool AddUser(string username, string role)
    {
        if (UserRoles.ContainsKey(username) || (role != "admin" && role != "user"))
        {
            return false; // Пользователь уже существует или роль недопустима
        }

        // Добавляем пользователя в словарь
        UserRoles[username] = role;

        // Обновляем файл
        File.AppendAllText(RolesFilePath, $"{username} {role}{Environment.NewLine}");

        return true;
    }

    private static bool ParseUserCommand(string input, out UserCommands command)
    {
        command = input.ToLower() switch
        {
            "/ip a" => UserCommands.IpA,
            "/speedtest" => UserCommands.Speedtest,
            "/ls" => UserCommands.Ls,
            _ => (UserCommands)(-1)
        };
        return Enum.IsDefined(typeof(UserCommands), command);
    }

    private static bool ParseAdminCommand(string input, out AdminCommands command)
    {
        command = input.ToLower() switch
        {
            "/adduser" => AdminCommands.AddUser,
            _ => (AdminCommands)(-1)
        };
        return Enum.IsDefined(typeof(AdminCommands), command);
    }

}
