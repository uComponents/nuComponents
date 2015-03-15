﻿namespace nuPickers
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using nuPickers.Shared.EnumDataSource;
    using nuPickers.Shared.RelationMapping;
    using nuPickers.Shared.SaveFormat;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using umbraco;
    using Umbraco.Core;
    using Umbraco.Core.Models;
    using Umbraco.Web;

    public class Picker
    {
        private int ContextId { get; set; }
        private string PropertyAlias { get; set; }
        private int DataTypeId { get; set; }
        private object SavedValue { get; set; }

        private IDictionary<string, PreValue> dataTypePreValues = null;
        private IDictionary<string, PreValue> DataTypePreValues
        {
            get
            {
                if (this.dataTypePreValues == null)
                {
                    this.dataTypePreValues = ApplicationContext
                                                .Current
                                                .Services
                                                .DataTypeService
                                                .GetPreValuesCollectionByDataTypeId(this.DataTypeId)
                                                .PreValuesAsDictionary;
                }

                return this.dataTypePreValues;
            }
        }

        /// <summary>
        /// Helper for DataTypePreValues dictionary collection
        /// </summary>
        /// <param name="key"></param>
        /// <returns>the PreValue if found, or null</returns>
        public PreValue GetDataTypePreValue(string key)
        {
            return this.DataTypePreValues.SingleOrDefault(x => string.Equals(x.Key, key, StringComparison.InvariantCultureIgnoreCase)).Value;                  
        }

        /// <summary>
        /// This is the constructor used by the PropertyValueConverter
        /// </summary>
        /// <param name="contextId">the id of the (content, media or member) item being edited (-1 means out of context)</param>
        /// <param name="propertyAlias">the property alias</param>
        /// <param name="dataTypeId">the id of the datatype - this allows access to all prevalues</param>
        /// <param name="savedValue">the actual value saved</param>
        internal Picker(int contextId, string propertyAlias, int dataTypeId, object savedValue)
        {
            this.ContextId = contextId;
            this.PropertyAlias = propertyAlias;
            this.DataTypeId = dataTypeId;
            this.SavedValue = savedValue;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="contextId"></param>
        /// <param name="propertyAlias"></param>
        /// <param name="usePublishedValue">when true uses the published value, otherwise when false uses the lastest saved value (which may also be the published value)</param>
        public Picker(int contextId, string propertyAlias, bool usePublishedValue = true)
        {
            this.ContextId = contextId;
            this.PropertyAlias = propertyAlias;

            UmbracoHelper umbracoHelper = new UmbracoHelper(UmbracoContext.Current);
            Picker picker;

            switch (uQuery.GetUmbracoObjectType(this.ContextId))
            {
                case uQuery.UmbracoObjectType.Document:                    
                    picker = umbracoHelper.TypedContent(this.ContextId).GetPropertyValue<Picker>(this.PropertyAlias);
                    this.DataTypeId = picker.DataTypeId;

                    if (usePublishedValue)
                    {
                        this.SavedValue = picker.SavedValue;
                    }
                    else
                    {
                        this.SavedValue = ApplicationContext.Current.Services.ContentService.GetById(this.ContextId).GetValue(propertyAlias);
                    }

                    break;

                case uQuery.UmbracoObjectType.Media:
                    picker = umbracoHelper.TypedMedia(this.ContextId).GetPropertyValue<Picker>(this.PropertyAlias);
                    this.DataTypeId = picker.DataTypeId;
                    this.SavedValue = picker.SavedValue;
                    break;

                case uQuery.UmbracoObjectType.Member:
                    picker = umbracoHelper.TypedMember(this.ContextId).GetPropertyValue<Picker>(this.PropertyAlias);
                    this.DataTypeId = picker.DataTypeId;
                    this.SavedValue = picker.SavedValue;
                    break;
            }
        }

        /// <summary>
        /// Returns a collection of all picked keys (regardless as to where they are persisted)
        /// </summary>
        public IEnumerable<string> PickedKeys
        {
            get
            {
                if (this.GetDataTypePreValue("saveFormat").Value == "relationsOnly")
                {
                    string relationTypeAlias = JObject.Parse(this.GetDataTypePreValue("RelationMapping").Value).GetValue("relationTypeAlias").ToString();

                    return new RelationMappingApiController().GetRelatedIds(this.ContextId, this.PropertyAlias, relationTypeAlias, true).Select(x => x.ToString());
                }
                
                return this.SavedValue != null ? SaveFormat.GetSavedKeys(this.SavedValue.ToString()) : Enumerable.Empty<string>();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>a collection of IPublishedContent, or an empty collection</returns>
        public IEnumerable<IPublishedContent> AsPublishedContent()
        {
            List<IPublishedContent> publishedContent = new List<IPublishedContent>();

            UmbracoHelper umbracoHelper = new UmbracoHelper(UmbracoContext.Current);

            foreach (var pickedKey in this.PickedKeys)
            {
                Attempt<int> attemptNodeId = pickedKey.TryConvertTo<int>();
                if (attemptNodeId.Success)
                {
                    switch (uQuery.GetUmbracoObjectType(attemptNodeId.Result))
                    {
                        case uQuery.UmbracoObjectType.Document:
                            publishedContent.Add(umbracoHelper.TypedContent(attemptNodeId.Result));
                            break;

                        case uQuery.UmbracoObjectType.Media:
                            publishedContent.Add(umbracoHelper.TypedMedia(attemptNodeId.Result));
                            break;

                        case uQuery.UmbracoObjectType.Member:
                            publishedContent.Add(umbracoHelper.TypedMember(attemptNodeId.Result));
                            break;
                    }
                }
            }

            return publishedContent.Where(x => x != null);
        }

        public IEnumerable<dynamic> AsDynamicPublishedContent()
        {
            return this.AsPublishedContent().Select(x => x.AsDynamic());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>a collection of Enums of type T, or an empty collection</returns>
        public IEnumerable<T> AsEnums<T>() where T : struct, IConvertible
        {
            if (!typeof(T).IsEnum)
            {
                throw new ArgumentException("T must be an enum");
            }

            List<T> enums = new List<T>();

            foreach (string pickedKey in this.PickedKeys)
            {
                foreach(Enum enumItem in Enum.GetValues(typeof(T)))
                {
                    if (pickedKey == enumItem.GetKey())
                    {
                        Attempt<T> attempt = enumItem.TryConvertTo<T>();
                        if (attempt.Success)
                        {
                            enums.Add(attempt.Result);
                        }
                    }
                }
            }

            return enums;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>a collection of Enums, or empty collection</returns>
        public IEnumerable<Enum> AsEnums()
        {
            List<Enum> enums = new List<Enum>();
            PreValue dataSourceJson = this.GetDataTypePreValue("dataSource");

            if (dataSourceJson != null)
            {
                EnumDataSource enumDataSouce = JsonConvert.DeserializeObject<EnumDataSource>(dataSourceJson.Value);

                Type enumType = Helper.GetAssembly(enumDataSouce.AssemblyName).GetType(enumDataSouce.EnumName);

                foreach(string pickedKey in this.PickedKeys)
                {
                    foreach(Enum enumItem in Enum.GetValues(enumType))
                    {
                       if (pickedKey == enumItem.GetKey())
                       {
                           enums.Add(enumItem);
                       }
                    }
                }
            }

            return enums;
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            if (this.SavedValue != null)
                return this.SavedValue.ToString();

            return base.ToString();
        }
    }
}
