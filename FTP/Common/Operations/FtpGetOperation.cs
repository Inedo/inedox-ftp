using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Inedo.Agents;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Extensions.FTP.Credentials;
using Inedo.IO;
#if BuildMaster
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Credentials;
using Inedo.BuildMaster.Extensibility.Operations;
#elif Otter
using Inedo.Otter.Extensibility;
using Inedo.Otter.Extensibility.Credentials;
using Inedo.Otter.Extensibility.Operations;
#endif

namespace Inedo.Extensions.FTP.Operations
{
    [DisplayName("Get FTP Files")]
    [Description("Downloads files from an FTP server.")]
    [ScriptAlias("Get-Files")]
    public sealed class FtpGetOperation : FtpTransferOperationBase
    {
        [DisplayName("Credentials")]
        [ScriptAlias("Credentials")]
        public override string CredentialName { get; set; }

        private long progressTotal = 0;
        private long progressNow = 0;

        public override OperationProgress GetProgress()
        {
            return new OperationProgress(this.progressTotal == 0 ? null : (int?)(this.progressNow * 100 / this.progressTotal));
        }

        public override async Task ExecuteAsync(IOperationExecutionContext context)
        {
            var fileOps = await context.Agent.GetServiceAsync<IFileOperationsExecuter>().ConfigureAwait(false);
            var basePath = context.ResolvePath(this.LocalPath);

            this.LogDebug("Retrieving remote file listing...");
            var files = await this.CreateRequest(this.ServerPath).GetDirectoryListingRecursiveAsync(path => this.CreateRequest(path), context.CancellationToken).ConfigureAwait(false);

            var mask = new MaskingContext(this.Includes, this.Excludes);
            var matches = files.Where(f => mask.IsMatch(f.FullName.Substring(this.ServerPath.Length).Trim('\\', '/'))).ToList();
            this.LogInformation($"File mask matched {matches.Count} of {files.Count} files.");

            var localFiles = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
            if (this.OnlyNewer)
            {
                this.LogDebug("Retrieving local file listing...");
                var localMatches = await fileOps.GetFileSystemInfosAsync(basePath, mask).ConfigureAwait(false);
                foreach (var file in localMatches.OfType<SlimFileInfo>())
                {
                    localFiles[file.FullName.Substring(basePath.Length).Trim('\\', '/').Replace('\\', '/')] = file.LastWriteTimeUtc;
                }
            }

            foreach (var dir in matches.OfType<SlimDirectoryInfo>())
            {
                await fileOps.CreateDirectoryAsync(fileOps.CombinePath(basePath, dir.FullName.Substring(this.ServerPath.Length))).ConfigureAwait(false);
            }

            var toTransfer = new List<SlimFileInfo>();

            foreach (var file in matches.OfType<SlimFileInfo>())
            {
                var fullPath = file.FullName;
                var path = fullPath.Substring(this.ServerPath.Length).Trim('\\', '/').Replace('\\', '/');
                if (localFiles.ContainsKey(path) && localFiles[path] > file.LastWriteTimeUtc)
                {
                    if (this.VerboseLogging)
                    {
                        this.LogDebug($"Not requesting \"{fullPath}\": remote file was last modified at {file.LastWriteTimeUtc}, but local file was last modified at {localFiles[path]}");
                    }
                    continue;
                }

                toTransfer.Add(file);
                progressTotal += 100 + file.Size;
            }

            using (var sem = new SemaphoreSlim(10, 10))
            {
                var tasks = new List<Task>();

                foreach (var f in toTransfer)
                {
                    // copy variable to inside of block
                    var file = f;

                    await sem.WaitAsync().ConfigureAwait(false);
                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            var path = file.FullName.Substring(this.ServerPath.Length).Trim('\\', '/').Replace('\\', '/');

                            var req = this.CreateRequest(file.FullName);
                            req.Method = WebRequestMethods.Ftp.DownloadFile;
                            try
                            {
                                using (var response = await req.GetResponseAsync(context.CancellationToken).ConfigureAwait(false))
                                {
                                    using (var responseStream = response.GetResponseStream())
                                    using (var fileStream = await fileOps.OpenFileAsync(fileOps.CombinePath(basePath, path), FileMode.Create, FileAccess.Write).ConfigureAwait(false))
                                    {
                                        Interlocked.Add(ref this.progressNow, 100);

                                        long lastProgress = 0;
                                        await responseStream.CopyToAsync(fileStream, 8192, context.CancellationToken, p =>
                                        {
                                            Interlocked.Add(ref this.progressNow, p - lastProgress);
                                            lastProgress = p;
                                        }).ConfigureAwait(false);

                                        Interlocked.Add(ref this.progressNow, file.Size - lastProgress);
                                    }
                                }
                            }
                            catch (WebException ex)
                            {
                                if (ex.Status == WebExceptionStatus.ProtocolError)
                                {
                                    this.LogError($"Retrieving {path} failed: server returned {(ex.Response as FtpWebResponse)?.StatusDescription}");
                                }
                                else
                                {
                                    this.LogError($"Retrieving {path} failed: {ex}");
                                }
                            }
                        }
                        finally
                        {
                            sem.Release();
                        }
                    }));
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            var credential = string.IsNullOrEmpty(config[nameof(CredentialName)]) ? null : ResourceCredentials.Create<FtpCredentials>(config[nameof(CredentialName)]);
            var hostName = config[nameof(HostName)].ToString() ?? credential?.HostName;
            return new ExtendedRichDescription(
                new RichDescription("Download ", new MaskHilite(config[nameof(Includes)], config[nameof(Excludes)])),
                new RichDescription("from ", new Hilite(hostName))
            );
        }
    }
}
