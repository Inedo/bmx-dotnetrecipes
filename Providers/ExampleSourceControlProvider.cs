using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ICSharpCode.SharpZipLib.Zip;
using Inedo.BuildMaster.Extensibility.Providers;
using Inedo.BuildMaster.Extensibility.Providers.SourceControl;
using Inedo.BuildMaster.Files;

namespace Inedo.BuildMasterExtensions.DotNetRecipes.Providers
{
    [ProviderProperties(
        "Example",
        "Contains the source code for sample applications.")]
    internal sealed class ExampleSourceControlProvider : SourceControlProviderBase, ILabelingProvider, IRevisionProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExampleSourceControlProvider"/> class.
        /// </summary>
        public ExampleSourceControlProvider()
        {
        }

        public override char DirectorySeparator
        {
            get { return '/'; }
        }

        public override void GetLatest(string sourcePath, string targetPath)
        {
            this.GetLabeled("1.1", sourcePath, targetPath);
        }
        public override DirectoryEntryInfo GetDirectoryEntryInfo(string sourcePath)
        {
            sourcePath = sourcePath ?? string.Empty;

            var root = new DirectoryEntryBuilder(string.Empty);
            var all = new Dictionary<string, DirectoryEntryBuilder>();
            all.Add(string.Empty, root);

            Func<string, DirectoryEntryBuilder> find = p =>
            {
                var parts = p.Split('/');
                var current = root;
                foreach (var _part in parts)
                {
                    var part = _part;
                    var next = current.Directories.Where(d => d.Name == part).FirstOrDefault();
                    if (next == null)
                    {
                        next = current.Directories.Add(part);
                        all[next.Path] = next;
                    }

                    current = next;
                }

                return current;
            };

            using (var stream = OpenZipFile("1.1"))
            using (var zipFile = new ZipFile(stream))
            {
                foreach (var entry in zipFile.Cast<ZipEntry>().Where(e => e.IsFile))
                {
                    var fileName = entry.Name.Substring(entry.Name.LastIndexOf('/') + 1);
                    var directoryName = entry.Name.Substring(0, entry.Name.Length - fileName.Length).Trim('/');

                    var dir = find(directoryName);
                    if (fileName != "__ZIP_MARKER__")
                        dir.AddFile(fileName, entry.Size, entry.DateTime, (FileAttributes)entry.ExternalFileAttributes);
                }
            }

            DirectoryEntryBuilder value;
            if (all.TryGetValue(sourcePath, out value))
                return value.ToDirectoryEntryInfo("/");
            else
                throw new FileNotFoundException();
        }
        public override byte[] GetFileContents(string filePath)
        {
            using (var stream = OpenZipFile("1.1"))
            using (var zipFile = new ZipFile(stream))
            {
                var entry = zipFile.GetEntry(filePath);
                using (var entryStream = zipFile.GetInputStream(entry))
                {
                    var buffer = new byte[entry.Size];
                    entryStream.Read(buffer, 0, buffer.Length);
                    return buffer;
                }
            }
        }
        public void ApplyLabel(string label, string sourcePath)
        {
        }
        public void GetLabeled(string label, string sourcePath, string targetPath)
        {
            sourcePath = (sourcePath ?? string.Empty).Trim('/');

            using (var stream = OpenZipFile(label))
            using (var zipFile = new ZipFile(stream))
            {
                var buffer = new byte[1000];

                foreach (var entry in zipFile.Cast<ZipEntry>().Where(e => e.IsFile && e.Name.StartsWith(sourcePath)))
                {
                    var fsPath = entry.Name.Substring(sourcePath.Length).Replace('/', '\\').Trim('\\');
                    if (fsPath.Contains("\\"))
                        Directory.CreateDirectory(Path.Combine(targetPath, Path.GetDirectoryName(fsPath)));

                    using (var inStream = zipFile.GetInputStream(entry))
                    using (var outStream = File.Create(Path.Combine(targetPath, fsPath)))
                    {
                        int length = inStream.Read(buffer, 0, buffer.Length);
                        while (length > 0)
                        {
                            outStream.Write(buffer, 0, length);
                            length = inStream.Read(buffer, 0, buffer.Length);
                        }
                    }

                    File.SetLastWriteTime(Path.Combine(targetPath, fsPath), entry.DateTime);
                }
            }
        }
        public object GetCurrentRevision(string path)
        {
            return 0;
        }
        public override bool IsAvailable()
        {
            return true;
        }
        public override void ValidateConnection()
        {
        }
        public override string ToString()
        {
            return "Contains the source code for sample applications.";
        }

        private static Stream OpenZipFile(string version)
        {
            try
            {
                var ver = new Version(version);
                if (ver >= new Version(1, 1))
                    return typeof(ExampleSourceControlProvider).Assembly.GetManifestResourceStream("Inedo.BuildMasterExtensions.DotNetRecipes.Examples1.1.zip");
            }
            catch
            {
            }

            return typeof(ExampleSourceControlProvider).Assembly.GetManifestResourceStream("Inedo.BuildMasterExtensions.DotNetRecipes.Examples1.0.zip");
        }
    }
}
