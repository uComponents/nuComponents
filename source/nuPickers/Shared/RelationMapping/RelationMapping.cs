﻿namespace nuPickers.Shared.RelationMapping
{
    using System.Collections.Generic;
    using System.Linq;
    using Umbraco.Core;
    using Umbraco.Core.Models;

    /// <summary>
    /// the core relation mapping functionality
    /// </summary>
    internal static class RelationMapping
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="contextId">the id of the content, media or member item</param>
        /// <param name="propertyAlias">the property alias of the picker using relation mapping</param>
        /// <param name="relationTypeAlias">the alias of the relation type to use</param>
        /// <param name="relationsOnly"></param>
        /// <returns></returns>
        internal static IEnumerable<int> GetRelatedIds(int contextId, string propertyAlias, string relationTypeAlias, bool relationsOnly)
        {
            IRelationType relationType = ApplicationContext.Current.Services.RelationService.GetRelationTypeByAlias(relationTypeAlias);

            if (relationType != null)
            {
                // get all relations of this type
                IEnumerable<IRelation> relations = FilterRelationsFromList(relationType, contextId, propertyAlias,relationsOnly);

                return relations.Select(x => (x.ParentId != contextId) ? x.ParentId : x.ChildId);
            }

            return null;
        }

        internal static IEnumerable<IRelation> FilterRelationsFromList(IRelationType relationType, int contextId, string propertyAlias, bool relationsOnly)
        {
            IEnumerable<IRelation> relations = ApplicationContext.Current.Services.RelationService.GetAllRelationsByRelationType(relationType.Id);

            // construct object used to identify a relation (this is serialized into the relation comment field)
            RelationMappingComment relationMappingComment = new RelationMappingComment(contextId, propertyAlias);

            // filter down potential relations, by relation type direction
            if (relationType.IsBidirectional && relationsOnly)
            {
                relations = relations.Where(x => x.ChildId == contextId || x.ParentId == contextId);
                relations = relations.Where(x => new RelationMappingComment(x.Comment).DataTypeDefinitionId == relationMappingComment.DataTypeDefinitionId);
            }
            else
            {
                relations = relations.Where(x => x.ChildId == contextId);
                relations = relations.Where(x => new RelationMappingComment(x.Comment).PropertyTypeId == relationMappingComment.PropertyTypeId);

                if (relationMappingComment.IsInArchetype())
                {
                    relations = relations.Where(x => new RelationMappingComment(x.Comment).MatchesArchetypeProperty(relationMappingComment.PropertyAlias));
                }
            }

            return relations.OrderByDescending(x => new RelationMappingComment(x.Comment).SortOrder);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="contextId">the id of the content, media or member item</param>
        /// <param name="propertyAlias">the property alias of the picker using relation mapping</param>
        /// <param name="relationTypeAlias">the alias of the relation type to use</param>
        /// <param name="relationsOnly"></param>
        /// <param name="pickedIds">the ids of all picked items that are to be related to the contextId</param>
        internal static void UpdateRelationMapping(int contextId, string propertyAlias, string relationTypeAlias, bool relationsOnly, int[] pickedIds)
        {
            
            IRelationType relationType = ApplicationContext.Current.Services.RelationService.GetRelationTypeByAlias(relationTypeAlias);

            if (relationType != null)
            {
                // get all relations of this type
                List<IRelation> relations = FilterRelationsFromList(relationType, contextId, propertyAlias,relationsOnly).ToList();

                

                // check current context is of the correct object type (as according to the relation type)
                if (ApplicationContext.Current.Services.EntityService.GetObjectType(contextId) == UmbracoObjectTypesExtensions.GetUmbracoObjectType(relationType.ChildObjectType))
                {
                    // we need a sort-order nr here.
                    var currentSortOrder = pickedIds.Length;

                    // for each picked item 
                    foreach (int pickedId in pickedIds)
                    {
                        // check picked item context if of the correct object type (as according to the relation type)
                        if (ApplicationContext.Current.Services.EntityService.GetObjectType(pickedId) == UmbracoObjectTypesExtensions.GetUmbracoObjectType(relationType.ParentObjectType))
                        {
                            // if relation doesn't already exist (new picked item)
                            if (!relations.Exists(x => x.ParentId == pickedId))
                            {
                                // create relation
                                Relation relation = new Relation(pickedId, contextId, relationType);
                                var comment = new RelationMappingComment(contextId, propertyAlias);
                                comment.SortOrder = currentSortOrder;
                                relation.Comment = comment.GetComment();
                                ApplicationContext.Current.Services.RelationService.Save(relation);
                            }
                            else
                            {
                                // update sort order
                                var relation = relations.First(x => x.ParentId == pickedId);
                                var mapping = new RelationMappingComment(relation.Comment);
                                mapping.SortOrder = currentSortOrder;
                                relation.Comment = mapping.GetComment();
                                ApplicationContext.Current.Services.RelationService.Save(relation);

                            }

                            currentSortOrder--;

                            // housekeeping - remove 'the' relation from the list being processed (there should be only one)
                            relations.RemoveAll(x => x.ChildId == contextId && x.ParentId == pickedId && x.RelationTypeId == relationType.Id);
                        }
                    }
                }

                // delete relations for any items left on the list being processed
                if (relations.Any())
                {
                    foreach (IRelation relation in relations)
                    {
                        ApplicationContext.Current.Services.RelationService.Delete(relation);
                    }
                }
            }
        }
    }
}
