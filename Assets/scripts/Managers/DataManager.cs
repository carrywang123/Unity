// ============================================================
// 文件名：DataManager.cs
// 功  能：数据持久化管理
//         - 直连模式：Unity 直接连接 MySQL(3306) 进行增删改查
// 作  者：化工虚拟仿真实验平台
// ============================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using ChemLab.Models;
using ChemLab.Database;
using ChemLab.Utils;

namespace ChemLab.Managers
{
    public class DataManager : MonoBehaviour
    {
        // ── 单例 ──────────────────────────────────────────────
        public static DataManager Instance { get; private set; }

        private const string ADMIN_USERNAME = "222";
        private const string ADMIN_PASSWORD = "222"; // 原始密码，存储时会MD5

        // ── 数据库连接设置（直连 MySQL）────────────────────────
        [Header("=== MySQL 连接设置（直连）===")]
        [Tooltip("MySQL Host，例如 127.0.0.1")]
        public string mysqlHost = "127.0.0.1";

        [Tooltip("MySQL 端口，默认 3306")]
        public int mysqlPort = 3306;

        [Tooltip("数据库名，例如 chemlab")]
        public string mysqlDatabase = "chemlab";

        [Tooltip("数据库用户名，例如 root")]
        public string mysqlUser = "root";

        [Tooltip("数据库密码")]
        public string mysqlPassword = "Cloud2023@";

        [Tooltip("连接超时（秒）")]
        public int mysqlConnectTimeoutSeconds = 5;

        private MySqlDb _db;
        private string _mysqlConfigPath;

        [Serializable]
        private class MySqlConfig
        {
            public string host = "127.0.0.1";
            public int port = 3306;
            public string database = "chemlab";
            public string user = "root";
            public string password = "";
            public int connectTimeoutSeconds = 5;
        }

        // ── 当前登录用户（两种模式都使用）─────────────────────
        public UserModel CurrentUser { get; private set; }

        // ─────────────────────────────────────────────────────
        #region Unity 生命周期
        // ─────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _mysqlConfigPath = Path.Combine(Application.persistentDataPath, "mysql_config.json");
            LoadOrCreateMySqlConfigJson();

            EnsureDatabaseExists();
            InitMySql();
            EnsureSchema();
            EnsureAdminUser();
        }

        #endregion

        private void LoadOrCreateMySqlConfigJson()
        {
            try
            {
                if (!File.Exists(_mysqlConfigPath))
                {
                    var cfg = new MySqlConfig
                    {
                        host = mysqlHost,
                        port = mysqlPort,
                        database = mysqlDatabase,
                        user = mysqlUser,
                        password = mysqlPassword,
                        connectTimeoutSeconds = mysqlConnectTimeoutSeconds
                    };

                    string json = JsonUtility.ToJson(cfg, true);
                    File.WriteAllText(_mysqlConfigPath, json, Encoding.UTF8);
                    Debug.Log($"[DataManager] 未找到 MySQL 配置，已生成：{_mysqlConfigPath}");
                    return;
                }

                string text = File.ReadAllText(_mysqlConfigPath, Encoding.UTF8);
                var loaded = JsonUtility.FromJson<MySqlConfig>(text);
                if (loaded == null)
                {
                    Debug.LogWarning("[DataManager] MySQL 配置解析失败，将继续使用 Inspector 默认值。");
                    return;
                }

                // 覆盖 Inspector 默认值（以 JSON 为准）
                mysqlHost = string.IsNullOrWhiteSpace(loaded.host) ? mysqlHost : loaded.host;
                mysqlPort = loaded.port > 0 ? loaded.port : mysqlPort;
                mysqlDatabase = string.IsNullOrWhiteSpace(loaded.database) ? mysqlDatabase : loaded.database;
                mysqlUser = string.IsNullOrWhiteSpace(loaded.user) ? mysqlUser : loaded.user;
                mysqlPassword = loaded.password ?? mysqlPassword;
                mysqlConnectTimeoutSeconds = loaded.connectTimeoutSeconds > 0 ? loaded.connectTimeoutSeconds : mysqlConnectTimeoutSeconds;

                Debug.Log($"[DataManager] 已加载 MySQL 配置：{_mysqlConfigPath}");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[DataManager] 读取/生成 MySQL 配置失败：" + e.Message + "（将继续使用 Inspector 默认值）");
            }
        }

