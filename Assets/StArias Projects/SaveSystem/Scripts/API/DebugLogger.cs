/*  
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.  
 * If a copy of the MPL was not distributed with this file, you can obtain one at  
 * https://mozilla.org/MPL/2.0/.  
 *  
 * Copyright (c) 2025 StArias - https://github.com/starias  
 */

using UnityEngine;

namespace StArias.API.SaveLoadSystem
{
    /// <summary>
    /// Debug colors for the log messages
    /// </summary>
    public enum DebugColor
    {
        Red = 0xFF0000,
        Green = 0x00FF00,
        Blue = 0x0000FF,
        Black = 0x000000,
        White = 0xFFFFFF,
        Yellow = 0xFFFF00,
        UnityDefault = 0xB8B8B8,
    }

    /// <summary>
    /// Provides a simple way to log messages with different colors
    /// </summary>
    public static class DebugLogger
    {
        /// <summary>
        /// Enable or disable the debug log
        /// </summary>
        public static bool EnableDebugLog = true;

        /// <summary>
        /// Given the <see cref="DebugColor"/> enum, it returns the corresponding color in HTML format"/>
        /// </summary>
        /// <param name="color">The color to be selected</param>
        /// <returns></returns>
        private static string SelectLogColor(DebugColor color)
        {
            string selectedColor = "<color=#B8B8B8>";

            switch (color)
            {
                case DebugColor.Red:
                    selectedColor = "<color=#FF0000>";
                    break;
                case DebugColor.Green:
                    selectedColor = "<color=#00FF00>";
                    break;
                case DebugColor.Blue:
                    selectedColor = "<color=#0000FF>";
                    break;
                case DebugColor.Black:
                    selectedColor = "<color=#000000>";
                    break;
                case DebugColor.White:
                    selectedColor = "<color=#FFFFFF>";
                    break;
                case DebugColor.Yellow:
                    selectedColor = "<color=#FFFF00>";
                    break;
                default:
                    break;
            }

            return selectedColor;
        }

        /// <summary>
        /// Logs a message with a specific color
        /// <para></para>
        /// It is possible to disable the debug log by setting <see cref="EnableDebugLog"/> to false
        /// <para></para>
        /// All the messages are prefixed with "SaveLoadManager - " by default
        /// </summary>
        /// <param name="message">The message to be displayed</param>
        /// <param name="color">The color of the message to be displayed</param>
        /// <param name="prefix">The string to be displayed at the start of the message</param>
        public static void Log(string message, DebugColor color = DebugColor.UnityDefault, string prefix = "SaveLoadManager - ")
        {
            if (!EnableDebugLog)
                return;

            string debugColor = SelectLogColor(color);
            Debug.Log($"{debugColor}{prefix}{message}</color>");
        }
    }
}