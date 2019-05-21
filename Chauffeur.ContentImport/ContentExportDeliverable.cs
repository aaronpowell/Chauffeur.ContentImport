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
    [DeliverableName("content-export")]
    public class ContentExportDeliverable : Deliverable
    {
        private readonly IFileSystem fileSystem;
        private readonly IChauffeurSettings settings;
        private readonly IPackagingService packagingService;
        private readonly IContentService contentService;

        public ContentExportDeliverable(
            TextReader reader,
            TextWriter writer,
            IFileSystem fileSystem,
            IChauffeurSettings settings,
            IPackagingService packagingService,
            IContentService contentService)
            : base(reader, writer)
        {
            this.fileSystem = fileSystem;
            this.settings = settings;
            this.packagingService = packagingService;
            this.contentService = contentService;
        }

        public override async Task<DeliverableResponse> Run(string command, string[] args)
        {
            if (!args.Any())
            {
                await Out.WriteLineAsync("No packages were provided, use `help content-export` to see usage");
                return DeliverableResponse.Continue;
            }

            string chauffeurFolder;
            if (!settings.TryGetChauffeurDirectory(out chauffeurFolder))
                return DeliverableResponse.Continue;

            var tasks = args.Select(arg => Pack(arg, chauffeurFolder));
            await Task.WhenAll(tasks);

            return DeliverableResponse.Continue;
        }

        private async Task Pack(string name, string chauffeurFolder)
        {
            var fileLocation = fileSystem.Path.Combine(chauffeurFolder, name + ".xml");
            if (fileSystem.File.Exists(fileLocation))
            {
                await Out.WriteLineAsync($"The package '{name}' already in the Chauffeur folder");
                return;
            }

            await Out.WriteLineAsync($"Exporting content to {name}.xml");

            var content = contentService.GetRootContent();

            var xml = new XDocument();
            xml.Add(new XElement("umbPackage",
                new XElement("Documents",
                    new XElement("DocumentSet",
                        content.Select(c => packagingService.Export(c, true)), new XAttribute("importMode", "root")
                    )
                )
            ));
            
            settings.TryGetChauffeurDirectory(out string dir);
            fileSystem.File.WriteAllText(fileSystem.Path.Combine(dir, $"{name}.xml"), xml.ToString());

            await Out.WriteLineAsync($"Content have been exported to {name}");
        }
    }
}
