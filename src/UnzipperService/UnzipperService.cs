using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace UnzipperService
{
    class UnzipperService
    {
        private const string filter = "*.zip";

        private readonly UnzipperConfig config;
        private FileSystemWatcher fileSystemWatcher;
        private BlockingCollection<string> filesToUnzip;

        private CancellationTokenSource cancellationTokenSource;
        private CancellationToken cancellationToken;

        private readonly List<Task> unzipperTasks = new List<Task>();

        public UnzipperService(UnzipperConfig config)
        {
            this.config = config;
        }

        public bool Start()
        {
            filesToUnzip = new BlockingCollection<string>();

            fileSystemWatcher = new FileSystemWatcher(config.SourceFolder, filter);
            fileSystemWatcher.Created += FileSystemWatcher_Created;
            fileSystemWatcher.EnableRaisingEvents = true;
            fileSystemWatcher.IncludeSubdirectories = false;

            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;

            for (int i = 0; i < config.WorkerTaskCount; i++)
            {
                unzipperTasks.Add(
                    Task.Factory.StartNew(() => UnzipperWorker(), cancellationToken));
            }

            return true;
        }

        public bool Stop()
        {
            cancellationTokenSource.Cancel();
            fileSystemWatcher.Dispose();

            return true;
        }

        private void UnzipperWorker()
        {
            while (true)
            {
                try
                {
                    var fullPath = filesToUnzip.Take(cancellationToken);
                    UnzipFile(fullPath);
                }
                catch(OperationCanceledException)
                {
                    break;
                }
            }
        }

        private void FileSystemWatcher_Created(object sender, FileSystemEventArgs e)
        {
            filesToUnzip.Add(e.FullPath);
            Log($"Files to unzip contains {filesToUnzip.Count} items");
        }

        private void UnzipFile(string fullPath)
        {
            var name = Path.GetFileName(fullPath);
            var destinationFilePath = Path.Combine(config.DestinationFolder, Path.GetFileNameWithoutExtension(fullPath));

            WaitUntilFileIsUnlocked(fullPath);

            try
            {
                ZipFile.ExtractToDirectory(fullPath, destinationFilePath);

                if (config.DeleteFileAfterUnzip)
                {
                    File.Delete(fullPath);
                }

                Log($"Unzipped {name} ({filesToUnzip.Count} files remaining)");
            }
            catch (Exception ex)
            {
                Log($"Failed to unzip {name}. Reason: {ex.Message}");
            }
        }

        private void WaitUntilFileIsUnlocked(string fileName)
        {
            while(IsFileLocked(fileName))
            {
                cancellationToken.ThrowIfCancellationRequested();
                Thread.Sleep(500);
            }
        }

        private bool IsFileLocked(string fileName)
        {
            var file = new FileInfo(fileName);

            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                return true;
            }
            finally
            {
                stream?.Close();
            }

            return false;
        }

        private void Log(string message)
        {
            lock (Console.Out)
            {
                Console.WriteLine($"{DateTime.Now.ToString("u")}: {message}");
            }
        }
    }
}
