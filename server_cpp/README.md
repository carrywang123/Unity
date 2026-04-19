# chemlab_gateway_cpp

一个极简 C++ HTTP 服务，默认监听 **7071**，用于在服务器侧直连 MySQL，给 WebGL/客户端提供 HTTP 访问入口。

## 1) Ubuntu 安装依赖

```bash
sudo apt-get update
sudo apt-get install -y build-essential cmake pkg-config \
  default-libmysqlclient-dev libjsoncpp-dev libssl-dev
```

还需要下载单头文件 **cpp-httplib**：

- 下载 `httplib.h` 放到：`server_cpp/third_party/httplib.h`
- 来源：`https://github.com/yhirose/cpp-httplib/blob/master/httplib.h`

> 当前仓库里的 `third_party/httplib.h` 是占位文件，你把真实的 header 覆盖掉即可。

## 2) 编译

```bash
cd server_cpp
cmake -S . -B build
cmake --build build -j
```

生成可执行文件：`server_cpp/build/chemlab_gateway`

## 3) 运行

### 方式 A：用环境变量配置数据库

```bash
export CHEMLAB_DB_HOST="127.0.0.1"
export CHEMLAB_DB_PORT="3306"
export CHEMLAB_DB_NAME="chemlab"
export CHEMLAB_DB_USER="root"
export CHEMLAB_DB_PASSWORD="Cloud2023@"
export CHEMLAB_PORT="7071"

./build/chemlab_gateway
```

### 方式 B：只改端口（数据库用默认值）

```bash
export CHEMLAB_PORT="7071"
./build/chemlab_gateway
```

## 4) 接口

### GET /health

返回数据库连通性：

```json
{ "ok": true }
```

### POST /login

请求：

```json
{ "username": "222", "password": "222" }
```

响应（成功）：

```json
{
  "ok": true,
  "user": { "userId": "...", "role": 0, "realName": "..." }
}
```

响应（失败）：

```json
{ "ok": false, "error": "invalid credentials" }
```

### POST /register

请求：

```json
{ "username": "u1", "password": "123456", "realName": "张三", "email": "a@b.com" }
```

### 管理员用户管理

- `POST /admin/user/add`
- `POST /admin/user/update`
- `POST /admin/user/delete`
- `POST /admin/user/toggleActive`

### 记录

- `POST /record/add`
- `POST /record/complete`
- `POST /record/delete`

### 实验

- `POST /experiment/upsert`
- `POST /experiment/delete`

## 5) 防火墙/安全提示

- 需要在服务器放行 `7071/tcp`
- 建议实际部署时加上：
  - HTTPS 反向代理（nginx）
  - Access token（避免任意人调用登录接口暴力破解）

