using UnityEngine;

namespace ModUtils {
    /// <summary>
    /// Static Logger class for to log things with. Now includes pretty colouring options!
    /// </summary>
    public static class ModLogger
    {
        private static string _title = "ModLogger";

        /// <summary>
        /// The title to use for the log
        /// </summary>
        public static string Title
        {
            get { return _title; }
            set
            {
                Log($"Log title changed to {value}", XKCDColors.GreenishBeige);
                _title = value;
            }
        }

        public static void Log(string message) {
            Debug.Log($"[{Title}]: {message}");
        }

        public static void Log(string message, Color color) {
            Log($"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{message}</color>");
        }
    }
}
