using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;

namespace Zucchetti
{
    internal class HtmlContentExtractor
    {
        internal static string ExtractAuthRoute(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var node = doc.GetElementbyId("kc-form-login");
            if (node is null)
            {
                throw new Exception("Cannot find login element");
            }

            var attrValue = node.GetAttributeValue("action", string.Empty);
            if (attrValue == string.Empty)
            {
                throw new Exception("Cannot find login url");
            }

            //Is there a better way to do the replacement (cleanup URL)?
            return attrValue.Replace("&amp;", "&");
        }

        internal static string ExtractSAML(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var node = doc.DocumentNode.Descendants("input").Where(node => node.GetAttributeValue("name", "") == "SAMLResponse").FirstOrDefault();
            if (node is null)
            {
                throw new Exception("Cannot find SAML element");
            }

            var attrValue = node.GetAttributeValue("value", string.Empty);
            if (attrValue == string.Empty)
            {
                throw new Exception("Cannot find SAML value");
            }

            return attrValue;
        }
    }
}