        public string GetMySqlConfigPath() => _mysqlConfigPath;
        // 兼容旧逻辑（SceneBootstrap 等脚本可能仍在调用 GetDatabasePath）
        public string GetDatabasePath() => _mysqlConfigPath;

        private void InitMySql()
        {
            var settings = new MySqlDb.Settings
            {
                host = mysqlHost,
                port = mysqlPort,
                database = mysqlDatabase,
                user = mysqlUser,
                password = mysqlPassword,
                connectTimeoutSeconds = mysqlConnectTimeoutSeconds
            };

            _db = new MySqlDb(settings);
            Debug.Log($"[DataManager] 已启用 MySQL 直连模式：{mysqlHost}:{mysqlPort}/{mysqlDatabase}");
        }

        private void EnsureDatabaseExists()
        {
            // 先连到 MySQL Server（不指定 database），创建数据库（若不存在）
            try
            {
                string dbName = (mysqlDatabase ?? "").Trim();
                if (string.IsNullOrWhiteSpace(dbName))
                {
                    Debug.LogWarning("[DataManager] mysqlDatabase 为空，跳过创建数据库步骤。");
                    return;
                }

                if (!IsSafeDbName(dbName))
                {
                    Debug.LogError("[DataManager] 数据库名不合法（只允许字母/数字/下划线）： " + dbName);
                    return;
                }

                var bootstrapSettings = new MySqlDb.Settings
                {
                    host = mysqlHost,
                    port = mysqlPort,
                    database = "", // 关键：不指定数据库
                    user = mysqlUser,
                    password = mysqlPassword,
                    connectTimeoutSeconds = mysqlConnectTimeoutSeconds
                };

                var bootstrapDb = new MySqlDb(bootstrapSettings);
                bootstrapDb.ExecuteNonQuery($"CREATE DATABASE IF NOT EXISTS `{dbName}` DEFAULT CHARACTER SET utf8mb4;");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[DataManager] 创建数据库失败（将继续尝试后续连接/建表）：" + e.Message);
            }
        }

