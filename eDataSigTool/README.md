INTRODUCTION

eDataSigTool is a simple Windows app for verifying and signing eData files. The software is written in C# and the setup program is written in InnoSetup. 

The eData standard guide has an optional digital signature feature. It specifies two types of digital signature per the XML Digital Signature standard - HMAC-SHA1 and DSA. HMAC-SHA1 uses a pre-shared secret as a message authenticiation code (MAC), something like a password; this is a simple mechanism that may be adequate for many users. On the other hand, DSA uses a public/private key pair which is usually contained in a certificate; this method is more complex and is recommended for more sophisticated implementations. 

GETTING STARTED

In order to compile and run the software you will need the following:

* Windows 7 Service Pack 1 or higher
* Microsoft .NET Framework 4.6.2 
* Visual Studio Express 2015 for Windows Desktop
* Inno Setup 5.5.9 or later (only needed to build the setup program)

EDATASIGTOOL PROJECT

The directory eDataSigTool contains the C# project for the eDataSigTool app. This app is able to parse eData files and check them for correct XML syntax and conformance with the eData XSD. In addition, it can validate digital signatures as well as apply digital signatures using both HMAC-SHA1 and DSA methods. 

DSATESTCERTTOOL PROJECT

Companies with existing certificate management infrastructure can readily generate DSA key pairs and manage the distribution and validation of trust relationships. In some cases, however, this infrastructure is not available and it is convenient to have a self-signed certificate that can be used for testing purposes. The project DSATestCertTool is a small command-line VB.NET project which generates a DSA key pair in a self-signed certificate using the BouncyCastle library and then installs it in the Windows "My" user certificate store.

SETUP PROJECT

The directory Setup contains the Inno Setup source code for generating a setup program mysetup.exe that will install eDataSigTool on another computer.
