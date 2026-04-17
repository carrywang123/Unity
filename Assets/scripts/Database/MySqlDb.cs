using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using UnityEngine;

// 注意：
// - Unity 里直连 MySQL 需要引入 MySQL 驱动 DLL（推荐 MySql.Data 或 MySqlConnector）。
// - 本项目代码默认使用 MySql.Data（命名空间 MySql.Data.MySqlClient）。
// - 如果你使用的是 MySqlConnector，请把 using/类型名替换为 MySqlConnector.*。

namespace ChemLab.Database
{
    /// <summary>
    /// MySQL 直连工具：负责连接串、参数化SQL、查询/执行、事务。
    /// </summary>
    public sealed class MySqlDb
    {
        public sealed class Settings
        {
            public string host = "127.0.0.1";
            public int port = 3306;
            public string database = "chemlab";
            public string user = "root";
            public string password = "";
            public string charset = "utf8mb4";
            public int connectTimeoutSeconds = 5;
        }

        private readonly Settings _settings;
        private readonly string _connectionString;

        public MySqlDb(Settings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _connectionString = BuildConnectionString(_settings);
        }

        public string ConnectionString => _connectionString;

        private static string BuildConnectionString(Settings s)
        {
            // MySql.Data 连接串格式
            var sb = new StringBuilder();
            sb.Append($"Server={s.host};");
            sb.Append($"Port={Mathf.Max(1, s.port)};");
            // Database 可为空：用于“先创建数据库”的引导连接
            if (!string.IsNullOrWhiteSpace(s.database))
                sb.Append($"Database={s.database};");
            sb.Append($"User ID={s.user};");
            sb.Append($"Password={s.password};");
            sb.Append($"CharSet={s.charset};");
            sb.Append("SslMode=None;");
            sb.Append($"Connection Timeout={Mathf.Max(1, s.connectTimeoutSeconds)};");
            sb.Append("Allow User Variables=True;");
            sb.Append("Pooling=True;");
            return sb.ToString();
        }

        private static Exception MissingDriver()
        {
            return new InvalidOperationException(
                "未找到 MySQL 驱动 DLL。请在 Unity 工程中导入 MySql.Data（或 MySqlConnector）对应 DLL。");
        }

        private IDbConnection CreateConn()
        {
            // 避免编译期依赖 MySqlConnection 类型：运行时用反射创建
            // 优先 MySqlConnector，其次 MySql.Data
            var t =
                Type.GetType("MySqlConnector.MySqlConnection, MySqlConnector") ??
                Type.GetType("MySql.Data.MySqlClient.MySqlConnection, MySql.Data");

            if (t == null) throw MissingDriver();
            var conn = Activator.CreateInstance(t) as IDbConnection;
            if (conn == null) throw MissingDriver();
            conn.ConnectionString = _connectionString;
            return conn;
        }

        private static IDbCommand CreateCmd(IDbConnection conn, IDbTransaction tx, string sql, IReadOnlyDictionary<string, object> args)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = sql;
            cmd.Transaction = tx;

            if (args != null)
            {
                foreach (var kv in args)
                {
                    var p = cmd.CreateParameter();
                    p.ParameterName = kv.Key.StartsWith("@") ? kv.Key : "@" + kv.Key;
                    p.Value = kv.Value ?? DBNull.Value;
                    cmd.Parameters.Add(p);
                }
            }

            return cmd;
        }

        public int ExecuteNonQuery(string sql, IReadOnlyDictionary<string, object> args = null)
        {
            using (var conn = CreateConn())
            {
                conn.Open();
                using (var cmd = CreateCmd(conn, null, sql, args))
                {
                    return cmd.ExecuteNonQuery();
                }
            }
        }

        public object ExecuteScalar(string sql, IReadOnlyDictionary<string, object> args = null)
        {
            using (var conn = CreateConn())
            {
                conn.Open();
                using (var cmd = CreateCmd(conn, null, sql, args))
                {
                    return cmd.ExecuteScalar();
                }
            }
        }

        public List<Dictionary<string, object>> Query(string sql, IReadOnlyDictionary<string, object> args = null)
        {
            var list = new List<Dictionary<string, object>>();
            using (var conn = CreateConn())
            {
                conn.Open();
                using (var cmd = CreateCmd(conn, null, sql, args))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var row = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                        }
                        list.Add(row);
                    }
                }
            }
            return list;
        }

        public void WithTransaction(Action<IDbConnection, IDbTransaction> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            using (var conn = CreateConn())
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        action(conn, tx);
                        tx.Commit();
                    }
                    catch
                    {
                        try { tx.Rollback(); } catch { /* ignore */ }
                        throw;
                    }
                }
            }
        }
    }
}

