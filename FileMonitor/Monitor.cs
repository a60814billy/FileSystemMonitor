using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;

namespace FileMonitor
{
    public partial class Monitor : Form
    {
        private String directoryName = "";
        private Timer waitTimer = new Timer();
        private bool dirty = false;
        public Monitor()
        {
            InitializeComponent();
            initDirectoryWatcher();
            filesDataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            waitTimer.Interval = 3000;
            waitTimer.Tick += WaitTimeer_Tick;
        }

        private void WaitTimeer_Tick(object sender, EventArgs e)
        {
            waitTimer.Enabled = false;
            if (this.dirty)
            {
                this.listSubDirFileCounts();
            }
        }

        private void initDirectoryWatcher()
        {
            directoryWatcher.Created += onDirectoryChangeed;
            directoryWatcher.Changed += onDirectoryChangeed;
            directoryWatcher.Deleted += onDirectoryChangeed;
            directoryWatcher.Renamed += onDirectoryChangeed;
        }

        public void onDirectoryChangeed(object source, FileSystemEventArgs e)
        {
            if (this.dirty && this.waitTimer.Enabled) return;
            if (this.waitTimer.Enabled && !this.dirty)
            {
                this.dirty = true;
            }
            else
            {
                this.waitTimer.Enabled = true;
                this.listSubDirFileCounts();
            }
        }

        private void btn_browser_Click(object sender, EventArgs e)
        {
            DialogResult result = directoryBrowserDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                directoryName = directoryBrowserDialog.SelectedPath;
                directoryPathTextBox.Text = directoryName;
                this.listSubDirFileCounts();
                setFileSystemWatcher();
            }
        }

        private void setFileSystemWatcher()
        {
            if (directoryName != "")
            {
                directoryWatcher.Path = directoryName;
                directoryWatcher.NotifyFilter = System.IO.NotifyFilters.Attributes | System.IO.NotifyFilters.CreationTime | System.IO.NotifyFilters.DirectoryName | System.IO.NotifyFilters.FileName | System.IO.NotifyFilters.LastAccess | System.IO.NotifyFilters.LastWrite | System.IO.NotifyFilters.Security | System.IO.NotifyFilters.Size;
                directoryWatcher.Filter = "*.*";
                directoryWatcher.IncludeSubdirectories = true;
                directoryWatcher.EnableRaisingEvents = true;
            }
        }

        private void listSubDirFileCounts()
        {
            filesDataGridView.Rows.Clear();

            try
            {
                List<string> subdirList = new List<string>(Directory.GetDirectories(directoryName));
                foreach (string subdirname in subdirList)
                {
                    string pathName = Path.GetFileName(subdirname);
                    string[] files = Directory.GetFiles(subdirname, "*.*", SearchOption.AllDirectories);
                    int count = files.Length;
                    long fileSize = 0;
                    DateTime lastUpdate = new DateTime(1911, 1, 1);

                    foreach (string filename in files)
                    {
                        FileInfo info = new FileInfo(filename);
                        fileSize += info.Length;
                        if (lastUpdate.CompareTo(info.LastWriteTime) <= 0)
                        {
                            lastUpdate = info.LastWriteTime;
                        }
                    }
                    filesDataGridView.Rows.Add(new Object[] { pathName, count, humanReadable(fileSize), lastUpdate });
                }
            }
            catch (Exception e)
            {
                directoryWatcher.EnableRaisingEvents = false;
                MessageBox.Show("偵測到系統異常，系統已停止監控檔案" + Environment.NewLine + "錯誤訊息：" + e.Message);
            }

            if (filesDataGridView.SortedColumn != null)
            {
                if (filesDataGridView.SortOrder == SortOrder.Ascending)
                {
                    filesDataGridView.Sort(filesDataGridView.SortedColumn, System.ComponentModel.ListSortDirection.Ascending);
                }
                else
                {
                    filesDataGridView.Sort(filesDataGridView.SortedColumn, System.ComponentModel.ListSortDirection.Descending);
                }
            }
        }

        private void filesDataGridView_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
        {
            try
            {
                // if fileCount equal zero, set the row backgroup to red 
                if ((int)(filesDataGridView.Rows[e.RowIndex].Cells[1].Value) == 0)
                {
                    DataGridViewRow row = filesDataGridView.Rows[e.RowIndex];
                    row.DefaultCellStyle.BackColor = System.Drawing.Color.Red;
                }
            }
            catch (Exception) { }
        }

        private string humanReadable(long len)
        {
            //from:http://stackoverflow.com/questions/281640/how-do-i-get-a-human-readable-file-size-in-bytes-abbreviation-using-net
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            while (len >= 1024 && order + 1 < sizes.Length)
            {
                order++;
                len = len / 1024;
            }

            // Adjust the format string to your preferences. For example "{0:0.#}{1}" would
            // show a single decimal place, and no space.
            string result = String.Format("{0:0.##} {1}", len, sizes[order]);
            return result;
        }
    }
}
