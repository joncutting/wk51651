using System;
using System.Diagnostics;
using System.Xml;
using System.Xml.Schema;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Deployment.Application;
using System.IO;

/// <summary>
/// This class represents an eData document per draft specification ASTM E55 workgroup WK51651.
/// </summary>
/// <remarks>Reference example code provided by Microsoft at https://msdn.microsoft.com/en-us/library/75wfb4f2(v=vs.110).aspx 
/// See also explanatory example code at http://stackoverflow.com/questions/4271689/xml-selectnodes-with-default-namespace-via-xmlnamespacemanager-not-working-as-ex
/// </remarks>
public class eDataSig
{
    // XML content in opened eData document
    private XmlDocument _doc;

    // XML namespace manager used to resolve namespaces in XPath queries
    private XmlNamespaceManager _nsmgr;

    // Validation errors and warnings
    private string _validationErrors = string.Empty;
    private string _validationWarnings = string.Empty;
    public string ValidationErrors { get { return _validationErrors; } }
    public string ValidationWarnings { get { return _validationWarnings; } }

    /// <summary>
    /// Enumeration of various possibilities for digital signature that could be present in eData document.
    /// </summary>
    public enum SignatureTypes { None, HMACSHA1, DSA, Invalid };

    /// <summary>
    /// Returns the type of digital signature present in the eData document. No attempt
    /// is made to verify the signature.
    /// </summary>
    public SignatureTypes SignatureType
    {
        get
        {
            // Find all signatures present in the FileInformation element of the XML document 
            XmlNodeList sigs = _doc.SelectNodes("/x:ASTMeDataXchange/x:FileInformation/y:Signature", _nsmgr);
            if (sigs.Count == 1)
            {
                try
                { 
                    // Load the first signature, ignore all others
                    SignedXml signedXml = new SignedXml(_doc);
                    signedXml.LoadXml((XmlElement)sigs[0]);

                    // Handle the various signature methods
                    switch (signedXml.SignatureMethod)
                    {
                        case SignedXml.XmlDsigDSAUrl:
                            // Signature is DSA
                            return SignatureTypes.DSA;
                        case SignedXml.XmlDsigHMACSHA1Url:
                            // Signature is HMAC-SHA1
                            return SignatureTypes.HMACSHA1;
                        default:
                            // Signature is readable but algorithm is not supported
                            return SignatureTypes.Invalid;
                    }
                }
                catch (System.Exception x)
                {
                    // Signature cannot not be read for whatever reason
                    Debug.WriteLine(x);
                    return SignatureTypes.Invalid;
                }
            }
            else if (sigs.Count >1)
            {
                // More than one signature present, this is not supported
                return SignatureTypes.Invalid;
            }

            // There is no signature 
            return SignatureTypes.None;
        }
    }

    /// <summary>
    /// Adds an enveloped digital signature of type DSA-SHA1 to the eData content and
    /// saves it to a new file.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="saveFile"></param>
    public void Sign(DSA key, string saveFile)
    {
        Sign(null, key, saveFile);
    }

    /// <summary>
    /// Internal method used for signing by DSA-SHA1 or HMAC-SHA1
    /// </summary>
    /// <param name="secret"></param>
    /// <param name="key"></param>
    /// <param name="saveFile"></param>
    private void Sign(string secret, DSA key, string saveFile)
    {
        // Create a SignedXml object
        SignedXml signedXml = new SignedXml(_doc);

        // Create a reference to be signed.
        Reference reference = new Reference();
        reference.Uri = string.Empty;

        // Add an enveloped transformation to the reference 
        XmlDsigEnvelopedSignatureTransform env = new XmlDsigEnvelopedSignatureTransform();

        reference.AddTransform(env);

        // Add the reference to the SignedXML object.
        signedXml.AddReference(reference);

        if (key != null)
        {
            // Specify the key
            signedXml.SigningKey = key;

            // Add optional information about public key
            KeyInfo ki = new KeyInfo();
            ki.AddClause(new DSAKeyValue(key));
            signedXml.KeyInfo = ki;

            // Compute the signature.
            signedXml.ComputeSignature();
        }
        else if (secret != null)
        {
            // Caution - this is not strictly secure
            KeyedHashAlgorithm hash = new HMACSHA1(Encoding.ASCII.GetBytes(secret));

            // Compute the signature.
            signedXml.ComputeSignature(hash);
        }

        // Get the XML representation of the signature and save
        // it to an XmlElement object.
        XmlElement xmlDigitalSignature = signedXml.GetXml();

        XmlNode fi = _doc.SelectSingleNode("/x:ASTMeDataXchange/x:FileInformation", _nsmgr);
        fi.AppendChild(_doc.ImportNode(xmlDigitalSignature, true));

        // Save the signed XML document to a file specified
        // using the passed string.
        XmlTextWriter xmltw = new XmlTextWriter(saveFile, new UTF8Encoding(false));
        _doc.WriteTo(xmltw);
        xmltw.Close();
    }

