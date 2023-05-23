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
            if (Mode == 0) { Console.WriteLine("[INFO] "+Info); }
            else { Console.WriteLine("[ERROR] " + Info); }
        }
    }
}
