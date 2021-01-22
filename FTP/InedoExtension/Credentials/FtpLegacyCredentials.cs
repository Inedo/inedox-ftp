using System.ComponentModel;
using System.Security;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Credentials;
using Inedo.Serialization;
using Inedo.Web;

namespace Inedo.Extensions.FTP.Credentials
{
    [SlimSerializable]
    [DisplayName("FTP")]
    [Description("Username and password for the File Transfer Protocol")]
    [ScriptAlias("FTP")]
    public sealed class FtpCredentials : ResourceCredentials
    {
        [Persistent]
        [Required]
        [DisplayName("FTP server")]
        public string HostName { get; set; }

        [Persistent]
        [DefaultValue(21)]
        public int Port { get; set; } = 21;

        [Persistent]
        [DisplayName("Username")]
        [DefaultValue("anonymous")]
        public string UserName { get; set; } = "anonymous";

        [Persistent(Encrypted = true)]
        [FieldEditMode(FieldEditMode.Password)]
        public SecureString Password { get; set; }

        public override RichDescription GetDescription()
        {
            return new RichDescription("ftp://", new Hilite(this.UserName), "@", new Hilite(this.HostName));
        }
    }
}
