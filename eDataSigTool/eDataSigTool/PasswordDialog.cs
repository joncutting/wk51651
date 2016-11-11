using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace eDataSigTool
{
    public partial class PasswordDialog : Form
    {
        public PasswordDialog()
        {
            InitializeComponent();
        }

        private void chkShowPassword_CheckedChanged(object sender, EventArgs e)
        {
            if (chkShowPassword.Checked)
            {
                this.txtPassword.PasswordChar = '\0';
            }
            else
            {
                this.txtPassword.PasswordChar = '*';
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            _key = txtPassword.Text;
            txtPassword.Text = string.Empty;
            Hide();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            txtPassword.Text = string.Empty;
            Close();
        }

        public string Password { get { return _key; } }
        private string _key;

        public string Prompt {
            get { return lblPrompt.Text; }
            set { lblPrompt.Text = value; }
        }

    }
}
