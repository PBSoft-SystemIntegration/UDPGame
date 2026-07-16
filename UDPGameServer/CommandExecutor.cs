public delegate void CommandHandler(string[] args);
public class CommandExecutor
{
    private Dictionary<string, CommandHandler> commandHandlers = new Dictionary<string, CommandHandler>();

    public void RegisterCommand(string command, CommandHandler handler)
    {
        if (!commandHandlers.ContainsKey(command))
        {
            commandHandlers.Add(command, handler);
        }
        else
        {
            Console.WriteLine($"Command {command} is already registered.");
        }
    }

    public void ExecuteCommand(string input)
    {
        var parts = input.Split(' ');
        var command = parts[0];
        var args = parts.Length > 1 ? parts[1..] : Array.Empty<string>();

        if (commandHandlers.ContainsKey(command))
        {
            commandHandlers[command](args);
        }
        else
        {
            Console.WriteLine("Unknown command");
        }
    }
}