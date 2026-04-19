-- Chemlab MySQL 初始化（与 Assets/scripts/Managers/DataManager.cs 中 EnsureSchema 一致）
-- 用法：mysql -u root -p < schema.sql
-- 或：mysql -u root -p chemlab < schema.sql

CREATE DATABASE IF NOT EXISTS chemlab DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE chemlab;

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
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS experiments (
  experiment_id          VARCHAR(32)   NOT NULL,
  experiment_name        VARCHAR(100)  NOT NULL DEFAULT '',
  experiment_description VARCHAR(500)  NOT NULL DEFAULT '',
  experiment_image       VARCHAR(255)  NOT NULL DEFAULT '',
  PRIMARY KEY (experiment_id),
  UNIQUE KEY uk_experiments_name (experiment_name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS records (
  record_id        VARCHAR(32)  NOT NULL,
  user_id          VARCHAR(32)  NOT NULL,
  experiment_name  VARCHAR(100) NOT NULL DEFAULT '',
  record_time      VARCHAR(19)  NOT NULL DEFAULT '',
  score            FLOAT        NOT NULL DEFAULT 0,
  PRIMARY KEY (record_id),
  KEY idx_records_user (user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 与 Unity 端一致的默认实验种子
INSERT INTO experiments (experiment_id, experiment_name, experiment_description, experiment_image)
VALUES ('1', '吸光度检验', '吸光度检验实验', '')
ON DUPLICATE KEY UPDATE
  experiment_name        = VALUES(experiment_name),
  experiment_description = VALUES(experiment_description),
  experiment_image       = VALUES(experiment_image);

-- 可选：默认管理员（与 DataManager 中 ADMIN_USERNAME/ADMIN_PASSWORD 一致：222 / 222）
-- 密码存 MD5 小写 32 位，与 C++ / Unity 一致
INSERT INTO users (user_id, username, password, real_name, email, role, create_time, last_login_time, is_active)
VALUES (
  '00000000000000000000000000000001',
  '222',
  LOWER(MD5('222')),
  '系统管理员',
  'admin@chemlab.com',
  0,
  DATE_FORMAT(NOW(), '%Y-%m-%d %H:%i:%s'),
  '',
  1
)
ON DUPLICATE KEY UPDATE user_id = VALUES(user_id);
