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
            
            // ���ó�ʼ�ļ���·��
            txtFolderPath.Text = @"D:\���";
            // �ڹ��캯���г�ʼ���ļ���·�������������ͼ
            PopulateTreeView(@"D:\���");
        }

        public class ShellIcon
        {
            // ����shell32.dll��ExtractIcon����
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
                // ��������ļ��д��ڵı��⣨ֻ��ͨ�������������ٽ������ĳɱ��������ã�
                fbd.Description = "ѡ���ݷ�ʽ�����ļ���";
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
            // ����ԭ��������
            imageList1.Images.Clear();
            treeView1.Nodes.Clear();

            // ����ļ���ͼ�꣬�����ȡʧ����ʹ��͸��ͼ
            Icon folderIcon = ShellIcon.GetFolderIcon(3) ?? Icon.FromHandle((new Bitmap(40, 40)).GetHicon());
            if (folderIcon != null) imageList1.Images.Add("folder", folderIcon);

            // �����ļ���������ͼ��
            TraverseFolder(folderPath, treeView1.Nodes, imageList1);
            treeView1.ImageList = imageList1;
        }

        private void TraverseFolder(string path, TreeNodeCollection nodes, ImageList imageList)
        {
            string[] directories = Directory.GetDirectories(path);  // ��ȡ��ǰĿ¼�����ļ���
            string[] files = Directory.GetFiles(path, "*.lnk");  // ��ȡ��ǰĿ¼��lnk�ļ�

            // �ݹ齨��
            foreach (string dir in directories)
            {
                TreeNode dirNode = nodes.Add(Path.GetFileName(dir));
                TraverseFolder(dir, dirNode.Nodes, imageList);
            }

            // ������ݷ�ʽ�ڵ�
            foreach (string file in files)
            {
                // ��ȡ��ݷ�ʽ��ͼ�꣬ע�ⲻ�ǿ�ݷ�ʽ��ָ�ļ���ͼ��
                imageList1.Images.Add("icon", Icon.ExtractAssociatedIcon(file));

                // ��ȡ�ļ�����ȥ���޹���Ϣ
                string fileName = Path.GetFileNameWithoutExtension(file);
                Regex regex = new(@"(.+)\.exe - ��ݷ�ʽ");
                Match match = regex.Match(fileName);
                if (match.Success)
                {
                    fileName = match.Groups[1].Value;
                }

                // ���ڵ�
                TreeNode fileNode = nodes.Add(fileName);
                fileNode.ImageIndex = imageList.Images.Count - 1;
                fileNode.SelectedImageIndex = imageList.Images.Count - 1;
                fileNode.Tag = file; // ����ݷ�ʽ�ļ�·���洢�ڽڵ��Tag������
            }
        }


        // ����˫���ļ����¼�
        private void treeView1_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            // �жϵ���ڵ��Ƿ����ַ�����ʽ��tag
            if (e.Node.Tag is string shortcutPath)
            {
                // ʹ��ProcessStartInfoָ��Ҫ�������ļ�
                ProcessStartInfo startInfo = new(shortcutPath)
                {
                    // ����UseShellExecuteΪtrue������������ȷ�����ݷ�ʽ
                    UseShellExecute = true
                };

                try
                {
                    // ����һ���µĽ��̲�������
                    using Process process = new() { StartInfo = startInfo };
                    process.Start();
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