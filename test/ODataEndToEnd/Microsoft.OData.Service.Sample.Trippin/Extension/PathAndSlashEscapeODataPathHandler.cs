using System;
using System.Text;
using System.Web.OData.Routing;
using Microsoft.OData.Edm;

namespace Microsoft.OData.Service.Sample.Trippin.Extension
{
    /// <summary>
    /// This class is similar to class PathAndSlashEscapeODataPathHandler.cs in Web API OData samples ODataPathAndSlashEscapeSample.
    /// There are two major purpose of this class
    /// 1. When URL has %2F, it will be converted to "/" by IIS, need to convert back to "%2F", also need to make Web Api code does not convert again.
    /// 2. When URL has %5C, it will be converted to "/" but not "\" by IIS, so "\" need to use double escape
    /// 3. When URL use double escape, need to change back to single escape.
    /// 
    /// For end user case, slash does not need double escape, but backslash need double escape.
    /// For data stored in database, it is not escaped. 
    /// TODO, need to revisit when key alias is supported, which is tracked with https://github.com/OData/odata.net/issues/570
    /// </summary>
    public class PathAndSlashEscapeODataPathHandler : DefaultODataPathHandler
    {

        private const string EscapedQuote = "'";

        public override ODataPath Parse(string serviceRoot, string odataPath,
            IServiceProvider requestContainer)
        {
            if (!odataPath.Contains(EscapedQuote))
            {
                return base.Parse(serviceRoot, odataPath, requestContainer);
            }

            var pathBuilder = new StringBuilder();
            var queryStringIndex = odataPath.IndexOf('?');
            if (queryStringIndex == -1)
            {
                // In case there is double escape, replace them
                odataPath = odataPath.Replace("%255C", "%5C").Replace("%252F", "%2F");
                EscapeSlashBackslash(odataPath, pathBuilder);
            }
            else
            {
                var path = odataPath.Substring(0, queryStringIndex);
                // In case there is double escape, replace them
                path = path.Replace("%255C", "%5C").Replace("%252F", "%2F");
                EscapeSlashBackslash(path, pathBuilder);
                pathBuilder.Append(odataPath.Substring(queryStringIndex));
            }
            return base.Parse(serviceRoot, pathBuilder.ToString(), requestContainer);
        }

        private void EscapeSlashBackslash(string uri, StringBuilder pathBuilder)
        {
            const string slash = "%2F";
            const string backSlash = "%5C";

            var startIndex = uri.IndexOf(EscapedQuote, StringComparison.OrdinalIgnoreCase);
            var endIndex = uri.IndexOf(EscapedQuote, startIndex + EscapedQuote.Length, StringComparison.OrdinalIgnoreCase);
            if (startIndex == -1 || endIndex == -1)
            {
                pathBuilder.Append(uri);
                return;
            }

            endIndex = endIndex + EscapedQuote.Length;
            pathBuilder.Append(uri.Substring(0, startIndex));
            for (var i = startIndex; i < endIndex; ++i)
            {
                switch (uri[i])
                {
                    case '/':
                        pathBuilder.Append(slash);
                        break;
                    // There will not such case now as IIS will convert "\" to "/",need to use double escape
                    case '\\':
                        pathBuilder.Append(backSlash);
                        break;
                    default:
                        pathBuilder.Append(uri[i]);
                        break;
                }
            }
            EscapeSlashBackslash(uri.Substring(endIndex), pathBuilder);
        }
    }
}