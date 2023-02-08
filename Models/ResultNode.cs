using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace DirectorySearcherTestApp.Models
{
    public class ResultNode
    {
        public int SortIndex { get; set; }
        public string Caption { get; set; }
        public Icon Icon { get; set; }
        public TreeNode TreeNode { get; set; }
        public ResultNode Parent { get; set; }
        public List<ResultNode> Childs { get; set; } = new List<ResultNode>();
    }
}
