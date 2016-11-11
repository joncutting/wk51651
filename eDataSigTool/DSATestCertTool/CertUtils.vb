'Options
Option Explicit On
Option Strict On

'System imports
Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Security.Cryptography.X509Certificates

'Org.BouncyCastle namespaces are not imported to avoid confusion with similar namespaces in .NET Framework

''' <summary>
''' Contains utility methods for handling digital certificates and digital signatures.
''' </summary>
''' <remarks></remarks>
Public NotInheritable Class CertUtils

    ''' <summary>
    ''' Creates a self-signed DSA-SHA1 certificate.
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks>Microsoft's .NET Framework includes classes for manipulating digital certificates.
    ''' However, these classes do not provide any way to create a self-signed certificate.  The only way
    ''' to do it with Microsoft tools is to use makecert, a command-line tool installed with the .NET
    ''' Framework SDK.
    ''' 
    ''' This code uses Bouncy Castle library to create the self-signed digital certificate and then 
    ''' converts it to an instance of X509Certificate2, which is the Microsoft format. The Bouncy Castle
    ''' library is not well documented.  See 
    ''' http://www.bouncycastle.org/wiki/display/JA1/X.509+Public+Key+Certificate+and+Certification+Request+Generation 
    ''' for sample code demonstrating certificate generation. Also see
    ''' http://www.wodka-fabrik.org/ilja/blog/ for further examples.</remarks>
    Public Shared Function CreateSelfSignedCertificate() As X509Certificate2
        Dim pwd As String = "_super*secret_"   'Not the best practice but we are not saving this to disk either
        Dim sr As New Org.BouncyCastle.Security.SecureRandom
        Dim dpg As New Org.BouncyCastle.Crypto.Generators.DsaParametersGenerator
        dpg.Init(1024, 80, sr)

        'Create a DSA key pair
        Dim params As New Org.BouncyCastle.Crypto.Parameters.DsaKeyGenerationParameters(sr, dpg.GenerateParameters)

        Dim dsa As New Org.BouncyCastle.Crypto.Generators.DsaKeyPairGenerator
        dsa.Init(params)
        Dim keyPair As Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair = dsa.GenerateKeyPair()

        'Set up the X509v1 digital certificate generator
        Dim certgen As New Org.BouncyCastle.X509.X509V3CertificateGenerator
        Dim cn As String = "CN=eData Test Certificate"
        certgen.SetSerialNumber(New Org.BouncyCastle.Math.BigInteger(DateTime.Now.Ticks.ToString))
        certgen.SetIssuerDN(New Org.BouncyCastle.Asn1.X509.X509Name(cn))
        certgen.SetNotBefore(DateTime.UtcNow)              'Valid starting now
        certgen.SetSubjectDN(New Org.BouncyCastle.Asn1.X509.X509Name(cn))
        certgen.SetPublicKey(keyPair.Public)
#Disable Warning BC40000 ' Type or member is obsolete
        certgen.SetSignatureAlgorithm("SHA1withDSA")    'Use SHA1 hash and DSA key pair
#Enable Warning BC40000 ' Type or member is obsolete

        certgen.SetNotAfter(DateTime.UtcNow.AddYears(10))

        'Generate usage constraint so that this can only be used for digital signatures
        Dim usage As New Org.BouncyCastle.Asn1.X509.KeyUsage(Org.BouncyCastle.Asn1.X509.KeyUsage.DigitalSignature)
        certgen.AddExtension(Org.BouncyCastle.Asn1.X509.X509Extensions.KeyUsage, False, usage)

        'Generate the X509v3 digital certificate
#Disable Warning BC40000 ' Type or member is obsolete
        Dim cert As Org.BouncyCastle.X509.X509Certificate = certgen.Generate(keyPair.Private, sr)
#Enable Warning BC40000 ' Type or member is obsolete

        'Create a PKCS12 store and load in the digital certificate and its private key
        Dim pkcs12 As New Org.BouncyCastle.Pkcs.Pkcs12Store
        pkcs12.SetKeyEntry(My.Application.Info.Title, New Org.BouncyCastle.Pkcs.AsymmetricKeyEntry(keyPair.Private),
                            New Org.BouncyCastle.Pkcs.X509CertificateEntry() {New Org.BouncyCastle.Pkcs.X509CertificateEntry(cert)})

        'Write the PKCS12 store to memory
        Dim ms As New MemoryStream
        pkcs12.Save(ms, pwd.ToCharArray, sr)

        'Read the memory back in as a .NET certificate
        Return New X509Certificate2(ms.GetBuffer, pwd, X509KeyStorageFlags.PersistKeySet Or X509KeyStorageFlags.Exportable)

        ms.Dispose()    'Don't let it hang out in memory

        'todo : check for other dispose calls

    End Function

    ''' <summary>
    ''' Installs a certificate in the specified certificate store.
    ''' </summary>
    ''' <param name="store"></param>
    ''' <param name="cert"></param>
    ''' <remarks></remarks>
    Public Shared Sub InstallCertificate(ByVal store As StoreName, ByVal cert As X509Certificate2)
        Dim cs As New X509Store(store)
        Try
            cs.Open(OpenFlags.ReadWrite)
            cs.Add(cert)
        Finally
            cs.Close()
        End Try
    End Sub

    Private Sub New()
        'stub out ctor since we're not instantiating the CertUtils class.
    End Sub
End Class
