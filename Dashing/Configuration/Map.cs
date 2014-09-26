namespace Dashing.Configuration {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public class Map : IMap {
        private MethodInfo nonGenericPrimaryKeyGetter;

        private readonly object nonGenericPrimaryKeyGetterLock = new object();

        private ICollection<Index> indexes;

        private ICollection<ForeignKey> foreignKeys;

        private bool hasCalculatedForeignKeys;

        private bool hasSetForeignKeys;

        private bool hasAddedForeignKeyIndexes;

        private bool hasSetIndexes;

        public Map(Type type) {
            this.Type = type;
            this.Columns = new Dictionary<string, IColumn>();
            this.indexes = new List<Index>();
        }

        public IConfiguration Configuration { get; set; }

        /// <summary>
        ///     Gets the type.
        /// </summary>
        public Type Type { get; private set; }

        /// <summary>
        ///     Gets or sets the table.
        /// </summary>
        public string Table { get; set; }

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
        public IDictionary<string, IColumn> Columns { get; set; }

        public ICollection<Index> Indexes {
            get {
                if (!this.hasSetIndexes && !this.hasAddedForeignKeyIndexes) {
                    // add in any indexes for the foreign keys in this map
                    foreach (var foreignKey in this.ForeignKeys) {
                        this.indexes.Add(new Index(this, new List<IColumn>{ foreignKey.ChildColumn }));
                    }

                    this.hasAddedForeignKeyIndexes = true;
                }

                return this.indexes;
            }

            set {
                this.indexes = value;
                this.hasSetIndexes = true;
            }
        }

        /// <summary>
        /// Returns the foreign keys for this map
        /// </summary>
        public ICollection<ForeignKey> ForeignKeys {
            get {
                if (!this.hasSetForeignKeys && !this.hasCalculatedForeignKeys && this.foreignKeys == null) {
                    this.foreignKeys =
                        this.Columns.Where(c => c.Value.Relationship == RelationshipType.ManyToOne)
                            .Select(
                                c =>
                                new ForeignKey(c.Value.ParentMap, c.Value))
                            .ToList();
                    hasCalculatedForeignKeys = true;
                }

                return this.foreignKeys;
            }

            set {
                this.foreignKeys = value;
                this.hasSetForeignKeys = true;
            }
        }

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
    }
}