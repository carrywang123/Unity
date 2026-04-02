// ============================================================
// 文件名：DataManager.cs
// 功  能：数据持久化管理
//         - 本地模式：JSON 文件（兼容旧逻辑）
//         - 远端模式：通过本地 Node/Express API 落库到 MySQL(3306)
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
using ChemLab.Networking;

namespace ChemLab.Managers
{
    public class DataManager : MonoBehaviour
    {
        // ── 单例 ──────────────────────────────────────────────
        public static DataManager Instance { get; private set; }

        // ── 常量（仅本地 JSON 模式使用）────────────────────────
        private const string DB_FILE_NAME = "chemlab_db.json";
        private const string ADMIN_USERNAME = "222";
        private const string ADMIN_PASSWORD = "222"; // 原始密码，存储时会MD5

        // ── 数据源设置 ────────────────────────────────────────
        [Header("=== 数据源设置 ===")]
        [Tooltip("启用后，通过本地 Node/Express API 写入 MySQL(3306)。关闭则使用本地 JSON 文件。")]
        public bool useRemoteDatabase = true;

        [Tooltip("API 配置（默认 BaseUrl: http://localhost:3000 ）")]
        public ApiConfig apiConfig;

        // ── 本地模式运行时数据 ────────────────────────────────
        private UserDatabase _db;
        private string _dbFilePath;

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

            _dbFilePath = Path.Combine(Application.persistentDataPath, DB_FILE_NAME);

            if (useRemoteDatabase)
            {
                EnsureApiClient();
                _db = new UserDatabase(); // 仅作为UI缓存容器
                Debug.Log("[DataManager] 已启用远端数据库模式（MySQL via API）。");
            }
            else
            {
                LoadDatabase();
            }
        }

        #endregion

        // ─────────────────────────────────────────────────────
        #region 本地数据库 读/写（JSON）
        // ─────────────────────────────────────────────────────

        /// <summary>从磁盘加载数据库，若不存在则初始化默认数据</summary>
        private void LoadDatabase()
        {
            if (File.Exists(_dbFilePath))
            {
                try
                {
                    string json = File.ReadAllText(_dbFilePath, Encoding.UTF8);
                    _db = JsonUtility.FromJson<UserDatabase>(json);
                    if (_db == null) _db = new UserDatabase();
                    Debug.Log($"[DataManager] 数据库加载成功，共 {_db.users.Count} 个用户。");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[DataManager] 数据库解析失败：{e.Message}，将重建数据库。");
                    InitDefaultDatabase();
                }
            }
            else
            {
                Debug.Log("[DataManager] 未找到数据库文件，初始化默认数据。");
                InitDefaultDatabase();
            }
        }

        /// <summary>将数据库保存到磁盘（远端模式下无效）</summary>
        public void SaveDatabase()
        {
            if (useRemoteDatabase) return;

            try
            {
                string json = JsonUtility.ToJson(_db, true);
                File.WriteAllText(_dbFilePath, json, Encoding.UTF8);
                Debug.Log("[DataManager] 数据库保存成功。");
            }
            catch (Exception e)
            {
                Debug.LogError($"[DataManager] 数据库保存失败：{e.Message}");
            }
        }

        /// <summary>初始化默认数据库（本地 JSON 模式用）</summary>
        private void InitDefaultDatabase()
        {
            _db = new UserDatabase();

            // 内置管理员
            var admin = new UserModel(
                ADMIN_USERNAME,
                MD5Encrypt(ADMIN_PASSWORD),
                "系统管理员",
                "admin@chemlab.com",
                UserRole.Admin
            );
            _db.users.Add(admin);

            // 示例普通用户
            var demo = new UserModel(
                "student01",
                MD5Encrypt("123456"),
                "张三",
                "zhangsan@chemlab.com",
                UserRole.User
            );
            _db.users.Add(demo);

            // 示例实验记录
            var record = new ExperimentRecord(
                demo.userId, demo.username,
                "乙醇蒸馏实验", "蒸馏"
            );
            record.endTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            record.score = 92.5f;
            record.result = "实验成功，乙醇纯度达到95%";
            record.isCompleted = true;
            _db.records.Add(record);

            SaveDatabase();
        }

        #endregion

        // ─────────────────────────────────────────────────────
        #region 用户 CRUD（本地模式：同步；远端模式：请用 *Async）
        // ─────────────────────────────────────────────────────

        public List<UserModel> GetAllUsers()
        {
            return new List<UserModel>(_db.users);
        }

        public UserModel FindUserByUsername(string username)
        {
            return _db.users.Find(u =>
                string.Equals(u.username, username, StringComparison.OrdinalIgnoreCase));
        }

        public UserModel FindUserById(string userId)
        {
            return _db.users.Find(u => u.userId == userId);
        }

