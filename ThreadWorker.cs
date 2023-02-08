using DirectorySearcherTestApp.Extensions;
using DirectorySearcherTestApp.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace DirectorySearcherTestApp
{
    public class ThreadWorker : IDisposable
    {
        public ThreadWorker(ISearchView searchView)
        {
            SearchView = searchView;
            Threads = new Dictionary<int, Thread>();
        }

        readonly ISearchView SearchView;
        readonly Dictionary<int, Thread> Threads;
        readonly List<ResultNode> FolderTree = new List<ResultNode>();
        readonly DateTime startTime = DateTime.Now;

        bool isAborted = false;
        int countFiles = 0;
        int findFiles = 0;

        string GetFindFilesCaption => $"Найдено файлов {findFiles} из {countFiles}";

        delegate TreeNode AddItemDelegate(TreeNode parentNode, ResultNode node);
        delegate void MessageDelegate(string message);
        delegate void EventDelegate();

        public void StartWork()
        {
            var thread = new Thread(() => SearchInDirRecure(null, SearchView.Path, true));
            thread.Start();
        }        

        void SearchInDirRecure(ResultNode parentNode, string path, bool isNewThread)
        {
            int npp = 0;
            if (isNewThread)
                AddOrRemoveThread(true);
            try
            {
                var dirs = Directory.GetDirectories(path);
                var files = Directory.GetFiles(path);
                countFiles += files.Length;
                foreach (var dir in dirs)
                {
                    try
                    {
                        var node = new ResultNode()
                        {
                            SortIndex = npp,
                            Caption = Path.GetFileName(dir),
                            Parent = parentNode,
                        };
                        npp++;
                        (parentNode?.Childs ?? FolderTree).Add(node);

                        if (Threads.Count < SearchView.CountThreads && !isAborted)
                        {
                            var thread = new Thread(() => SearchInDirRecure(node, dir, true));
                            thread.Start();
                        }
                        else
                            SearchInDirRecure(node, dir, false);
                    }
                    catch (ThreadAbortException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error in directory {dir}: {ex.Message}");
                    }
                }
                foreach (var file in files)
                {
                    try
                    {
                        if (SearchView.Mask.IsNullOrWhiteSpace()
                            || StringExtension.IsFileNameMatchMask(file, SearchView.Mask))
                        {
                            findFiles++;
                            var node = new ResultNode()
                            {
                                SortIndex = npp,
                                Caption = Path.GetFileName(file),
                                Parent = parentNode,
                                Icon = Icon.ExtractAssociatedIcon(file),
                            };
                            npp++;
                            var parentFolder = GetNodeFolderRecure(node);
                            AddNodeToResultView(parentFolder, node);                            
                        }
                    }
                    catch (ThreadAbortException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error with file {file}: {ex.Message}");
                    }
                }
                UpdateStatus($"{GetFindFilesCaption}");
            }
            catch (ThreadAbortException ex)
            {
                Console.WriteLine($"{DateTime.Now}: {Thread.CurrentThread.ManagedThreadId} terminated {(string)ex.ExceptionState}");
            }
            catch (Exception ex)
            {
                LogError($"Error in directory {path}: {ex.Message}");
            }
            finally
            {
                if (isNewThread && !isAborted)
                {
                    AddOrRemoveThread(false);
                    if (Threads.Count == 0)
                    {
                        var timeCaption = Math.Round((DateTime.Now - startTime).TotalMilliseconds);
                        UpdateStatus($"Завершено за {timeCaption} мс! {(findFiles > 0 ? GetFindFilesCaption : "Файлы не найдены")}");
                        SearchView.MainControl.Invoke(new EventDelegate(SearchView.FinishWork));
                    }
                }
            }
        }
        void AddOrRemoveThread(bool isNew, Thread thread = null)
        {
            lock (Threads)
            {
                if (thread == null)
                    thread = Thread.CurrentThread;
                if (isNew)
                    Threads.Add(thread.ManagedThreadId, thread);
                else
                    Threads.Remove(thread.ManagedThreadId);
            }
        }

        readonly object NodeLocker = new object();
        TreeNode GetNodeFolderRecure(ResultNode node)
        {
            lock (NodeLocker)
            {
                var parent = node.Parent;
                if (parent != null)
                {
                    if (parent.TreeNode == null)
                    {
                        var upper = GetNodeFolderRecure(parent);
                        parent.TreeNode = AddNodeToResultView(upper, parent);
                        return parent.TreeNode;
                    }
                    else
                        return parent.TreeNode;
                }
                else // first level
                    return null;
            }
        }

        #region run in UI thread
        TreeNode AddNodeToResultView(TreeNode parentNode, ResultNode node)
        {
            if (isAborted)
                return null;
            return (TreeNode)SearchView.MainControl.Invoke(
                           new AddItemDelegate(SearchView.AddResultToView),
                           parentNode, node);
        }
        void LogError(string message)
        {
            if (!isAborted)
                SearchView.MainControl.Invoke(new MessageDelegate(SearchView.AddErrorMessage), message);
        }

        void UpdateStatus(string message)
        {
            if (!isAborted)
                SearchView.MainControl.Invoke(new MessageDelegate(SearchView.UpdateStatus), message);
        }
        #endregion

        public void Dispose()
        {
            if (Threads.Count > 0)
            {
                UpdateStatus("Принудительное завершение потоков");
                isAborted = true;
                var threads = Threads.Values.Where(r => r != null).ToArray();
                foreach (var thread in threads)
                {
                    thread.Abort();
                    AddOrRemoveThread(false, thread);
                    thread.Join();
                }
                Threads?.Clear();
            }
        }
    }
}
