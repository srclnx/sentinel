namespace Sentinel.Logs
{
    using System;
    using System.IO;
    using System.Text;

    using Sentinel.Logs.Interfaces;
    using Sentinel.Views.Interfaces;

    public class LogFileExporter : ILogFileExporter
    {
        public LogFileExporter()
        {
        }

        public void SaveLogViewerToFile(IWindowFrame windowFrame, string filePath)
        {
            if (windowFrame == null)
            {
                throw new ArgumentNullException(nameof(windowFrame));
            }

            // Check if file exists; delete it
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            using (var fs = File.Create(filePath))
            {
                var messages = windowFrame.PrimaryView.Messages;
                lock (messages)
                {
                    foreach (var msg in messages)
                    {
                        AddText(
                            fs,
                            $"{msg.DateTime.ToString("yyyy-MM-dd HH:mm:ss.ffff")}|{msg.Type}|{msg.System}|{msg.Description}\r\n");
                    }
                }
            }
        }

        private static void AddText(FileStream fs, string value)
        {
            byte[] info = new UTF8Encoding(true).GetBytes(value);
            fs.Write(info, 0, info.Length);
        }
    }
}