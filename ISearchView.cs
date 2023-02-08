using DirectorySearcherTestApp.Models;
using System.Windows.Forms;

namespace DirectorySearcherTestApp
{
    public interface ISearchView
    {
        string Path { get; }
        string Mask { get; }
        int CountThreads { get; }
        Control MainControl { get; }
        TreeNode AddResultToView(TreeNode parentNode, ResultNode node);
        void AddErrorMessage(string message);
        void UpdateStatus(string message);
        void FinishWork();
    }
}
