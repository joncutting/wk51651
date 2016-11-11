using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Xml;

namespace eDataSigTool
{
    public partial class MainForm : Form
    {
        private eDataSig _edsig;
        private bool _signable = false;

        public MainForm()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            enableControls();
        }

        private void enableControls()
        {
            bool _en = ((_edsig != null) && _signable);
            radCertificate.Enabled = _en;
            radPassword.Enabled = _en;
            btnSign.Enabled = _en;
            lblSigType.Enabled = _en;
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            // Select a file to open - we only want to pick one XML file
            openFileDialog1.Filter = "XML files|*.xml";
            openFileDialog1.Multiselect = false;
            openFileDialog1.FileName = string.Empty;
            if (openFileDialog1.ShowDialog() != DialogResult.OK)
            {
                // User pressed Cancel so don't take any action, just exit
                return;
            }

            // User pressed OK so we need to clear any previous status
            _signable = false;
            clearInfo();
            _edsig = null;
            enableControls();

            // Now attempt to open the specified file
            try
            {
                addInfo("Opening file " + openFileDialog1.FileName);
                _edsig = new eDataSig(openFileDialog1.FileName);
            }
            catch (XmlException x)
            {
                // An XML parsing error occurred
                addInfo("The file contains invalid XML");
                Debug.WriteLine(x);
                return;
            }
            catch (Exception x)
            {
                // Some other error occurred, don't go any further
                addInfo("The file could not be read");
                Debug.WriteLine(x);
                return;
            }
            // XML format is OK
            addInfo("The XML format is valid");

            // Validate the document format against the XSD Schema and display any errors/warnings
            bool _valid = _edsig.ValidateFormat();
            if (_valid)
            {
                addInfo("The eData format is valid");
            }
            else
            {
                addInfo("The eData format is not valid");
            }
            if (_edsig.ValidationErrors.Length > 0)
            {
                addInfo("eData format errors: " + _edsig.ValidationErrors);
            }
            if (_edsig.ValidationWarnings.Length > 0)
            {
                addInfo("eData format warnings: " + _edsig.ValidationWarnings);
            }
            if (!_valid)
            {
                return;
            }

            // Determine number of MaterialData elements present
            addInfo(_edsig.MaterialDataCount + " MaterialData elements found");

            // Determine the type of digital signature.
            switch (_edsig.SignatureType)
            {
                case eDataSig.SignatureTypes.None:
                    addInfo("File has no digital signature");
                    _signable = true;
                    break;
                case eDataSig.SignatureTypes.DSA:
                    addInfo("File has a DSA digital signature");
                    X509Certificate2 cert = selectCert("Select a certificate to use for digital signature verification:");
                    if (cert != null)
                    {
                        // Verify the trust chain of the certificate
                        if (cert.Verify())
                        {
                            addInfo("The certificate trust chain is valid");
                        }
                        else
                        {
                            addInfo("The certificate trust chain cannot be verified");
                        }

                        // Retrieve the DSA public key from the certificate
                        DSA key = DSACertificateExtensions.GetDSAPublicKey(cert);
                        if (key == null)
                        {
                            addInfo("The DSA public key cannot be retrieved from the certificate");           
                        }
                        else
                        {
                            // Public key retrieved, now attempt to verify the signature 
                            if (_edsig.VerifySignature(key))
                            {
                                // Digital signature valid
                                addInfo("The digital signature is valid.");
                            }
                            else
                            {
                                // Digital signature not valid
                                addInfo("Either the digital signature is not valid or the wrong certificate was selected");

                                // All we can do now is try to validate using public key embedded in the signature.
                                if (_edsig.VerifySignature())
                                {
                                    addInfo("The digital signature is valid according to the embedded public key but the integrity of the data cannot be guaranteed");
                                }
                                else
                                {
                                    addInfo("The digital signature is not valid according to the embedded public key");
                                }
                            }
                        }

                    }
                    else
                    {
                        // Certificate not supplied
                        addInfo("The digital signature was not checked because no certificate was selected");

                        // All we can do now is try to validate using public key embedded in the signature.
                        if (_edsig.VerifySignature())
                        {
                            addInfo("The digital signature is valid according to the embedded public key but the integrity of the data cannot be guaranteed");
                        }
                        else
                        {
                            addInfo("The digital signature is not valid according to the embedded public key");
                        }
                    }
                    break;
                case eDataSig.SignatureTypes.HMACSHA1:
                    addInfo("File has HMAC-SHA1 digital signature");
                    PasswordDialog frmPwd = new PasswordDialog();
                    if (frmPwd.ShowDialog() == DialogResult.OK)
                    {
                        // Password supplied
                        if (_edsig.VerifySignature(frmPwd.Password))
                        {
                            // Digital signature valid
                            addInfo("Digital signature is valid");
                        }
                        else
                        {
                            // Digital signature not valid
                            addInfo("Digital signature is not valid or the password is incorrect");
                        }
                    }
                    else
                    {
                        // Password not supplied
                        addInfo("Digital signature was not checked because no password was provided");
                    }
                    break;
                case eDataSig.SignatureTypes.Invalid:
                default:
                    addInfo("File has invalid digital signature");
                    break;
            }

            // Enable/disable controls as needed
            enableControls();
        }

