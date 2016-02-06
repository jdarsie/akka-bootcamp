using System;
using System.IO;
using System.Text;
using Akka.Actor;

namespace WinTail
{
    public class TailActor : UntypedActor
    {
        private readonly StreamReader _fileStreamReader;
        private readonly IActorRef _reporterActor;

        public TailActor(IActorRef reporterActor, string filePath)
        {
            if (reporterActor == null)
            {
                throw new ArgumentNullException(nameof(reporterActor));
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            _reporterActor = reporterActor;

            // start watching file for changes
            var observer = new FileObserver(Self, Path.GetFullPath(filePath));
            observer.Start();

            // open the file stream with shared read/write permissions (so file can be written to while open)
            var fileStream = new FileStream(Path.GetFullPath(filePath), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            _fileStreamReader = new StreamReader(fileStream, Encoding.UTF8);

            // read the initial contents of the file and send it to console as first msg
            var text = _fileStreamReader.ReadToEnd();

            Self.Tell(new InitialRead(filePath, text));
        }

        protected override void OnReceive(object message)
        {
            if (message is FileWrite)
            {
                // move file cursor forward
                // pull results from cursor to end of file and write to output
                // (this is assuming a log file type format that is append-only)
                var text = _fileStreamReader.ReadToEnd();
                if (!string.IsNullOrEmpty(text))
                {
                    _reporterActor.Tell(text);
                }
            }
            else if (message is FileError)
            {
                var fe = (FileError) message;
                _reporterActor.Tell($"Tail error: {fe.Reason}");
            }
            else if (message is InitialRead)
            {
                var ir = (InitialRead) message;
                _reporterActor.Tell(ir.Text);
            }
        }

        #region Message types

        public class FileWrite
        {
            public string FileName { get; private set; }

            public FileWrite(string fileName)
            {
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    throw new ArgumentNullException(nameof(fileName));
                }
                FileName = fileName;
            }
        }

        public class FileError
        {
            public string FileName { get; private set; }

            public string Reason { get; }

            public FileError(string fileName, string reason)
            {
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    throw new ArgumentNullException(nameof(fileName));
                }
                if (string.IsNullOrWhiteSpace(reason))
                {
                    throw new ArgumentNullException(nameof(reason));
                }

                FileName = fileName;
                Reason = reason;
            }
        }

        public class InitialRead
        {
            public string FileName { get; private set; }
            public string Text { get; }

            public InitialRead(string fileName, string text)
            {
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    throw new ArgumentNullException(nameof(fileName));
                }
                if (string.IsNullOrWhiteSpace(text))
                {
                    throw new ArgumentNullException(nameof(text));
                }

                FileName = fileName;
                Text = text;
            }
        }

        #endregion
    }
}