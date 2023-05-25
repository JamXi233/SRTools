using Spectre.Console;

namespace SRTools.Depend
{
    internal class Logging
    {
        public static void Write(string Info, int Mode)
        {
            if (Mode == 0) { AnsiConsole.Write(new Markup("[bold White][[INFO]][/]" + Info + "\n")); }
            else if (Mode == 1) { AnsiConsole.Write(new Markup("[bold Yellow][[WARN]][/]" + Info + "\n")); }
            else { AnsiConsole.Write(new Markup("[bold Red][[ERROR]]" + Info + "[/]\n")); }
        }
    }
}
