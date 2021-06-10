using System;
using System.IO;
using System.Windows.Forms;
using System.Management;

namespace USBcopy
{
    class Program
    {
        public static string volume;
        public static string fileExtention = "*.pdf";
        public static string destinationCopyDirectory = "\\copied\\";

        static void Main(string[] args)
        {
            /**
             * ManagementEventWatcher is useful to interact to Windows to get the event of plugged usb
             * */
            ManagementEventWatcher watcher = new ManagementEventWatcher();
            WqlEventQuery query = new WqlEventQuery("SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 2");
            while (true)
            {
                watcher.EventArrived += new EventArrivedEventHandler(watcher_EventArrived);
                watcher.Query = query;
                watcher.Start();
                watcher.WaitForNextEvent();
            }
        }

        /// <summary>
        /// Method recorsive to populate tree directory from a root node with the starter path
        /// </summary>
        /// <param name="root"> node contain the root path </param>
        /// <param name="dir"> all directory under root path </param>
        /// <returns> return the tree populated </returns>
        public static TreeNode popolateTreeDirectory(TreeNode root, string[] dir)
        {
            if (dir.Length != 0)
            {
                int i = 0;
                if (dir.Length > 1 &&  (dir[0].Equals(volume + "System Volume Information") || dir[0].Equals(volume + "$RECYCLE.BIN")))
                {
                    i = 1;
                }
                if (dir.Length > 2 && (dir[1].Equals(volume + "System Volume Information") || dir[1].Equals(volume + "$RECYCLE.BIN")))
                {
                    i = 2;
                }
                for (; i < dir.Length; i++)
                {
                    root.Nodes.Add(new TreeNode(dir[i] + "\\"));
                    popolateTreeDirectory(root.Nodes[root.Nodes.Count-1], Directory.GetDirectories(root.Nodes[root.Nodes.Count - 1].Text));
                }
            }
            return root;
        }

        /// <summary>
        /// Method to create all tree directory into destination path
        /// </summary>
        /// <param name="root"> root path </param>
        public static void createTreeDirectory(TreeNode root)
        {
            string dest = Path.GetFullPath(".") + destinationCopyDirectory;
            foreach (TreeNode node in root.Nodes)
            {
                Directory.CreateDirectory(dest + node.Text.Substring(volume.Length));
                createTreeDirectory(node);
            }
        }

        /// <summary>
        /// Method to copy all file of the same extenction of fileExtention
        /// </summary>
        /// <param name="root"> The files' path to copy  </param>
        /// <param name="fileExtention"> Extenction of file to copy </param>
        public static void copyFileSelected(TreeNode root, string fileExtention)
        {
            string destPath = "";
            string []files = Directory.GetFiles(root.Text, fileExtention);
            foreach (string f in files)
            {
                destPath = Path.GetFullPath(".") + destinationCopyDirectory;
                Console.WriteLine(destPath);
                Directory.CreateDirectory(destPath);
                try
                {
                    File.Copy(f, destPath + f.Substring(volume.Length));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            foreach (TreeNode node in root.Nodes)
            {
                copyFileSelected(node, fileExtention);
            }
        }

        public static void watcher_EventArrived (object sender, EventArrivedEventArgs e)
        {
            volume = e.NewEvent.Properties["DriveName"].Value.ToString() + "\\";

            Console.WriteLine("USB driver plugged in: " + volume);

            TreeNode root = new TreeNode(volume);

            popolateTreeDirectory(root, Directory.GetDirectories(root.Text));

            createTreeDirectory(root);

            copyFileSelected(root, fileExtention);
        }
    }
}
