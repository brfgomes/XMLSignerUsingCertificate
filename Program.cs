using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;
using poc.Utils;

class Program
{
    static void Main()
    {
        var filePath = "config.txt";
        if (!File.Exists(filePath))
        {
            Utils.CreateConfigFile(filePath);
            Console.WriteLine("Arquivo config não configurado");
            return;
        }
        
        var configs = Utils.ReadConfigFile(filePath);
        if (!Utils.validateConfigs(configs))
            return;
        var doc = new XmlDocument();
        using var client = new WebClient();
        {
            var xmlBytes = client.DownloadData(configs.xmlPathToSign);
            doc.LoadXml(Encoding.UTF8.GetString(xmlBytes));
            var cert = new X509Certificate2(File.ReadAllBytes(configs.certPath), configs.certPassword);
            Utils.SignXmlDocumentWithCertificate(doc, cert);
            File.WriteAllText(configs.signedXmlPath, doc.OuterXml);
            Console.WriteLine("XML Assinado com sucesso!");
        }
    }
}
