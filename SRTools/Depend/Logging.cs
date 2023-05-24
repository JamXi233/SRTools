using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRTools.Depend
{
    internal class Logging
    {
        public static void Write(String Info, int Mode)
        {
            try
            {
                if (Mode == 0) { AnsiConsole.Write(new Markup("[bold White]INFO:[/]" + Info + "\n")); }
                else if (Mode == 1) { AnsiConsole.Write(new Markup("[bold Yellow]WARN:[/]" + Info + "\n")); }
                else { AnsiConsole.Write(new Markup("[bold Red]ERROR:[/]" + Info + "\n")); }
            }
            catch (Exception) { }
        }
    }
}
