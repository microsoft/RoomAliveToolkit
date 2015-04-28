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
    public partial class NewDialog : Form
    {
        public NewDialog()
        {
            InitializeComponent();
        }

        //protected override void OnLoad(EventArgs e)
        //{
        //    base.OnLoad(e);
        //}

        public int NumProjectors
        {
            get {return (int) numericUpDown1.Value; }
        }

        public int NumCameras
        {
            get {return (int) numericUpDown2.Value; }
        }
    }
}
