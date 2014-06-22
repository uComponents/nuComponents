﻿namespace nuComponents.DataTypes.Shared.JsonDataSource
{
    using System;
    using Newtonsoft.Json.Linq;
    using nuComponents.DataTypes.Shared.CustomLabel;
    using nuComponents.DataTypes.Shared.Editor;
    using nuComponents.DataTypes.Shared.TypeaheadListPicker;
    using System.Collections.Generic;
    using System.Web.Http;
    using Umbraco.Web.Editors;
    using Umbraco.Web.Mvc;
    using Umbraco.Core.Logging;

    [PluginController("nuComponents")]
    public class JsonDataSourceApiController : UmbracoAuthorizedJsonController
    {
        [HttpPost]
        public IEnumerable<EditorDataItem> GetEditorDataItems([FromUri] int contextId, [FromBody] dynamic data)
        {
            try
            {
                JsonDataSource jsonDataSource = ((JObject) data.config.dataSource).ToObject<JsonDataSource>();

                IEnumerable<EditorDataItem> editorDataItems = jsonDataSource.GetEditorDataItems(contextId);

                CustomLabel customLabel = new CustomLabel((string) data.config.customLabel, contextId);
                TypeaheadListPicker typeaheadListPicker = new TypeaheadListPicker((string) data.typeahead);

                // process the labels and then handle any type ahead text
                return typeaheadListPicker.ProcessEditorDataItems(customLabel.ProcessEditorDataItems(editorDataItems));
            }
            catch (Exception e)
            {
                LogHelper.Error<JsonDataSourceApiController>("Error getting datasource data", e);
                throw e;
            }
        }
    }
}
