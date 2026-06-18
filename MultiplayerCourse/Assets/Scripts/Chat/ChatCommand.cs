using System;
using System.Collections.Generic;
using UnityEngine;

//parsing and execution of chat commands
public static class ChatCommand
{
    private static readonly Dictionary<string, Action<string[]>> Commands = new Dictionary<string, Action<string[]>>
    {
        { "help", ShowHelp },
        { "clear", ClearChat },
        { "players", ListPlayers },
        { "msg", HandlePrivateMessage }
    };

    // Processes a chat input to determine if it's a command
    
    public static bool ProcessCommand(string input)
    {
        if (string.IsNullOrWhiteSpace(input) || !input.StartsWith("/"))
            return false;

        string[] parts = input.Substring(1).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return false;

        string command = parts[0].ToLower();
        
        if (Commands.TryGetValue(command, out Action<string[]> handler))
        {
            try
            {
                handler(parts);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChatCommand] Error executing command '{command}': {ex.Message}");
                ChatManager.RaiseSystemMessage($"Error executing command: {ex.Message}");
                return true;
            }
        }

        // Unknown command
        ChatManager.RaiseSystemMessage($"Unknown command: {command}. Type /help for available commands.");
        return true;
    }

    private static void ShowHelp(string[] args)
    {
        string helpText = "Available commands:\n" +
                         "/help - Show this help\n" +
                         "/clear - Clear chat history\n" +
                         "/players - List online players\n" +
                         "/msg <player> <message> - Send private message";
        
        ChatManager.RaiseSystemMessage(helpText);
    }

    private static void ClearChat(string[] args)
    {
        if (ChatManager.Instance != null)
        {
            ChatManager.Instance.ClearMessageHistory();
            ChatManager.RaiseSystemMessage("Chat history cleared.");
        }
    }

    private static void ListPlayers(string[] args)
    {
        var playerManagers = UnityEngine.Object.FindObjectsOfType<PlayerManager>();
        
        if (playerManagers.Length == 0)
        {
            ChatManager.RaiseSystemMessage("No players found.");
            return;
        }

        string playerList = "Online players:\n";
        foreach (var pm in playerManagers)
        {
            string nickname = pm.Nickname.Value;
            if (!string.IsNullOrEmpty(nickname))
            {
                playerList += $"- {nickname}\n";
            }
        }

        ChatManager.RaiseSystemMessage(playerList.TrimEnd());
    }

    private static void HandlePrivateMessage(string[] args)
    {
        if (args.Length < 3)
        {
            ChatManager.RaiseSystemMessage("Usage: /msg <player> <message>");
            return;
        }

        string targetPlayer = args[1];
        string message = string.Join(" ", args, 2, args.Length - 2);
        
        if (ChatManager.Instance != null)
        {
            ChatManager.Instance.SendPrivateMessage(targetPlayer, message);
        }
    }

}