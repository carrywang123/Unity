// ============================================================
// 文件名：UserModel.cs
// 功  能：用户数据模型 & 实验记录模型
// 作  者：化工虚拟仿真实验平台
// ============================================================

using System;
using System.Collections.Generic;

namespace ChemLab.Models
{
    /// <summary>
    /// 用户角色枚举
    /// </summary>
    public enum UserRole
    {
        Admin = 0,   // 管理员
        User  = 1    // 普通用户
    }

    /// <summary>
    /// 用户账号模型
    /// </summary>
    [Serializable]
    public class UserModel
    {
        public string userId;           // 用户唯一ID（GUID）
        public string username;         // 用户名
        public string password;         // 密码（MD5加密存储）
        public string realName;         // 真实姓名
        public string email;            // 邮箱
        public UserRole role;           // 角色
        public string createTime;       // 注册时间
        public string lastLoginTime;    // 最后登录时间
        public bool   isActive;         // 账号是否启用

        public UserModel() { }

        public UserModel(string username, string password, string realName,
                         string email, UserRole role = UserRole.User)
        {
            this.userId        = Guid.NewGuid().ToString("N");
            this.username      = username;
            this.password      = password;
            this.realName      = realName;
            this.email         = email;
            this.role          = role;
            this.createTime    = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            this.lastLoginTime = "";
            this.isActive      = true;
        }
    }

    /// <summary>
    /// 实验记录模型
    /// </summary>
    [Serializable]
    public class ExperimentRecord
    {
        public string recordId;        // 记录ID
        public string userId;          // 用户ID
        public string username;        // 用户名（展示用；通过 userId 联表获取，不存 records 表）
        public string experimentName;  // 实验名称
        public string recordTime;      // 记录时间
        public float score;            // 分数

        public ExperimentRecord() { }

        public ExperimentRecord(string userId, string experimentName)
        {
            this.recordId = Guid.NewGuid().ToString("N");
            this.userId = userId;
            this.experimentName = experimentName;
            this.recordTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            this.score = 0f;
        }
    }

    /// <summary>
    /// 用户数据库（序列化容器）
    /// </summary>
    [Serializable]
    public class UserDatabase
    {
        public List<UserModel>       users   = new List<UserModel>();
        public List<ExperimentRecord> records = new List<ExperimentRecord>();
    }
}
