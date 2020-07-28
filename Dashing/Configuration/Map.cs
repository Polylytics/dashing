namespace Dashing.Configuration {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using Dashing.Extensions;

    public class Map : IMap {
        private IList<Index> indexes;

        private IList<ForeignKey> foreignKeys;

        private bool hasCalculatedForeignKeys;

        private bool hasSetForeignKeys;

        private bool hasAddedForeignKeyIndexes;

        private bool hasSetIndexes;

        private bool isOwned;

        public Map(Type type) {
            this.Type = type;
            this.Columns = new OrderedDictionary<string, IColumn>();
            this.indexes = new List<Index>();
        }

        public IConfiguration Configuration { get; set; }

        public bool IsOwned
        {
            get => this.isOwned;
            set
            {
                if (this.isOwned == value) {
                    return;
                }

                if (value) {
                    // update all columns of this type of be owned relationship type
                    foreach (var map in this.Configuration.Maps) {
                        foreach (var mappedColumns in map.Columns.Where(c => c.Value.Type == this.Type)) {
                            mappedColumns.Value.Relationship = RelationshipType.Owned;
                        }
                    }
                }
                else {
                    // update all columns of this type 
                    foreach (var map in this.Configuration.Maps) {
                        foreach (var mappedColumns in map.Columns.Where(c => c.Value.Type == this.Type)) {
                            // TODO dry up this logic
                            mappedColumns.Value.Relationship = RelationshipType.ManyToOne;
                            mappedColumns.Value.DbName = mappedColumns.Value.Name + "Id";
                        }
                    }
                }

                this.isOwned = value;
            }
        }

        /// <summary>
        ///     Gets the type.
        /// </summary>
        public Type Type { get; private set; }

        /// <summary>
        ///     Gets or sets the table.
        /// </summary>
        public string Table { get; set; }

        /// <summary>
        ///     Gets or sets the history table name
        /// </summary>
        public string HistoryTable { get; set; }

        /// <summary>
        ///     Gets or sets the schema.
        /// </summary>
        public string Schema { get; set; }

        /// <summary>
        ///     Gets or sets the primary key.
        /// </summary>
        public IColumn PrimaryKey { get; set; }

        /// <summary>
        ///     Gets or sets the columns.
        /// </summary>
        public IDictionary<string, IColumn> Columns { get; private set; }

        public IEnumerable<Index> Indexes
        {
            get
            {
                if (!this.hasSetIndexes && !this.hasAddedForeignKeyIndexes) {
                    // add in any indexes for the foreign keys in this map
                    foreach (var foreignKey in this.ForeignKeys) {
                        var index = new Index(this, new List<IColumn> { foreignKey.ChildColumn });
                        if (!this.indexes.Any(i => i.Columns.SequenceEqual(index.Columns, new IndexColumnComparer()))) {
                            this.indexes.Add(index);
                        }
                    }

                    this.hasAddedForeignKeyIndexes = true;
                }

                return this.indexes;
            }

            set
            {
                this.indexes = value.ToList();
                this.hasSetIndexes = true;
            }
        }

        public void AddIndex(Index index) {
            this.indexes.Add(index);
        }

        /// <summary>
        ///     Returns the foreign keys for this map
        /// </summary>
        public IEnumerable<ForeignKey> ForeignKeys
        {
            get
            {
                if (!this.hasSetForeignKeys && !this.hasCalculatedForeignKeys && this.foreignKeys == null) {
                    this.foreignKeys =
                        this.Columns.Where(
                            c =>
                            (c.Value.Relationship == RelationshipType.ManyToOne || c.Value.Relationship == RelationshipType.OneToOne)
                            && !c.Value.IsIgnored)
                            .Select(
                                c =>
                                new ForeignKey(
                                    c.Value.Relationship == RelationshipType.ManyToOne ? c.Value.ParentMap : this.Configuration.GetMap(c.Value.Type),
                                    c.Value))
                            .ToList();
                    hasCalculatedForeignKeys = true;
                }

                return this.foreignKeys;
            }

            set
            {
                this.foreignKeys = value.ToList();
                this.hasSetForeignKeys = true;
            }
        }

        private MethodInfo nonGenericPrimaryKeyGetter;

        private readonly object nonGenericPrimaryKeyGetterLock = new object();

        public object GetPrimaryKeyValue(object entity) {
            if (this.nonGenericPrimaryKeyGetter == null) {
                lock (this.nonGenericPrimaryKeyGetterLock) {
                    if (this.nonGenericPrimaryKeyGetter == null) {
                        this.nonGenericPrimaryKeyGetter =
                            typeof(Map<>).MakeGenericType(this.Type)
                                         .GetMethods()
                                         .First(m => m.Name == "GetPrimaryKeyValue" && m.GetParameters().Any(p => p.ParameterType == this.Type));
                    }
                }
            }

            return this.nonGenericPrimaryKeyGetter.Invoke(this, new[] { entity });
        }

        private MethodInfo nonGenericColumnValueGetter;

        private readonly object nonGenericColumnValueGetterLock = new object();

        public object GetColumnValue(object entity, IColumn column) {
            if (this.nonGenericColumnValueGetter == null) {
                lock (this.nonGenericColumnValueGetterLock) {
                    if (this.nonGenericColumnValueGetter == null) {
                        this.nonGenericColumnValueGetter =
                            typeof(Map<>).MakeGenericType(this.Type)
                                         .GetMethods()
                                         .First(m => m.Name == nameof(Map<object>.GetColumnValue) 
                                                     && m.GetParameters().Any(p => p.ParameterType == this.Type));
                    }
                }
            }

            return this.nonGenericColumnValueGetter.Invoke(this, new[] { entity, column });
        }
    }
}