namespace Dashing.Engine.InMemory {
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
#if COREFX
    using System.Reflection;
#endif

    using Dashing.Configuration;
    using Dashing.Engine.DML;

    public static class InMemoryEnumerableExtensions {
        public static IEnumerable<T> Fetch<T>(this IEnumerable<T> enumerable, FetchNode fetchNode, Dictionary<Type, object> tables) {
            foreach (var entity in enumerable) {
                yield return (T)Expand(entity, fetchNode, tables);
            }
        }

        private static object Expand(object entity, FetchNode fetchNode, Dictionary<Type, object> tables) {
            foreach (var node in fetchNode.Children) {
                var prop = entity.GetType().GetProperty(node.Key);
                if (node.Value.Column.Relationship == RelationshipType.ManyToOne || node.Value.Column.Relationship == RelationshipType.OneToOne) {
                    // this value should just contain the pk, so we fetch the entity from its table, expand all it's properties and then set
                    var value = prop.GetValue(entity);
                    if (value != null) {
                        var tableType = node.Value.Column.Type;
                        var relatedMap = node.Value.Column.Relationship == RelationshipType.ManyToOne
                                             ? node.Value.Column.ParentMap
                                             : node.Value.Column.OppositeColumn.Map;
                        var table = tables[tableType];
                        var tableValue = typeof(InMemoryTable<,>).MakeGenericType(tableType, relatedMap.PrimaryKey.Type)
                                                                 .GetMethod("Get")
                                                                 .Invoke(table, new[] { relatedMap.GetPrimaryKeyValue(value) });
                        if (tableValue == null) {
                            throw new Exception(string.Format("You've specified a non-existant relationship for property {0} on {1}", node.Key, entity));
                        }

                        value = Expand(tableValue, node.Value, tables);
                        prop.SetValue(entity, value);
                    }
                }
                else if (node.Value.Column.Relationship == RelationshipType.OneToMany) {
                    // for collections we need to query the related table for all referenced entities
                    // expand them and then match them back
                    var childColumn = node.Value.Column.ChildColumn;
                    var param = Expression.Parameter(childColumn.Map.Type);
                    var whereClause = Expression.Lambda(
                        Expression.AndAlso(
                            Expression.NotEqual(Expression.Property(param, childColumn.Name), Expression.Constant(null)),
                            Expression.Equal(Expression.Property(Expression.Property(param, childColumn.Name), node.Value.Column.Map.PrimaryKey.Name), Expression.Constant(node.Value.Column.Map.GetPrimaryKeyValue(entity)))
                            ),
                        param).Compile();
                    var tableType = childColumn.Map.Type;
                    var table = tables[tableType];
                    var enumerable =
                        typeof(InMemoryTable<,>).MakeGenericType(tableType, childColumn.Map.PrimaryKey.Type)
                                                .GetMethod("Query")
                                                .Invoke(table, new object[0]) as IEnumerable;
                    var wheredEnumerable =
                        typeof(Enumerable).GetMethods()
                                          .Where(m => m.Name == "Where" && m.GetParameters().Last().ParameterType.GenericTypeArguments.Length == 2)
                                          .Single()
                                          .MakeGenericMethod(tableType)
                                          .Invoke(null, new object[] { enumerable, whereClause }) as IEnumerable;
                    var list = Activator.CreateInstance(typeof(List<>).MakeGenericType(childColumn.Map.Type)) as IList;
                    foreach (var child in wheredEnumerable) {
                        list.Add(Expand(child, node.Value, tables));
                    }

                    prop.SetValue(entity, list);
                }
            }

            return entity;
        }
    }
}