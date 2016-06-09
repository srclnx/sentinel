namespace Sentinel.FileMonitor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;

    using Common.Logging;

    using Sentinel.FileMonitor.Support;

    public static class LogMessageEntryConsumer
    {
        private static readonly ILog Log = LogManager.GetLogger(nameof(LogMessageEntryConsumer));

        public static IEnumerable<LogMessage> GetMessages(TextReader inputStream, Regex messageStartPattern)
        {
            if (inputStream == null)
            {
                throw new ArgumentNullException(nameof(inputStream));
            }

            if (messageStartPattern == null)
            {
                throw new ArgumentNullException(nameof(messageStartPattern));
            }

            var messages = new List<LogMessage>();
            var peekingReader = new PeekLineTextReader(inputStream);

            for (var line = peekingReader.ReadLine(); line != null; line = peekingReader.ReadLine())
            {
                if (!messageStartPattern.IsMatch(line))
                {
                    Log.Warn($"Expecting something that matches the pattern, but got {line}");
                }
                else
                {
                    var message = new LogMessage
                                      {
                                          Entry = line
                                      };

                    List<string> extras = null;

                    // See if any extra, peek until a line is found matching against the pattern.
                    // Any lines not matching the pattern should be added to the .Extra property of
                    // the current message.
                    for (var nextLine = peekingReader.PeekLine(); nextLine != null; nextLine = peekingReader.PeekLine())
                    {
                        if (messageStartPattern.IsMatch(nextLine))
                        {
                            break;
                        }

                        if (extras == null)
                        {
                            extras = new List<string>();
                        }

                        extras.Add(nextLine);
                    }

                    message.Extra = extras;
                    messages.Add(message);
                }
            }

            Log.Debug($"Found {messages.Count} message(s)");
            return messages;
        }
    }
}