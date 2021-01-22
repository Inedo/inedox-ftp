using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Credentials;
using Inedo.Extensibility.Operations;
using Inedo.Extensions.FTP.Credentials;
using Inedo.IO;

namespace Inedo.Extensions.FTP.Operations
{
    [DisplayName("Delete FTP Files")]
    [Description("Deletes files from an FTP server.")]
    [ScriptAlias("Delete-Files")]
    public sealed class FtpDeleteOperation : FtpOperationBase
    {

        private long progressTotal = 0;
        private long progressNow = 0;

        public override OperationProgress GetProgress()
        {
            return new OperationProgress(this.progressTotal == 0 ? null : (int?)(this.progressNow * 100 / this.progressTotal));
        }

        public override async Task ExecuteAsync(IOperationExecutionContext context)
        {
            this.LogDebug("Retrieving remote file listing...");
            var (credentials, resource) = this.GetCredentialsAndResource(context as ICredentialResolutionContext);
            var files = await this.CreateRequest(credentials, resource, this.ServerPath).GetDirectoryListingRecursiveAsync(path => this.CreateRequest(credentials, resource, path), this.UseCurrentDateOnDateParseError, context.CancellationToken).ConfigureAwait(false);

            var mask = new MaskingContext(this.Includes, this.Excludes);
            var matches = files.Where(f => mask.IsMatch(f.FullName.Substring(this.ServerPath.Length).Trim('\\', '/'))).ToList();
            this.LogInformation($"File mask matched {matches.Count} of {files.Count} files.");

            // Make sure directories are deleted after their contents.
            matches.Reverse();

            progressTotal = matches.Count;

            foreach (var file in matches)
            {
                try
                {
                    var req = this.CreateRequest(credentials, resource, file.FullName);
                    req.Method = file is SlimFileInfo ? WebRequestMethods.Ftp.DeleteFile : WebRequestMethods.Ftp.RemoveDirectory;
                    (await req.GetResponseAsync(context.CancellationToken).ConfigureAwait(false))?.Dispose();
                    progressNow++;
                }
                catch (WebException ex)
                {
                    if (ex.Status == WebExceptionStatus.ProtocolError)
                    {
                        this.LogError($"Deleting {file.FullName} failed: server returned {(ex.Response as FtpWebResponse)?.StatusDescription}");
                    }
                    else
                    {
                        this.LogError($"Deleting {file.FullName} failed: {ex}");
                    }
                }
            }
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            var hostname = config[nameof(HostName)].ToString();
            hostname = string.IsNullOrWhiteSpace(hostname) ? config[nameof(ResourceName)] : hostname;
            return new ExtendedRichDescription(
                new RichDescription("Delete ", new MaskHilite(config[nameof(Includes)], config[nameof(Excludes)])),
                new RichDescription("from ", new Hilite(hostname))
            );
        }
    }
}
