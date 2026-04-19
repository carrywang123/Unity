#include <cstdlib>
#include <cstring>
#include <iostream>
#include <optional>
#include <random>
#include <string>

#include <mysql/mysql.h>
#include <json/json.h>
#include <openssl/md5.h>

#include "../third_party/httplib.h"

static std::string md5_hex_lower(const std::string& s) {
    unsigned char digest[MD5_DIGEST_LENGTH];
    MD5(reinterpret_cast<const unsigned char*>(s.data()), s.size(), digest);
    static const char* hex = "0123456789abcdef";
    std::string out;
    out.resize(MD5_DIGEST_LENGTH * 2);
    for (int i = 0; i < MD5_DIGEST_LENGTH; i++) {
        out[i * 2 + 0] = hex[(digest[i] >> 4) & 0xF];
        out[i * 2 + 1] = hex[digest[i] & 0xF];
    }
    return out;
}

static std::string random_hex_32() {
    static const char* hex = "0123456789abcdef";
    std::random_device rd;
    std::mt19937 gen(rd());
    std::uniform_int_distribution<int> dis(0, 15);
    std::string out;
    out.resize(32);
    for (int i = 0; i < 32; i++) out[i] = hex[dis(gen)];
    return out;
}

struct DbConfig {
    std::string host = "127.0.0.1";
    unsigned int port = 3306;
    std::string database = "chemlab";
    std::string user = "root";
    std::string password = "Cloud2023@";
};

static const char* env_or_null(const char* key) {
    const char* v = std::getenv(key);
    return (v && *v) ? v : nullptr;
}

static DbConfig load_config_from_env() {
    DbConfig cfg;
    if (auto v = env_or_null("CHEMLAB_DB_HOST")) cfg.host = v;
    if (auto v = env_or_null("CHEMLAB_DB_PORT")) cfg.port = static_cast<unsigned int>(std::strtoul(v, nullptr, 10));
    if (auto v = env_or_null("CHEMLAB_DB_NAME")) cfg.database = v;
    if (auto v = env_or_null("CHEMLAB_DB_USER")) cfg.user = v;
    if (auto v = env_or_null("CHEMLAB_DB_PASSWORD")) cfg.password = v;
    return cfg;
}

class MySqlConn {
public:
    explicit MySqlConn(const DbConfig& cfg) : cfg_(cfg) {
        conn_ = mysql_init(nullptr);
        if (!conn_) throw std::runtime_error("mysql_init failed");

        unsigned int timeout = 5;
        mysql_options(conn_, MYSQL_OPT_CONNECT_TIMEOUT, &timeout);
        mysql_options(conn_, MYSQL_OPT_READ_TIMEOUT, &timeout);
        mysql_options(conn_, MYSQL_OPT_WRITE_TIMEOUT, &timeout);

        if (!mysql_real_connect(
                conn_,
                cfg_.host.c_str(),
                cfg_.user.c_str(),
                cfg_.password.c_str(),
                cfg_.database.c_str(),
                cfg_.port,
                nullptr,
                0)) {
            std::string err = mysql_error(conn_);
            mysql_close(conn_);
            conn_ = nullptr;
            throw std::runtime_error("mysql_real_connect failed: " + err);
        }

        mysql_set_character_set(conn_, "utf8mb4");
    }

    ~MySqlConn() {
        if (conn_) mysql_close(conn_);
    }

    MySqlConn(const MySqlConn&) = delete;
    MySqlConn& operator=(const MySqlConn&) = delete;

    bool ping() {
        return mysql_ping(conn_) == 0;
    }

    Json::Value get_experiments() {
        const char* sql = "SELECT experiment_id, experiment_name, experiment_description, experiment_image FROM experiments;";
        if (mysql_real_query(conn_, sql, std::strlen(sql)) != 0) {
            throw std::runtime_error(std::string("query experiments failed: ") + mysql_error(conn_));
        }
        MYSQL_RES* res = mysql_store_result(conn_);
        if (!res) throw std::runtime_error(std::string("store_result failed: ") + mysql_error(conn_));

        Json::Value arr(Json::arrayValue);
        MYSQL_ROW row;
        while ((row = mysql_fetch_row(res)) != nullptr) {
            unsigned long* lengths = mysql_fetch_lengths(res);
            Json::Value e(Json::objectValue);
            e["experimentId"] = std::string(row[0] ? row[0] : "", lengths ? lengths[0] : 0);
            e["experimentName"] = std::string(row[1] ? row[1] : "", lengths ? lengths[1] : 0);
            e["experimentDescription"] = std::string(row[2] ? row[2] : "", lengths ? lengths[2] : 0);
            e["experimentImage"] = std::string(row[3] ? row[3] : "", lengths ? lengths[3] : 0);
            arr.append(e);
        }
        mysql_free_result(res);
        return arr;
    }

    Json::Value get_users() {
        const char* sql =
            "SELECT user_id, username, password, real_name, email, role, create_time, last_login_time, is_active "
            "FROM users;";
        if (mysql_real_query(conn_, sql, std::strlen(sql)) != 0) {
            throw std::runtime_error(std::string("query users failed: ") + mysql_error(conn_));
        }
        MYSQL_RES* res = mysql_store_result(conn_);
        if (!res) throw std::runtime_error(std::string("store_result failed: ") + mysql_error(conn_));

        Json::Value arr(Json::arrayValue);
        MYSQL_ROW row;
        while ((row = mysql_fetch_row(res)) != nullptr) {
            unsigned long* lengths = mysql_fetch_lengths(res);
            Json::Value u(Json::objectValue);
            u["userId"] = std::string(row[0] ? row[0] : "", lengths ? lengths[0] : 0);
            u["username"] = std::string(row[1] ? row[1] : "", lengths ? lengths[1] : 0);
            u["password"] = std::string(row[2] ? row[2] : "", lengths ? lengths[2] : 0);
            u["realName"] = std::string(row[3] ? row[3] : "", lengths ? lengths[3] : 0);
            u["email"] = std::string(row[4] ? row[4] : "", lengths ? lengths[4] : 0);
            u["role"] = row[5] ? std::atoi(row[5]) : 1;
            u["createTime"] = std::string(row[6] ? row[6] : "", lengths ? lengths[6] : 0);
            u["lastLoginTime"] = std::string(row[7] ? row[7] : "", lengths ? lengths[7] : 0);
            u["isActive"] = (row[8] ? std::atoi(row[8]) : 1) != 0;
            arr.append(u);
        }
        mysql_free_result(res);
        return arr;
    }

    Json::Value get_records_all() {
        const char* sql =
            "SELECT r.record_id, r.user_id, u.username AS username, r.experiment_name, r.record_time, r.score "
            "FROM records r "
            "LEFT JOIN users u ON u.user_id = r.user_id;";
        if (mysql_real_query(conn_, sql, std::strlen(sql)) != 0) {
            throw std::runtime_error(std::string("query records failed: ") + mysql_error(conn_));
        }
        MYSQL_RES* res = mysql_store_result(conn_);
        if (!res) throw std::runtime_error(std::string("store_result failed: ") + mysql_error(conn_));

        Json::Value arr(Json::arrayValue);
        MYSQL_ROW row;
        while ((row = mysql_fetch_row(res)) != nullptr) {
            unsigned long* lengths = mysql_fetch_lengths(res);
            Json::Value r(Json::objectValue);
            r["recordId"] = std::string(row[0] ? row[0] : "", lengths ? lengths[0] : 0);
            r["userId"] = std::string(row[1] ? row[1] : "", lengths ? lengths[1] : 0);
            r["username"] = std::string(row[2] ? row[2] : "", lengths ? lengths[2] : 0);
            r["experimentName"] = std::string(row[3] ? row[3] : "", lengths ? lengths[3] : 0);
            r["recordTime"] = std::string(row[4] ? row[4] : "", lengths ? lengths[4] : 0);
            r["score"] = row[5] ? std::atof(row[5]) : 0.0;
            arr.append(r);
        }
        mysql_free_result(res);
        return arr;
    }

