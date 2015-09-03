using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Chauffeur.Host;
using Umbraco.Core.Services;

namespace Chauffeur.ContentImport
{
    [DeliverableName("content-import")]
    public class ContentImportDeliverable : Deliverable
    {
        private readonly IFileSystem fileSystem;
        private readonly IChauffeurSettings settings;
        private readonly IPackagingService packagingService;

        public ContentImportDeliverable(
            TextReader reader,
            TextWriter writer,
            IFileSystem fileSystem,
            IChauffeurSettings settings,
            IPackagingService packagingService)
            : base(reader, writer)
        {
            this.fileSystem = fileSystem;
            this.settings = settings;
            this.packagingService = packagingService;
        }

        public override async Task<DeliverableResponse> Run(string command, string[] args)
        {
            if (!args.Any())
            {
                await Out.WriteLineAsync("No packages were provided, use `help content-import` to see usage");
                return DeliverableResponse.Continue;
            }

            string chauffeurFolder;
            if (!settings.TryGetChauffeurDirectory(out chauffeurFolder))
                return DeliverableResponse.Continue;

            var tasks = args.Select(arg => Unpack(arg, chauffeurFolder));
            await Task.WhenAll(tasks);

            return DeliverableResponse.Continue;
        }

        private async Task Unpack(string name, string chauffeurFolder)
        {
            var fileLocation = fileSystem.Path.Combine(chauffeurFolder, name + ".xml");
            if (!fileSystem.File.Exists(fileLocation))
            {
                await Out.WriteLineAsync(string.Format("The package '{0}' is not found in the Chauffeur folder", name));
                return;
            }

            using (var stream = fileSystem.File.OpenRead(fileLocation))
            {
                var xml = XDocument.Load(stream);

                var documentNode = xml.Root.Element("Documents");

                if (documentNode == null)
                {
                    await Out.WriteLineAsync(string.Format("No documents found in package '{0}'. Moving on", name));
                    return;
                }

                var documentSets = documentNode.Elements("DocumentSet");

                documentSets.Select(ds => packagingService.ImportContent(ds));

                await Out.WriteLineAsync(string.Format("Documents have been imported from '{0}'.", name));
            }
        }
    }
}
