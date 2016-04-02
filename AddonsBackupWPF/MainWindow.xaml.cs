using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AddonsBackupWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Backup _backup = new Backup();

        public MainWindow()
        {
            InitializeComponent();

            backupDir.Text = Properties.Settings.Default.BackupDir;
            warcraftDir.Text = Properties.Settings.Default.WarcraftDir;
            uiName.Text = Properties.Settings.Default.InterfaceName;
            uiRevision.Text = Properties.Settings.Default.InterfaceNumber;
        }

        #region Interactives

        private void backupDirBrowse_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.Title = "My Title";
            dialog.IsFolderPicker = true;
            dialog.InitialDirectory = backupDir.Text;

            dialog.AddToMostRecentlyUsedList = false;
            dialog.AllowNonFileSystemItems = false;
            dialog.DefaultDirectory = backupDir.Text;
            dialog.EnsurePathExists = true;
            dialog.EnsureReadOnly = false;
            dialog.EnsureValidNames = true;
            dialog.Multiselect = false;
            dialog.ShowPlacesList = true;

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                backupDir.Text = dialog.FileName;
                //var folder = dlg.FileName;
                // Do something with selected folder string
            }
        }

        private void warcraftDirBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog()
            {
                Filter = "Warcraft Executable|wow.exe;wow-64.exe",
                InitialDirectory = warcraftDir.Text
            };
            if (openFileDialog.ShowDialog() == true)
                warcraftDir.Text = System.IO.Path.GetDirectoryName(openFileDialog.FileName);
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            statusText.Text = "Starting backup";
            logText.Text = "";

            #region Clean names
            string pathRegexSearch = new string(System.IO.Path.GetInvalidPathChars());
            string fileRegexSearch = new string(System.IO.Path.GetInvalidFileNameChars());
            Regex pathRegex = new Regex(string.Format("[{0}]", Regex.Escape(pathRegexSearch)));
            Regex fileRegex = new Regex(string.Format("[{0}]", Regex.Escape(fileRegexSearch)));

            ProcessInfo details = new ProcessInfo()
            {
                SourceDirectory = pathRegex.Replace(warcraftDir.Text, ""),
                DestinationDirectory = pathRegex.Replace(backupDir.Text, ""),
                InterfaceName = fileRegex.Replace(uiName.Text, ""),
                InterfaceVersion = fileRegex.Replace(uiRevision.Text, ""),
                DesiredSubDirectories = new List<string>()
                {
                    @"\Interface",
                    @"\WTF"
                }
            };
            #endregion

            Logger logger = _backup.VerifyInput(details);

            logText.AppendText("Backing up from " + details.SourceDirectory + Environment.NewLine);
            logText.AppendText("Backing up folders " + string.Join(", ", details.DesiredSubDirectories) + Environment.NewLine);
            logText.AppendText("Backing up to " + details.BackupPath + Environment.NewLine);

            StringBuilder sb = new StringBuilder();
            foreach (Log log in logger.Logs)
            {
                switch (log.ErrorType)
                {
                    case LogType.Error:
                        sb.Append("Error: ");
                        break;
                    case LogType.Warning:
                        sb.Append("Warning: ");
                        break;
                    case LogType.Info:
                        sb.Append("Information: ");
                        break;
                    default:
                        break;
                }
                sb.Append(log.Message);
                sb.Append("\n");
                logText.AppendText(sb.ToString());
            }

            if (logger.hasErrors)
            {
                statusText.Text = "Error occured, check log.";
                return;
            }

            RunBackupBackgroundWorker(details);
        }

        private void configSave_Click(object sender, RoutedEventArgs e)
        {
            SaveProperties();
        }

        #endregion

        private void SaveProperties()
        {
            Properties.Settings.Default.BackupDir = backupDir.Text;
            Properties.Settings.Default.WarcraftDir = warcraftDir.Text;
            Properties.Settings.Default.InterfaceName = uiName.Text;
            Properties.Settings.Default.InterfaceNumber = uiRevision.Text;

            Properties.Settings.Default.Save();

            statusText.Text = "Saved!";
        }

        private void RunBackupBackgroundWorker(ProcessInfo details)
        {
            try
            {
                BackgroundWorker bw = new BackgroundWorker();

                bw.WorkerReportsProgress = true;

                // what to do in the background thread
                bw.DoWork += new DoWorkEventHandler(
                delegate (object o, DoWorkEventArgs args)
                {
                    BackgroundWorker b = o as BackgroundWorker;

                    _backup.BackupTask(b, details);
                });

                // what to do when progress changed (update the progress bar for example)
                bw.ProgressChanged += new ProgressChangedEventHandler(
                delegate (object o, ProgressChangedEventArgs args)
                {
                    statusText.Text = string.Format("{0}% Completed - {1}", args.ProgressPercentage, args.UserState);
                    progressBar.Value = args.ProgressPercentage;
                });

                // what to do when worker completes its task (notify the user)
                bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(
                delegate (object o, RunWorkerCompletedEventArgs args)
                {
                    if (args.Error == null)
                    {
                        statusText.Text = "Finished!";
                    }
                    else
                    {
                        statusText.Text = "Error Occurred :(";
                        logText.AppendText("FATAL ERROR: " + args.Error.ToString() + Environment.NewLine);
                    }
                });

                bw.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                statusText.Text = "Error Occurred :(";
                logText.AppendText("FATAL ERROR: " + ex.ToString() + Environment.NewLine);
            }
        }
    }
}
