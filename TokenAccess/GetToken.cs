using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adventure.TokenAccess
{
    public class GetToken
    {
        /// <summary>
        /// Haalt het Discord bot-token op uit het 'get_token.csv' bestand in de projectroot.
        /// </summary>
        public static string GetTokenFromCSV()
        {
            string filePath = Path.Combine(AppContext.BaseDirectory, "get_token.csv");

            if (File.Exists(filePath))
                return File.ReadAllText(filePath).Trim();
            else
            {
                throw new FileNotFoundException("Token bestand niet gevonden.", filePath);
            }
        }
    }
}
