using DirectorySearcherTestApp.Extensions;
using DirectorySearcherTestApp.Helpers;
using DirectorySearcherTestApp.Models;
using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace DirectorySearcherTestApp
{
    public partial class Form1 : Form, ISearchView, IDisposable
    {
        ThreadWorker worker;
        public string Path => edPath.Text;
        public string Mask => edMask.Text;
        public int CountThreads => (int)cntThreads.Value;
        public Control MainControl => tvResults;

        public Form1()
        {
            InitializeComponent();
            edPath.Text = Application.StartupPath;
            //folder icon wtih index 0
            imageList1.Images.Add(Shell.OfFolder());
            Thread.CurrentThread.Priority = ThreadPriority.Highest;
        }

        private void btnSelectDirectory_Click_1(object sender, EventArgs e)
        {
            if (Directory.Exists(edPath.Text))
                folderBrowserDialog1.SelectedPath = edPath.Text;
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                edPath.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void btnRun_Click(object sender, EventArgs e)
        {
            SetEnabled(false);
            tvResults.Nodes.Clear();
            reLogs.Clear();
            if (Directory.Exists(Path))
            {
                if (Mask.IsNullOrWhiteSpace() &&
                   MessageBox.Show("Не указана маска для поиска, выполнить поиск всех файлов?",
                   "Подтвердите действие", MessageBoxButtons.YesNo) != DialogResult.Yes)
                {
                    SetEnabled(true);
                    return;
                }
                worker = new ThreadWorker(this);
                var workerThread = new Thread(new ThreadStart(worker.StartWork))
                {
                    IsBackground = true
                };
                workerThread.Start();
            }
            else
            {
                MessageBox.Show("Выбранный каталог для поиска не существует!");
                SetEnabled(true);
            }
        }

        private void SetEnabled(bool val)
        {
            edPath.Enabled = val;
            btnRun.Enabled = val;
            edMask.Enabled = val;
            cntThreads.Enabled = val;
            btnSelectDirectory.Enabled = val;
            btnCancel.Enabled = !val;
        }

        public TreeNode AddResultToView(TreeNode parentNode, ResultNode node)
        {
            var nodes = parentNode == null ? tvResults.Nodes : parentNode.Nodes;
            var result = new TreeNode(node.Caption);
            var iconIndex = 0;
            if (node.Icon != null)
            {
                imageList1.Images.Add(node.Icon);
                iconIndex = imageList1.Images.Count - 1;
            }
            result.ImageIndex = iconIndex;
            result.SelectedImageIndex = iconIndex;
            nodes.Insert(node.SortIndex, result);
            Application.DoEvents();
            return result;
        }

        public void AddErrorMessage(string message)
        {
            if (!message.IsNullOrWhiteSpace())
                reLogs.Text = reLogs.Text.AppendInBuilder(message.NewLine());
        }

        public void FinishWork()
        {
            worker.Dispose();
            SetEnabled(true);
        }

        public void UpdateStatus(string message)
        {
            lbStatus.Text = message;
            Application.DoEvents();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            worker?.Dispose();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            btnCancel.Enabled = false;
            worker?.Dispose();
            FinishWork();
            UpdateStatus("Отменено пользователем");
        }
    }
}