    Json::Value get_records_by_user(const std::string& user_id) {
        MYSQL_STMT* stmt = mysql_stmt_init(conn_);
        if (!stmt) throw std::runtime_error("mysql_stmt_init failed");

        const char* sql =
            "SELECT r.record_id, r.user_id, u.username AS username, r.experiment_name, r.record_time, r.score "
            "FROM records r "
            "LEFT JOIN users u ON u.user_id = r.user_id "
            "WHERE r.user_id=?;";
        if (mysql_stmt_prepare(stmt, sql, std::strlen(sql)) != 0) {
            std::string err = mysql_stmt_error(stmt);
            mysql_stmt_close(stmt);
            throw std::runtime_error("mysql_stmt_prepare failed: " + err);
        }

        MYSQL_BIND bind[1];
        std::memset(bind, 0, sizeof(bind));
        unsigned long uid_len = static_cast<unsigned long>(user_id.size());
        bind[0].buffer_type = MYSQL_TYPE_STRING;
        bind[0].buffer = const_cast<char*>(user_id.data());
        bind[0].buffer_length = uid_len;
        bind[0].length = &uid_len;
        if (mysql_stmt_bind_param(stmt, bind) != 0) {
            std::string err = mysql_stmt_error(stmt);
            mysql_stmt_close(stmt);
            throw std::runtime_error("mysql_stmt_bind_param failed: " + err);
        }

        if (mysql_stmt_execute(stmt) != 0) {
            std::string err = mysql_stmt_error(stmt);
            mysql_stmt_close(stmt);
            throw std::runtime_error("mysql_stmt_execute failed: " + err);
        }

        char record_id_buf[64] = {0};
        unsigned long record_id_len = 0;
        char user_id_buf[64] = {0};
        unsigned long user_id_len = 0;
        char username_buf[128] = {0};
        unsigned long username_len = 0;
        char exp_name_buf[256] = {0};
        unsigned long exp_name_len = 0;
        char record_time_buf[64] = {0};
        unsigned long record_time_len = 0;
        double score = 0.0;

        MYSQL_BIND out[6];
        std::memset(out, 0, sizeof(out));
        out[0].buffer_type = MYSQL_TYPE_STRING;
        out[0].buffer = record_id_buf;
        out[0].buffer_length = sizeof(record_id_buf);
        out[0].length = &record_id_len;

        out[1].buffer_type = MYSQL_TYPE_STRING;
        out[1].buffer = user_id_buf;
        out[1].buffer_length = sizeof(user_id_buf);
        out[1].length = &user_id_len;

        out[2].buffer_type = MYSQL_TYPE_STRING;
        out[2].buffer = username_buf;
        out[2].buffer_length = sizeof(username_buf);
        out[2].length = &username_len;

        out[3].buffer_type = MYSQL_TYPE_STRING;
        out[3].buffer = exp_name_buf;
        out[3].buffer_length = sizeof(exp_name_buf);
        out[3].length = &exp_name_len;

        out[4].buffer_type = MYSQL_TYPE_STRING;
        out[4].buffer = record_time_buf;
        out[4].buffer_length = sizeof(record_time_buf);
        out[4].length = &record_time_len;

        out[5].buffer_type = MYSQL_TYPE_DOUBLE;
        out[5].buffer = &score;

        if (mysql_stmt_bind_result(stmt, out) != 0) {
            std::string err = mysql_stmt_error(stmt);
            mysql_stmt_close(stmt);
            throw std::runtime_error("mysql_stmt_bind_result failed: " + err);
        }
        if (mysql_stmt_store_result(stmt) != 0) {
            std::string err = mysql_stmt_error(stmt);
            mysql_stmt_close(stmt);
            throw std::runtime_error("mysql_stmt_store_result failed: " + err);
        }

        Json::Value arr(Json::arrayValue);
        while (true) {
            int rc = mysql_stmt_fetch(stmt);
            if (rc == MYSQL_NO_DATA) break;
            if (rc != 0) break;
            Json::Value r(Json::objectValue);
            r["recordId"] = std::string(record_id_buf, record_id_len);
            r["userId"] = std::string(user_id_buf, user_id_len);
            r["username"] = std::string(username_buf, username_len);
            r["experimentName"] = std::string(exp_name_buf, exp_name_len);
            r["recordTime"] = std::string(record_time_buf, record_time_len);
            r["score"] = score;
            arr.append(r);
        }

        mysql_stmt_free_result(stmt);
        mysql_stmt_close(stmt);
        return arr;
    }

