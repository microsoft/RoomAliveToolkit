using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RoomAliveToolkit
{
    public partial class VideoPanel : UserControl
    {
        public VideoPanel()
        {
            InitializeComponent();
        }

        protected override void OnPaintBackground(PaintEventArgs e) 
        {
            if (DesignMode)
                base.OnPaintBackground(e);
        }
    }
}
