using System;
using Apprenda.API.Extension.Bootstrapping;
using System.IO;
using System.Xml;

namespace BSP.WindowsAuthenticationP
{
    public class WindowsAuthenticationBootstrapPolicy : BootstrapperBase
    {
        public override BootstrappingResult Bootstrap(BootstrappingRequest bootstrappingRequest)
        {
            //Only modify .NET Websites Components that do not belong to the Apprenda Team
            if ((bootstrappingRequest.ComponentType == ComponentType.AspNet | 
                bootstrappingRequest.ComponentType == ComponentType.PublicAspNet | 
                bootstrappingRequest.ComponentType == ComponentType.WcfService) &&
                !bootstrappingRequest.DevelopmentTeamAlias.Equals("apprenda", StringComparison.InvariantCultureIgnoreCase))
            {
                return ModifyConfigFiles(bootstrappingRequest);
            }
            else
            {
                return BootstrappingResult.Success();
            }
        }

        private static BootstrappingResult ModifyConfigFiles(BootstrappingRequest bootstrappingRequest)
        {
            //Search for all web.config files within the component being deployed
            string[] configFiles = Directory.GetFiles(bootstrappingRequest.ComponentPath, "web.config", SearchOption.AllDirectories);

            foreach (string file in configFiles)
            {
                var result = ModifyXML(bootstrappingRequest, file);
                if (!result.Succeeded)
                {
                    //If an XML modification fails, return a failure for the BSP
                    return result;
                }
            }
            return BootstrappingResult.Success();

        }

        private static BootstrappingResult ModifyXML(BootstrappingRequest bootstrappingRequest, string filePath)
        {

            try
            {
                //Traverse the web.config file and find the required section
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(filePath);
                
                XmlNode systemWebServer = xmlDoc.SelectSingleNode("//system.webServer");

                XmlNode security = xmlDoc.CreateNode(XmlNodeType.Element, "security", null);
                XmlNode authentication = xmlDoc.CreateNode(XmlNodeType.Element, "authentication", null);
                XmlNode anonymousAuthentication = xmlDoc.CreateNode(XmlNodeType.Element, "anonymousAuthentication", null);
                XmlNode windowsAuthentication = xmlDoc.CreateNode(XmlNodeType.Element, "windowsAuthentication", null);
                XmlAttribute anonymousAuthenticationEnabled = xmlDoc.CreateAttribute("enabled");
                XmlAttribute windowsAuthenticationEnabled = xmlDoc.CreateAttribute("enabled");
                anonymousAuthenticationEnabled.Value = "false";
                windowsAuthenticationEnabled.Value = "true";
                anonymousAuthentication.Attributes.Append(anonymousAuthenticationEnabled);
                windowsAuthentication.Attributes.Append(windowsAuthenticationEnabled);
                authentication.AppendChild(anonymousAuthentication);
                authentication.AppendChild(windowsAuthentication);
                security.AppendChild(authentication);

                //If there is no Windows Authentication, add the section
                if (null == systemWebServer)
                {
                    systemWebServer = xmlDoc.CreateNode(XmlNodeType.Element, "system.webServer", null);
                    systemWebServer.AppendChild(security);

                    XmlNode configuration = xmlDoc.SelectSingleNode("//configuration");
                    configuration.AppendChild(systemWebServer);
                    xmlDoc.Save(filePath);
                    return BootstrappingResult.Success();
                }
                //If System.webserver is found, append the security section
                else if (null != systemWebServer)
                {
                    systemWebServer.AppendChild(security);
                    xmlDoc.Save(filePath);
                    return BootstrappingResult.Success();
                }
                return BootstrappingResult.Success();
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                {
                    return BootstrappingResult.Failure(new[] { ex.InnerException.Message });
                }
                else
                {
                    return BootstrappingResult.Failure(new[] { ex.Message });
                }
            }

        }
    }
}