    // Returns: {user_id, role, real_name} if ok, nullopt if not found/invalid
    std::optional<Json::Value> login(const std::string& username, const std::string& raw_password) {
        std::string pwd_md5 = md5_hex_lower(raw_password);

        MYSQL_STMT* stmt = mysql_stmt_init(conn_);
        if (!stmt) throw std::runtime_error("mysql_stmt_init failed");

        const char* sql =
            "SELECT user_id, username, password, real_name, email, role, create_time, last_login_time, is_active "
            "FROM users "
            "WHERE LOWER(username)=LOWER(?) AND password=? "
            "LIMIT 1;";

        if (mysql_stmt_prepare(stmt, sql, std::strlen(sql)) != 0) {
            std::string err = mysql_stmt_error(stmt);
            mysql_stmt_close(stmt);
            throw std::runtime_error("mysql_stmt_prepare failed: " + err);
        }

        MYSQL_BIND bind[2];
        std::memset(bind, 0, sizeof(bind));

        unsigned long username_len = static_cast<unsigned long>(username.size());
        unsigned long pwd_len = static_cast<unsigned long>(pwd_md5.size());

        bind[0].buffer_type = MYSQL_TYPE_STRING;
        bind[0].buffer = const_cast<char*>(username.data());
        bind[0].buffer_length = username_len;
        bind[0].length = &username_len;

        bind[1].buffer_type = MYSQL_TYPE_STRING;
        bind[1].buffer = const_cast<char*>(pwd_md5.data());
        bind[1].buffer_length = pwd_len;
        bind[1].length = &pwd_len;

        if (mysql_stmt_bind_param(stmt, bind) != 0) {
            std::string err = mysql_stmt_error(stmt);
            mysql_stmt_close(stmt);
            throw std::runtime_error("mysql_stmt_bind_param failed: " + err);
        }

        if (mysql_stmt_execute(stmt) != 0) {
            std::string err = mysql_stmt_error(stmt);
            mysql_stmt_close(stmt);
            throw std::runtime_error("mysql_stmt_execute failed: " + err);
        }

        char user_id_buf[64] = {0};
        unsigned long user_id_len = 0;
        char username_buf2[128] = {0};
        unsigned long username_len2 = 0;
        char password_buf2[64] = {0};
        unsigned long password_len2 = 0;
        char real_name_buf[256] = {0};
        unsigned long real_name_len = 0;
        char email_buf[256] = {0};
        unsigned long email_len = 0;
        int role = 1;
        char create_time_buf[64] = {0};
        unsigned long create_time_len = 0;
        char last_login_buf[64] = {0};
        unsigned long last_login_len = 0;
        int is_active = 1;

        MYSQL_BIND out[9];
        std::memset(out, 0, sizeof(out));

        out[0].buffer_type = MYSQL_TYPE_STRING;
        out[0].buffer = user_id_buf;
        out[0].buffer_length = sizeof(user_id_buf);
        out[0].length = &user_id_len;

        out[1].buffer_type = MYSQL_TYPE_STRING;
        out[1].buffer = username_buf2;
        out[1].buffer_length = sizeof(username_buf2);
        out[1].length = &username_len2;

        out[2].buffer_type = MYSQL_TYPE_STRING;
        out[2].buffer = password_buf2;
        out[2].buffer_length = sizeof(password_buf2);
        out[2].length = &password_len2;

        out[3].buffer_type = MYSQL_TYPE_STRING;
        out[3].buffer = real_name_buf;
        out[3].buffer_length = sizeof(real_name_buf);
        out[3].length = &real_name_len;

        out[4].buffer_type = MYSQL_TYPE_STRING;
        out[4].buffer = email_buf;
        out[4].buffer_length = sizeof(email_buf);
        out[4].length = &email_len;

        out[5].buffer_type = MYSQL_TYPE_LONG;
        out[5].buffer = &role;

        out[6].buffer_type = MYSQL_TYPE_STRING;
        out[6].buffer = create_time_buf;
        out[6].buffer_length = sizeof(create_time_buf);
        out[6].length = &create_time_len;

        out[7].buffer_type = MYSQL_TYPE_STRING;
        out[7].buffer = last_login_buf;
        out[7].buffer_length = sizeof(last_login_buf);
        out[7].length = &last_login_len;

        out[8].buffer_type = MYSQL_TYPE_LONG;
        out[8].buffer = &is_active;

        if (mysql_stmt_bind_result(stmt, out) != 0) {
            std::string err = mysql_stmt_error(stmt);
            mysql_stmt_close(stmt);
            throw std::runtime_error("mysql_stmt_bind_result failed: " + err);
        }

        if (mysql_stmt_store_result(stmt) != 0) {
            std::string err = mysql_stmt_error(stmt);
            mysql_stmt_close(stmt);
            throw std::runtime_error("mysql_stmt_store_result failed: " + err);
        }

        int fetch_rc = mysql_stmt_fetch(stmt);
        mysql_stmt_free_result(stmt);
        mysql_stmt_close(stmt);

        if (fetch_rc != 0) return std::nullopt;
        if (is_active == 0) return std::nullopt;

        // best-effort update last_login_time
        try {
            const char* usql = "UPDATE users SET last_login_time=DATE_FORMAT(NOW(), '%Y-%m-%d %H:%i:%s') WHERE user_id=?;";
            MYSQL_STMT* ust = mysql_stmt_init(conn_);
            if (ust) {
                if (mysql_stmt_prepare(ust, usql, std::strlen(usql)) == 0) {
                    MYSQL_BIND ub[1];
                    std::memset(ub, 0, sizeof(ub));
                    ub[0].buffer_type = MYSQL_TYPE_STRING;
                    ub[0].buffer = user_id_buf;
                    ub[0].buffer_length = user_id_len;
                    ub[0].length = &user_id_len;
                    mysql_stmt_bind_param(ust, ub);
                    mysql_stmt_execute(ust);
                }
                mysql_stmt_close(ust);
            }
        } catch (...) {
            // ignore
        }

        Json::Value user(Json::objectValue);
        user["userId"] = std::string(user_id_buf, user_id_len);
        user["username"] = std::string(username_buf2, username_len2);
        user["password"] = std::string(password_buf2, password_len2);
        user["role"] = role;
        user["realName"] = std::string(real_name_buf, real_name_len);
        user["email"] = std::string(email_buf, email_len);
        user["createTime"] = std::string(create_time_buf, create_time_len);
        user["lastLoginTime"] = std::string(last_login_buf, last_login_len);
        user["isActive"] = true;
        return user;
    }

    void register_user(const std::string& username,
                       const std::string& raw_password,
                       const std::string& real_name,
                       const std::string& email) {
        const std::string user_id = random_hex_32();
        const std::string pwd_md5 = md5_hex_lower(raw_password);

        MYSQL_STMT* stmt = mysql_stmt_init(conn_);
        if (!stmt) throw std::runtime_error("mysql_stmt_init failed");

        const char* sql =
            "INSERT INTO users (user_id, username, password, real_name, email, role, create_time, last_login_time, is_active) "
            "VALUES (?, ?, ?, ?, ?, 1, DATE_FORMAT(NOW(), '%Y-%m-%d %H:%i:%s'), '', 1);";
        if (mysql_stmt_prepare(stmt, sql, std::strlen(sql)) != 0) {
            std::string err = mysql_stmt_error(stmt);
            mysql_stmt_close(stmt);
            throw std::runtime_error("mysql_stmt_prepare failed: " + err);
        }

        MYSQL_BIND b[5];
        std::memset(b, 0, sizeof(b));
        unsigned long l0 = static_cast<unsigned long>(user_id.size());
        unsigned long l1 = static_cast<unsigned long>(username.size());
        unsigned long l2 = static_cast<unsigned long>(pwd_md5.size());
        unsigned long l3 = static_cast<unsigned long>(real_name.size());
        unsigned long l4 = static_cast<unsigned long>(email.size());

        b[0].buffer_type = MYSQL_TYPE_STRING; b[0].buffer = const_cast<char*>(user_id.data()); b[0].buffer_length = l0; b[0].length = &l0;
        b[1].buffer_type = MYSQL_TYPE_STRING; b[1].buffer = const_cast<char*>(username.data()); b[1].buffer_length = l1; b[1].length = &l1;
        b[2].buffer_type = MYSQL_TYPE_STRING; b[2].buffer = const_cast<char*>(pwd_md5.data()); b[2].buffer_length = l2; b[2].length = &l2;
        b[3].buffer_type = MYSQL_TYPE_STRING; b[3].buffer = const_cast<char*>(real_name.data()); b[3].buffer_length = l3; b[3].length = &l3;
        b[4].buffer_type = MYSQL_TYPE_STRING; b[4].buffer = const_cast<char*>(email.data()); b[4].buffer_length = l4; b[4].length = &l4;

        if (mysql_stmt_bind_param(stmt, b) != 0) {
            std::string err = mysql_stmt_error(stmt);
            mysql_stmt_close(stmt);
            throw std::runtime_error("mysql_stmt_bind_param failed: " + err);
        }
        if (mysql_stmt_execute(stmt) != 0) {
            std::string err = mysql_stmt_error(stmt);
            mysql_stmt_close(stmt);
            throw std::runtime_error("mysql_stmt_execute failed: " + err);
        }
        mysql_stmt_close(stmt);
    }

