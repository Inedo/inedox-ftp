using System;
using System.ComponentModel;
using Inedo.Documentation;
using Inedo.Extensibility.SecureResources;
using Inedo.Serialization;
using UsernamePasswordCredentials = Inedo.Extensions.Credentials.UsernamePasswordCredentials;

namespace Inedo.Extensions.FTP.Credentials
{
    [DisplayName("FTP Server")]
    [Description("Host and port for the File Transfer Protocol")]
    [Serializable]
    public class FtpSecureResource : SecureResource
    {
        [Persistent]
        [Required]
        [DisplayName("FTP server")]
        public string HostName { get; set; }

        [Persistent]
        [DefaultValue(21)]
        public int Port { get; set; } = 21;

        public override Type[] CompatibleCredentials => new[] { typeof(UsernamePasswordCredentials) };

        public override RichDescription GetDescription()
        {
            return new RichDescription("ftp://", new Hilite(this.HostName));
        }
    }
}
