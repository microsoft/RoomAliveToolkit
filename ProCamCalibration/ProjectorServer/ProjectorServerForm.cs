using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RoomAliveToolkit
{
    public partial class ProjectorServerForm : Form
    {
        public ProjectorServerForm()
        {
            InitializeComponent();
        }

        private void ProjectorServerForm_Resize(object sender, EventArgs e)
        {
            if (FormWindowState.Minimized == WindowState) 
                Hide();
        }

        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