    void admin_add_user(const std::string& username,
                        const std::string& raw_password,
                        const std::string& real_name,
                        const std::string& email,
                        int role) {
        const std::string user_id = random_hex_32();
        const std::string pwd_md5 = md5_hex_lower(raw_password);

        MYSQL_STMT* stmt = mysql_stmt_init(conn_);
        if (!stmt) throw std::runtime_error("mysql_stmt_init failed");

        const char* sql =
            "INSERT INTO users (user_id, username, password, real_name, email, role, create_time, last_login_time, is_active) "
            "VALUES (?, ?, ?, ?, ?, ?, DATE_FORMAT(NOW(), '%Y-%m-%d %H:%i:%s'), '', 1);";
        if (mysql_stmt_prepare(stmt, sql, std::strlen(sql)) != 0) {
            std::string err = mysql_stmt_error(stmt);
            mysql_stmt_close(stmt);
            throw std::runtime_error("mysql_stmt_prepare failed: " + err);
        }

        MYSQL_BIND b[6];
        std::memset(b, 0, sizeof(b));
        unsigned long l0 = static_cast<unsigned long>(user_id.size());
        unsigned long l1 = static_cast<unsigned long>(username.size());
        unsigned long l2 = static_cast<unsigned long>(pwd_md5.size());
        unsigned long l3 = static_cast<unsigned long>(real_name.size());
        unsigned long l4 = static_cast<unsigned long>(email.size());
        int role_i = role;

        b[0].buffer_type = MYSQL_TYPE_STRING; b[0].buffer = const_cast<char*>(user_id.data()); b[0].buffer_length = l0; b[0].length = &l0;
        b[1].buffer_type = MYSQL_TYPE_STRING; b[1].buffer = const_cast<char*>(username.data()); b[1].buffer_length = l1; b[1].length = &l1;
        b[2].buffer_type = MYSQL_TYPE_STRING; b[2].buffer = const_cast<char*>(pwd_md5.data()); b[2].buffer_length = l2; b[2].length = &l2;
        b[3].buffer_type = MYSQL_TYPE_STRING; b[3].buffer = const_cast<char*>(real_name.data()); b[3].buffer_length = l3; b[3].length = &l3;
        b[4].buffer_type = MYSQL_TYPE_STRING; b[4].buffer = const_cast<char*>(email.data()); b[4].buffer_length = l4; b[4].length = &l4;
        b[5].buffer_type = MYSQL_TYPE_LONG;   b[5].buffer = &role_i;

        if (mysql_stmt_bind_param(stmt, b) != 0) {
            std::string err = mysql_stmt_error(stmt);
            mysql_stmt_close(stmt);
            throw std::runtime_error("mysql_stmt_bind_param failed: " + err);
        }
        if (mysql_stmt_execute(stmt) != 0) {
            std::string err = mysql_stmt_error(stmt);
            mysql_stmt_close(stmt);
            throw std::runtime_error("mysql_stmt_execute failed: " + err);
        }
        mysql_stmt_close(stmt);
    }

    void admin_update_user(const std::string& user_id,
                           const std::string& real_name,
                           const std::string& email,
                           int role,
                           const std::string& new_raw_password) {
        const bool update_pwd = !new_raw_password.empty();
        const std::string pwd_md5 = update_pwd ? md5_hex_lower(new_raw_password) : "";

        const char* sql_no_pwd = "UPDATE users SET real_name=?, email=?, role=? WHERE user_id=?;";
        const char* sql_with_pwd = "UPDATE users SET real_name=?, email=?, role=?, password=? WHERE user_id=?;";

        MYSQL_STMT* stmt = mysql_stmt_init(conn_);
        if (!stmt) throw std::runtime_error("mysql_stmt_init failed");
        const char* sql = update_pwd ? sql_with_pwd : sql_no_pwd;
        if (mysql_stmt_prepare(stmt, sql, std::strlen(sql)) != 0) {
            std::string err = mysql_stmt_error(stmt);
            mysql_stmt_close(stmt);
            throw std::runtime_error("mysql_stmt_prepare failed: " + err);
        }

        MYSQL_BIND b[5];
        std::memset(b, 0, sizeof(b));
        unsigned long l0 = static_cast<unsigned long>(real_name.size());
        unsigned long l1 = static_cast<unsigned long>(email.size());
        int role_i = role;
        unsigned long l3 = static_cast<unsigned long>(user_id.size());
        unsigned long lp = static_cast<unsigned long>(pwd_md5.size());

        b[0].buffer_type = MYSQL_TYPE_STRING; b[0].buffer = const_cast<char*>(real_name.data()); b[0].buffer_length = l0; b[0].length = &l0;
        b[1].buffer_type = MYSQL_TYPE_STRING; b[1].buffer = const_cast<char*>(email.data()); b[1].buffer_length = l1; b[1].length = &l1;
        b[2].buffer_type = MYSQL_TYPE_LONG;   b[2].buffer = &role_i;
        if (update_pwd) {
            b[3].buffer_type = MYSQL_TYPE_STRING; b[3].buffer = const_cast<char*>(pwd_md5.data()); b[3].buffer_length = lp; b[3].length = &lp;
            b[4].buffer_type = MYSQL_TYPE_STRING; b[4].buffer = const_cast<char*>(user_id.data()); b[4].buffer_length = l3; b[4].length = &l3;
        } else {
            b[3].buffer_type = MYSQL_TYPE_STRING; b[3].buffer = const_cast<char*>(user_id.data()); b[3].buffer_length = l3; b[3].length = &l3;
        }

        if (mysql_stmt_bind_param(stmt, b) != 0) {
            std::string err = mysql_stmt_error(stmt);
            mysql_stmt_close(stmt);
            throw std::runtime_error("mysql_stmt_bind_param failed: " + err);
        }
        if (mysql_stmt_execute(stmt) != 0) {
            std::string err = mysql_stmt_error(stmt);
            mysql_stmt_close(stmt);
            throw std::runtime_error("mysql_stmt_execute failed: " + err);
        }
        mysql_stmt_close(stmt);
    }

    void reset_password(const std::string& user_id, const std::string& new_raw_password) {
        const std::string pwd_md5 = md5_hex_lower(new_raw_password);
        MYSQL_STMT* stmt = mysql_stmt_init(conn_);
        if (!stmt) throw std::runtime_error("mysql_stmt_init failed");
        const char* sql = "UPDATE users SET password=? WHERE user_id=?;";
        if (mysql_stmt_prepare(stmt, sql, std::strlen(sql)) != 0) {
            std::string err = mysql_stmt_error(stmt);
            mysql_stmt_close(stmt);
            throw std::runtime_error("mysql_stmt_prepare failed: " + err);
        }
        MYSQL_BIND b[2];
        std::memset(b, 0, sizeof(b));
        unsigned long l0 = static_cast<unsigned long>(pwd_md5.size());
        unsigned long l1 = static_cast<unsigned long>(user_id.size());
        b[0].buffer_type = MYSQL_TYPE_STRING; b[0].buffer = const_cast<char*>(pwd_md5.data()); b[0].buffer_length = l0; b[0].length = &l0;
        b[1].buffer_type = MYSQL_TYPE_STRING; b[1].buffer = const_cast<char*>(user_id.data()); b[1].buffer_length = l1; b[1].length = &l1;
        mysql_stmt_bind_param(stmt, b);
        if (mysql_stmt_execute(stmt) != 0) {
            std::string err = mysql_stmt_error(stmt);
            mysql_stmt_close(stmt);
            throw std::runtime_error("mysql_stmt_execute failed: " + err);
        }
        mysql_stmt_close(stmt);
    }

