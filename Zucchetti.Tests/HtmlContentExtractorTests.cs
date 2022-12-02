using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Zucchetti;

namespace Zucchetti.Tests
{
    public class HtmlContentExtractorTests
    {
        [Fact]
        public void ShouldExtractAuthRoute()
        {
            var expectedAuthRoute = "https://www.XXXX.it/auth/realms/XXXX/login-actions/authenticate?session_code=CODE&execution=EXEC&client_id=CLIENT";
            var paload = "<form id=\"kc-form-login\" onsubmit=\"login.disabled = true; return true;\"\r\naction=\"https://www.XXXX.it/auth/realms/XXXX/login-actions/authenticate?session_code=CODE&execution=EXEC&client_id=CLIENT\"\r\nmethod=\"post\">\r\n</form>";
            var authRoute = HtmlContentExtractor.ExtractAuthRoute(paload);

            Assert.Equal(expectedAuthRoute, authRoute);
        }

        [Fact]
        public void ShouldExtractSAML()
        {
            var expectedSAML = "SAML";
            var payload = "<form name=\"saml-post-binding\" method=\"post\" action=\"https://hrelas.hrz.it/HRElas/servlet/ushp_btrustsite\">\r\n    <input type=\"hidden\" name=\"SAMLResponse\" value=\"SAML\" />\r\n</form>";
            var SAML = HtmlContentExtractor.ExtractSAML(payload);

            Assert.Equal(expectedSAML, SAML);
        }
    }
}
