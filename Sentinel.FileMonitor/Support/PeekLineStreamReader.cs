namespace Sentinel.FileMonitor.Support
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Wrapper class for StreamReader that allows peeking at the next line
    /// </summary>
    public class PeekLineStreamReader
    {
        public PeekLineStreamReader(TextReader source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            Source = source;
        }

        private TextReader Source { get; set; }

        private Queue<string> PrereadEntries { get; } = new Queue<string>();

        public string ReadLine()
        {
            if (PrereadEntries.Any())
            {
                return PrereadEntries.Dequeue();
            }

            return Source.ReadLine();
        }

        /// <summary>
        /// Gets the next line from the stream.  Will Always return the same one until a
        /// <see cref="ReadLine"/> is performed.
        /// </summary>
        /// <returns>Next unseen line from the stream (or first of the previously peeked)</returns>
        public string PeekNextLineOnly()
        {
            if (PrereadEntries.Any())
            {
                return PrereadEntries.Peek();
            }

            var line = ReadLine();
            PrereadEntries.Enqueue(line);
            return line;
        }

        /// <summary>
        /// Gets the next unseen line from the stream, repeated calls will keep working
        /// through the stream.  Entries will only be consumed when calling <see cref="ReadLine"/>
        /// </summary>
        /// <returns>Next unseen line from the stream</returns>
        public string PeekLine()
        {
            var line = Source.ReadLine();
            PrereadEntries.Enqueue(line);
            return line;
        }
    }
}