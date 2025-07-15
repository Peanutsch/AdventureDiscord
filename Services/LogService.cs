using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.Services
{
    /// <summary>
    /// Provides logging utilities for the AdventureBot application.
    /// Includes methods for logging information, errors, session markers, and bot status.
    /// </summary>
    public class LogService
    {
        /// <summary>
        /// Prints a decorated session divider to the console and debug output.
        /// Useful for marking the start or end of a session with a visual separator.
        /// </summary>
        /// <param name="divider">The character used to repeat for the divider (e.g., '=' or '-').</param>
        /// <param name="session">The name of the session or label.</param>
        public static void SessionDivider(char divider, string session)
        {
            string format = $"[{session.ToUpper()}]{new string(divider, 10)}[{DateTime.Today:dd/MM/yyyy}][{DateTime.Now:HH:mm:ss}]{new string(divider, 10)}[{session.ToUpper()}]";

            Console.WriteLine(format);
            Debug.WriteLine(format);
        }

        /// <summary>
        /// Logs a general info message to the console and debug output.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void Info(string message)
        {
            string format = $"[Info] {message}";

            Console.WriteLine(format);
            Debug.WriteLine(format);
        }

        /// <summary>
        /// Logs an error message to the console and debug output.
        /// </summary>
        /// <param name="message">The error message to log.</param>
        public static void Error(string message)
        {
            string format = $"[Error] {message}";

            Console.WriteLine(format);
            Debug.WriteLine(format);
        }

        /// <summary>
        /// Logs the bot status (e.g., Started, Stopped) in a highlighted format.
        /// </summary>
        /// <param name="status">The current status of the bot.</param>
        public static void BotStatus(string status)
        {
            string message = $"\n[ADVENTUREBOT IS {status.ToUpper()}]\n";

            Console.WriteLine(message);
            Debug.WriteLine(message);
        }

        /// <summary>
        /// Prints a labeled divider section to mark the beginning (1) or end (2) of a part.
        /// </summary>
        /// <param name="placement">1 for start, 2 for end.</param>
        /// <param name="part">The name of the part being marked.</param>
        public static void DividerParts(int placement, string part)
        {
            // Create the divider line depending on whether it's the start or end
            string? divider = placement switch
            {
                1 => $"\n[Start]==========[{part}]==========[Start]",
                2 => $"[End]==========[{part}]==========[End]\n",
                _ => null // Ignore invalid placement values
            };

            // Output the divider if a valid one was created
            if (divider != null)
            {
                Console.WriteLine(divider);
                Debug.WriteLine(divider);
            }
        }
    }
}