        public bool RegisterUser(string username, string rawPassword,
                                 string realName, string email,
                                 out string errorMsg)
        {
            errorMsg = "";

            if (useRemoteDatabase)
            {
                errorMsg = "远端数据库模式下请使用 RegisterUserAsync";
                return false;
            }

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
            _db.users.Add(newUser);
            SaveDatabase();

            Debug.Log($"[DataManager] 新用户注册成功：{username}");
            return true;
        }

        public bool AdminAddUser(string username, string rawPassword,
                                 string realName, string email,
                                 UserRole role, out string errorMsg)
        {
            errorMsg = "";
            if (useRemoteDatabase)
            {
                errorMsg = "远端数据库模式下请使用管理员接口（暂未在 Unity 同步实现）";
                return false;
            }

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
            _db.users.Add(newUser);
            SaveDatabase();
            return true;
        }

        public bool DeleteUser(string userId, out string errorMsg)
        {
            errorMsg = "";
            if (useRemoteDatabase)
            {
                errorMsg = "远端数据库模式下请使用管理员接口（暂未在 Unity 同步实现）";
                return false;
            }

            var user = FindUserById(userId);
            if (user == null)
            { errorMsg = "用户不存在！"; return false; }

            if (CurrentUser != null && CurrentUser.userId == userId)
            { errorMsg = "不能删除当前登录的账号！"; return false; }

            _db.users.Remove(user);
            _db.records.RemoveAll(r => r.userId == userId);
            SaveDatabase();
            return true;
        }

        public bool ToggleUserActive(string userId, out string errorMsg)
        {
            errorMsg = "";
            if (useRemoteDatabase)
            {
                errorMsg = "远端数据库模式下请使用管理员接口（暂未在 Unity 同步实现）";
                return false;
            }

            var user = FindUserById(userId);
            if (user == null)
            { errorMsg = "用户不存在！"; return false; }

            if (user.role == UserRole.Admin)
            { errorMsg = "不能禁用管理员账号！"; return false; }

            user.isActive = !user.isActive;
            SaveDatabase();
            return true;
        }

        public bool ResetPassword(string userId, string newRawPassword, out string errorMsg)
        {
            errorMsg = "";
            if (useRemoteDatabase)
            {
                errorMsg = "远端数据库模式下请使用 /api/user/password 或 /api/admin/users/:id/password";
                return false;
            }

            if (string.IsNullOrWhiteSpace(newRawPassword) || newRawPassword.Length < 6)
            { errorMsg = "新密码长度不能少于 6 位！"; return false; }

            var user = FindUserById(userId);
            if (user == null)
            { errorMsg = "用户不存在！"; return false; }

            user.password = MD5Encrypt(newRawPassword);
            SaveDatabase();
            return true;
        }

        #endregion

        // ─────────────────────────────────────────────────────
        #region 认证（本地模式：同步；远端模式：请用 LoginAsync/LogoutAsync）
        // ─────────────────────────────────────────────────────

