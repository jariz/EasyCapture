using System;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace EasyCapture
{
    /// <summary>
    /// JConsole V2 (after 5 years, there's finally a V2)
    /// Uses part of Google's API Samples 'CommandLine' class.
    /// </summary>
    public class Out
    {
        public static void WriteLine(string logText)
        {
            DateTime _DTN = DateTime.Now;
            StackFrame _SF = new StackTrace().GetFrame(1);

            WriteBlank("^9[{0}:{1}] [^2{2}.{3}^9] » ^4{4}^1", _DTN.ToLongTimeString(), _DTN.Millisecond.ToString(), _SF.GetMethod().ReflectedType.Name, _SF.GetMethod().Name, logText);
        }

        public static void WriteDebug(string logText)
        {
            DateTime _DTN = DateTime.Now;
            StackFrame _SF = new StackTrace().GetFrame(1);

            WriteBlank("^9[{0}:{1}] [^2{2}.{3}^9] » ^8{4}^1", _DTN.ToLongTimeString(), _DTN.Millisecond.ToString(), _SF.GetMethod().ReflectedType.Name, _SF.GetMethod().Name, logText);
        }

        public static void WriteError(string logText)
        {
            DateTime _DTN = DateTime.Now;
            StackFrame _SF = new StackTrace().GetFrame(1);

            WriteBlank("^9[{0}:{1}] [^2{2}.{3}^9] » ^6{4}^1", _DTN.ToLongTimeString(), _DTN.Millisecond.ToString(), _SF.GetMethod().ReflectedType.Name, _SF.GetMethod().Name, logText);
        }

        /// <summary>
        /// Writes a plain text line.
        /// </summary>
        /// <param name="logText">The log line to be printed.</param>
        public static void WritePlain(string logText)
        {
            WriteBlank("^9" + logText);
        }

        /// <summary>
        /// Writes the specified text to the console
        /// Applies special color filters (^0, ^1, ...)
        /// </summary>
        public static void WriteBlank(string format)
        {
            Write(format + Environment.NewLine, false, new object[] { });
        }

        /// <summary>
        /// Writes the specified text to the console
        /// Applies special color filters (^0, ^1, ...)
        /// </summary>
        public static void WriteBlank(string format, params object[] values)
        {
            Write(format + Environment.NewLine, false, values);
        }

        public static string Buffer
        {
            get
            {
                return _buffer;
            }
        }

        static string _buffer = "";

        /// <summary>
        /// Writes the specified text to the console
        /// Applies special color filters (^0, ^1, ...)
        /// </summary>
        public static void Write(string format, bool bypassBuffer, params object[] values)
        {
            string text;
            if (values.Length > 0)
                text = String.Format(format, values);
            else text = format;
            if(!bypassBuffer)
                _buffer += text;
            if (WINAPI.GetConsoleWindow() != IntPtr.Zero)
            {
                Console.ForegroundColor = ConsoleColor.Gray;

                // Replace ^1, ... color tags.
                while (text.Contains("^"))
                {
                    int index = text.IndexOf("^");

                    // Check if a number follows the index
                    if (index + 1 < text.Length && Char.IsDigit(text[index + 1]))
                    {
                        // Yes - it is a color notation
                        InternalWrite(text.Substring(0, index)); // Pre-Colornotation text
                        Console.ForegroundColor = (ConsoleColor)(text[index + 1] - '0' + 6);
                        text = text.Substring(index + 2); // Skip the two-char notation
                    }
                    else
                    {
                        // Skip ahead
                        InternalWrite(text.Substring(0, index));
                        text = text.Substring(index + 1);
                    }
                }

                // Write the remaining text
                InternalWrite(text);
            }
        }

        private static readonly Regex ColorRegex = new Regex("{([a-z]+)}", RegexOptions.Compiled);
        private static void InternalWrite(string text)
        {
            // Check for color tags.
            Match match;
            while ((match = ColorRegex.Match(text)).Success)
            {
                // Write the text before the tag.
                Console.Write(text.Substring(0, match.Index));

                // Change the color
                Console.ForegroundColor = GetColor(match.Groups[1].ToString());
                text = text.Substring(match.Index + match.Length);
            }

            // Write the remaining text.
            Console.Write(text);
        }

        private static ConsoleColor GetColor(string id)
        {
            return (ConsoleColor)Enum.Parse(typeof(ConsoleColor), id, true);
        }
     }
}
