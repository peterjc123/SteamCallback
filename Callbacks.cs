using System;

namespace SteamCallback
{
    public static class Callbacks
    {
        public static event Action<int, DateTime> AppStarted;
        public static event Action<int, DateTime> AppEnded;

        public static event Action<int, DateTime> AppUpdateStarted;
        public static event Action<int, DateTime> AppUpdateEnded;

        static Callbacks()
        {
            Backend.Init();
        }

        internal static void TriggerAppUpdateStarted(int appid, DateTime time)
        {
            AppUpdateStarted(appid, time);
        }

        internal static void TriggerAppUpdateEnded(int appid, DateTime time)
        {
            AppUpdateEnded(appid, time);
        }

        internal static void TriggerAppStarted(int appid, DateTime time)
        {
            AppStarted(appid, time);
        }

        internal static void TriggerAppEnded(int appid, DateTime time)
        {
            AppEnded(appid, time);
        }

        public static void Remove()
        {
            Backend.End();
        }
    }
}
