﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Dashing.Tools.ReverseEngineering;

namespace Dashing.Tests.Tools.ReverseEngineering
{
    public class EngineerTests
    {
        [Fact]
        public void PrimaryKeySetCorrectly()
        {
            var engineer = new Engineer();
            var maps = engineer.ReverseEngineer(this.GetSchema());
            Assert.Equal("PostId", maps.First(m => m.Table == "Posts").PrimaryKey.Name);
        }

        [Fact]
        public void AutoIncSetCorrectly()
        {
            var engineer = new Engineer();
            var maps = engineer.ReverseEngineer(this.GetSchema());
            Assert.True(maps.First(m => m.Table == "Posts").PrimaryKey.IsAutoGenerated);
        }

        [Fact]
        public void ManyToOneSetCorrectly()
        {
            var engineer = new Engineer();
            var maps = engineer.ReverseEngineer(this.GetSchema());
            Assert.True(maps.First(m => m.Table == "Posts").Columns.First(c => c.Key == "Blog").Value.Relationship == Dashing.Configuration.RelationshipType.ManyToOne);
        }

        private DatabaseSchemaReader.DataSchema.DatabaseSchema GetSchema()
        {
            return MakeSchema();
        }

        private DatabaseSchemaReader.DataSchema.DatabaseSchema MakeSchema()
        {
            var schema = new DatabaseSchemaReader.DataSchema.DatabaseSchema(string.Empty, DatabaseSchemaReader.DataSchema.SqlType.SqlServer);
            var postTable = new DatabaseSchemaReader.DataSchema.DatabaseTable
            {
                Name = "Posts"
            };

            postTable.Columns.Add(new DatabaseSchemaReader.DataSchema.DatabaseColumn { IsIdentity = true, IsPrimaryKey = true, Name = "PostId", DataType = new DatabaseSchemaReader.DataSchema.DataType("int", "System.Int32") });
            postTable.Columns.Add(new DatabaseSchemaReader.DataSchema.DatabaseColumn { Name = "BlogId", IsForeignKey = true, ForeignKeyTableName = "Blogs", DataType = new DatabaseSchemaReader.DataSchema.DataType("int", "System.Int32") });

            schema.Tables.Add(postTable);
            return schema;
        }
    }
}