    /// <summary>
    /// Adds an enveloped digital signature of type HMAC-SHA1 to the eData content and
    /// saves it to a new file.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="saveFile">Name of </param>
    public void Sign(string key, string saveFile)
    {
        Sign(key, null, saveFile);
    }

    /// <summary>
    /// Verifies an enveloped digital signature of type DSA-SHA1 in the eData content. This
    /// method does not verify the trust chain of the certificate.
    /// </summary>
    /// <param name="key">DSA key used for verification.</param>
    /// <returns>true if the digital signature is valid, otherwise false.</returns>
    public bool VerifySignature(DSA key)
    {
        try
        {
            SignedXml signedXml = getSignature();
            return signedXml.CheckSignature(key);
        }
        catch(Exception x)
        {
            Debug.WriteLine(x);
        }
        return false;
    }

    /// <summary>
    /// Verifies an enveloped digital signature of type HMAC-SHA1 in the eData content.
    /// </summary>
    /// <param name="key"></param>
    /// <returns>true if the digital signature is valid, otherwise false.</returns>
    public bool VerifySignature(string key)
    {
        try
        {
            // Caution - this is not strictly secure
            KeyedHashAlgorithm hash = new HMACSHA1(Encoding.ASCII.GetBytes(key));

            // Check the signature and return the result.
            SignedXml signedXml = getSignature();
            return signedXml.CheckSignature(hash);
        }
        catch(Exception x)
        {
            Debug.WriteLine(x);
        }
        return false;
    }

    /// <summary>
    /// Verifies an enveloped digital signature in the eData content using the public key embedded in
    /// the signature. This does not necessarily ensure data integrity since the public key could have been
    /// maliciously compromised.
    /// </summary>
    /// <returns>true if the digital signature is valid, otherwise false.</returns>
    public bool VerifySignature()
    {
        try
        {
            SignedXml signedXml = getSignature();
            return signedXml.CheckSignature();
        }
        catch(Exception x)
        {
            Debug.WriteLine(x);
        }
        return false;
    }

    // Private method to retrieve the first XMLDsig Signature element in the document
    private SignedXml getSignature()
    {
        // Create a new SignedXML object and pass it
        // the XMl document class.
        SignedXml signedXml = new SignedXml(_doc);
        
        // Find all signatures present in the FileInformation element of the XML document 
        XmlNodeList sigs = _doc.SelectNodes("/x:ASTMeDataXchange/x:FileInformation/y:Signature", _nsmgr);

        // Load the signature node.
        signedXml.LoadXml((XmlElement)sigs[0]);

        return signedXml;
    }

    /// <summary>
    /// Validates the document content according to the eData XML Schema Definition (XSD)
    /// </summary>
    /// <returns>True if the format is OK, otherwise false</returns>
    public bool ValidateFormat()
    {
        string _xsdPath = "eData.xsd";
        // Need to check for ClickOnce deployment since XSD will be elsewhere in that case
        if (ApplicationDeployment.IsNetworkDeployed)
        {
            _xsdPath = Path.Combine(ApplicationDeployment.CurrentDeployment.DataDirectory, _xsdPath);
        }
        _doc.Schemas.Add("http://www.astm.org/E55/03/eDataXchange", _xsdPath);        
        ValidationEventHandler eventHandler = new ValidationEventHandler(ValidationEventHandler);
        _validationErrors = string.Empty;
        _validationWarnings = string.Empty;
        _doc.Validate(eventHandler);
        return (_validationErrors == string.Empty);
    }

    /// <summary>
    /// Read only property specifying the number of MaterialData elements present in the document
    /// </summary>
    public int MaterialDataCount
    {
        get
        {
            // Find all MaterialData elements present in the MaterialDataGroup element of the XML document 
            XmlNodeList mds = _doc.SelectNodes("/x:ASTMeDataXchange/x:MaterialDataGroup/x:MaterialData", _nsmgr);
            if (mds!=null)
            {
                return mds.Count;
            }
            return 0;
        }
    }

    // Private method handling XSD validation errors/warnings
    private void ValidationEventHandler(object sender, ValidationEventArgs e)
    {
        switch (e.Severity)
        {
            case XmlSeverityType.Error:
                _validationErrors += e.Message;
                break;
            case XmlSeverityType.Warning:
                _validationWarnings += e.Message;
                break;
        }
    }

    // Private constructor
    private eDataSig() { }

    /// <summary>
    /// Public constructor
    /// </summary>
    /// <param name="xmlFile">Path to an eData XML file</param>
    public eDataSig(string xmlFile)
    {
        // Read the eData file into memory
        XmlReader reader = XmlReader.Create(xmlFile);
        _doc = new XmlDocument();
        _doc.PreserveWhitespace = true;
        _doc.Load(reader);
        reader.Close();

        // Prepare the namespace manager for XPath queries
        _nsmgr = new XmlNamespaceManager(_doc.NameTable);
        _nsmgr.AddNamespace("x", "http://www.astm.org/E55/03/eDataXchange");
        _nsmgr.AddNamespace("y", "http://www.w3.org/2000/09/xmldsig#");
        
    }

}