# Chauffeur.ContentImport

The Umbraco Packaging API allows you to import content through it, this [Chauffeur](https://github.com/aaronpowell/chauffeur) plugin allows content to be imported through that API.

**Note: This should not be used as a replacement of a full content promotion tool like Courier, it's for one-time content importing.**

# Usage

Add this plugin to your Umbraco + Chauffeur platform and then it can be run like so:

    umbraco> content-import PackageName

This will look for a file (sans `.xml` extension) which is an Umbraco Package and will import the content from within it.
