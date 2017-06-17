using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks.Dataflow;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace SteamCallback
{
    static class Backend
    {
        private static Process cmd;

        private static Dictionary<int, bool> stateDict = new Dictionary<int, bool>();

        private static Dictionary<int, bool> updateDict = new Dictionary<int, bool>();

        private static DateTime startTime = DateTime.Now;

        private static string steamPath;

        private static bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        private static ActionBlock<string> processor = new ActionBlock<string>(s =>
            {
                try
                {
                    // arguments parsing
                    var time = ParseRegexEx(s, @"\[(.*)\]", DateTime.Parse);
                    var appid = ParseRegexEx(s, @"AppID (\d+)", Convert.ToInt32);
                    var type = ParseRegex(s, @"(\w+) changed");
                    var state = ParseRegex(s, @"changed : (.*)");

                    var states = state.Trim().Split(new char[] { ',' });

                    HandleUpdate(time, appid, type, states);

                }
                catch (FormatException)
                {
                    return;
                }

            });

        private static void HandleUpdate(DateTime time, int appid, string type, string[] states)
        {
            bool running;
            bool pre_running;
            Action<int, DateTime> startEvent;
            Action<int, DateTime> endEvent;
            Dictionary<int, bool> dict;
            string stateStr;


            switch (type)
            {
                case "update":
                    dict = updateDict;
                    startEvent = Callbacks.TriggerAppUpdateStarted;
                    endEvent = Callbacks.TriggerAppUpdateEnded;
                    stateStr = "Running";
                    break;

                case "state":
                    dict = stateDict;
                    startEvent = Callbacks.TriggerAppStarted;
                    endEvent = Callbacks.TriggerAppEnded;
                    stateStr = "App Running";
                    break;

                default:
                    return;
            }

            running = states.Any(t => t == stateStr);
            if (!dict.ContainsKey(appid))
            {
                dict.Add(appid, running);
                pre_running = false;
            }
            else
            {
                pre_running = dict[appid];
                dict[appid] = running;
            }

            if (running != pre_running && time > startTime)
            {
                if (running)
                {
                    startEvent(appid, time);
                }
                else
                {
                    endEvent(appid, time);
                }
            }
        }

        private static string ParseRegex(string source, string pattern)
        {
            string pat = @"\[(.*)\]";
            var m = Regex.Match(source, pat);

            if (m.Length == 0)
            {
                throw new FormatException();
            }

            var result = m.Groups[1].Value;
            return result;
        }

        private static T ParseRegexEx<T>(string source, string pattern, Func<string, T> func)
        {
            string pat = @"\[(.*)\]";
            var m = Regex.Match(source, pat);

            if (m.Length == 0)
            {
                throw new FormatException();
            }

            var result = func(m.Groups[1].Value);
            return result;
        }

        internal static void Init()
        {
            if (steamPath == null)
            {
                InitSteamPath();
            }

            var steamLogPath = isWindows ? $"{steamPath}\\logs\\content_log.txt" : $"{steamPath}/logs/content_log.txt";

            if (!File.Exists(steamLogPath))
            {
                var stream = File.Create(steamLogPath);
                stream.Dispose();
            }

            if (cmd == null)
            {
                cmd = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = isWindows ? "cmd.exe" : "bash";
                startInfo.Arguments = isWindows ? $"/c tail -f \"{steamLogPath}\"" : $"-c \"tail -f {steamLogPath}\"";
                startInfo.WorkingDirectory = Directory.GetCurrentDirectory();
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardInput = true;
                startInfo.RedirectStandardOutput = true;
                startInfo.CreateNoWindow = true;
                cmd.OutputDataReceived += new DataReceivedEventHandler(Data_Received);
                cmd.EnableRaisingEvents = true;
                cmd.Exited += Cmd_Exited;
                cmd.StartInfo = startInfo;
                cmd.Start();
                cmd.BeginOutputReadLine();
            }
        }

        internal static void End()
        {
            if (cmd != null)
            {
                cmd.Exited -= Cmd_Exited;
                cmd.Kill();
            }
        }

        private static void Cmd_Exited(object sender, EventArgs e)
        {
            cmd.OutputDataReceived -= Data_Received;
            cmd.Exited -= Cmd_Exited;
            cmd = null;

            var steamLogPath = isWindows ? $"{steamPath}\\logs\\content_log.txt" : $"{steamPath}/logs/content_log.txt";

            if (!File.Exists(steamLogPath))
            {
                var stream = File.Create(steamLogPath);
                stream.Dispose();
            }

            Init();
        }

        private static void Data_Received(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                processor.Post(e.Data);
            }
        }

        private static void InitSteamPath()
        {
            if (isWindows)
            {
                RegistryKey Key;
                Key = Registry.CurrentUser;
                var myreg = Key.OpenSubKey("Software\\Valve\\Steam");
                steamPath = myreg.GetValue("SteamPath").ToString().Replace('/', '\\');
                myreg.Dispose();
            }
            else
            {
                steamPath = "~/.local/share/Steam";
            }
        }
    }
}
