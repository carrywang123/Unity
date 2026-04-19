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
#if UNITY_WEBGL
using ChemLab.Networking;
#endif

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

        // 注意：这个字段必须在 Editor 与 Player 中都存在，否则会触发序列化不一致报错。
        // WebGL 运行时才会使用它；PC/Server 下不会用到。
        [Header("=== WebGL API 设置 ===")]
        [Tooltip("WebGL API 根地址（须含 /api），例如 http://118.25.40.159:7070/api")]
        public string apiBaseUrl = "http://118.25.40.159:7070/api";

#if UNITY_WEBGL
        private ApiClient _api;

        private List<UserModel> _cacheUsers = new List<UserModel>();
        private List<ExperimentModel> _cacheExperiments = new List<ExperimentModel>();
        private List<ExperimentRecord> _cacheAllRecords = new List<ExperimentRecord>();
        private List<ExperimentRecord> _cacheMyRecords = new List<ExperimentRecord>();
#endif

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

#if UNITY_WEBGL
            _api = new ApiClient(apiBaseUrl);
#else
            EnsureDatabaseExists();
            InitMySql();
            EnsureSchema();
            EnsureAdminUser();
#endif
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
#if UNITY_WEBGL && !UNITY_EDITOR
            return new List<UserModel>(_cacheUsers);
#else
            var rows = _db.Query("SELECT * FROM users;");
            var list = new List<UserModel>(rows.Count);
            foreach (var r in rows) list.Add(RowToUser(r));
            return list;
#endif
        }

        public UserModel FindUserByUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return null;
#if UNITY_WEBGL && !UNITY_EDITOR
            // WebGL：从缓存/当前用户中查找
            if (CurrentUser != null && string.Equals(CurrentUser.username, username, StringComparison.OrdinalIgnoreCase))
                return CurrentUser;

            for (int i = 0; i < _cacheUsers.Count; i++)
            {
                var u = _cacheUsers[i];
                if (u == null) continue;
                if (string.Equals(u.username, username, StringComparison.OrdinalIgnoreCase))
                    return u;
            }
            return null;
#else
            var rows = _db.Query(
                "SELECT * FROM users WHERE LOWER(username)=LOWER(@username) LIMIT 1;",
                new Dictionary<string, object> { { "@username", username } });
            return rows.Count > 0 ? RowToUser(rows[0]) : null;
