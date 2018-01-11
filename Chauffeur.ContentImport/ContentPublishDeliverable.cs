using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

namespace Chauffeur.ContentImport
{
    [DeliverableName("content-publish")]
    public class ContentPublishDeliverable : Deliverable
    {
        private readonly IContentService contentService;
        private static Regex flagsRegex = new Regex("-(?<flag>user|children)=(?<value>\\w*)");

        public ContentPublishDeliverable(
            TextReader reader,
            TextWriter writer,
            IContentService contentService) : base(reader, writer)
        {
            this.contentService = contentService;
        }

        public async override Task<DeliverableResponse> Run(string command, string[] args)
        {
            if (!args.Any())
                return await PublishRootContent();

            var flags = args.Where(arg => flagsRegex.IsMatch(arg))
                        .Select(arg => flagsRegex.Match(arg))
                        .ToDictionary(x => x.Groups["flag"].Value, x => x.Groups["value"].Value);

            var contentIds = args.Where(arg => !flagsRegex.IsMatch(arg)).Select(int.Parse);

            if (!contentIds.Any())
                return await PublishRootContent(
                    flags.ContainsKey("user") ? int.Parse(flags["user"]) : 0,
                    flags.ContainsKey("children") ? bool.Parse(flags["children"]) : false
                );

            return await PublishContent(
                contentIds,
                flags.ContainsKey("user") ? int.Parse(flags["user"]) : 0,
                flags.ContainsKey("children") && bool.TryParse(flags["children"], out bool children) ?
                        children :
                        false
            );
        }

        private async Task<DeliverableResponse> PublishContent(IEnumerable<int> contentIds, int userId, bool includeChildren)
        {
            var contents = contentService.GetByIds(contentIds);
            await Out.WriteLineAsync($"Publishing the following content: {string.Join(", ", contentIds)}");

            PublishContentInternal(contents, userId, includeChildren);

            return DeliverableResponse.Continue;
        }

        private async Task<DeliverableResponse> PublishRootContent(int userId = 0, bool includeChildren = false)
        {
            var contents = contentService.GetRootContent();
            await Out.WriteLineAsync("Publishing all root level content");

            PublishContentInternal(contents, userId, includeChildren);

            return DeliverableResponse.Continue;
        }

        private void PublishContentInternal(IEnumerable<IContent> contents, int userId, bool includeChildren)
        {
            foreach (var item in contents)
                if (includeChildren)
                    contentService.PublishWithChildrenWithStatus(item, userId, true);
                else
                    contentService.Publish(item, userId);
        }
    }
}
