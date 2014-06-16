﻿namespace TopHat.Tests.Engine {
    using System.Diagnostics;

    using TopHat.Configuration;
    using TopHat.Engine;
    using TopHat.Tests.TestDomain;

    using Xunit;

    public class InsertWriterTests {
        [Fact]
        public void SimpleInsertWorks() {
            var insertWriter = new InsertWriter(new SqlServerDialect(), MakeConfig());
            var post = new Post { PostId = 1, Title = "Boo", Rating = 11 };
            var result = insertWriter.GenerateSql(new InsertEntityQuery<Post>(post));
            Debug.Write(result.Sql);
            Assert.Equal("insert into [Posts] ([Title], [Content], [Rating], [AuthorId], [BlogId], [DoNotMap]) values (@p_1, @p_2, @p_3, @p_4, @p_5, @p_6)", result.Sql);
        }

        [Fact]
        public void SimpleInsertMultipleWorks() {
            var insertWriter = new InsertWriter(new SqlServerDialect(), MakeConfig());
            var post = new Post { PostId = 1, Title = "Boo", Rating = 11 };
            var secondPost = new Post { PostId = 2, Title = "Boo Hoo" };
            var result = insertWriter.GenerateSql(new InsertEntityQuery<Post>(post, secondPost));
            Debug.Write(result.Sql);
            Assert.Equal(
                "insert into [Posts] ([Title], [Content], [Rating], [AuthorId], [BlogId], [DoNotMap]) values (@p_1, @p_2, @p_3, @p_4, @p_5, @p_6), (@p_7, @p_8, @p_9, @p_10, @p_11, @p_12)",
                result.Sql);
        }

        private static IConfiguration MakeConfig(bool withIgnore = false) {
            if (withIgnore) {
                return new CustomConfigWithIgnore();
            }

            return new CustomConfig();
        }

        private class CustomConfig : DefaultConfiguration {
            public CustomConfig()
                : base(new SqlServerEngine(), string.Empty) {
                this.AddNamespaceOf<Post>();
            }
        }

        private class CustomConfigWithIgnore : DefaultConfiguration {
            public CustomConfigWithIgnore()
                : base(new SqlServerEngine(), string.Empty) {
                this.AddNamespaceOf<Post>();
                this.Setup<Post>().Property(p => p.DoNotMap).Ignore();
            }
        }
    }
}