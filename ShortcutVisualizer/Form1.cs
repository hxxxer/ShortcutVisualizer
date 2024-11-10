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
            
            // 设置初始文件夹路径
            txtFolderPath.Text = @"D:\软件";
            // 在构造函数中初始化文件夹路径并填充树形视图
            PopulateTreeView(@"D:\软件");
        }

        public class ShellIcon
        {
            // 导入shell32.dll的ExtractIcon函数
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
                // 设置浏览文件夹窗口的标题（只能通过先设置描述再将描述改成标题来设置）
                fbd.Description = "选择快捷方式所在文件夹";
                fbd.UseDescriptionForTitle = true;

                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtFolderPath.Text = fbd.SelectedPath;
                    PopulateTreeView(fbd.SelectedPath);
                }
            }
        }


        private void PopulateTreeView(string folderPath)
        {
            // 清理原来的内容
            imageList1.Images.Clear();
            treeView1.Nodes.Clear();

            // 添加文件夹图标，如果获取失败则使用透明图
            Icon folderIcon = ShellIcon.GetFolderIcon(3) ?? Icon.FromHandle((new Bitmap(40, 40)).GetHicon());
            if (folderIcon != null) imageList1.Images.Add("folder", folderIcon);

            // 构建文件树并关联图标
            TraverseFolder(folderPath, treeView1.Nodes, imageList1);
            treeView1.ImageList = imageList1;
        }

        private void TraverseFolder(string path, TreeNodeCollection nodes, ImageList imageList)
        {
            string[] directories = Directory.GetDirectories(path);  // 获取当前目录的子文件夹
            string[] files = Directory.GetFiles(path, "*.lnk");  // 获取当前目录的lnk文件

            // 递归建树
            foreach (string dir in directories)
            {
                TreeNode dirNode = nodes.Add(Path.GetFileName(dir));
                TraverseFolder(dir, dirNode.Nodes, imageList);
            }

            // 构建快捷方式节点
            foreach (string file in files)
            {
                // 获取快捷方式的图标，注意不是快捷方式所指文件的图标
                imageList1.Images.Add("icon", Icon.ExtractAssociatedIcon(file));

                // 获取文件名并去除无关信息
                string fileName = Path.GetFileNameWithoutExtension(file);
                Regex regex = new(@"(.+)\.exe - 快捷方式");
                Match match = regex.Match(fileName);
                if (match.Success)
                {
                    fileName = match.Groups[1].Value;
                }

                // 建节点
                TreeNode fileNode = nodes.Add(fileName);
                fileNode.ImageIndex = imageList.Images.Count - 1;
                fileNode.SelectedImageIndex = imageList.Images.Count - 1;
                fileNode.Tag = file; // 将快捷方式文件路径存储在节点的Tag属性中
            }
        }


        // 定义双击文件树事件
        private void treeView1_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            // 判断点击节点是否有字符串格式的tag
            if (e.Node.Tag is string shortcutPath)
            {
                // 使用ProcessStartInfo指定要启动的文件
                ProcessStartInfo startInfo = new(shortcutPath)
                {
                    // 设置UseShellExecute为true，这样可以正确处理快捷方式
                    UseShellExecute = true
                };

                try
                {
                    // 创建一个新的进程并启动它
                    using Process process = new() { StartInfo = startInfo };
                    process.Start();
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