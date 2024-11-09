using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace ShortcutVisualizer
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();

            txtFolderPath.Text = @"D:\软件"; // 设置初始文件夹路径
            // 在构造函数中初始化文件夹路径并填充树形视图
            PopulateTreeView(@"D:\软件");
        }

        public class ShellIcon
        {
            [DllImport("shell32.dll", CharSet = CharSet.Auto)]
            private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

            public static Icon? GetFolderIcon(int index)
            {
                // 从shell32.dll提取文件夹图标 (索引3是标准的关闭文件夹图标)
                IntPtr hIcon = ExtractIcon(IntPtr.Zero, "shell32.dll", index);

                if (hIcon != IntPtr.Zero)
                {
                    return Icon.FromHandle(hIcon);
                }

                return null;
            }
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Select the folder containing the shortcut files";
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtFolderPath.Text = fbd.SelectedPath;
                    PopulateTreeView(fbd.SelectedPath);
                }
            }
        }


        private void PopulateTreeView(string folderPath)
        {
            imageList1.Images.Clear();
            treeView1.Nodes.Clear();

            // 添加文件夹图标
            Icon folderIcon = ShellIcon.GetFolderIcon(3);
            if (folderIcon != null)
            {
                imageList1.Images.Add("folder", folderIcon);
            }

            TraverseFolder(folderPath, treeView1.Nodes, imageList1);
            treeView1.ImageList = imageList1;
        }

        private void TraverseFolder(string path, TreeNodeCollection nodes, ImageList imageList)
        {
            string[] directories = Directory.GetDirectories(path);
            string[] files = Directory.GetFiles(path, "*.lnk");

            foreach (string dir in directories)
            {
                TreeNode dirNode = nodes.Add(Path.GetFileName(dir));
                TraverseFolder(dir, dirNode.Nodes, imageList);
            }

            foreach (string file in files)
            {
                imageList1.Images.Add("icon", Icon.ExtractAssociatedIcon(file));

                string fileName = Path.GetFileNameWithoutExtension(file);
                Regex regex = new Regex(@"(.+)\.exe - 快捷方式");
                Match match = regex.Match(fileName);
                if (match.Success)
                {
                    fileName = match.Groups[1].Value;
                }

                TreeNode fileNode = nodes.Add(fileName);
                fileNode.ImageIndex = imageList.Images.Count - 1;
                fileNode.SelectedImageIndex = imageList.Images.Count - 1;
                fileNode.Tag = file; // 将快捷方式文件路径存储在节点的Tag属性中
            }
        }


        private void treeView1_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node.Tag is string shortcutPath)
            {
                // 使用ProcessStartInfo指定要启动的文件
                ProcessStartInfo startInfo = new ProcessStartInfo(shortcutPath);

                // 设置UseShellExecute为true，这样可以正确处理快捷方式
                startInfo.UseShellExecute = true;

                try
                {
                    // 创建一个新的进程并启动它
                    using (Process process = new Process { StartInfo = startInfo })
                    {
                        process.Start();
                    }
                }
                catch (Exception ex)
                {
                    // 处理可能发生的异常
                    Console.WriteLine("启动快捷方式时发生错误: " + ex.Message);
                }
            }
        }
    }
}