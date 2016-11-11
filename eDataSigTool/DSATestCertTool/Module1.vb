Imports System.Security.Cryptography.X509Certificates

Module Module1

    Sub Main()
        Dim cert As X509Certificate2 = CertUtils.CreateSelfSignedCertificate()
        CertUtils.InstallCertificate(StoreName.My, cert)
    End Sub

End Module
