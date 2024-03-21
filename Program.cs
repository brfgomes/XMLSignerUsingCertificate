using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;

class Program
{
    static void Main()
    {
        var filePath = "config.txt";
        if (!File.Exists(filePath))
        {
            CreateConfigFile(filePath);
            Console.WriteLine("Arquivo config não configurado");
            return;
        }
        
        var configs = ReadConfigFile(filePath);
        if (!validateConfigs(configs))
            return;
        var doc = new XmlDocument();
        using var client = new WebClient();
        {
            var xmlBytes = client.DownloadData(configs.xmlPathToSign);
            doc.LoadXml(Encoding.UTF8.GetString(xmlBytes));
            var cert = new X509Certificate2(File.ReadAllBytes(configs.certPath), configs.certPassword);
            SignXmlDocumentWithCertificate(doc, cert);
            File.WriteAllText(configs.signedXmlPath, doc.OuterXml);
            Console.WriteLine("XML Assinado com sucesso!");
        }
        static void SignXmlDocumentWithCertificate(XmlDocument doc, X509Certificate2 cert)
        {
            var signedXml = new SignedXml(doc);
            signedXml.SigningKey = cert.PrivateKey;
            var reference = new Reference();
            reference.Uri = "";
            reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
            signedXml.AddReference(reference);
            var keyinfo = new KeyInfo();
            keyinfo.AddClause(new KeyInfoX509Data(cert));
            signedXml.KeyInfo = keyinfo;
            signedXml.ComputeSignature();
            doc.DocumentElement?.AppendChild(doc.ImportNode( signedXml.GetXml(), true));
        }
        static void CreateConfigFile(string filePath)
        {
            try
            {
                var initialData = "xmlPathToSign;\nsignedXmlPath;\ncertPath;\ncertPassword;";
                File.WriteAllText(filePath, initialData);
                Console.WriteLine("Arquivo config.txt criado com sucesso.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro ao criar o arquivo config.txt: " + ex.Message);
            }
        }
        static configFile ReadConfigFile(string filePath)
        {
            var content = File.ReadAllText(filePath);
            var lines = content.Split('\n');
            foreach (var line in lines)
            {
                var parts = line.Trim().Split(';');
                if (parts.Length == 4)
                    return new configFile(parts[0].Trim().Replace(";", ""), parts[1].Trim().Replace(";", ""), parts[2].Trim().Replace(";", ""), parts[3].Trim().Replace(";", ""));
            }
            return new configFile(GetValueIfExists(lines, 0).Replace(";", ""), GetValueIfExists(lines, 1).Replace(";", ""), GetValueIfExists(lines, 2).Replace(";", ""), GetValueIfExists(lines, 3).Replace(";", ""));
        }
        static string GetPropertyValue(Func<configFile, string> propertySelector, configFile configFile) { return propertySelector(configFile); }
        static bool validateConfigs(configFile configFile)
        {
            List<(Func<configFile, bool> condition, string errorMessage)> validations = new List<(Func<configFile, bool>, string)>
            {
                ((cfg) => !string.IsNullOrEmpty(cfg.xmlPathToSign), "Caminho do XML para assinar não informado"),
                ((cfg) => File.Exists(cfg.xmlPathToSign), "Arquivo do XML para assinar não existe no caminho informado, verifique"),
                ((cfg) => !string.IsNullOrEmpty(cfg.signedXmlPath), "Caminho para salvar XML assinado não informado"),
                ((cfg) => File.Exists(cfg.signedXmlPath), "Arquivo para salvar XML assinado não existe no caminho informado, verifique"),
                ((cfg) => !string.IsNullOrEmpty(cfg.certPath), "Caminho do certificado não informado"),
                ((cfg) => File.Exists(cfg.certPath), "Certificado para assinar não existe no caminho informado, verifique"),
                ((cfg) => !string.IsNullOrEmpty(cfg.certPassword), "Senha do certificado não informada")
            };
            foreach (var (condition, errorMessage) in validations)
            {
                var teste = File.Exists(configFile.xmlPathToSign);
                var validation = condition(configFile);
                if (!validation)
                {
                    Console.WriteLine(errorMessage);
                    return false;
                }
            }
            return true;
        }
        static string GetValueIfExists(string[] lines, int index)
        {
            if (index >= 0 && index < lines.Length)
                return lines[index];
            return null;
        }
    }
    private record configFile(string xmlPathToSign, string signedXmlPath, string certPath, string certPassword);
}
