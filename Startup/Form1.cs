using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Startup
{
    public partial class Form1 : Form
    {
        private readonly OpenFileDialog openFileDialog = new OpenFileDialog();

        public Form1()
        {
            InitializeComponent();
        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                iwadTextBox.Text = openFileDialog.FileName;
            }
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                pwadListBox.Items.Add(openFileDialog.FileName);
            }
        }

        private void RemoveButton_Click(object sender, EventArgs e)
        {
            pwadListBox.Items.RemoveAt(pwadListBox.SelectedIndex);
        }

        private void LaunchButton_Click(object sender, EventArgs e)
        {
            // IWAD missing?
            if (string.IsNullOrEmpty(iwadTextBox.Text))
            {
                MessageBox.Show("IWAD field empty!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Files to load
            string arguments = $"-f \"{iwadTextBox.Text}\" ";
            if (pwadListBox.Items.Count != 0)
            {
                arguments += '\"' + string.Join("\" \"", pwadListBox.Items.Cast<string>().ToArray()) + '\"';
            }
            // Map number
            arguments += $" --warp {mapNumericUpDown.Value} ";
            // Additional arguments
            arguments += argsTextBox.Text;

            Process.Start("Client.exe", arguments);
        }
    }
}
