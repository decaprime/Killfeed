using System;
using System.Collections.Generic;

namespace Killfeed.Utils;

public static class AnsiColor
{

    private static readonly Dictionary<string, string> Colors = new Dictionary<string, string>
        {
            {"reset", "</color>"},
            {"black", "<color=black>"},
            {"red", "<color=red>"},
            {"green", "<color=green>"},
            {"yellow", "<color=yellow>"},
            {"blue", "<color=blue>"},
            {"white", "<color=white>"},
        };
        
    public static string ColorText(string text, string color)
    {
        if (Colors.ContainsKey(color.ToLower()))
        {
            return $"{Colors[color.ToLower()]}{text}{Colors["reset"]}";
        }
        else
        {
            throw new ArgumentException($"Couleur inconnue: {color}");
        }
    }

   
    public static IEnumerable<string> GetAvailableColors()
    {
        return Colors.Keys;
    }
    
    public static void PrintColoredText(string text, string color)
    {
        Console.WriteLine(ColorText(text, color));
    }
}
