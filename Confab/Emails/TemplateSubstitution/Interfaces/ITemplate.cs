namespace Confab.Emails.TemplateSubstitution.Interfaces
{
    public interface ITemplate
    {
        public void Substitute(ref string template);
        public string GetTemplateFile();
    }
}
