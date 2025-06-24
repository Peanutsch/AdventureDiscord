using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Services
{
    public class LogService
    {
        public static void SessionDivider(char divider, string session)
        {
            string format = $"[{session.ToUpper()}]{new string(divider, 10)}[{DateTime.Today:dd/MM/yyyy}][{DateTime.Now:HH:mm:ss}]{new string(divider, 10)}[{session.ToUpper()}]";

            Console.WriteLine(format);
            Debug.WriteLine(format);
        }

        public static void Info(string message)
        {
            string format = $"[Info] {message}";

            Console.WriteLine(format);
            Debug.WriteLine(format);
        }

        public static void Error(string message)
        {
            string format = $"[Error] {message}";

            Console.WriteLine(format);
            Debug.WriteLine(format);
        }

        public static void BotStatus(string status)
        {

            string message = $"\n[ADVENTUREBOT IS {status.ToUpper()}]\n";

            Console.WriteLine(message);
            Debug.WriteLine(message);
        }
    }
}
