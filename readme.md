# Chauffeur.ContentImport

| Build type | Status | NuGet |
| --- | --- | --- |
| master | [![Build status](https://ci.appveyor.com/api/projects/status/ih7j4u7yyl7xj6re/branch/master?svg=true)](https://ci.appveyor.com/project/aaronpowell/chauffeur-ContentImport/branch/master) | [![NuGet Badge](https://buildstats.info/nuget/Chauffeur.ContentImport)](https://www.nuget.org/packages/Chauffeur.ContentImport/) |
| dev | [![Build status](https://ci.appveyor.com/api/projects/status/ih7j4u7yyl7xj6re?svg=true)](https://ci.appveyor.com/project/aaronpowell/chauffeur-ContentImport) | [![NuGet Badge](https://buildstats.info/nuget/Chauffeur.ContentImport?includePreReleases=true)](https://www.nuget.org/packages/Chauffeur.ContentImport/) |


The Umbraco Packaging API allows you to import and publish content through it, this [Chauffeur](https://github.com/aaronpowell/chauffeur) plugin allows content to be imported through that API.

**Note: This should not be used as a replacement of a full content promotion tool like Courier, it's for one-time content importing.**

# Usage

## `content-import`

    umbraco> content-import PackageName

This will look for a file (sans `.xml` extension) which is an Umbraco Package and will import the content from within it.

## `content-export`

    umbraco> content-export PackageName

This will export an xml file which is an Umbraco Package containing the content of your site.

## `content-publish`

    umbraco> content-publish <?ids> -user=<userId> -children=true|false

This will publish the provided content ID's and allow you to optionally provide the UserID to publish the content under and whether or not you want to include the children in your publish run.

Some notes on how it works:

- If you don't provide any ID's then it'll publish all root content items
- If you don't provide a userID it'll default to `0` (which is what Umbraco does internally)
- If you don't provide the `children` flag it won't publish the child nodes
- If the web server is running you'll need to recycle the app pool afterwards as this can only update the `umbraco.config` and Umbraco database, it can't do the in-memory cache