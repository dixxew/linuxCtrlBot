using System;
using System.Diagnostics;

public static class ExecuteBash
{
    public static string RunCommand(string command)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{command}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            return string.IsNullOrEmpty(output) ? error : output;
        }
        catch (Exception ex)
        {
            return $"Ошибка выполнения команды: {ex.Message}";
        }
    }
}