        public bool Login(string username, string rawPassword, out string errorMsg)
        {
            errorMsg = "";

            if (useRemoteDatabase)
            {
                errorMsg = "远端数据库模式下请使用 LoginAsync";
                return false;
            }

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
            SaveDatabase();

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
        #region 实验记录 CRUD（本地模式：同步；远端模式：请用后端接口）
        // ─────────────────────────────────────────────────────

        public List<ExperimentRecord> GetAllRecords()
        {
            return new List<ExperimentRecord>(_db.records);
        }

        public List<ExperimentRecord> GetRecordsByUser(string userId)
        {
            return _db.records.FindAll(r => r.userId == userId);
        }

        public void AddRecord(ExperimentRecord record)
        {
            if (useRemoteDatabase) return;
            _db.records.Add(record);
            SaveDatabase();
        }

        public bool DeleteRecord(string recordId, out string errorMsg)
        {
            errorMsg = "";
            if (useRemoteDatabase)
            {
                errorMsg = "远端数据库模式下请使用管理员接口 /api/admin/records（暂未在 Unity 同步实现）";
                return false;
            }

            var record = _db.records.Find(r => r.recordId == recordId);
            if (record == null)
            { errorMsg = "记录不存在！"; return false; }

            _db.records.Remove(record);
            SaveDatabase();
            return true;
        }

        public void CompleteRecord(string recordId, float score, string result)
        {
            if (useRemoteDatabase) return;
            var record = _db.records.Find(r => r.recordId == recordId);
            if (record == null) return;

            record.endTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            record.score = score;
            record.result = result;
            record.isCompleted = true;
            SaveDatabase();
        }

        #endregion

        // ─────────────────────────────────────────────────────
        #region 远端 API（MySQL via Node/Express）
        // ─────────────────────────────────────────────────────

        private void EnsureApiClient()
        {
            if (ApiClient.Instance == null)
            {
                var go = new GameObject("ApiClient");
                go.AddComponent<ApiClient>();
            }
            if (apiConfig != null)
                ApiClient.Instance.SetConfig(apiConfig);
        }

        [Serializable] private class ApiResponse<T>
        {
            public int code;
            public string msg;
            public T data;
        }

        [Serializable] private class AuthUserData
        {
            public int id;
            public string username;
            public string role;
            public string avatar;
            public string real_name;
        }

        public IEnumerator LoginAsync(string username, string rawPassword, bool isAdmin, Action<bool, string> onDone)
        {
            if (!useRemoteDatabase)
            {
                bool ok = Login(username, rawPassword, out string err);
                onDone?.Invoke(ok, err);
                yield break;
            }

            EnsureApiClient();
            string body = JsonUtility.ToJson(new LoginBody
            {
                username = username,
                password = rawPassword,
                role = isAdmin ? "admin" : "user"
            });

            bool finished = false;
            bool success = false;
            string errMsg = "";

            yield return ApiClient.Instance.PostJson("/api/auth/login", body, (code, text) =>
            {
                try
                {
                    var resp = JsonUtility.FromJson<ApiResponse<AuthUserData>>(text);
                    if (resp == null) { errMsg = "响应解析失败"; return; }
                    if (resp.code != 200) { errMsg = string.IsNullOrEmpty(resp.msg) ? "登录失败" : resp.msg; return; }

                    var u = resp.data;
                    var userModel = new UserModel
                    {
                        userId = u.id.ToString(),
                        username = u.username,
                        password = "", // 远端模式不保存密码hash
                        realName = string.IsNullOrEmpty(u.real_name) ? u.username : u.real_name,
                        email = "",
                        role = (u.role == "admin") ? UserRole.Admin : UserRole.User,
                        createTime = "",
                        lastLoginTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        isActive = true
                    };
                    CurrentUser = userModel;
                    success = true;
                }
                catch (Exception e)
                {
                    errMsg = "登录响应解析异常：" + e.Message;
                }
                finally { finished = true; }
            });

            while (!finished) yield return null;
            onDone?.Invoke(success, errMsg);
        }

        public IEnumerator RegisterUserAsync(string username, string rawPassword, string realName, string email, Action<bool, string> onDone)
        {
            if (!useRemoteDatabase)
            {
                bool ok = RegisterUser(username, rawPassword, realName, email, out string err);
                onDone?.Invoke(ok, err);
                yield break;
            }

            EnsureApiClient();
            string body = JsonUtility.ToJson(new RegisterBody
            {
                username = username,
                password = rawPassword,
                real_name = realName,
                student_id = "",
                email = email
            });

            bool finished = false;
            bool success = false;
            string errMsg = "";

            yield return ApiClient.Instance.PostJson("/api/auth/register", body, (code, text) =>
            {
                try
                {
                    var resp = JsonUtility.FromJson<ApiResponse<object>>(text);
                    if (resp == null) { errMsg = "响应解析失败"; return; }
                    if (resp.code != 200) { errMsg = string.IsNullOrEmpty(resp.msg) ? "注册失败" : resp.msg; return; }
                    success = true;
                }
                catch (Exception e)
                {
                    errMsg = "注册响应解析异常：" + e.Message;
                }
                finally { finished = true; }
            });

            while (!finished) yield return null;
            onDone?.Invoke(success, errMsg);
        }

        public IEnumerator LogoutAsync(Action<bool, string> onDone)
        {
            if (!useRemoteDatabase)
            {
                Logout();
                onDone?.Invoke(true, "");
                yield break;
            }

            EnsureApiClient();
            bool finished = false;
            bool success = false;
            string errMsg = "";

            yield return ApiClient.Instance.PostJson("/api/auth/logout", "{}", (code, text) =>
            {
                try
                {
                    var resp = JsonUtility.FromJson<ApiResponse<object>>(text);
                    if (resp == null) { errMsg = "响应解析失败"; return; }
                    if (resp.code != 200) { errMsg = string.IsNullOrEmpty(resp.msg) ? "登出失败" : resp.msg; return; }
                    success = true;
                    CurrentUser = null;
                }
                catch (Exception e)
                {
                    errMsg = "登出响应解析异常：" + e.Message;
                }
                finally { finished = true; }
            });

            while (!finished) yield return null;
            onDone?.Invoke(success, errMsg);
        }

        [Serializable] private class LoginBody
        {
            public string username;
            public string password;
            public string role;
        }

        [Serializable] private class RegisterBody
        {
            public string username;
            public string password;
            public string real_name;
            public string student_id;
            public string email;
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

        public string GetDatabasePath() => _dbFilePath;

        #endregion
    }
}

