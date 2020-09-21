using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Inedo.IO;

namespace Inedo.Extensions.FTP
{
    internal static class FtpWebRequestExtensions
    {
        internal static async Task<FtpWebResponse> GetResponseAsync(this FtpWebRequest ftp, CancellationToken cancellationToken)
        {
            using (cancellationToken.Register(ftp.Abort))
            {
                return (FtpWebResponse)await ftp.GetResponseAsync().ConfigureAwait(false);
            }
        }

        internal static async Task<IReadOnlyList<SlimFileSystemInfo>> GetDirectoryListingAsync(this FtpWebRequest ftp, bool useCurrentDateOnDateParseError, CancellationToken cancellationToken)
        {
            ftp.Method = WebRequestMethods.Ftp.ListDirectoryDetails;

            var lines = new List<string>();

            using (var response = await ftp.GetResponseAsync(cancellationToken).ConfigureAwait(false))
            {
                using (var responseReader = new StreamReader(response.GetResponseStream(), InedoLib.UTF8Encoding))
                {
                    for (;;)
                    {
                        var line = await responseReader.ReadLineAsync().ConfigureAwait(false);
                        if (line == null)
                            break;

                        lines.Add(line);
                    }
                }
            }

            if (lines.Count == 0)
            {
                return new SlimFileSystemInfo[0];
            }
            var parseFile = GuessFileEntryParser(lines);
            return lines.Select(line => parseFile(line, ftp.RequestUri.AbsolutePath, useCurrentDateOnDateParseError)).Where(f => f.Name != "." && f.Name != "..").ToList();
        }

        private static readonly LazyRegex UnixFileListStyle = new LazyRegex(@"^[d\-](?:[r\-][w\-][x\-]){3}");
        private static readonly LazyRegex WindowsFileListStyle = new LazyRegex(@"[0-9][0-9]-[0-9][0-9]-[0-9][0-9]");

        private static Func<string, string, bool, SlimFileSystemInfo> GuessFileEntryParser(IEnumerable<string> lines)
        {
            foreach (string line in lines)
            {
                if (UnixFileListStyle.IsMatch(line))
                {
                    return ParseUnixStyleFileEntry;
                }
                else if (WindowsFileListStyle.IsMatch(line))
                {
                    return ParseWindowsStyleFileEntry;
                }
            }
            throw new InvalidOperationException("Cannot parse file entry with format " + lines.First());
        }

        // Examples are from http://cr.yp.to/ftpparse.html
        private static SlimFileSystemInfo ParseWindowsStyleFileEntry(string line, string basePath, bool UseCurrentDateOnDateParseError)
        {
            // 04-27-00  09:09PM       <DIR>          licensed
            // 07-18-00  10:16AM       <DIR>          pub
            // 04-14-00  03:47PM                  589 readme.htm
            var parts = line.Split(new[] { ' ' }, 4, StringSplitOptions.RemoveEmptyEntries);
            var path = Path.Combine(basePath, parts[3]);
            DateTime lastWriteTime;
            try
            {
                lastWriteTime = DateTime.Parse(parts[0] + " " + parts[1]);
            }
            catch(FormatException ex)
            {
                if (UseCurrentDateOnDateParseError)
                {
                    lastWriteTime = DateTime.UtcNow;
                }
                else
                {
                    throw new FormatException($"String was not recognized as a valid DateTime. Parsed \"{parts[0]} {parts[1]}\" from line \"{line}\" for the date.", ex);
                }               
            }

            if (string.Equals(parts[2], "<DIR>", StringComparison.OrdinalIgnoreCase))
            {
                return new SlimDirectoryInfo(path, lastWriteTime, FileAttributes.Directory);
            }

            return new SlimFileInfo(path, lastWriteTime, long.Parse(parts[2]), 0);
        }

        private static SlimFileSystemInfo ParseUnixStyleFileEntry(string line, string basePath, bool UseCurrentDateOnDateParseError)
        {
            // -rw-r--r--   1 root     other        531 Jan 29 03:26 README
            // dr-xr-xr-x   2 root     other        512 Apr  8  1994 etc
            // lrwxrwxrwx   1 root     other          7 Jan 25 00:17 bin -> usr/bin
            // ----------   1 owner    group         1803128 Jul 10 10:18 ls-lR.Z
            // d---------   1 owner    group               0 May  9 19:45 Softlib
            // -rwxrwxrwx   1 noone    nogroup      322 Aug 19  1996 message.ftp
            var parts = line.Split(new[] { ' ' }, 9, StringSplitOptions.RemoveEmptyEntries);
            var path = PathEx.Combine(basePath, parts[8]);
            DateTime lastWriteTime;
            try {
                lastWriteTime = DateTime.Parse(parts[5] + " " + parts[6] + " " + parts[7]);
            }
            catch (FormatException ex)
            {
                if (UseCurrentDateOnDateParseError)
                {
                    lastWriteTime = DateTime.UtcNow;
                }
                else
                {
                    throw new FormatException($"String was not recognized as a valid DateTime. Parsed \"{parts[0]} {parts[1]}\" from line \"{line}\" for the date.", ex);
                }
            }

            FileAttributes attributes = 0;
            if (parts[8].StartsWith("."))
                attributes |= FileAttributes.Hidden;
            if (!parts[0].Contains("w"))
                attributes |= FileAttributes.ReadOnly;

            if (parts[0][0] == 'l')
            {
                var link = parts[8].Split(new[] { " -> " }, 2, StringSplitOptions.None);
                if (link.Length == 2)
                {
                    path = PathEx.Combine(basePath, link[0]);
                }
            }

            if (parts[0][0] == 'd')
            {
                return new SlimDirectoryInfo(path, lastWriteTime, attributes | FileAttributes.Directory);
            }
            return new SlimFileInfo(path, lastWriteTime, long.Parse(parts[4]), attributes);
        }

        internal static async Task<IReadOnlyList<SlimFileSystemInfo>> GetDirectoryListingRecursiveAsync(this FtpWebRequest ftp, Func<string, FtpWebRequest> newRequest, bool useCurrentDateOnDateParseError, CancellationToken cancellationToken)
        {
            var list = new List<SlimFileSystemInfo>();
            var added = await ftp.GetDirectoryListingAsync(useCurrentDateOnDateParseError, cancellationToken).ConfigureAwait(false);
            await AddDirectoryListingRecursiveAsync(list, added, newRequest, useCurrentDateOnDateParseError, cancellationToken).ConfigureAwait(false);
            return list;
        }

        private static async Task AddDirectoryListingRecursiveAsync(List<SlimFileSystemInfo> list, IReadOnlyList<SlimFileSystemInfo> added, Func<string, FtpWebRequest> newRequest, bool useCurrentDateOnDateParseError, CancellationToken cancellationToken)
        {
            list.AddRange(added);
            var directories = added.OfType<SlimDirectoryInfo>();
            if (directories.Any())
            {
                var contents = await Task.WhenAll(directories.Select(d => newRequest(d.FullName).GetDirectoryListingAsync(useCurrentDateOnDateParseError, cancellationToken))).ConfigureAwait(false);
                await AddDirectoryListingRecursiveAsync(list, contents.SelectMany(d => d).ToList(), newRequest, useCurrentDateOnDateParseError, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