        private static bool IsSafeDbName(string name)
        {
            // 只允许 a-zA-Z0-9_，避免 SQL 注入/转义问题（库名不能参数化）
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                bool ok = (c >= 'a' && c <= 'z') ||
                          (c >= 'A' && c <= 'Z') ||
                          (c >= '0' && c <= '9') ||
                          c == '_';
                if (!ok) return false;
            }
            return true;
        }

        private void EnsureSchema()
        {
            // users 表
            _db.ExecuteNonQuery(@"
CREATE TABLE IF NOT EXISTS users (
  user_id         VARCHAR(32)  NOT NULL,
  username        VARCHAR(50)  NOT NULL,
  password        VARCHAR(32)  NOT NULL,
  real_name       VARCHAR(50)  NOT NULL DEFAULT '',
  email           VARCHAR(100) NOT NULL DEFAULT '',
  role            INT          NOT NULL DEFAULT 1,
  create_time     VARCHAR(19)  NOT NULL DEFAULT '',
  last_login_time VARCHAR(19)  NOT NULL DEFAULT '',
  is_active       TINYINT      NOT NULL DEFAULT 1,
  PRIMARY KEY (user_id),
  UNIQUE KEY uk_users_username (username)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");

            // experiments 表（实验信息）
            _db.ExecuteNonQuery(@"
CREATE TABLE IF NOT EXISTS experiments (
  experiment_id          VARCHAR(32)   NOT NULL,
  experiment_name        VARCHAR(100)  NOT NULL DEFAULT '',
  experiment_description VARCHAR(500)  NOT NULL DEFAULT '',
  experiment_image       VARCHAR(255)  NOT NULL DEFAULT '',
  PRIMARY KEY (experiment_id),
  UNIQUE KEY uk_experiments_name (experiment_name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");

            // experiments 种子数据
            _db.ExecuteNonQuery(@"
INSERT INTO experiments (experiment_id, experiment_name, experiment_description, experiment_image)
VALUES (@experiment_id, @experiment_name, @experiment_description, @experiment_image)
ON DUPLICATE KEY UPDATE
  experiment_name        = VALUES(experiment_name),
  experiment_description = VALUES(experiment_description),
  experiment_image       = VALUES(experiment_image);",
                new Dictionary<string, object>
                {
                    { "@experiment_id", "1" },
                    { "@experiment_name", "吸光度检验" },
                    { "@experiment_description", "吸光度检验实验" },
                    { "@experiment_image", "" } // 约定：null 用空字符串存储
                });

            // records 表
            _db.ExecuteNonQuery(@"
CREATE TABLE IF NOT EXISTS records (
  record_id        VARCHAR(32)  NOT NULL,
  user_id          VARCHAR(32)  NOT NULL,
  experiment_name  VARCHAR(100) NOT NULL DEFAULT '',
  record_time      VARCHAR(19)  NOT NULL DEFAULT '',
  score            FLOAT        NOT NULL DEFAULT 0,
  PRIMARY KEY (record_id),
  KEY idx_records_user (user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;");
        }

        private void EnsureAdminUser()
        {
            // 如果管理员不存在则创建（用户名固定 222）
            object existsObj = _db.ExecuteScalar(
                "SELECT COUNT(1) FROM users WHERE username=@username LIMIT 1;",
                new Dictionary<string, object> { { "@username", ADMIN_USERNAME } }
            );

            long cnt = 0;
            if (existsObj != null && long.TryParse(existsObj.ToString(), out long v)) cnt = v;

            if (cnt > 0) return;

            var admin = new UserModel(
                ADMIN_USERNAME,
                MD5Encrypt(ADMIN_PASSWORD),
                "系统管理员",
                "admin@chemlab.com",
                UserRole.Admin
            );

            _db.ExecuteNonQuery(@"
INSERT INTO users (user_id, username, password, real_name, email, role, create_time, last_login_time, is_active)
VALUES (@user_id, @username, @password, @real_name, @email, @role, @create_time, @last_login_time, @is_active);",
                new Dictionary<string, object>
                {
                    { "@user_id", admin.userId },
                    { "@username", admin.username },
                    { "@password", admin.password },
                    { "@real_name", admin.realName ?? "" },
                    { "@email", admin.email ?? "" },
                    { "@role", (int)admin.role },
                    { "@create_time", admin.createTime ?? "" },
                    { "@last_login_time", admin.lastLoginTime ?? "" },
                    { "@is_active", admin.isActive ? 1 : 0 }
                });

            Debug.Log("[DataManager] 已初始化管理员账号（222/222）。");
        }

        // ─────────────────────────────────────────────────────
        #region 用户 CRUD（MySQL）
        // ─────────────────────────────────────────────────────

        public List<UserModel> GetAllUsers()
        {
            var rows = _db.Query("SELECT * FROM users;");
            var list = new List<UserModel>(rows.Count);
            foreach (var r in rows) list.Add(RowToUser(r));
            return list;
        }

        public UserModel FindUserByUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return null;
            var rows = _db.Query(
                "SELECT * FROM users WHERE LOWER(username)=LOWER(@username) LIMIT 1;",
                new Dictionary<string, object> { { "@username", username } });
            return rows.Count > 0 ? RowToUser(rows[0]) : null;
        }

        public UserModel FindUserById(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return null;
            var rows = _db.Query(
                "SELECT * FROM users WHERE user_id=@user_id LIMIT 1;",
                new Dictionary<string, object> { { "@user_id", userId } });
            return rows.Count > 0 ? RowToUser(rows[0]) : null;
        }

        public bool RegisterUser(string username, string rawPassword,
                                 string realName, string email,
                                 out string errorMsg)
        {
            errorMsg = "";

            if (string.IsNullOrWhiteSpace(username))
            { errorMsg = "用户名不能为空！"; return false; }

            if (username.Length < 3 || username.Length > 20)
            { errorMsg = "用户名长度须在 3~20 个字符之间！"; return false; }

            if (string.IsNullOrWhiteSpace(rawPassword))
            { errorMsg = "密码不能为空！"; return false; }

            if (rawPassword.Length < 6)
            { errorMsg = "密码长度不能少于 6 位！"; return false; }

            if (FindUserByUsername(username) != null)
            { errorMsg = $"用户名 \"{username}\" 已被注册！"; return false; }

            var newUser = new UserModel(
                username,
                MD5Encrypt(rawPassword),
                realName,
                email,
                UserRole.User
            );
            try
            {
                _db.ExecuteNonQuery(@"
INSERT INTO users (user_id, username, password, real_name, email, role, create_time, last_login_time, is_active)
VALUES (@user_id, @username, @password, @real_name, @email, @role, @create_time, @last_login_time, @is_active);",
                    new Dictionary<string, object>
                    {
                        { "@user_id", newUser.userId },
                        { "@username", newUser.username },
                        { "@password", newUser.password },
                        { "@real_name", newUser.realName ?? "" },
                        { "@email", newUser.email ?? "" },
                        { "@role", (int)newUser.role },
                        { "@create_time", newUser.createTime ?? "" },
                        { "@last_login_time", newUser.lastLoginTime ?? "" },
                        { "@is_active", newUser.isActive ? 1 : 0 }
                    });
            }
            catch (Exception e)
            {
                errorMsg = "注册失败：" + e.Message;
                return false;
            }

            Debug.Log($"[DataManager] 新用户注册成功：{username}");
            return true;
        }

        public bool AdminAddUser(string username, string rawPassword,
                                 string realName, string email,
                                 UserRole role, out string errorMsg)
        {
            errorMsg = "";

            if (string.IsNullOrWhiteSpace(username))
            { errorMsg = "用户名不能为空！"; return false; }

            if (string.IsNullOrWhiteSpace(rawPassword))
            { errorMsg = "密码不能为空！"; return false; }

            if (rawPassword.Length < 6)
            { errorMsg = "密码长度不能少于 6 位！"; return false; }

            if (FindUserByUsername(username) != null)
            { errorMsg = $"用户名 \"{username}\" 已存在！"; return false; }

            var newUser = new UserModel(username, MD5Encrypt(rawPassword),
                                        realName, email, role);
            try
            {
                _db.ExecuteNonQuery(@"
INSERT INTO users (user_id, username, password, real_name, email, role, create_time, last_login_time, is_active)
VALUES (@user_id, @username, @password, @real_name, @email, @role, @create_time, @last_login_time, @is_active);",
                    new Dictionary<string, object>
                    {
                        { "@user_id", newUser.userId },
                        { "@username", newUser.username },
                        { "@password", newUser.password },
                        { "@real_name", newUser.realName ?? "" },
                        { "@email", newUser.email ?? "" },
                        { "@role", (int)newUser.role },
                        { "@create_time", newUser.createTime ?? "" },
                        { "@last_login_time", newUser.lastLoginTime ?? "" },
                        { "@is_active", newUser.isActive ? 1 : 0 }
                    });
            }
            catch (Exception e)
            {
                errorMsg = "添加用户失败：" + e.Message;
                return false;
            }
            return true;
        }

        public bool DeleteUser(string userId, out string errorMsg)
        {
            errorMsg = "";

            var user = FindUserById(userId);
            if (user == null)
            { errorMsg = "用户不存在！"; return false; }

            if (CurrentUser != null && CurrentUser.userId == userId)
            { errorMsg = "不能删除当前登录的账号！"; return false; }

            try
            {
                // 这里不强依赖事务API，避免在未导入 MySQL DLL / 未启用宏时编译失败。
                // 先删关联记录，再删用户。
                _db.ExecuteNonQuery("DELETE FROM records WHERE user_id=@user_id;",
                    new Dictionary<string, object> { { "@user_id", userId } });
                _db.ExecuteNonQuery("DELETE FROM users WHERE user_id=@user_id;",
                    new Dictionary<string, object> { { "@user_id", userId } });
            }
            catch (Exception e)
            {
                errorMsg = "删除用户失败：" + e.Message;
                return false;
            }
            return true;
        }

        public bool ToggleUserActive(string userId, out string errorMsg)
        {
            errorMsg = "";

            var user = FindUserById(userId);
            if (user == null)
            { errorMsg = "用户不存在！"; return false; }

            if (user.role == UserRole.Admin)
            { errorMsg = "不能禁用管理员账号！"; return false; }

            bool newActive = !user.isActive;
            try
            {
                _db.ExecuteNonQuery("UPDATE users SET is_active=@is_active WHERE user_id=@user_id;",
                    new Dictionary<string, object>
                    {
                        { "@is_active", newActive ? 1 : 0 },
                        { "@user_id", userId }
                    });
            }
            catch (Exception e)
            {
                errorMsg = "更新状态失败：" + e.Message;
                return false;
            }
            return true;
        }

        public bool ResetPassword(string userId, string newRawPassword, out string errorMsg)
        {
            errorMsg = "";

            if (string.IsNullOrWhiteSpace(newRawPassword) || newRawPassword.Length < 6)
            { errorMsg = "新密码长度不能少于 6 位！"; return false; }

            var user = FindUserById(userId);
            if (user == null)
            { errorMsg = "用户不存在！"; return false; }

            try
            {
                _db.ExecuteNonQuery("UPDATE users SET password=@pwd WHERE user_id=@user_id;",
                    new Dictionary<string, object>
                    {
                        { "@pwd", MD5Encrypt(newRawPassword) },
                        { "@user_id", userId }
                    });
            }
            catch (Exception e)
            {
                errorMsg = "重置密码失败：" + e.Message;
                return false;
            }
            return true;
        }

        /// <summary>
        /// 管理员更新用户信息（真实姓名/邮箱/角色/可选重置密码；校验规则与注册一致）
        /// </summary>
        public bool AdminUpdateUser(string userId, string realName, string email, UserRole role,
            string newRawPassword, out string errorMsg)
        {
            errorMsg = "";

            var user = FindUserById(userId);
            if (user == null)
            { errorMsg = "用户不存在！"; return false; }

            // 内置管理员账号不允许被改成普通用户（避免锁死后台）
            if (user.role == UserRole.Admin && role != UserRole.Admin)
            { errorMsg = "不能修改管理员账号的角色！"; return false; }

            if (!Validator.IsValidRealName(realName, out string rnErr))
            { errorMsg = rnErr; return false; }

            if (!Validator.IsValidEmail(email, out string emailErr))
            { errorMsg = emailErr; return false; }

            bool updatePassword = !string.IsNullOrWhiteSpace(newRawPassword);
            if (updatePassword && !Validator.IsValidPassword(newRawPassword, out string pwdErr))
            { errorMsg = pwdErr; return false; }

            try
            {
                var sql = "UPDATE users SET real_name=@real_name, email=@email, role=@role" +
                          (updatePassword ? ", password=@pwd" : "") +
                          " WHERE user_id=@user_id;";

                var p = new Dictionary<string, object>
                {
                    { "@real_name", realName ?? "" },
                    { "@email", email ?? "" },
                    { "@role", (int)role },
                    { "@user_id", userId }
                };
                if (updatePassword) p["@pwd"] = MD5Encrypt(newRawPassword);

                _db.ExecuteNonQuery(sql, p);
            }
            catch (Exception e)
            {
                errorMsg = "更新用户失败：" + e.Message;
                return false;
            }

            return true;
        }

        #endregion

        // ─────────────────────────────────────────────────────
        #region 认证（MySQL）
        // ─────────────────────────────────────────────────────

        public bool Login(string username, string rawPassword, out string errorMsg)
        {
            errorMsg = "";

            if (string.IsNullOrWhiteSpace(username))
            { errorMsg = "请输入用户名！"; return false; }

            if (string.IsNullOrWhiteSpace(rawPassword))
            { errorMsg = "请输入密码！"; return false; }

            var user = FindUserByUsername(username);
            if (user == null)
            { errorMsg = "用户名不存在！"; return false; }

            if (!user.isActive)
            { errorMsg = "该账号已被禁用，请联系管理员！"; return false; }

            if (user.password != MD5Encrypt(rawPassword))
            { errorMsg = "密码错误！"; return false; }

            user.lastLoginTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            CurrentUser = user;
            try
            {
                _db.ExecuteNonQuery("UPDATE users SET last_login_time=@t WHERE user_id=@user_id;",
                    new Dictionary<string, object>
                    {
                        { "@t", user.lastLoginTime ?? "" },
                        { "@user_id", user.userId }
                    });
            }
            catch (Exception e)
            {
                // 不阻断登录，只提示
                Debug.LogWarning("[DataManager] 更新 last_login_time 失败：" + e.Message);
            }

            Debug.Log($"[DataManager] 用户 [{username}] 登录成功，角色：{user.role}");
            return true;
        }

        public void Logout()
        {
            Debug.Log($"[DataManager] 用户 [{CurrentUser?.username}] 已登出。");
            CurrentUser = null;
        }

        #endregion

        // ─────────────────────────────────────────────────────
        #region 实验表 CRUD（MySQL）
        // ─────────────────────────────────────────────────────

        public List<ExperimentModel> GetAllExperiments()
        {
            var rows = _db.Query("SELECT * FROM experiments;");
            var list = new List<ExperimentModel>(rows.Count);
            foreach (var r in rows) list.Add(RowToExperiment(r));
            return list;
        }

        public ExperimentModel FindExperimentById(string experimentId)
        {
            if (string.IsNullOrWhiteSpace(experimentId)) return null;
            var rows = _db.Query(
                "SELECT * FROM experiments WHERE experiment_id=@experiment_id LIMIT 1;",
                new Dictionary<string, object> { { "@experiment_id", experimentId } }
            );
            return rows.Count > 0 ? RowToExperiment(rows[0]) : null;
        }

        public void UpsertExperiment(ExperimentModel exp)
        {
            if (exp == null) return;
            if (string.IsNullOrWhiteSpace(exp.experimentId)) return;

            try
            {
                _db.ExecuteNonQuery(@"
INSERT INTO experiments (experiment_id, experiment_name, experiment_description, experiment_image)
VALUES (@experiment_id, @experiment_name, @experiment_description, @experiment_image)
ON DUPLICATE KEY UPDATE
  experiment_name        = VALUES(experiment_name),
  experiment_description = VALUES(experiment_description),
  experiment_image       = VALUES(experiment_image);",
                    new Dictionary<string, object>
                    {
                        { "@experiment_id", exp.experimentId },
                        { "@experiment_name", exp.experimentName ?? "" },
                        { "@experiment_description", exp.experimentDescription ?? "" },
                        { "@experiment_image", exp.experimentImage ?? "" },
                    });
            }
            catch (Exception e)
            {
                Debug.LogError("[DataManager] UpsertExperiment 失败：" + e.Message);
            }
        }

        public bool DeleteExperiment(string experimentId, out string errorMsg)
        {
            errorMsg = "";
            if (string.IsNullOrWhiteSpace(experimentId))
            { errorMsg = "实验ID不能为空！"; return false; }

            try
            {
                int affected = _db.ExecuteNonQuery(
                    "DELETE FROM experiments WHERE experiment_id=@experiment_id;",
                    new Dictionary<string, object> { { "@experiment_id", experimentId } }
                );
                if (affected <= 0)
                { errorMsg = "实验不存在！"; return false; }
            }
            catch (Exception e)
            {
                errorMsg = "删除实验失败：" + e.Message;
                return false;
            }

            return true;
        }

        #endregion

        // ─────────────────────────────────────────────────────
        #region 实验记录 CRUD（MySQL）
        // ─────────────────────────────────────────────────────

        public List<ExperimentRecord> GetAllRecords()
        {
            var rows = _db.Query(@"
SELECT r.record_id, r.user_id, u.username AS username, r.experiment_name, r.record_time, r.score
FROM records r
LEFT JOIN users u ON u.user_id = r.user_id;");
            var list = new List<ExperimentRecord>(rows.Count);
            foreach (var r in rows) list.Add(RowToRecord(r));
            return list;
        }

        public List<ExperimentRecord> GetRecordsByUser(string userId)
        {
            var rows = _db.Query(@"
SELECT r.record_id, r.user_id, u.username AS username, r.experiment_name, r.record_time, r.score
FROM records r
LEFT JOIN users u ON u.user_id = r.user_id
WHERE r.user_id=@user_id;",
                new Dictionary<string, object> { { "@user_id", userId } });
            var list = new List<ExperimentRecord>(rows.Count);
            foreach (var r in rows) list.Add(RowToRecord(r));
            return list;
        }

        public void AddRecord(ExperimentRecord record)
        {
            if (record == null) return;
            try
            {
                _db.ExecuteNonQuery(@"
INSERT INTO records (record_id, user_id, experiment_name, record_time, score)
VALUES (@record_id, @user_id, @experiment_name, @record_time, @score);",
                    new Dictionary<string, object>
                    {
                        { "@record_id", record.recordId },
                        { "@user_id", record.userId },
                        { "@experiment_name", record.experimentName ?? "" },
                        { "@record_time", record.recordTime ?? "" },
                        { "@score", record.score }
                    });
            }
            catch (Exception e)
            {
                Debug.LogError("[DataManager] 添加记录失败：" + e.Message);
            }
        }

        public bool DeleteRecord(string recordId, out string errorMsg)
        {
            errorMsg = "";
            if (string.IsNullOrWhiteSpace(recordId))
            { errorMsg = "记录ID不能为空！"; return false; }

            try
            {
                int affected = _db.ExecuteNonQuery("DELETE FROM records WHERE record_id=@record_id;",
                    new Dictionary<string, object> { { "@record_id", recordId } });
                if (affected <= 0)
                { errorMsg = "记录不存在！"; return false; }
            }
            catch (Exception e)
            {
                errorMsg = "删除记录失败：" + e.Message;
                return false;
            }
            return true;
        }

        public void CompleteRecord(string recordId, float score, string result)
        {
            if (string.IsNullOrWhiteSpace(recordId)) return;
            try
            {
                _db.ExecuteNonQuery(@"
UPDATE records
SET score=@score
WHERE record_id=@record_id;",
                    new Dictionary<string, object>
                    {
                        { "@score", score },
                        { "@record_id", recordId }
                    });
            }
            catch (Exception e)
            {
                Debug.LogError("[DataManager] 完成记录失败：" + e.Message);
            }
        }

        #endregion

        // ─────────────────────────────────────────────────────
        #region 异步协程接口（兼容 UI 调用；内部走 MySQL）
        // ─────────────────────────────────────────────────────

        public IEnumerator LoginAsync(string username, string rawPassword, bool isAdmin, Action<bool, string> onDone)
        {
            // 兼容旧UI传参 isAdmin；数据库中以 role 字段为准，这里不做强制。
            bool ok = false;
            string err = "";
            try
            {
                ok = Login(username, rawPassword, out err);
            }
            catch (Exception e)
            {
                ok = false;
                err = e.Message;
            }
            yield return null;
            onDone?.Invoke(ok, err);
        }

        public IEnumerator RegisterUserAsync(string username, string rawPassword, string realName, string email, Action<bool, string> onDone)
        {
            bool ok = false;
            string err = "";
            try
            {
                ok = RegisterUser(username, rawPassword, realName, email, out err);
            }
            catch (Exception e)
            {
                ok = false;
                err = e.Message;
            }
            yield return null;
            onDone?.Invoke(ok, err);
        }

        public IEnumerator LogoutAsync(Action<bool, string> onDone)
        {
            Logout();
            yield return null;
            onDone?.Invoke(true, "");
        }

        #endregion

        // ─────────────────────────────────────────────────────
        #region 工具方法
        // ─────────────────────────────────────────────────────

        /// <summary>MD5 加密（32位小写）</summary>
        public static string MD5Encrypt(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                StringBuilder sb = new StringBuilder();
                foreach (byte b in hashBytes)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        private static UserModel RowToUser(Dictionary<string, object> r)
        {
            var u = new UserModel();
            u.userId = ToStr(r, "user_id");
            u.username = ToStr(r, "username");
            u.password = ToStr(r, "password");
            u.realName = ToStr(r, "real_name");
            u.email = ToStr(r, "email");
            u.role = (UserRole)ToInt(r, "role", 1);
            u.createTime = ToStr(r, "create_time");
            u.lastLoginTime = ToStr(r, "last_login_time");
            u.isActive = ToInt(r, "is_active", 1) != 0;
            return u;
        }

        private static ExperimentRecord RowToRecord(Dictionary<string, object> r)
        {
            var rec = new ExperimentRecord();
            rec.recordId = ToStr(r, "record_id");
            rec.userId = ToStr(r, "user_id");
            rec.username = ToStr(r, "username");
            rec.experimentName = ToStr(r, "experiment_name");
            rec.recordTime = ToStr(r, "record_time");
            rec.score = ToFloat(r, "score", 0f);
            return rec;
        }

        private static ExperimentModel RowToExperiment(Dictionary<string, object> r)
        {
            var e = new ExperimentModel();
            e.experimentId = ToStr(r, "experiment_id");
            e.experimentName = ToStr(r, "experiment_name");
            e.experimentDescription = ToStr(r, "experiment_description");
            e.experimentImage = ToStr(r, "experiment_image");
            return e;
        }

        private static string ToStr(Dictionary<string, object> r, string key)
        {
            if (r == null || !r.TryGetValue(key, out var v) || v == null) return "";
            return v.ToString();
        }

        private static int ToInt(Dictionary<string, object> r, string key, int def)
        {
            if (r == null || !r.TryGetValue(key, out var v) || v == null) return def;
            if (int.TryParse(v.ToString(), out var i)) return i;
            return def;
        }

        private static float ToFloat(Dictionary<string, object> r, string key, float def)
        {
            if (r == null || !r.TryGetValue(key, out var v) || v == null) return def;
            if (float.TryParse(v.ToString(), out var f)) return f;
            return def;
        }

        #endregion
    }
}

