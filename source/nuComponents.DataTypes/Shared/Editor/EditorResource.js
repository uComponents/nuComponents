﻿
angular.module('umbraco.resources')
    .factory('nuComponents.DataTypes.Shared.Editor.EditorResource',
        ['$q',
        'nuComponents.DataTypes.Shared.DataSource.DataSourceResource',
        'nuComponents.DataTypes.Shared.SaveFormat.SaveFormatResource',
        'nuComponents.DataTypes.Shared.RelationMapping.RelationMappingResource',
        function ($q, dataSourceResource, saveFormatResource, relationMappingResource) {

            return {

                getEditorDataItems: function (config, typeahead) {
                    return dataSourceResource.getEditorDataItems(config, typeahead);
                },

                getPickedKeys: function (model) {

                    // create a new promise....
                    var deferred = $q.defer();

                    if (model.config.saveFormat == 'relationsOnly') {

                        relationMappingResource.getRelatedIds(model).then(function (response) {
                            deferred.resolve(response.data.map(function (id) { return id.toString(); })); // ensure returning an array of strings
                        });

                    } else {
                        deferred.resolve(saveFormatResource.getSavedKeys(model.value));                        
                    }

                    return deferred.promise;
                },

                createSaveValue: function (config, pickedOptions) {
                    return saveFormatResource.createSaveValue(config, pickedOptions);
                },

                updateRelationMapping: function (model, pickedOptions) {
                    if (model.config.relationMapping != null) {
                        relationMappingResource.updateRelationMapping(model, pickedOptions);
                    }
                }

            };
        }
    ]);