using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CameraDetection
{
    public partial class Form2 : Form
    {
        String comPORT;

        public Form2()
        {
            InitializeComponent();
            comPORT = "";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.Threshold = textBox1.Text;
            Properties.Settings.Default.BlobWidth = textBox2.Text;
            Properties.Settings.Default.BlobHeight = textBox3.Text;
            this.Close();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            textBox1.Text = Properties.Settings.Default.Threshold;
            textBox2.Text = Properties.Settings.Default.BlobWidth;
            textBox3.Text = Properties.Settings.Default.BlobHeight;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(comboBox1.SelectedItem != null)
                {
                comPORT = Convert.ToString(comboBox1.SelectedItem);
                }
         
       
        }
    }
}
