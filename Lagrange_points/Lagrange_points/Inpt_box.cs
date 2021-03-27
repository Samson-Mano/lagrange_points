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
    public partial class Inpt_box : Form
    {
        public double mass_ratio;
        public double mass_distance;

        public Inpt_box(double i_mass_ratio, double i_mass_distance)
        {
            InitializeComponent();

            mass_ratio = i_mass_ratio;
            mass_distance = i_mass_distance;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Ok Button
            // Mass control
            mass_ratio = trackBar1.Value / 200.0f;
            mass_distance = Convert.ToDouble(textBox_mdist.Text);

            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // Cancel Button
            this.Close();
        }

        private void Inpt_box_Load(object sender, EventArgs e)
        {
            this.CenterToParent();

            // Mass data
            label_mratio.Text = mass_ratio.ToString();
            trackBar1.Value = (int)(mass_ratio * 200);
            textBox_mdist.Text = mass_distance.ToString();
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            // Change in trackbar update mass ratio
            label_mratio.Text = (trackBar1.Value / 200.0f).ToString();
            label_mratio.Refresh();
        }
    }
}
