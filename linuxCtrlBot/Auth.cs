using System;
using System.Collections.Generic;
using System.IO;

public static class Auth
{
    private static readonly Dictionary<string, string> UserRoles = new();

    public static void LoadUserRoles()
    {
        const string rolesFilePath = "user_roles.txt";

        if (!File.Exists(rolesFilePath))
        {
            File.WriteAllText(rolesFilePath, "# user roles file\n# Format: username role (e.g., user1 admin)\n");
            Console.WriteLine($"Roles file created at {rolesFilePath}. Add users before running.");
            Environment.Exit(0);
        }

        foreach (var line in File.ReadAllLines(rolesFilePath))
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

    public static bool HasAccessToCommand(string username, string command)
    {
        string role = GetUserRole(username);
        // Пример ограничения доступа по ролям
        return role switch
        {
            "admin" => true,
            "user" => command == "/ip a" || command == "/ls",
            _ => false
        };
    }
}
