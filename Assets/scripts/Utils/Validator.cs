// ============================================================
// 文件名：Validator.cs
// 功  能：表单验证工具类
// 作  者：化工虚拟仿真实验平台
// ============================================================

using System.Text.RegularExpressions;

namespace ChemLab.Utils
{
    public static class Validator
    {
        /// <summary>验证用户名（3-20位，字母/数字/下划线/中文）</summary>
        public static bool IsValidUsername(string username, out string error)
        {
            error = "";
            if (string.IsNullOrWhiteSpace(username))
            { error = "用户名不能为空"; return false; }

            if (username.Length < 3 || username.Length > 20)
            { error = "用户名长度须在3~20个字符之间"; return false; }

            if (!Regex.IsMatch(username, @"^[a-zA-Z0-9_\u4e00-\u9fa5]+$"))
            { error = "用户名只能包含字母、数字、下划线或中文"; return false; }

            return true;
        }

        /// <summary>验证密码（至少6位）</summary>
        public static bool IsValidPassword(string password, out string error)
        {
            error = "";
            if (string.IsNullOrEmpty(password))
            { error = "密码不能为空"; return false; }

            if (password.Length < 6)
            { error = "密码长度不能少于6位"; return false; }

            if (password.Length > 32)
            { error = "密码长度不能超过32位"; return false; }

            return true;
        }

        /// <summary>验证邮箱格式（可为空）</summary>
        public static bool IsValidEmail(string email, out string error)
        {
            error = "";
            if (string.IsNullOrWhiteSpace(email)) return true; // 邮箱可选

            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            { error = "邮箱格式不正确"; return false; }

            return true;
        }

        /// <summary>验证两次密码是否一致</summary>
        public static bool IsPasswordMatch(string pwd, string confirm, out string error)
        {
            error = "";
            if (pwd != confirm)
            { error = "两次密码输入不一致"; return false; }
            return true;
        }

        /// <summary>验证真实姓名（1-20位，不能为空）</summary>
        public static bool IsValidRealName(string realName, out string error)
        {
            error = "";
            if (string.IsNullOrWhiteSpace(realName))
            { error = "真实姓名不能为空"; return false; }

            if (realName.Length > 20)
            { error = "姓名不能超过20个字符"; return false; }

            return true;
        }
    }
}
