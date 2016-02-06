using System;
using System.IO;
using Akka.Actor;

namespace WinTail
{
    public class FileObserver : IDisposable
    {
        private readonly string _fileDir;
        private readonly string _fileNameOnly;
        private readonly IActorRef _tailActor;
        private FileSystemWatcher _watcher;

        public FileObserver(IActorRef tailActor, string absoluteFilePath)
        {
            if (tailActor == null)
            {
                throw new ArgumentNullException(nameof(tailActor));
            }
            if (string.IsNullOrWhiteSpace(absoluteFilePath))
            {
                throw new ArgumentNullException(nameof(absoluteFilePath));
            }

            _tailActor = tailActor;

            _fileDir = Path.GetDirectoryName(absoluteFilePath);
            _fileNameOnly = Path.GetFileName(absoluteFilePath);
        }

        public void Dispose()
        {
            _watcher.Dispose();
        }

        public void Start()
        {
            _watcher = new FileSystemWatcher(_fileDir, _fileNameOnly)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite
            };

            _watcher.Changed += OnFileChanged;
            _watcher.Error += OnFileError;
            _watcher.EnableRaisingEvents = true;
        }

        private void OnFileError(object sender, ErrorEventArgs e)
        {
            _tailActor.Tell(new TailActor.FileError(_fileNameOnly, e.GetException().Message), ActorRefs.NoSender);
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Changed)
            {
                // here we use a special ActorRefs.NoSender since this event can happen many times,
                // this is a little microoptimization
                _tailActor.Tell(new TailActor.FileWrite(e.Name), ActorRefs.NoSender);
            }
        }
    }
}