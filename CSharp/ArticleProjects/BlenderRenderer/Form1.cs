using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BlenderRenderer
{
    public partial class Form1 : Form
    {
        public static Form1 FormIns;
        public SynchronizationContext sc;
        public Form1()
        {
            sc=SynchronizationContext.Current;
            InitializeComponent();
            FormIns = this;
        }
        public void UpdateStatus(string text)
        {
            sc.Post(state => richTextBox1.Text += $"\n{text}",null);
        }
        private void button1_Click(object sender, EventArgs e)
        {
            UpdateStatus("connecting...");
            BackEndFunctionality.Connect();
        }

        private void ButtonCreateBlobsClick(object sender, EventArgs e)
        {
            BackEndFunctionality.CreatingBlobs();
        }

        private void ButtonUploadBlenderClick(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "Zip files (*.zip)|*.zip|All files (*.*)|*.*"; 
            DialogResult dr = openFileDialog1.ShowDialog();
            if (dr == DialogResult.OK)
            {
                BackEndFunctionality.UploadApp(openFileDialog1.FileName);
            }

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void button4_Click(object sender, EventArgs e)
        {
            DialogResult dr = folderBrowserDialog1.ShowDialog();
            if (dr == DialogResult.OK)
            {
                BackEndFunctionality.UploadFolder(folderBrowserDialog1.SelectedPath);
            }

        }
    }
}
