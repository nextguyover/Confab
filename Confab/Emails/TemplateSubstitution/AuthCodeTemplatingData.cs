using Confab.Emails.TemplateSubstitution.Interfaces;

namespace Confab.Emails.TemplateSubstitution
{
    public class AuthCodeTemplatingData : AllEmailsTemplatingScaffold, ITemplate
    {
        public static string TemplateFile { private get; set; }
        public string GetTemplateFile() {  return TemplateFile; }

        public string AuthCode { get; set; }
        public string AuthCodeAutoLoginURL { get; set; }

        new public void Substitute(ref string template)
        {
            base.Substitute(ref template);

            template = template.Replace("#AuthCode#", AuthCode);
            template = template.Replace("#AuthCodeAutoLoginURL#", AuthCodeAutoLoginURL);
        }
    }
}
