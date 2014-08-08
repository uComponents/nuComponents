﻿
namespace nuPickers.Shared.XmlDataSource
{
    using nuPickers.Shared.Editor;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;
    using System.Xml;
    using System.Xml.XPath;
    using umbraco;

    public class XmlDataSource
    {
        public string XmlData { get; set; }

        public string Url { get; set; }

        public string XPath { get; set; }
        
        public string KeyXPath { get; set; }
        
        public string LabelXPath { get; set; }

        public IEnumerable<EditorDataItem> GetEditorDataItems(int contextId)
        {
            XmlDocument xmlDocument;
            List<EditorDataItem> editorDataItems = new List<EditorDataItem>();

            switch (this.XmlData)
            {
                case "content":
                    xmlDocument = uQuery.GetPublishedXml(uQuery.UmbracoObjectType.Document);
                    break;

                case "media":
                    xmlDocument = uQuery.GetPublishedXml(uQuery.UmbracoObjectType.Media);
                    break;

                case "members":
                    xmlDocument = uQuery.GetPublishedXml(uQuery.UmbracoObjectType.Member);
                    break;

                case "url":
                    xmlDocument = new XmlDocument();
                    xmlDocument.Load(this.Url);
                    break;

                default:
                    xmlDocument = null;
                    break;
            }

            if (xmlDocument != null)
            {                
                HttpContext.Current.Items["pageID"] = contextId; // set here, as this is required for the uQuery.ResolveXPath

                XPathNavigator xPathNavigator = xmlDocument.CreateNavigator();
                XPathNodeIterator xPathNodeIterator = xPathNavigator.Select(uQuery.ResolveXPath(this.XPath));
                List<string> keys = new List<string>(); // used to keep track of keys, so that duplicates aren't added

                string key;
                string label;

                while (xPathNodeIterator.MoveNext())
                {
                    // media xml is wrapped in a <Media id="-1" /> node to be valid, exclude this from any results
                    // member xml is wrapped in <Members id="-1" /> node to be valid, exclude this from any results
                    // TODO: nuQuery should append something unique to this root wrapper to simplify check here
                    if (xPathNodeIterator.CurrentPosition > 1 ||
                        !(xPathNodeIterator.Current.GetAttribute("id", string.Empty) == "-1" &&
                         (xPathNodeIterator.Current.Name == "Media" || xPathNodeIterator.Current.Name == "Members")))
                    {
                        key = xPathNodeIterator.Current.SelectSingleNode(this.KeyXPath).Value;

                        // only add item if it has a unique key - failsafe
                        if (!string.IsNullOrWhiteSpace(key) && !keys.Any(x => x == key))
                        {
                            // TODO: ensure key doens't contain any commas (keys are converted saved as csv)
                            keys.Add(key); // add key so that it's not reused

                            // set default markup to use the configured label XPath
                            label = xPathNodeIterator.Current.SelectSingleNode(this.LabelXPath).Value;

                            editorDataItems.Add(new EditorDataItem()
                            {
                                Key = key,
                                Label = label
                            });
                        }
                    }
                }
            }

            return editorDataItems;
        }

        public IEnumerable<EditorDataItem> GetEditorDataItemsFilteredByIds(int contextId, string ids)
        {
            List<EditorDataItem> result = new List<EditorDataItem>();
            if (ids != null)
            {
                IEnumerable<string> collectionIds = ids.Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries).AsEnumerable<string>();
                result = GetEditorDataItems(contextId).Where(x => ids.Contains(x.Key)).ToList<EditorDataItem>();
            }
            return result;
        }
    }
}
