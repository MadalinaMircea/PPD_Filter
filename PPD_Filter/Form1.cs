using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PPD_Filter
{
    public partial class Form1 : Form
    {
        string filePath = "";

        public Form1()
        {
            InitializeComponent();
            ProgressBar.Visible = false;
            ProgressLabel.Visible = false;
            ReadyLabel.Visible = false;
        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            ProgressBar.Visible = false;
            ProgressLabel.Visible = false;
            ReadyLabel.Visible = false;

            openFileDialog1.Filter = "ppm files (*.ppm)|*.ppm";
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.RestoreDirectory = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                //Get the path of specified file
                filePath = openFileDialog1.FileName;

                FilePathLabel.Text = filePath;
            }
        }

        private void GrayscaleButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (filePath == "")
                {
                    throw new Exception("Choose a file.");
                }

                ProgressBar.Visible = true;
                ProgressBar.Value = 0;
                ProgressBar.Invalidate();

                ProgressLabel.Visible = true;
                ProgressLabel.Text = "In progress";
                ProgressLabel.Invalidate();
                Invalidate();
                //ReadyLabel.Visible = false;

                PPMOperations operations = new PPMOperations(filePath, this.ProgressBar);

                operations.GenerateGrayscaleImage(Environment.CurrentDirectory + "\\generatedPicture.ppm");

                ProgressLabel.Text = "File Ready!";
                ProgressLabel.Invalidate();
                //ReadyLabel.Visible = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.StackTrace, "Error");
            }
        }

        private void RegularButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (filePath == "")
                {
                    throw new Exception("Choose a file.");
                }

                ProgressBar.Visible = true;
                ProgressBar.Value = 0;
                ProgressBar.Invalidate();

                ProgressLabel.Visible = true;
                ProgressLabel.Text = "In progress";
                ProgressLabel.Invalidate();
                Invalidate();
                //ReadyLabel.Visible = false;

                PPMOperations operations = new PPMOperations(filePath, ProgressBar);

                operations.GenerateRegularImage(Environment.CurrentDirectory + "\\generatedPicture.ppm");

                ProgressLabel.Text = "File Ready!";
                ProgressLabel.Invalidate();
                //ReadyLabel.Visible = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
        }
    }
}
