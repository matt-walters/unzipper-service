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
        private readonly string sourceFolder;
        private readonly string destinationFolder;
        private readonly int workerTaskCount;

        private const string filter = "*.zip";

        private FileSystemWatcher fileSystemWatcher;
        private BlockingCollection<string> filesToUnzip;

        private CancellationTokenSource cancellationTokenSource;
        private CancellationToken cancellationToken;

        private List<Task> unzipperTasks = new List<Task>();

        public UnzipperService(string sourceFolder, string destinationFolder, int workerTaskCount)
        {
            this.sourceFolder = sourceFolder;
            this.destinationFolder = destinationFolder;
            this.workerTaskCount = workerTaskCount;
        }

        public bool Start()
        {
            filesToUnzip = new BlockingCollection<string>();

            fileSystemWatcher = new FileSystemWatcher(sourceFolder, filter);
            fileSystemWatcher.Created += FileSystemWatcher_Created;
            fileSystemWatcher.EnableRaisingEvents = true;
            fileSystemWatcher.IncludeSubdirectories = false;

            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;

            for (int i = 0; i < workerTaskCount; i++)
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
            var destinationFilePath = Path.Combine(destinationFolder, Path.GetFileNameWithoutExtension(fullPath));

            WaitUntilFileIsUnlocked(fullPath);

            try
            {
                ZipFile.ExtractToDirectory(fullPath, destinationFilePath);

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
