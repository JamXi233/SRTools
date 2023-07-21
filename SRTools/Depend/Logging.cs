using Spectre.Console;
using System;

namespace SRTools.Depend
{
    internal class Logging
    {
        public static void Write(string Info, int Mode, string ProgramName = null)
        {
            if (Mode == 0) { AnsiConsole.Write(new Markup("[bold White][[INFO]][/]"));Console.WriteLine(Info); }
            else if (Mode == 1) { AnsiConsole.Write(new Markup("[bold Yellow][[WARN]][/]")); Console.WriteLine(Info); }
            else if (Mode == 2) { AnsiConsole.Write(new Markup("[bold Red][[ERROR]][/]")); Console.WriteLine(Info); }
            else if (Mode == 3) { AnsiConsole.Write(new Markup("[bold White][[BOARDCAST]][/][bold Magenta][[" + ProgramName + "]][/]")); Console.WriteLine(Info); }
        }
    }
}
