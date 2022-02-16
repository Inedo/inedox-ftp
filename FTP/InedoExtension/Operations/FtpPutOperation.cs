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
using Inedo.Extensibility;
using Inedo.Extensibility.Credentials;
using Inedo.Extensibility.Operations;
using Inedo.Extensions.FTP.Credentials;
using Inedo.IO;

namespace Inedo.Extensions.FTP.Operations
{
    [DisplayName("Send FTP Files")]
    [Description("Uploads files to an FTP server.")]
    [ScriptAlias("Send-Files")]
    public sealed class FtpPutOperation : FtpTransferOperationBase
    {

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

            this.LogDebug("Retrieving local file listing...");
            var mask = new MaskingContext(this.Includes, this.Excludes);
            var matches = await fileOps.GetFileSystemInfosAsync(basePath, mask).ConfigureAwait(false);
            this.LogInformation($"File mask matched {matches.Count} files.");
            var (credentials, resource) = this.GetCredentialsAndResource(context as ICredentialResolutionContext);

            var remoteFiles = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
            if (this.OnlyNewer)
            {
                this.LogDebug("Retrieving file listing...");
                var files = await this.CreateRequest(credentials, resource, this.ServerPath).GetDirectoryListingRecursiveAsync(path => this.CreateRequest(credentials, resource, path), this.UseCurrentDateOnDateParseError, context.CancellationToken).ConfigureAwait(false);

                var remoteMatches = files.Where(f => mask.IsMatch(f.FullName.Substring(this.ServerPath.Length).Trim('\\', '/'))).ToList();
                foreach (var file in remoteMatches.OfType<SlimFileInfo>())
                {
                    remoteFiles[file.FullName.Substring(this.ServerPath.Length).Trim('\\', '/').Replace('\\', '/')] = file.LastWriteTimeUtc;
                }
            }

            foreach (var dir in matches.OfType<SlimDirectoryInfo>())
            {
                try
                {
                    var req = this.CreateRequest(credentials, resource, fileOps.CombinePath(this.ServerPath, dir.FullName.Substring(basePath.Length)));
                    req.Method = WebRequestMethods.Ftp.MakeDirectory;
                    (await req.GetResponseAsync(context.CancellationToken).ConfigureAwait(false))?.Dispose();
                }
                catch (WebException ex) when (ex.Status == WebExceptionStatus.ProtocolError && (ex.Response as FtpWebResponse)?.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
                {
                    // ignore directory creation failures as they're probably due to the directory already existing.
                }
                catch (WebException ex)
                {
                    if (ex.Status == WebExceptionStatus.ProtocolError)
                    {
                        this.LogError($"Creating {dir.FullName.Substring(basePath.Length)} failed: server returned {(ex.Response as FtpWebResponse)?.StatusDescription}");
                    }
                    else
                    {
                        this.LogError($"Creating {dir.FullName.Substring(basePath.Length)} failed: {ex}");
                    }
                }
            }

            var toTransfer = new List<SlimFileInfo>();

            foreach (var file in matches.OfType<SlimFileInfo>())
            {
                var fullPath = file.FullName;
                var path = fullPath.Substring(basePath.Length).Trim('\\', '/').Replace('\\', '/');
                if (remoteFiles.ContainsKey(path) && remoteFiles[path] > file.LastWriteTimeUtc)
                {
                    if (this.VerboseLogging)
                    {
                        this.LogDebug($"Not sending \"{fullPath}\": local file was last modified at {file.LastWriteTimeUtc}, but remote file was last modified at {remoteFiles[path]}");
                    }
                    continue;
                }

                toTransfer.Add(file);
                progressTotal += 100 + file.Size;
            }

            using var sem = new SemaphoreSlim(10, 10);
            var tasks = new List<Task>();

            foreach (var f in toTransfer)
            {
                // copy variable to inside of block
                var file = f;

                await sem.WaitAsync().ConfigureAwait(false);
                tasks.Add(Task.Run(async () =>
                {
                    var path = file.FullName.Substring(this.LocalPath.Length).Trim('\\', '/').Replace('\\', '/');

                    try
                    {
                        var req = this.CreateRequest(credentials, resource, PathEx.Combine(this.ServerPath, path));
                        req.Method = WebRequestMethods.Ftp.UploadFile;
                        using (var fileStream = await fileOps.OpenFileAsync(file.FullName, FileMode.Open, FileAccess.Read).ConfigureAwait(false))
                        using (var requestStream = await req.GetRequestStreamAsync().ConfigureAwait(false))
                        {
                            long lastProgress = 0;
                            await fileStream.CopyToAsync(requestStream, 8192, context.CancellationToken, p =>
                            {
                                Interlocked.Add(ref this.progressNow, p - lastProgress);
                                lastProgress = p;
                            }).ConfigureAwait(false);

                            Interlocked.Add(ref this.progressNow, file.Size - lastProgress);
                        }
                        using (await req.GetResponseAsync(context.CancellationToken).ConfigureAwait(false))
                        {
                            Interlocked.Add(ref this.progressNow, 100);
                        }
                    }
                    catch (WebException ex)
                    {
                        if (ex.Status == WebExceptionStatus.ProtocolError)
                        {
                            this.LogError($"Sending {path} failed: server returned {(ex.Response as FtpWebResponse)?.StatusDescription}");
                        }
                        else
                        {
                            this.LogError($"Sending {path} failed: {ex}");
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

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            var hostname = config[nameof(HostName)].ToString();
            hostname = string.IsNullOrWhiteSpace(hostname) ? config[nameof(ResourceName)] : hostname;
            return new ExtendedRichDescription(
                new RichDescription("Upload ", new MaskHilite(config[nameof(Includes)], config[nameof(Excludes)])),
                new RichDescription("to ", new Hilite(hostname))
            );
        }
    }
}