    void delete_user(const std::string& user_id) {
        // delete records first
        {
            MYSQL_STMT* stmt = mysql_stmt_init(conn_);
            if (!stmt) throw std::runtime_error("mysql_stmt_init failed");
            const char* sql = "DELETE FROM records WHERE user_id=?;";
            if (mysql_stmt_prepare(stmt, sql, std::strlen(sql)) != 0) {
                std::string err = mysql_stmt_error(stmt);
                mysql_stmt_close(stmt);
                throw std::runtime_error("mysql_stmt_prepare failed: " + err);
            }
            MYSQL_BIND b[1];
            std::memset(b, 0, sizeof(b));
            unsigned long l0 = static_cast<unsigned long>(user_id.size());
            b[0].buffer_type = MYSQL_TYPE_STRING; b[0].buffer = const_cast<char*>(user_id.data()); b[0].buffer_length = l0; b[0].length = &l0;
            mysql_stmt_bind_param(stmt, b);
            mysql_stmt_execute(stmt);
            mysql_stmt_close(stmt);
        }
        {
            MYSQL_STMT* stmt = mysql_stmt_init(conn_);
            if (!stmt) throw std::runtime_error("mysql_stmt_init failed");
            const char* sql = "DELETE FROM users WHERE user_id=?;";
            if (mysql_stmt_prepare(stmt, sql, std::strlen(sql)) != 0) {
                std::string err = mysql_stmt_error(stmt);
                mysql_stmt_close(stmt);
                throw std::runtime_error("mysql_stmt_prepare failed: " + err);
            }
            MYSQL_BIND b[1];
            std::memset(b, 0, sizeof(b));
            unsigned long l0 = static_cast<unsigned long>(user_id.size());
            b[0].buffer_type = MYSQL_TYPE_STRING; b[0].buffer = const_cast<char*>(user_id.data()); b[0].buffer_length = l0; b[0].length = &l0;
            mysql_stmt_bind_param(stmt, b);
            if (mysql_stmt_execute(stmt) != 0) {
                std::string err = mysql_stmt_error(stmt);
                mysql_stmt_close(stmt);
                throw std::runtime_error("mysql_stmt_execute failed: " + err);
            }
            mysql_stmt_close(stmt);
        }
    }

    void toggle_user_active(const std::string& user_id, int is_active) {
        MYSQL_STMT* stmt = mysql_stmt_init(conn_);
        if (!stmt) throw std::runtime_error("mysql_stmt_init failed");
        const char* sql = "UPDATE users SET is_active=? WHERE user_id=?;";
        if (mysql_stmt_prepare(stmt, sql, std::strlen(sql)) != 0) {
            std::string err = mysql_stmt_error(stmt);
            mysql_stmt_close(stmt);
            throw std::runtime_error("mysql_stmt_prepare failed: " + err);
        }
        MYSQL_BIND b[2];
        std::memset(b, 0, sizeof(b));
        int v = is_active;
        unsigned long l1 = static_cast<unsigned long>(user_id.size());
        b[0].buffer_type = MYSQL_TYPE_LONG; b[0].buffer = &v;
        b[1].buffer_type = MYSQL_TYPE_STRING; b[1].buffer = const_cast<char*>(user_id.data()); b[1].buffer_length = l1; b[1].length = &l1;
        mysql_stmt_bind_param(stmt, b);
        if (mysql_stmt_execute(stmt) != 0) {
            std::string err = mysql_stmt_error(stmt);
            mysql_stmt_close(stmt);
            throw std::runtime_error("mysql_stmt_execute failed: " + err);
        }
        mysql_stmt_close(stmt);
    }

    void add_record(const std::string& record_id,
                    const std::string& user_id,
                    const std::string& experiment_name,
                    const std::string& record_time,
                    double score) {
        MYSQL_STMT* stmt = mysql_stmt_init(conn_);
        if (!stmt) throw std::runtime_error("mysql_stmt_init failed");
        const char* sql =
            "INSERT INTO records (record_id, user_id, experiment_name, record_time, score) "
            "VALUES (?, ?, ?, ?, ?);";
        if (mysql_stmt_prepare(stmt, sql, std::strlen(sql)) != 0) {
            std::string err = mysql_stmt_error(stmt);
            mysql_stmt_close(stmt);
            throw std::runtime_error("mysql_stmt_prepare failed: " + err);
        }
        MYSQL_BIND b[5];
        std::memset(b, 0, sizeof(b));
        unsigned long l0 = static_cast<unsigned long>(record_id.size());
        unsigned long l1 = static_cast<unsigned long>(user_id.size());
        unsigned long l2 = static_cast<unsigned long>(experiment_name.size());
        unsigned long l3 = static_cast<unsigned long>(record_time.size());
        double sc = score;
        b[0].buffer_type = MYSQL_TYPE_STRING; b[0].buffer = const_cast<char*>(record_id.data()); b[0].buffer_length = l0; b[0].length = &l0;
        b[1].buffer_type = MYSQL_TYPE_STRING; b[1].buffer = const_cast<char*>(user_id.data()); b[1].buffer_length = l1; b[1].length = &l1;
        b[2].buffer_type = MYSQL_TYPE_STRING; b[2].buffer = const_cast<char*>(experiment_name.data()); b[2].buffer_length = l2; b[2].length = &l2;
        b[3].buffer_type = MYSQL_TYPE_STRING; b[3].buffer = const_cast<char*>(record_time.data()); b[3].buffer_length = l3; b[3].length = &l3;
        b[4].buffer_type = MYSQL_TYPE_DOUBLE; b[4].buffer = &sc;
        mysql_stmt_bind_param(stmt, b);
        if (mysql_stmt_execute(stmt) != 0) {
            std::string err = mysql_stmt_error(stmt);
            mysql_stmt_close(stmt);
            throw std::runtime_error("mysql_stmt_execute failed: " + err);
        }
        mysql_stmt_close(stmt);
    }

    void complete_record(const std::string& record_id, double score) {
        MYSQL_STMT* stmt = mysql_stmt_init(conn_);
        if (!stmt) throw std::runtime_error("mysql_stmt_init failed");
        const char* sql = "UPDATE records SET score=? WHERE record_id=?;";
        if (mysql_stmt_prepare(stmt, sql, std::strlen(sql)) != 0) {
            std::string err = mysql_stmt_error(stmt);
            mysql_stmt_close(stmt);
            throw std::runtime_error("mysql_stmt_prepare failed: " + err);
        }
        MYSQL_BIND b[2];
        std::memset(b, 0, sizeof(b));
        double sc = score;
        unsigned long l1 = static_cast<unsigned long>(record_id.size());
        b[0].buffer_type = MYSQL_TYPE_DOUBLE; b[0].buffer = &sc;
        b[1].buffer_type = MYSQL_TYPE_STRING; b[1].buffer = const_cast<char*>(record_id.data()); b[1].buffer_length = l1; b[1].length = &l1;
        mysql_stmt_bind_param(stmt, b);
        if (mysql_stmt_execute(stmt) != 0) {
            std::string err = mysql_stmt_error(stmt);
            mysql_stmt_close(stmt);
            throw std::runtime_error("mysql_stmt_execute failed: " + err);
        }
        mysql_stmt_close(stmt);
    }

    void delete_record(const std::string& record_id) {
        MYSQL_STMT* stmt = mysql_stmt_init(conn_);
        if (!stmt) throw std::runtime_error("mysql_stmt_init failed");
        const char* sql = "DELETE FROM records WHERE record_id=?;";
        if (mysql_stmt_prepare(stmt, sql, std::strlen(sql)) != 0) {
            std::string err = mysql_stmt_error(stmt);
            mysql_stmt_close(stmt);
            throw std::runtime_error("mysql_stmt_prepare failed: " + err);
        }
        MYSQL_BIND b[1];
        std::memset(b, 0, sizeof(b));
        unsigned long l0 = static_cast<unsigned long>(record_id.size());
        b[0].buffer_type = MYSQL_TYPE_STRING; b[0].buffer = const_cast<char*>(record_id.data()); b[0].buffer_length = l0; b[0].length = &l0;
        mysql_stmt_bind_param(stmt, b);
        if (mysql_stmt_execute(stmt) != 0) {
            std::string err = mysql_stmt_error(stmt);
            mysql_stmt_close(stmt);
            throw std::runtime_error("mysql_stmt_execute failed: " + err);
        }
        mysql_stmt_close(stmt);
    }

