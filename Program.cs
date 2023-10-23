using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;

class Program
{
    static void Main()
    {
        XmlDocument doc = new XmlDocument();
        string filePath = "C:\\Users\\dev1\\Desktop\\NFSeSP\\TesteEnvioDeLoteRPS.txt.xml";
        
        if (!File.Exists(filePath))
            throw new Exception("Caminho de arquivo XML não encontrado!");
        
        using (WebClient client = new WebClient())
        {
            byte[] xmlBytes = client.DownloadData(filePath);
            
            doc.LoadXml(Encoding.UTF8.GetString(xmlBytes));
            
            string certPath = "C:\\Users\\dev1\\Desktop\\NFSeSP\\cert.pfx";
            string certPassword = "1234";
            X509Certificate2 cert = new X509Certificate2(File.ReadAllBytes(certPath), certPassword);
            
            SignXmlDocumentWithCertificate(doc, cert);

            Console.WriteLine(doc.OuterXml);
            File.WriteAllText($"C:\\Users\\dev1\\Desktop\\NFSeSP\\teste\\xmlAssinadoNFSe{new Random().Next(0, 100)}.xml", doc.OuterXml);
        }
        
        static void SignXmlDocumentWithCertificate(XmlDocument doc, X509Certificate2 cert)
        {
            SignedXml signedXml = new SignedXml(doc);
            signedXml.SigningKey = cert.PrivateKey;
            Reference reference = new Reference();
            reference.Uri = "";
            reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
            signedXml.AddReference(reference);

            KeyInfo keyinfo = new KeyInfo();
            keyinfo.AddClause(new KeyInfoX509Data(cert));

            signedXml.KeyInfo = keyinfo;
            signedXml.ComputeSignature();
            XmlElement xmlSig = signedXml.GetXml();

            doc.DocumentElement.AppendChild(doc.ImportNode(xmlSig, true));
        }
    }
}
