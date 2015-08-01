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
    public partial class UnsavedChangesDialog : Form
    {
        public UnsavedChangesDialog(string filename)
        {
            InitializeComponent();
            messageText.Text = "Do you want to save changes to " + filename + "?";

        }
    }
}