    void upsert_experiment(const std::string& experiment_id,
                           const std::string& experiment_name,
                           const std::string& experiment_description,
                           const std::string& experiment_image) {
        MYSQL_STMT* stmt = mysql_stmt_init(conn_);
        if (!stmt) throw std::runtime_error("mysql_stmt_init failed");
        const char* sql =
            "INSERT INTO experiments (experiment_id, experiment_name, experiment_description, experiment_image) "
            "VALUES (?, ?, ?, ?) "
            "ON DUPLICATE KEY UPDATE "
            "experiment_name=VALUES(experiment_name), "
            "experiment_description=VALUES(experiment_description), "
            "experiment_image=VALUES(experiment_image);";
        if (mysql_stmt_prepare(stmt, sql, std::strlen(sql)) != 0) {
            std::string err = mysql_stmt_error(stmt);
            mysql_stmt_close(stmt);
            throw std::runtime_error("mysql_stmt_prepare failed: " + err);
        }
        MYSQL_BIND b[4];
        std::memset(b, 0, sizeof(b));
        unsigned long l0 = static_cast<unsigned long>(experiment_id.size());
        unsigned long l1 = static_cast<unsigned long>(experiment_name.size());
        unsigned long l2 = static_cast<unsigned long>(experiment_description.size());
        unsigned long l3 = static_cast<unsigned long>(experiment_image.size());
        b[0].buffer_type = MYSQL_TYPE_STRING; b[0].buffer = const_cast<char*>(experiment_id.data()); b[0].buffer_length = l0; b[0].length = &l0;
        b[1].buffer_type = MYSQL_TYPE_STRING; b[1].buffer = const_cast<char*>(experiment_name.data()); b[1].buffer_length = l1; b[1].length = &l1;
        b[2].buffer_type = MYSQL_TYPE_STRING; b[2].buffer = const_cast<char*>(experiment_description.data()); b[2].buffer_length = l2; b[2].length = &l2;
        b[3].buffer_type = MYSQL_TYPE_STRING; b[3].buffer = const_cast<char*>(experiment_image.data()); b[3].buffer_length = l3; b[3].length = &l3;
        mysql_stmt_bind_param(stmt, b);
        if (mysql_stmt_execute(stmt) != 0) {
            std::string err = mysql_stmt_error(stmt);
            mysql_stmt_close(stmt);
            throw std::runtime_error("mysql_stmt_execute failed: " + err);
        }
        mysql_stmt_close(stmt);
    }

    void delete_experiment(const std::string& experiment_id) {
        MYSQL_STMT* stmt = mysql_stmt_init(conn_);
        if (!stmt) throw std::runtime_error("mysql_stmt_init failed");
        const char* sql = "DELETE FROM experiments WHERE experiment_id=?;";
        if (mysql_stmt_prepare(stmt, sql, std::strlen(sql)) != 0) {
            std::string err = mysql_stmt_error(stmt);
            mysql_stmt_close(stmt);
            throw std::runtime_error("mysql_stmt_prepare failed: " + err);
        }
        MYSQL_BIND b[1];
        std::memset(b, 0, sizeof(b));
        unsigned long l0 = static_cast<unsigned long>(experiment_id.size());
        b[0].buffer_type = MYSQL_TYPE_STRING; b[0].buffer = const_cast<char*>(experiment_id.data()); b[0].buffer_length = l0; b[0].length = &l0;
        mysql_stmt_bind_param(stmt, b);
        if (mysql_stmt_execute(stmt) != 0) {
            std::string err = mysql_stmt_error(stmt);
            mysql_stmt_close(stmt);
            throw std::runtime_error("mysql_stmt_execute failed: " + err);
        }
        mysql_stmt_close(stmt);
    }

private:
    DbConfig cfg_;
    MYSQL* conn_ = nullptr;
};

