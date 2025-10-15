using System;
using System.IO;
using System.Runtime.InteropServices;
using Decal.Adapter;

namespace Commander.Lib.Common
{
    public interface Logger
    {
        void Info(string message);
        void Error(Exception ex);
        void Warn(string message);
        void WriteToChat(string message);
        void Think(string message, string name);
        void WriteToWindow(string message);
        Logger Scope(string scope);
    }

    public class LoggerImpl : Logger
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetWindowText(IntPtr hwnd, string lpString);

        [DllImport("Decal.dll")]
        private static extern int DispatchOnChatCommand(ref IntPtr str, [MarshalAs(UnmanagedType.U4)] int target);



        private GlobalProvider _globals;
        private string _scope;
        private Logger _instance;

        private static bool Decal_DispatchOnChatCommand(string cmd)
        {
            IntPtr bstr = Marshal.StringToBSTR(cmd);

            try
            {
                bool eaten = (DispatchOnChatCommand(ref bstr, 1) & 0x1) > 0;

                return eaten;
            }
            finally
            {
                Marshal.FreeBSTR(bstr);
            }
        }

        /// <summary>
        /// This will first attempt to send the messages to all plugins. If no plugins set e.Eat to true on the message, it will then simply call InvokeChatParser.
        /// </summary>
        /// <param name="cmd"></param>
        public static void DispatchChatToBoxWithPluginIntercept(string cmd)
        {
            if (!Decal_DispatchOnChatCommand(cmd))
                CoreManager.Current.Actions.InvokeChatParser(cmd);
        }


        public LoggerImpl(GlobalProvider globals)
        {
            _instance = this;
            _globals = globals;
            _scope = "Default";
        }

        public void Think(string message, string name)
        {
            try
            {
                DispatchChatToBoxWithPluginIntercept(string.Format("/tell {0}, {1}", name, message));
            }
            catch (Exception ex) { Error(ex); }
        }

        public void WriteToWindow(string message)
        {
            IntPtr hwnd = _globals.Host.Decal.Hwnd;
            SetWindowText(hwnd, message);
        }

        public void Info(string message)
        {
            Console.WriteLine(Message(message, "INFO"));
        }

        public void Warn(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow; 
            Console.WriteLine(Message(message, "WARN"));   
            Console.ResetColor();                         
        }

        private string Message(string message, string level)
        {
            return $@"{((level == "IN-GAME") ? String.Empty : DateTime.Now.ToString())}-[{level}]-[{_scope}]: {message}";
        }

        private void ErrorMessage(StreamWriter writer, string message)
        {
            Info(Message(message, "ERROR"));
            writer.WriteLine(message);
        }

        public void Error(Exception e)
        {
            try
            {
                string pluginPath = _globals.PluginPath;
                using (StreamWriter writer = new StreamWriter($@"{pluginPath}\errors.txt", true))
                {
                    ErrorMessage(writer, ("================================="));
                    ErrorMessage(writer, DateTime.Now.ToString());
                    ErrorMessage(writer, "Error: " + e.Message);
                    ErrorMessage(writer, "Source: " + e.Source);
                    ErrorMessage(writer, "Stack: " + e.StackTrace);
                    if (e.InnerException != null)
                    {
                        ErrorMessage(writer, "Inner: " + e.InnerException.Message);
                        ErrorMessage(writer, "Inner Stack: " + e.InnerException.StackTrace);
                    }
                    ErrorMessage(writer, "=================================");
                    ErrorMessage(writer, "");
                    writer.Close();
                }
            } catch (IOException ex) { }
        }

        public void WriteToChat(string message)
        {
            Info(message);
            message = Message(message, "IN-GAME");
            _globals.Host.Actions.AddChatText(message, 5);
        }

        public Logger Scope(string scope)
        {
            _scope = scope;
            return _instance;
        }
    }
}
