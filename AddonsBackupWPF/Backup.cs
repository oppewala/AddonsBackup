using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AddonsBackupWPF
{
    public class ProcessInfo
    {
        public string SourceDirectory { get; set; }
        public string DestinationDirectory { get; set; }
        public string InterfaceName { get; set; }
        public string InterfaceVersion { get; set; }

        public List<string> DesiredSubDirectories { get; set; }

        private DateTime Created { get; set; }
        public string BackupPath
        {
            get
            {
                string dateTimeString = Created.ToString("dd.MM.yyyy_HH.mm");

                return DestinationDirectory + @"\" + InterfaceName + @"\" + InterfaceVersion + " - " + dateTimeString;
            }
        }

        public ProcessInfo()
        {
            Created = DateTime.Now;
        }
    }

    public class Logger
    {
        public List<Log> Logs { get; set; }

        public bool hasErrors
        {
            get
            {
                return Logs.Find(l => l.ErrorType == LogType.Error) != null;
            }
        }
        public bool hasWarnings
        {
            get
            {
                return Logs.Find(l => l.ErrorType == LogType.Warning) != null;
            }
        }

        public Logger()
        {
            Logs = new List<Log>();
        }

        public void LogError(LogType logType, string message)
        {
            Logs.Add(new Log()
            {
                ErrorType = logType,
                Message = message
            });
        }
    }

    public class Log
    {
        public LogType ErrorType { get; set; }
        public string Message { get; set; }
    }

    public enum LogType
    {
        Error,
        Warning,
        Info
    }

    class Backup
    {
        public Logger VerifyInput(ProcessInfo details)
        {
            Logger results = new Logger();

            if (!File.Exists(details.SourceDirectory + @"\wow.exe") && !File.Exists(details.SourceDirectory + @"\wow-64.exe"))
            {
                results.LogError(LogType.Error, "Warcraft executable not found. Please verify warcraft directory is correct.");
            }
            if (!Directory.Exists(details.DestinationDirectory))
            {
                results.LogError(LogType.Error, "Backup directory not found. Please verify backup directory is correct.");
            }
            if (string.IsNullOrWhiteSpace(details.InterfaceName))
            {
                results.LogError(LogType.Warning, "No interface name specified.");
                details.InterfaceName = string.Empty;
            }
            if (string.IsNullOrWhiteSpace(details.InterfaceVersion))
            {
                results.LogError(LogType.Warning, "No revision number specified.");
                details.InterfaceVersion = string.Empty;
            }
            if (details.DesiredSubDirectories.Count == 2)
            {
                results.LogError(LogType.Info, "Only backing up default folders.");
            }

            return results;
        }

        public void BackupTask(BackgroundWorker b, ProcessInfo details)
        {
            BackupFiles(b, details);

            CleanupDirectory(details);
        }

        private void BackupFiles(BackgroundWorker b, ProcessInfo details)
        {
            CreateDirectories(details);
            b.ReportProgress(5, "Main Directories Created");

            int subDirCounter = 0;
            foreach (string subDirectory in details.DesiredSubDirectories)
            {
                subDirCounter++;
                int counter = 0;
                string subDirectoryPath = details.SourceDirectory + subDirectory;

                string[] dirs = Directory.GetDirectories(subDirectoryPath, "*", SearchOption.AllDirectories);
                int total = dirs.Count();
                counter = 0;
                foreach (string dir in dirs)
                {
                    counter++;
                    ProgressTracker(b, counter, total, subDirCounter, details.DesiredSubDirectories.Count, 1);
                    Directory.CreateDirectory(dir.Replace(details.SourceDirectory, details.BackupPath));
                }
                Console.WriteLine("\n");

                string[] files = Directory.GetFiles(subDirectoryPath, "*.*", SearchOption.AllDirectories);
                total = files.Count();
                counter = 0;
                foreach (string file in files)
                {
                    counter++;
                    ProgressTracker(b, counter, total, subDirCounter, details.DesiredSubDirectories.Count, 2);
                    File.Copy(file, file.Replace(details.SourceDirectory, details.BackupPath));
                }
            }
        }

        private void ProgressTracker(BackgroundWorker b, int counter, int total, int parentCounter, int parentTotal, int stage)
        {
            decimal currentTaskProgress = (((decimal)counter / (decimal)total) * 100m);
            if (stage == 1)
            {
                currentTaskProgress = currentTaskProgress / 2;
            }
            else if (stage == 2)
            {
                currentTaskProgress = (currentTaskProgress / 2) + 50;
            }

            int progressPercent = (int)Math.Round(((currentTaskProgress / parentTotal * parentCounter) * 0.95m) + 5);

            string message = "Creating Directory " + counter + " out of " + total;
            if (stage == 2)
            {
                message = "Copying File " + counter + " out of " + total;
            }
            b.ReportProgress(progressPercent, message);
        }

        private void CreateDirectories(ProcessInfo details)
        {
            List<string> paths = new List<string>()
            {
                details.BackupPath
            };
            foreach (string subDirectory in details.DesiredSubDirectories)
            {
                paths.Add(details.BackupPath + subDirectory);
            }
            foreach (string path in paths)
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
        }

        private void CleanupDirectory(ProcessInfo details)
        {

        }
    }


}