#endif
        }

        public UserModel FindUserById(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return null;
#if UNITY_WEBGL && !UNITY_EDITOR
            // WebGL：从缓存/当前用户中查找
            if (CurrentUser != null && string.Equals(CurrentUser.userId, userId, StringComparison.OrdinalIgnoreCase))
                return CurrentUser;

            for (int i = 0; i < _cacheUsers.Count; i++)
            {
                var u = _cacheUsers[i];
                if (u == null) continue;
                if (string.Equals(u.userId, userId, StringComparison.OrdinalIgnoreCase))
                    return u;
            }
            return null;
#else
            var rows = _db.Query(
                "SELECT * FROM users WHERE user_id=@user_id LIMIT 1;",
                new Dictionary<string, object> { { "@user_id", userId } });
            return rows.Count > 0 ? RowToUser(rows[0]) : null;
#endif
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
#if UNITY_WEBGL && !UNITY_EDITOR
            return new List<ExperimentModel>(_cacheExperiments);
#else
            var rows = _db.Query("SELECT * FROM experiments;");
            var list = new List<ExperimentModel>(rows.Count);
            foreach (var r in rows) list.Add(RowToExperiment(r));
            return list;
#endif
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
            #if UNITY_WEBGL && !UNITY_EDITOR
            return new List<ExperimentRecord>(_cacheAllRecords);
            #else
            var rows = _db.Query(@"
SELECT r.record_id, r.user_id, u.username AS username, r.experiment_name, r.record_time, r.score
FROM records r
LEFT JOIN users u ON u.user_id = r.user_id;");
            var list = new List<ExperimentRecord>(rows.Count);
            foreach (var r in rows) list.Add(RowToRecord(r));
            return list;
            #endif
        }

        public List<ExperimentRecord> GetRecordsByUser(string userId)
        {
            #if UNITY_WEBGL && !UNITY_EDITOR
            // WebGL：WarmupAfterLoginAsync 已拉取当前用户记录；这里只返回缓存
            return new List<ExperimentRecord>(_cacheMyRecords);
            #else
            var rows = _db.Query(@"
SELECT r.record_id, r.user_id, u.username AS username, r.experiment_name, r.record_time, r.score
FROM records r
LEFT JOIN users u ON u.user_id = r.user_id
WHERE r.user_id=@user_id;",
                new Dictionary<string, object> { { "@user_id", userId } });
            var list = new List<ExperimentRecord>(rows.Count);
            foreach (var r in rows) list.Add(RowToRecord(r));
            return list;
            #endif
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
#if UNITY_WEBGL
            bool ok = false;
            string err = "";
            yield return _api.PostJson("/login",
                JsonUtility.ToJson(new LoginRequest { username = username, password = rawPassword }),
                (succ, e2, text) =>
                {
                    if (!succ) { ok = false; err = e2; return; }
                    var resp = JsonUtility.FromJson<LoginResponse>(text ?? "");
                    if (resp == null || !resp.ok || resp.user == null)
                    {
                        ok = false;
                        err = (resp != null && !string.IsNullOrEmpty(resp.error)) ? resp.error : "登录失败";
                        return;
                    }
                    CurrentUser = resp.user.ToModel();
                    ok = true;
                });
            onDone?.Invoke(ok, err);
#else
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
#endif
        }

#if UNITY_WEBGL
        [Serializable] private class LoginRequest { public string username; public string password; }
        [Serializable] private class LoginResponse { public bool ok; public string error; public UserDto user; }

        [Serializable]
        private class UserDto
        {
            public string userId;
            public string username;
            public string password;
            public string realName;
            public string email;
            public int role;
            public string createTime;
            public string lastLoginTime;
            public bool isActive;

            public UserModel ToModel()
            {
                var u = new UserModel();
                u.userId = userId;
                u.username = username;
                u.password = password;
                u.realName = realName;
                u.email = email;
                u.role = (UserRole)role;
                u.createTime = createTime;
                u.lastLoginTime = lastLoginTime;
                u.isActive = isActive;
                return u;
            }
        }

        [Serializable] private class ExperimentsResponse { public bool ok; public string error; public ExperimentDto[] experiments; }
        [Serializable] private class UsersResponse { public bool ok; public string error; public UserDto[] users; }
        [Serializable] private class RecordsResponse { public bool ok; public string error; public RecordDto[] records; }

        [Serializable]
        private class ExperimentDto
        {
            public string experimentId;
            public string experimentName;
            public string experimentDescription;
            public string experimentImage;

            public ExperimentModel ToModel()
            {
                return new ExperimentModel(experimentId, experimentName, experimentDescription, experimentImage);
            }
        }

        [Serializable]
        private class RecordDto
        {
            public string recordId;
            public string userId;
            public string username;
            public string experimentName;
            public string recordTime;
            public float score;

            public ExperimentRecord ToModel()
            {
                var r = new ExperimentRecord();
                r.recordId = recordId;
                r.userId = userId;
                r.username = username;
                r.experimentName = experimentName;
                r.recordTime = recordTime;
                r.score = score;
                return r;
            }
        }

        public IEnumerator WarmupAfterLoginAsync(Action<bool, string> onDone)
        {
            if (CurrentUser == null) { onDone?.Invoke(false, "未登录"); yield break; }

            bool ok = true;
            string err = "";

            // experiments
            yield return _api.Get("/experiments", (succ, e2, text) =>
            {
                if (!succ) { ok = false; err = e2; return; }
                var resp = JsonUtility.FromJson<ExperimentsResponse>(text ?? "");
                if (resp == null || !resp.ok) { ok = false; err = resp != null ? resp.error : "获取实验列表失败"; return; }
                _cacheExperiments.Clear();
                if (resp.experiments != null)
                    for (int i = 0; i < resp.experiments.Length; i++)
                        if (resp.experiments[i] != null) _cacheExperiments.Add(resp.experiments[i].ToModel());
            });
            if (!ok) { onDone?.Invoke(false, err); yield break; }

            // records (my)
            yield return _api.Get("/records/byUser?userId=" + Uri.EscapeDataString(CurrentUser.userId ?? ""), (succ, e2, text) =>
            {
                if (!succ) { ok = false; err = e2; return; }
                var resp = JsonUtility.FromJson<RecordsResponse>(text ?? "");
                if (resp == null || !resp.ok) { ok = false; err = resp != null ? resp.error : "获取我的记录失败"; return; }
                _cacheMyRecords.Clear();
                if (resp.records != null)
                    for (int i = 0; i < resp.records.Length; i++)
                        if (resp.records[i] != null) _cacheMyRecords.Add(resp.records[i].ToModel());
            });
            if (!ok) { onDone?.Invoke(false, err); yield break; }

            if (CurrentUser.role == UserRole.Admin)
            {
                yield return _api.Get("/users", (succ, e2, text) =>
                {
                    if (!succ) { ok = false; err = e2; return; }
                    var resp = JsonUtility.FromJson<UsersResponse>(text ?? "");
                    if (resp == null || !resp.ok) { ok = false; err = resp != null ? resp.error : "获取用户列表失败"; return; }
                    _cacheUsers.Clear();
                    if (resp.users != null)
                        for (int i = 0; i < resp.users.Length; i++)
                            if (resp.users[i] != null) _cacheUsers.Add(resp.users[i].ToModel());
                });
                if (!ok) { onDone?.Invoke(false, err); yield break; }

                yield return _api.Get("/records", (succ, e2, text) =>
                {
                    if (!succ) { ok = false; err = e2; return; }
                    var resp = JsonUtility.FromJson<RecordsResponse>(text ?? "");
                    if (resp == null || !resp.ok) { ok = false; err = resp != null ? resp.error : "获取记录列表失败"; return; }
                    _cacheAllRecords.Clear();
                    if (resp.records != null)
                        for (int i = 0; i < resp.records.Length; i++)
                            if (resp.records[i] != null) _cacheAllRecords.Add(resp.records[i].ToModel());
                });
                if (!ok) { onDone?.Invoke(false, err); yield break; }
            }

            onDone?.Invoke(true, "");
        }
#endif

        public IEnumerator RegisterUserAsync(string username, string rawPassword, string realName, string email, Action<bool, string> onDone)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            bool ok = false;
            string err = "";
            yield return _api.PostJson("/register",
                JsonUtility.ToJson(new RegisterRequest { username = username, password = rawPassword, realName = realName, email = email }),
                (succ, e2, text) =>
                {
                    if (!succ) { ok = false; err = e2; return; }
                    var resp = JsonUtility.FromJson<SimpleOkResponse>(text ?? "");
                    if (resp == null || !resp.ok)
                    {
                        ok = false;
                        err = (resp != null && !string.IsNullOrEmpty(resp.error)) ? resp.error : "注册失败";
                        return;
                    }
                    ok = true;
                });
            onDone?.Invoke(ok, err);
#else
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
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [Serializable] private class SimpleOkResponse { public bool ok; public string error; }
        [Serializable] private class RegisterRequest { public string username; public string password; public string realName; public string email; }

        [Serializable] private class AdminAddUserRequest { public string username; public string password; public string realName; public string email; public int role; }
        [Serializable] private class AdminUpdateUserRequest { public string userId; public string realName; public string email; public int role; public string newPassword; }
        [Serializable] private class ToggleActiveRequest { public string userId; public int isActive; }
        [Serializable] private class DeleteUserRequest { public string userId; }
        [Serializable] private class ResetPasswordRequest { public string userId; public string newPassword; }
        [Serializable] private class AddRecordRequest { public string recordId; public string userId; public string experimentName; public string recordTime; public float score; }
        [Serializable] private class CompleteRecordRequest { public string recordId; public float score; }
        [Serializable] private class DeleteRecordRequest { public string recordId; }
        [Serializable] private class UpsertExperimentRequest { public string experimentId; public string experimentName; public string experimentDescription; public string experimentImage; }
        [Serializable] private class DeleteExperimentRequest { public string experimentId; }

        public IEnumerator AdminAddUserAsync(string username, string rawPassword, string realName, string email, UserRole role, Action<bool, string> onDone)
        {
            bool ok = false;
            string err = "";
            yield return _api.PostJson("/admin/user/add",
                JsonUtility.ToJson(new AdminAddUserRequest { username = username, password = rawPassword, realName = realName, email = email, role = (int)role }),
                (succ, e2, text) =>
                {
                    if (!succ) { ok = false; err = e2; return; }
                    var resp = JsonUtility.FromJson<SimpleOkResponse>(text ?? "");
                    if (resp == null || !resp.ok) { ok = false; err = resp != null ? resp.error : "添加用户失败"; return; }
                    ok = true;
                });
            if (ok) yield return WarmupAfterLoginAsync((_, __) => { });
            onDone?.Invoke(ok, err);
        }

        public IEnumerator AdminUpdateUserAsync(string userId, string realName, string email, UserRole role, string newRawPassword, Action<bool, string> onDone)
        {
            bool ok = false;
            string err = "";
            yield return _api.PostJson("/admin/user/update",
                JsonUtility.ToJson(new AdminUpdateUserRequest { userId = userId, realName = realName, email = email, role = (int)role, newPassword = newRawPassword ?? "" }),
                (succ, e2, text) =>
                {
                    if (!succ) { ok = false; err = e2; return; }
                    var resp = JsonUtility.FromJson<SimpleOkResponse>(text ?? "");
                    if (resp == null || !resp.ok) { ok = false; err = resp != null ? resp.error : "更新用户失败"; return; }
                    ok = true;
                });
            if (ok) yield return WarmupAfterLoginAsync((_, __) => { });
            onDone?.Invoke(ok, err);
        }

        public IEnumerator DeleteUserAsync(string userId, Action<bool, string> onDone)
        {
            bool ok = false;
            string err = "";
            yield return _api.PostJson("/admin/user/delete",
                JsonUtility.ToJson(new DeleteUserRequest { userId = userId }),
                (succ, e2, text) =>
                {
                    if (!succ) { ok = false; err = e2; return; }
                    var resp = JsonUtility.FromJson<SimpleOkResponse>(text ?? "");
                    if (resp == null || !resp.ok) { ok = false; err = resp != null ? resp.error : "删除用户失败"; return; }
                    ok = true;
                });
            if (ok) yield return WarmupAfterLoginAsync((_, __) => { });
            onDone?.Invoke(ok, err);
        }

        public IEnumerator ToggleUserActiveAsync(string userId, bool isActive, Action<bool, string> onDone)
        {
            bool ok = false;
            string err = "";
            yield return _api.PostJson("/admin/user/toggleActive",
                JsonUtility.ToJson(new ToggleActiveRequest { userId = userId, isActive = isActive ? 1 : 0 }),
                (succ, e2, text) =>
                {
                    if (!succ) { ok = false; err = e2; return; }
                    var resp = JsonUtility.FromJson<SimpleOkResponse>(text ?? "");
                    if (resp == null || !resp.ok) { ok = false; err = resp != null ? resp.error : "更新状态失败"; return; }
                    ok = true;
                });
            if (ok) yield return WarmupAfterLoginAsync((_, __) => { });
            onDone?.Invoke(ok, err);
        }

        public IEnumerator ResetPasswordAsync(string userId, string newRawPassword, Action<bool, string> onDone)
        {
            bool ok = false;
            string err = "";
            yield return _api.PostJson("/user/resetPassword",
                JsonUtility.ToJson(new ResetPasswordRequest { userId = userId, newPassword = newRawPassword }),
                (succ, e2, text) =>
                {
                    if (!succ) { ok = false; err = e2; return; }
                    var resp = JsonUtility.FromJson<SimpleOkResponse>(text ?? "");
                    if (resp == null || !resp.ok) { ok = false; err = resp != null ? resp.error : "重置密码失败"; return; }
                    ok = true;
                });
            if (ok) yield return WarmupAfterLoginAsync((_, __) => { });
            onDone?.Invoke(ok, err);
        }

        public IEnumerator AddRecordAsync(ExperimentRecord record, Action<bool, string> onDone)
        {
            bool ok = false;
            string err = "";
            if (record == null) { onDone?.Invoke(false, "record 为空"); yield break; }
            yield return _api.PostJson("/record/add",
                JsonUtility.ToJson(new AddRecordRequest
                {
                    recordId = record.recordId,
                    userId = record.userId,
                    experimentName = record.experimentName,
                    recordTime = record.recordTime,
                    score = record.score
                }),
                (succ, e2, text) =>
                {
                    if (!succ) { ok = false; err = e2; return; }
                    var resp = JsonUtility.FromJson<SimpleOkResponse>(text ?? "");
                    if (resp == null || !resp.ok) { ok = false; err = resp != null ? resp.error : "添加记录失败"; return; }
                    ok = true;
                });
            if (ok) yield return WarmupAfterLoginAsync((_, __) => { });
            onDone?.Invoke(ok, err);
        }

        public IEnumerator CompleteRecordAsync(string recordId, float score, Action<bool, string> onDone)
        {
            bool ok = false;
            string err = "";
            yield return _api.PostJson("/record/complete",
                JsonUtility.ToJson(new CompleteRecordRequest { recordId = recordId, score = score }),
                (succ, e2, text) =>
                {
                    if (!succ) { ok = false; err = e2; return; }
                    var resp = JsonUtility.FromJson<SimpleOkResponse>(text ?? "");
                    if (resp == null || !resp.ok) { ok = false; err = resp != null ? resp.error : "完成记录失败"; return; }
                    ok = true;
                });
            if (ok) yield return WarmupAfterLoginAsync((_, __) => { });
            onDone?.Invoke(ok, err);
        }

        public IEnumerator DeleteRecordAsync(string recordId, Action<bool, string> onDone)
        {
            bool ok = false;
            string err = "";
            yield return _api.PostJson("/record/delete",
                JsonUtility.ToJson(new DeleteRecordRequest { recordId = recordId }),
                (succ, e2, text) =>
                {
                    if (!succ) { ok = false; err = e2; return; }
                    var resp = JsonUtility.FromJson<SimpleOkResponse>(text ?? "");
                    if (resp == null || !resp.ok) { ok = false; err = resp != null ? resp.error : "删除记录失败"; return; }
                    ok = true;
                });
            if (ok) yield return WarmupAfterLoginAsync((_, __) => { });
            onDone?.Invoke(ok, err);
        }

        public IEnumerator UpsertExperimentAsync(ExperimentModel exp, Action<bool, string> onDone)
        {
            bool ok = false;
            string err = "";
            if (exp == null) { onDone?.Invoke(false, "exp 为空"); yield break; }
            yield return _api.PostJson("/experiment/upsert",
                JsonUtility.ToJson(new UpsertExperimentRequest
                {
                    experimentId = exp.experimentId,
                    experimentName = exp.experimentName,
                    experimentDescription = exp.experimentDescription,
                    experimentImage = exp.experimentImage
                }),
                (succ, e2, text) =>
                {
                    if (!succ) { ok = false; err = e2; return; }
                    var resp = JsonUtility.FromJson<SimpleOkResponse>(text ?? "");
                    if (resp == null || !resp.ok) { ok = false; err = resp != null ? resp.error : "保存实验失败"; return; }
                    ok = true;
                });
            if (ok) yield return WarmupAfterLoginAsync((_, __) => { });
            onDone?.Invoke(ok, err);
        }

        public IEnumerator DeleteExperimentAsync(string experimentId, Action<bool, string> onDone)
        {
            bool ok = false;
            string err = "";
            yield return _api.PostJson("/experiment/delete",
                JsonUtility.ToJson(new DeleteExperimentRequest { experimentId = experimentId }),
                (succ, e2, text) =>
                {
                    if (!succ) { ok = false; err = e2; return; }
                    var resp = JsonUtility.FromJson<SimpleOkResponse>(text ?? "");
                    if (resp == null || !resp.ok) { ok = false; err = resp != null ? resp.error : "删除实验失败"; return; }
                    ok = true;
                });
            if (ok) yield return WarmupAfterLoginAsync((_, __) => { });
            onDone?.Invoke(ok, err);
        }
#endif

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