        // Private method for displaying information about the eData file
        private void addInfo(string message)
        {
            dataGridView1.Rows.Add(message);
            dataGridView1.AutoResizeRows();
        }

        // Private method for clearing information about the eData file
        private void clearInfo()
        {
            dataGridView1.Rows.Clear();        
        }

        // Sign button event handler
        private void btnSign_Click(object sender, EventArgs e)
        {
            // Set up save file dialog - this is common to either algorithm
            saveFileDialog1.OverwritePrompt = true;
            saveFileDialog1.CheckPathExists = true;
            saveFileDialog1.AddExtension = true;
            saveFileDialog1.Filter = "XML files|*.xml";

            // For HMAC-SHA1
            if (radPassword.Checked)
            {
                PasswordDialog pwd = new PasswordDialog();
                pwd.Prompt = "Enter password:";
                if (pwd.ShowDialog() == DialogResult.OK)
                {
                    string pwd1 = pwd.Password;

                    // Ensure that password meets minimum length requirement
                    if (pwd1.Length < 6)
                    {
                        MessageBox.Show("Password must be at least 6 characters.");
                        return;
                    }
                    pwd.Prompt = "Re-enter password:";
                    if (pwd.ShowDialog() == DialogResult.OK)
                    {
                        string pwd2 = pwd.Password;

                        // Ensure that two passwords match
                        if (!pwd2.Equals(pwd1))
                        {
                            MessageBox.Show("Passwords do not match!");
                            return;
                        }

                        // Everything is good, now sign and save the file
                        if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                        {
                            try
                            {
                                // Sign and save to a new file
                                _edsig.Sign(pwd1, saveFileDialog1.FileName);
                                // Success!
                                MessageBox.Show("Signing and saving were successful.");
                            }
                            catch (System.Exception x)
                            {
                                // There was an error during the sign/save operation.
                                Debug.WriteLine(x);
                                MessageBox.Show("Error while signing and saving.");
                            }

                        }
                    }
                }

            }

            // For DSA
            else if (radCertificate.Checked)
            {
                X509Certificate2 cert = selectCert("Select certificate to use for digital signature:");
                if (cert != null)
                {
                    // Grab the private key from the cert so that it can be signed.
                    DSA key = cert.GetDSAPrivateKey(); 
                    
                    // Everything is good, now sign and save the file
                    if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                    {
                        try
                        {
                            // Sign and save to a new file
                            _edsig.Sign(key, saveFileDialog1.FileName);
                            // Success!
                            MessageBox.Show("Signing and saving were successful.");
                        }
                        catch (System.Exception x)
                        {
                            // There was an error during the sign/save operation.
                            Debug.WriteLine(x);
                            MessageBox.Show("Error while signing and saving.");
                        }

                    }
                }
            }
        }

        // Private method for selecting a suitable digital certificate containing a DSA key
        private X509Certificate2 selectCert(string prompt)
        {
            try
            {
                // Open the store for the current user - we need readonly access for existing certificates
                X509Store store = new X509Store("MY", StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

                // Narrow down the selection to certs that are valid now
                X509Certificate2Collection collection = store.Certificates;
                X509Certificate2Collection fcollection = collection.Find(X509FindType.FindByTimeValid, DateTime.Now, false);

                // Narrow down the selection to certs that contain DSA keys
                X509Certificate2Collection dcollection = new X509Certificate2Collection();
                foreach (X509Certificate2 c in fcollection)
                {
                    // Funky ASN notation for DSA key pair
                    if (c.GetKeyAlgorithm().Equals("1.2.840.10040.4.1"))
                    {
                        dcollection.Add(c);
                    }
                }

                // Show dialog to user
                X509Certificate2Collection scollection = X509Certificate2UI.SelectFromCollection(dcollection, Application.ProductName, prompt, X509SelectionFlag.SingleSelection);

                // Close the store
                store.Close();

                // Return the selected certificate or null
                if ((scollection != null) && (scollection.Count == 1))
                {
                    return scollection[0];
                }
            }
            catch (System.Exception x)
            {
                Debug.WriteLine(x);
            }
            return null;
        }

    }
}
