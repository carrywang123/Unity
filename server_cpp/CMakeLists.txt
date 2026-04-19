cmake_minimum_required(VERSION 3.16)
project(chemlab_gateway_cpp LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

add_executable(chemlab_gateway
    src/main.cpp
)

find_package(Threads REQUIRED)

# MySQL C client (libmysqlclient)
find_path(MYSQL_INCLUDE_DIR mysql/mysql.h)
find_library(MYSQL_CLIENT_LIB mysqlclient)
if (NOT MYSQL_INCLUDE_DIR OR NOT MYSQL_CLIENT_LIB)
    message(FATAL_ERROR "MySQL client library not found. Install: sudo apt-get install -y default-libmysqlclient-dev")
endif()

# jsoncpp (for parsing request bodies)
find_path(JSONCPP_INCLUDE_DIR json/json.h
    PATHS
        /usr/include/jsoncpp
        /usr/local/include/jsoncpp
)
find_library(JSONCPP_LIB jsoncpp)
if (NOT JSONCPP_INCLUDE_DIR OR NOT JSONCPP_LIB)
    message(FATAL_ERROR "jsoncpp not found. Install: sudo apt-get install -y libjsoncpp-dev")
endif()

# OpenSSL (for MD5)
find_package(OpenSSL REQUIRED)

target_include_directories(chemlab_gateway PRIVATE
    ${MYSQL_INCLUDE_DIR}
    ${JSONCPP_INCLUDE_DIR}
    ${CMAKE_CURRENT_SOURCE_DIR}/third_party
)

target_link_libraries(chemlab_gateway PRIVATE
    Threads::Threads
    ${MYSQL_CLIENT_LIB}
    ${JSONCPP_LIB}
    OpenSSL::Crypto
)
