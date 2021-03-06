namespace Dashing.Migration {
    using System.Collections.Generic;
    using System.Data;
    using System.Text;

    using Dashing.Configuration;
    using Dashing.Engine.Dialects;

    public class StatisticsProvider : IStatisticsProvider {
        private readonly IDbConnection connection;

        private readonly ISqlDialect dialect;

        public StatisticsProvider(IDbConnection connection, ISqlDialect dialect) {
            this.connection = connection;
            this.dialect = dialect;
        }

        public IDictionary<string, Statistics> GetStatistics(IEnumerable<IMap> fromMaps) {
            bool wasOpen = true;
            if (this.connection.State != ConnectionState.Open) {
                wasOpen = false;
                this.connection.Open();
            }

            var result = new Dictionary<string, Statistics>();
            foreach (var map in fromMaps) {
                var cmd = this.connection.CreateCommand();
                var sql = new StringBuilder("select * from ");
                this.dialect.AppendQuotedTableName(sql, map);
                this.dialect.ApplySkipTake(sql, new StringBuilder(), 1, 0);
                cmd.CommandText = sql.ToString();
                cmd.CommandType = CommandType.Text;
                var takeParam = cmd.CreateParameter();
                takeParam.DbType = DbType.Int32;
                takeParam.Direction = ParameterDirection.Input;
                takeParam.ParameterName = "@take";
                takeParam.Value = 1;
                cmd.Parameters.Add(takeParam);
                using (var reader = cmd.ExecuteReader()) {
                    result.Add(map.Type.Name.ToLowerInvariant(), new Statistics { HasRows = reader.Read() });
                }
            }

            if (!wasOpen) {
                this.connection.Close();
            }

            return result;
        }
    }
}