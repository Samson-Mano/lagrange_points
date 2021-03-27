using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Lagrange_points
{
    public partial class main_form : Form
    {
        double mass_ratio;
        double mass_distance;

        lagrange_pt_store l_pt;

        public main_form()
        {
            mass_ratio = Properties.Settings.Default.sett_m_ratio;
            mass_distance = Properties.Settings.Default.sett_m_distance;

            // mass distance has no purpose in visualization so let it be 300
            l_pt = new lagrange_pt_store(mass_ratio, 300);

            InitializeComponent();

            mt_pic.Refresh();
        }

        private void main_pic_Paint(object sender, PaintEventArgs e)
        {
            if (suspendPaintToolStripMenuItem.Checked == false)
            {
                // Smoothing 
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                float mid_x = (float)(main_pic.Width * 0.5);
                float mid_y = (float)(1 * main_pic.Height * 0.5);

                e.Graphics.ScaleTransform(1, -1);
                e.Graphics.TranslateTransform(mid_x, -1 * mid_y);

                // main_pic Paint
                Graphics gr0 = e.Graphics;
                l_pt.paint_all(ref gr0, main_pic.Width, main_pic.Height);
            }
            else
            {
                e.Graphics.DrawString("Paint supended \nClick on \nContour -> Suspend Layout \nto uncheck suspend layout to paint the contour ", new Font("Verdana", 16), Brushes.Red, new PointF(100, (float)(main_pic.Height * 0.5) - 30));

            }
        }

        private void main_pic_SizeChanged(object sender, EventArgs e)
        {
            mt_pic.Refresh();
        }

        private void inputToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Open the input box
            Inpt_box i_box = new Inpt_box(mass_ratio, mass_distance);
            i_box.FormClosing += new FormClosingEventHandler(this.Inpt_box_FormClosing);

            if (i_box.ShowDialog() == DialogResult.OK)
            {
                mass_ratio = i_box.mass_ratio;
                mass_distance = i_box.mass_distance;

                l_pt = new lagrange_pt_store(mass_ratio, 300);
                mt_pic.Refresh();
            }
        }

        private void Inpt_box_FormClosing(object sender, FormClosingEventArgs e)
        {
            //Input box closing
            mt_pic.Refresh();
        }

        private void main_form_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.sett_m_ratio = mass_ratio;
            Properties.Settings.Default.sett_m_distance = mass_distance;

            Properties.Settings.Default.Save();
        }

        private void suspendPaintToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mt_pic.Refresh();
        }

        private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Refresh
            l_pt.g_mesh.update_maximum_vals();
            mt_pic.Refresh();
        }
    }
}
