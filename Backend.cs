using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks.Dataflow;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;

namespace SteamCallback
{
    static class Backend
    {
        private static Process cmd;

        private static Dictionary<int, bool> stateDict = new Dictionary<int, bool>();

        private static Dictionary<int, bool> updateDict = new Dictionary<int, bool>();

        private static DateTime startTime = DateTime.Now;

        private static ActionBlock<string> processor = new ActionBlock<string>(s =>
        {
            // time parsing
            string pat = @"\[(.*)\]";
            var m = Regex.Match(s, pat);

            if (m.Length == 0)
            {
                return;
            }

            var time = DateTime.Parse(m.Groups[1].Value);

            // appid parsing
            pat = @"AppID (\d+)";
            m = Regex.Match(s, pat);

            if (m.Length == 0)
            {
                return;
            }

            var appid = Convert.ToInt32(m.Groups[1].Value);

            // update/state switch
            pat = @"(\w+) changed";
            m = Regex.Match(s, pat);

            if (m.Length == 0)
            {
                return;
            }

            var type = m.Groups[1].Value;

            // state string
            pat = @"changed : (.*)";
            m = Regex.Match(s, pat);

            if (m.Length == 0)
            {
                return;
            }

            var state = m.Groups[1].Value;

            var states = state.Trim().Split(new char[] { ',' });

            bool running;
            bool pre_running;

            if (type == "update")
            {
                running = states.Any(t => t == "Running");
                if (!updateDict.ContainsKey(appid))
                {
                    updateDict.Add(appid, running);
                    pre_running = false;
                }
                else
                {
                    pre_running = updateDict[appid];
                    updateDict[appid] = running;
                }

                if (running != pre_running && time > startTime)
                {
                    if (running)
                    {
                        Callbacks.TriggerAppUpdateStarted(appid, time);
                    }
                    else
                    {
                        Callbacks.TriggerAppUpdateEnded(appid, time);
                    }
                }
            }
            else if (type == "state")
            {
                running = states.Any(t => t == "App Running");
                if (!stateDict.ContainsKey(appid))
                {
                    stateDict.Add(appid, running);
                    pre_running = false;
                }
                else
                {
                    pre_running = stateDict[appid];
                    stateDict[appid] = running;
                }

                if (running != pre_running && time > startTime)
                {
                    if (running)
                    {
                        Callbacks.TriggerAppStarted(appid, time);
                    }
                    else
                    {
                        Callbacks.TriggerAppEnded(appid, time);
                    }
                }
            }

        });

        internal static void Init()
        {
            if (cmd == null)
            {
                ///TODO: Add support for Linux/OSX
                cmd = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = "/c " + "tail -f \"C:\\Program Files (x86)\\Steam\\logs\\content_log.txt\"";
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
            Init();
        }

        private static void Data_Received(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                processor.Post(e.Data);
            }
        }
    }
}
