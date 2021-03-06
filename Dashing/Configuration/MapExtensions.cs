﻿namespace Dashing.Configuration {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
#if COREFX
    using System.Reflection;
#endif

    /// <summary>
    ///     The map extensions.
    /// </summary>
    public static class MapExtensions {
        public static IMap Table(this IMap map, string tableName) {
            map.Table = tableName;
            return map;
        }

        public static IMap Schema(this IMap map, string schema) {
            map.Schema = schema;
            return map;
        }

        public static IMap<T> PrimaryKey<T, TResult>(this IMap<T> map, Expression<Func<T, TResult>> expression) {
            foreach (var column in map.Columns.Values) {
                column.IsPrimaryKey = false;
            }

            map.PrimaryKey = map.Columns[NameFromMemberExpression(expression)];
            map.PrimaryKey.IsPrimaryKey = true;
            return map;
        }

        public static IMap<T> Index<T, TResult>(this IMap<T> map, Expression<Func<T, TResult>> indexExpression, bool isUnique = false) {
            var columns = new List<IColumn>();
            if (indexExpression.Body.NodeType == ExpressionType.MemberAccess) {
                // simple, just accessing a property on the type
                var propInfo = ((MemberExpression)indexExpression.Body).Member;
                if (!map.Columns.TryGetValue(propInfo.Name, out var column)) {
                    throw new InvalidOperationException($"Unable to find property {propInfo.Name} on map {map.Type}");
                }

                if (column.IsIgnored) {
                    throw new InvalidOperationException($"You can not add an index to {propInfo.Name} on map {map.Type} as it is ignored");
                }

                columns.Add(column);
            }
            else if (indexExpression.Body.NodeType == ExpressionType.New) {
                var columnNames = typeof(TResult).GetProperties();

                foreach (var columnName in columnNames.Select(p => p.Name)) {
                    if (!map.Columns.ContainsKey(columnName)) {
                        throw new InvalidOperationException("The index must be on a property in the entity");
                    }

                    var column = map.Columns[columnName];
                    if (column.IsIgnored) {
                        throw new InvalidOperationException("The index can not be on an ignored column");
                    }

                    columns.Add(column);
                }
            }

            map.AddIndex(new Index(map, columns, isUnique: isUnique));
            return map;
        }

        /////// <summary>
        ///////   The index.
        /////// </summary>
        /////// <param name="map">
        ///////   The map.
        /////// </param>
        /////// <param name="newExpression">
        ///////   The new expression.
        /////// </param>
        /////// <typeparam name="T">
        /////// </typeparam>
        /////// <typeparam name="TProperty">
        /////// </typeparam>
        /////// <returns>
        ///////   The <see cref="Map" />.
        /////// </returns>
        /////// <exception cref="NotImplementedException">
        /////// </exception>
        ////public static IMap<T> Index<T, TProperty>(this IMap<T> map, Expression<Func<T, TProperty>> newExpression) {
        ////  throw new NotImplementedException();
        ////}

        public static Column<TProperty> Property<T, TProperty>(this IMap<T> map, Expression<Func<T, TProperty>> expression) {
            var columnName = NameFromMemberExpression(expression);

            IColumn column;
            if (!map.Columns.TryGetValue(columnName, out column)) {
                throw new KeyNotFoundException();
            }

            var columnT = column as Column<TProperty>;

            if (columnT == null) {
                // lift the Column into a Column<T>
                map.Columns[columnName] = columnT = Column<TProperty>.From(column);
            }

            return columnT;
        }

        private static string NameFromMemberExpression<T, TResult>(Expression<Func<T, TResult>> expression) {
            var memberExpression = expression.Body as MemberExpression;
            if (memberExpression == null) {
                throw new ArgumentException("expression must be a MemberExpression");
            }

            return memberExpression.Member.Name;
        }

        public static IEnumerable<IColumn> OwnedColumns(this IMap map, bool includeExcludedByDefault = false) {
            return
                map.Columns.Values.Where(
                    c =>
                    !c.IsIgnored && (includeExcludedByDefault || !c.IsExcludedByDefault)
                    && (c.Relationship == RelationshipType.None || c.Relationship == RelationshipType.ManyToOne
                        || c.Relationship == RelationshipType.OneToOne));
        }
    }
}