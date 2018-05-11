using System.ComponentModel;
using Inedo.Documentation;
using Inedo.Extensibility;

namespace Inedo.Extensions.FTP.Operations
{
    public abstract class FtpTransferOperationBase : FtpOperationBase
    {
        [DisplayName("Local path")]
        [ScriptAlias("LocalPath")]
        [PlaceholderText("$WorkingDirectory")]
        public string LocalPath { get; set; }

        [Description("Only transfer files that are newer than the destination.")]
        [ScriptAlias("OnlyNewer")]
        [DefaultValue(false)]
        [Category("Advanced")]
        public bool OnlyNewer { get; set; } = false;
    }
}