static void add_cors(httplib::Response& res) {
    res.set_header("Access-Control-Allow-Origin", "*");
    res.set_header("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
    res.set_header("Access-Control-Allow-Headers", "Content-Type, Authorization");
}

static bool parse_json_body(const httplib::Request& req, Json::Value& out, std::string& err) {
    Json::CharReaderBuilder b;
    b["collectComments"] = false;
    std::string errs;
    std::unique_ptr<Json::CharReader> reader(b.newCharReader());
    const char* begin = req.body.data();
    const char* end = begin + req.body.size();
    if (!reader->parse(begin, end, &out, &errs)) {
        err = errs;
        return false;
    }
    return true;
}

static std::string json_stringify(const Json::Value& v) {
    Json::StreamWriterBuilder w;
    w["indentation"] = "";
    return Json::writeString(w, v);
}

int main() {
    try {
        DbConfig cfg = load_config_from_env();

        httplib::Server svr;

        // Preflight
        svr.Options(R"(.*)", [](const httplib::Request&, httplib::Response& res) {
            add_cors(res);
            res.status = 204;
        });

        svr.Get("/health", [cfg](const httplib::Request&, httplib::Response& res) {
            add_cors(res);
            Json::Value out(Json::objectValue);
            out["ok"] = false;
            try {
                MySqlConn db(cfg);
                out["ok"] = db.ping();
            } catch (const std::exception& e) {
                out["error"] = e.what();
            }
            res.set_content(json_stringify(out), "application/json; charset=utf-8");
        });

        svr.Get("/experiments", [cfg](const httplib::Request&, httplib::Response& res) {
            add_cors(res);
            Json::Value out(Json::objectValue);
            out["ok"] = false;
            try {
                MySqlConn db(cfg);
                out["experiments"] = db.get_experiments();
                out["ok"] = true;
            } catch (const std::exception& e) {
                res.status = 500;
                out["error"] = e.what();
            }
            res.set_content(json_stringify(out), "application/json; charset=utf-8");
        });

        svr.Get("/users", [cfg](const httplib::Request&, httplib::Response& res) {
            add_cors(res);
            Json::Value out(Json::objectValue);
            out["ok"] = false;
            try {
                MySqlConn db(cfg);
                out["users"] = db.get_users();
                out["ok"] = true;
            } catch (const std::exception& e) {
                res.status = 500;
                out["error"] = e.what();
            }
            res.set_content(json_stringify(out), "application/json; charset=utf-8");
        });

        svr.Get("/records", [cfg](const httplib::Request&, httplib::Response& res) {
            add_cors(res);
            Json::Value out(Json::objectValue);
            out["ok"] = false;
            try {
                MySqlConn db(cfg);
                out["records"] = db.get_records_all();
                out["ok"] = true;
            } catch (const std::exception& e) {
                res.status = 500;
                out["error"] = e.what();
            }
            res.set_content(json_stringify(out), "application/json; charset=utf-8");
        });

        svr.Get("/records/byUser", [cfg](const httplib::Request& req, httplib::Response& res) {
            add_cors(res);
            Json::Value out(Json::objectValue);
            out["ok"] = false;
            auto it = req.params.find("userId");
            std::string userId = (it != req.params.end()) ? it->second : "";
            if (userId.empty()) {
                res.status = 400;
                out["error"] = "userId required";
                res.set_content(json_stringify(out), "application/json; charset=utf-8");
                return;
            }
            try {
                MySqlConn db(cfg);
                out["records"] = db.get_records_by_user(userId);
                out["ok"] = true;
            } catch (const std::exception& e) {
                res.status = 500;
                out["error"] = e.what();
            }
            res.set_content(json_stringify(out), "application/json; charset=utf-8");
        });

        // POST /login  { "username": "...", "password": "..." }
        svr.Post("/login", [cfg](const httplib::Request& req, httplib::Response& res) {
            add_cors(res);

            Json::Value body;
            std::string perr;
            if (!parse_json_body(req, body, perr)) {
                res.status = 400;
                Json::Value out(Json::objectValue);
                out["ok"] = false;
                out["error"] = "invalid json";
                out["detail"] = perr;
                res.set_content(json_stringify(out), "application/json; charset=utf-8");
                return;
            }

            std::string username = body.get("username", "").asString();
            std::string password = body.get("password", "").asString();
            if (username.empty() || password.empty()) {
                res.status = 400;
                Json::Value out(Json::objectValue);
                out["ok"] = false;
                out["error"] = "username/password required";
                res.set_content(json_stringify(out), "application/json; charset=utf-8");
                return;
            }

            Json::Value out(Json::objectValue);
            out["ok"] = false;
            try {
                MySqlConn db(cfg);
                auto user = db.login(username, password);
                if (!user.has_value()) {
                    res.status = 401;
                    out["error"] = "invalid credentials";
                } else {
                    out["ok"] = true;
                    out["user"] = *user;
                }
            } catch (const std::exception& e) {
                res.status = 500;
                out["error"] = e.what();
            }

            res.set_content(json_stringify(out), "application/json; charset=utf-8");
        });

        // POST /register { username, password, realName, email }
        svr.Post("/register", [cfg](const httplib::Request& req, httplib::Response& res) {
            add_cors(res);
            Json::Value body;
            std::string perr;
            if (!parse_json_body(req, body, perr)) {
                res.status = 400;
                Json::Value out(Json::objectValue);
                out["ok"] = false;
                out["error"] = "invalid json";
                out["detail"] = perr;
                res.set_content(json_stringify(out), "application/json; charset=utf-8");
                return;
            }
            std::string username = body.get("username", "").asString();
            std::string password = body.get("password", "").asString();
            std::string realName = body.get("realName", "").asString();
            std::string email = body.get("email", "").asString();

            Json::Value out(Json::objectValue);
            out["ok"] = false;
            if (username.empty() || password.empty() || realName.empty()) {
                res.status = 400;
                out["error"] = "missing fields";
                res.set_content(json_stringify(out), "application/json; charset=utf-8");
                return;
            }
            try {
                MySqlConn db(cfg);
                db.register_user(username, password, realName, email);
                out["ok"] = true;
            } catch (const std::exception& e) {
                res.status = 500;
                out["error"] = e.what();
            }
            res.set_content(json_stringify(out), "application/json; charset=utf-8");
        });

        // POST /admin/user/add { username, password, realName, email, role }
        svr.Post("/admin/user/add", [cfg](const httplib::Request& req, httplib::Response& res) {
            add_cors(res);
            Json::Value body;
            std::string perr;
            if (!parse_json_body(req, body, perr)) {
                res.status = 400;
                Json::Value out(Json::objectValue);
                out["ok"] = false;
                out["error"] = "invalid json";
                res.set_content(json_stringify(out), "application/json; charset=utf-8");
                return;
            }
            std::string username = body.get("username", "").asString();
            std::string password = body.get("password", "").asString();
            std::string realName = body.get("realName", "").asString();
            std::string email = body.get("email", "").asString();
            int role = body.get("role", 1).asInt();
            Json::Value out(Json::objectValue);
            out["ok"] = false;
            if (username.empty() || password.empty()) { res.status = 400; out["error"] = "missing fields"; res.set_content(json_stringify(out), "application/json; charset=utf-8"); return; }
            try {
                MySqlConn db(cfg);
                db.admin_add_user(username, password, realName, email, role);
                out["ok"] = true;
            } catch (const std::exception& e) {
                res.status = 500;
                out["error"] = e.what();
            }
            res.set_content(json_stringify(out), "application/json; charset=utf-8");
        });

        // POST /admin/user/update { userId, realName, email, role, newPassword(optional) }
        svr.Post("/admin/user/update", [cfg](const httplib::Request& req, httplib::Response& res) {
            add_cors(res);
            Json::Value body;
            std::string perr;
            if (!parse_json_body(req, body, perr)) { res.status = 400; Json::Value out(Json::objectValue); out["ok"]=false; out["error"]="invalid json"; res.set_content(json_stringify(out),"application/json; charset=utf-8"); return; }
            std::string userId = body.get("userId", "").asString();
            std::string realName = body.get("realName", "").asString();
            std::string email = body.get("email", "").asString();
            int role = body.get("role", 1).asInt();
            std::string newPassword = body.get("newPassword", "").asString();
            Json::Value out(Json::objectValue);
            out["ok"] = false;
            if (userId.empty()) { res.status=400; out["error"]="userId required"; res.set_content(json_stringify(out),"application/json; charset=utf-8"); return; }
            try {
                MySqlConn db(cfg);
                db.admin_update_user(userId, realName, email, role, newPassword);
                out["ok"] = true;
            } catch (const std::exception& e) {
                res.status = 500;
                out["error"] = e.what();
            }
            res.set_content(json_stringify(out), "application/json; charset=utf-8");
        });

        // POST /admin/user/delete { userId }
        svr.Post("/admin/user/delete", [cfg](const httplib::Request& req, httplib::Response& res) {
            add_cors(res);
            Json::Value body; std::string perr;
            if (!parse_json_body(req, body, perr)) { res.status=400; Json::Value out(Json::objectValue); out["ok"]=false; out["error"]="invalid json"; res.set_content(json_stringify(out),"application/json; charset=utf-8"); return; }
            std::string userId = body.get("userId", "").asString();
            Json::Value out(Json::objectValue); out["ok"]=false;
            if (userId.empty()) { res.status=400; out["error"]="userId required"; res.set_content(json_stringify(out),"application/json; charset=utf-8"); return; }
            try { MySqlConn db(cfg); db.delete_user(userId); out["ok"]=true; }
            catch (const std::exception& e) { res.status=500; out["error"]=e.what(); }
            res.set_content(json_stringify(out), "application/json; charset=utf-8");
        });

        // POST /admin/user/toggleActive { userId, isActive }
        svr.Post("/admin/user/toggleActive", [cfg](const httplib::Request& req, httplib::Response& res) {
            add_cors(res);
            Json::Value body; std::string perr;
            if (!parse_json_body(req, body, perr)) { res.status=400; Json::Value out(Json::objectValue); out["ok"]=false; out["error"]="invalid json"; res.set_content(json_stringify(out),"application/json; charset=utf-8"); return; }
            std::string userId = body.get("userId", "").asString();
            int isActive = body.get("isActive", 1).asInt();
            Json::Value out(Json::objectValue); out["ok"]=false;
            if (userId.empty()) { res.status=400; out["error"]="userId required"; res.set_content(json_stringify(out),"application/json; charset=utf-8"); return; }
            try { MySqlConn db(cfg); db.toggle_user_active(userId, isActive); out["ok"]=true; }
            catch (const std::exception& e) { res.status=500; out["error"]=e.what(); }
            res.set_content(json_stringify(out), "application/json; charset=utf-8");
        });

        // POST /user/resetPassword { userId, newPassword }
        svr.Post("/user/resetPassword", [cfg](const httplib::Request& req, httplib::Response& res) {
            add_cors(res);
            Json::Value body; std::string perr;
            if (!parse_json_body(req, body, perr)) { res.status=400; Json::Value out(Json::objectValue); out["ok"]=false; out["error"]="invalid json"; res.set_content(json_stringify(out),"application/json; charset=utf-8"); return; }
            std::string userId = body.get("userId", "").asString();
            std::string newPassword = body.get("newPassword", "").asString();
            Json::Value out(Json::objectValue); out["ok"]=false;
            if (userId.empty() || newPassword.empty()) { res.status=400; out["error"]="missing fields"; res.set_content(json_stringify(out),"application/json; charset=utf-8"); return; }
            try { MySqlConn db(cfg); db.reset_password(userId, newPassword); out["ok"]=true; }
            catch (const std::exception& e) { res.status=500; out["error"]=e.what(); }
            res.set_content(json_stringify(out), "application/json; charset=utf-8");
        });

        // POST /record/add { recordId, userId, experimentName, recordTime, score }
        svr.Post("/record/add", [cfg](const httplib::Request& req, httplib::Response& res) {
            add_cors(res);
            Json::Value body; std::string perr;
            if (!parse_json_body(req, body, perr)) { res.status=400; Json::Value out(Json::objectValue); out["ok"]=false; out["error"]="invalid json"; res.set_content(json_stringify(out),"application/json; charset=utf-8"); return; }
            std::string recordId = body.get("recordId", "").asString();
            std::string userId = body.get("userId", "").asString();
            std::string experimentName = body.get("experimentName", "").asString();
            std::string recordTime = body.get("recordTime", "").asString();
            double score = body.get("score", 0.0).asDouble();
            Json::Value out(Json::objectValue); out["ok"]=false;
            if (recordId.empty() || userId.empty()) { res.status=400; out["error"]="missing fields"; res.set_content(json_stringify(out),"application/json; charset=utf-8"); return; }
            try { MySqlConn db(cfg); db.add_record(recordId, userId, experimentName, recordTime, score); out["ok"]=true; }
            catch (const std::exception& e) { res.status=500; out["error"]=e.what(); }
            res.set_content(json_stringify(out), "application/json; charset=utf-8");
        });

        // POST /record/complete { recordId, score }
        svr.Post("/record/complete", [cfg](const httplib::Request& req, httplib::Response& res) {
            add_cors(res);
            Json::Value body; std::string perr;
            if (!parse_json_body(req, body, perr)) { res.status=400; Json::Value out(Json::objectValue); out["ok"]=false; out["error"]="invalid json"; res.set_content(json_stringify(out),"application/json; charset=utf-8"); return; }
            std::string recordId = body.get("recordId", "").asString();
            double score = body.get("score", 0.0).asDouble();
            Json::Value out(Json::objectValue); out["ok"]=false;
            if (recordId.empty()) { res.status=400; out["error"]="recordId required"; res.set_content(json_stringify(out),"application/json; charset=utf-8"); return; }
            try { MySqlConn db(cfg); db.complete_record(recordId, score); out["ok"]=true; }
            catch (const std::exception& e) { res.status=500; out["error"]=e.what(); }
            res.set_content(json_stringify(out), "application/json; charset=utf-8");
        });

        // POST /record/delete { recordId }
        svr.Post("/record/delete", [cfg](const httplib::Request& req, httplib::Response& res) {
            add_cors(res);
            Json::Value body; std::string perr;
            if (!parse_json_body(req, body, perr)) { res.status=400; Json::Value out(Json::objectValue); out["ok"]=false; out["error"]="invalid json"; res.set_content(json_stringify(out),"application/json; charset=utf-8"); return; }
            std::string recordId = body.get("recordId", "").asString();
            Json::Value out(Json::objectValue); out["ok"]=false;
            if (recordId.empty()) { res.status=400; out["error"]="recordId required"; res.set_content(json_stringify(out),"application/json; charset=utf-8"); return; }
            try { MySqlConn db(cfg); db.delete_record(recordId); out["ok"]=true; }
            catch (const std::exception& e) { res.status=500; out["error"]=e.what(); }
            res.set_content(json_stringify(out), "application/json; charset=utf-8");
        });

        // POST /experiment/upsert { experimentId, experimentName, experimentDescription, experimentImage }
        svr.Post("/experiment/upsert", [cfg](const httplib::Request& req, httplib::Response& res) {
            add_cors(res);
            Json::Value body; std::string perr;
            if (!parse_json_body(req, body, perr)) { res.status=400; Json::Value out(Json::objectValue); out["ok"]=false; out["error"]="invalid json"; res.set_content(json_stringify(out),"application/json; charset=utf-8"); return; }
            std::string experimentId = body.get("experimentId", "").asString();
            std::string experimentName = body.get("experimentName", "").asString();
            std::string experimentDescription = body.get("experimentDescription", "").asString();
            std::string experimentImage = body.get("experimentImage", "").asString();
            Json::Value out(Json::objectValue); out["ok"]=false;
            if (experimentId.empty()) { res.status=400; out["error"]="experimentId required"; res.set_content(json_stringify(out),"application/json; charset=utf-8"); return; }
            try { MySqlConn db(cfg); db.upsert_experiment(experimentId, experimentName, experimentDescription, experimentImage); out["ok"]=true; }
            catch (const std::exception& e) { res.status=500; out["error"]=e.what(); }
            res.set_content(json_stringify(out), "application/json; charset=utf-8");
        });

        // POST /experiment/delete { experimentId }
        svr.Post("/experiment/delete", [cfg](const httplib::Request& req, httplib::Response& res) {
            add_cors(res);
            Json::Value body; std::string perr;
            if (!parse_json_body(req, body, perr)) { res.status=400; Json::Value out(Json::objectValue); out["ok"]=false; out["error"]="invalid json"; res.set_content(json_stringify(out),"application/json; charset=utf-8"); return; }
            std::string experimentId = body.get("experimentId", "").asString();
            Json::Value out(Json::objectValue); out["ok"]=false;
            if (experimentId.empty()) { res.status=400; out["error"]="experimentId required"; res.set_content(json_stringify(out),"application/json; charset=utf-8"); return; }
            try { MySqlConn db(cfg); db.delete_experiment(experimentId); out["ok"]=true; }
            catch (const std::exception& e) { res.status=500; out["error"]=e.what(); }
            res.set_content(json_stringify(out), "application/json; charset=utf-8");
        });

        // Listen
        int port = 7071;
        if (auto v = env_or_null("CHEMLAB_PORT")) port = std::atoi(v);

        std::cerr << "[chemlab_gateway] Listening on 0.0.0.0:" << port << "\n";
        std::cerr << "[chemlab_gateway] DB: " << cfg.host << ":" << cfg.port << "/" << cfg.database
                  << " user=" << cfg.user << "\n";

        if (!svr.listen("0.0.0.0", port)) {
            std::cerr << "Failed to listen\n";
            return 1;
        }
        return 0;
    } catch (const std::exception& e) {
        std::cerr << "Fatal: " << e.what() << "\n";
        return 1;
    }
}

