namespace LibraryOS.UI
{
    /// <summary>
    /// Helper class for styled console output — FIX: improved GetIntInput with overflow message
    /// </summary>
    public static class ConsoleUI
    {
        public static void Clear() => Console.Clear();

        public static void ShowLogo()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(@"
 _     _ _                          ____   _____ 
| |   (_) |                        / __ \ / ____|
| |    _| |__   ___ _ __ __ _ _ __| |  | | (___  
| |   | | '_ \ / _ \ '__/ _` | '__| |  | |\___ \ 
| |___| | |_) |  __/ | | (_| | |  | |__| |____) |
|_____|_|_.__/ \___|_|  \__,_|_|   \____/|_____/ 
            ");
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("         Console Library Manager  v1.1 (polished)");
            Console.ResetColor();
        }

        public static void ShowHeader(string title)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\n════════════════════════════════════════");
            Console.WriteLine($"  {title}");
            Console.WriteLine($"════════════════════════════════════════");
            Console.ResetColor();
        }

        public static void ShowSuccess(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n{message}");
            Console.ResetColor();
        }

        public static void ShowError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n{message}");
            Console.ResetColor();
        }

        public static void ShowWarning(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n{message}");
            Console.ResetColor();
        }

        public static void ShowInfo(string message)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"{message}");
            Console.ResetColor();
        }

        public static void ShowHighlight(string message)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"{message}");
            Console.ResetColor();
        }

        public static string GetInput(string prompt)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"\n{prompt} ");
            Console.ResetColor();
            return Console.ReadLine()?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// FIX: Handles overflow gracefully with a clear message instead of infinite silent loop
        /// </summary>
        public static int GetIntInput(string prompt)
        {
            while (true)
            {
                string input = GetInput(prompt);
                if (int.TryParse(input, out int result))
                    return result;

                if (long.TryParse(input, out _))
                    ShowError("❌ Number too large. Please enter a smaller value.");
                else
                    ShowError("❌ Invalid number. Please enter digits only (e.g. 1, 42, 100).");
            }
        }

        public static decimal GetDecimalInput(string prompt)
        {
            while (true)
            {
                string input = GetInput(prompt);
                if (decimal.TryParse(input, out decimal result))
                    return result;
                ShowError("❌ Invalid amount. Enter a number (e.g. 100 or 150.50).");
            }
        }

        public static bool GetYesNoInput(string prompt)
        {
            while (true)
            {
                string input = GetInput($"{prompt} (Y/N)").ToUpper();
                if (input == "Y" || input == "YES") return true;
                if (input == "N" || input == "NO")  return false;
                ShowError("❌ Please type Y (yes) or N (no).");
            }
        }

        public static void ShowMenu(string title, params string[] options)
        {
            ShowHeader(title);
            Console.WriteLine();
            for (int i = 0; i < options.Length; i++)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write($"  {i + 1}. ");
                Console.ResetColor();
                Console.WriteLine(options[i]);
            }
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"  0. Back / Exit");
            Console.ResetColor();
        }

        public static void WaitForKey(string message = "\nPress any key to continue...")
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(message);
            Console.ResetColor();
            Console.ReadKey(true);
        }

        public static void ShowDivider()
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("════════════════════════════════════════");
            Console.ResetColor();
        }

        public static void ShowLoading(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"\n⏳ {message}");
            for (int i = 0; i < 3; i++)
            {
                Thread.Sleep(200);
                Console.Write(".");
            }
            Console.WriteLine();
            Console.ResetColor();
        }
    }
}
