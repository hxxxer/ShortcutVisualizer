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

            txtFolderPath.Text = @"D:\���"; // ���ó�ʼ�ļ���·��
            // �ڹ��캯���г�ʼ���ļ���·�������������ͼ
            PopulateTreeView(@"D:\���");
        }

        public class ShellIcon
        {
            [DllImport("shell32.dll", CharSet = CharSet.Auto)]
            private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

            public static Icon? GetFolderIcon(int index)
            {
                // ��shell32.dll��ȡ�ļ���ͼ�� (����3�Ǳ�׼�Ĺر��ļ���ͼ��)
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

            // ����ļ���ͼ��
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
                Regex regex = new Regex(@"(.+)\.exe - ��ݷ�ʽ");
                Match match = regex.Match(fileName);
                if (match.Success)
                {
                    fileName = match.Groups[1].Value;
                }

                TreeNode fileNode = nodes.Add(fileName);
                fileNode.ImageIndex = imageList.Images.Count - 1;
                fileNode.SelectedImageIndex = imageList.Images.Count - 1;
                fileNode.Tag = file; // ����ݷ�ʽ�ļ�·���洢�ڽڵ��Tag������
            }
        }


        private void treeView1_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node.Tag is string shortcutPath)
            {
                // ʹ��ProcessStartInfoָ��Ҫ�������ļ�
                ProcessStartInfo startInfo = new ProcessStartInfo(shortcutPath);

                // ����UseShellExecuteΪtrue������������ȷ�����ݷ�ʽ
                startInfo.UseShellExecute = true;

                try
                {
                    // ����һ���µĽ��̲�������
                    using (Process process = new Process { StartInfo = startInfo })
                    {
                        process.Start();
                    }
                }
                catch (Exception ex)
                {
                    // ������ܷ������쳣
                    Console.WriteLine("������ݷ�ʽʱ��������: " + ex.Message);
                }
            }
        }
    }
}