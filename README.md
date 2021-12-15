# MyTwitter

## 服务器启动方式

在`MyTwitter.Server`目录下执行`dotnet run -c Release`即可。

## 客户端启动方式

在`MyTwitter.ClientCUI`目录下执行`dotnet run -c Release`即可。

该程序内已经自带操作说明。

## 性能测试模拟器启动方式

在`MyTwitter.Simulator`目录下执行`dotnet run -c Release`即可。

模拟器中的用户名已经添加了PID，可以同时启动多个实例。

# 性能测试数据

Intel Core i5-6300HQ CPU

普通用户数量(以Zipf分布关注发推用户) | 发推用户数量 | 每发推用户的发推数 | 耗时
---- | --- | ----- | ---
100  | 1   | 1000  | 00:00:00.0378123
500  | 1   | 1000  | 00:00:00.0399290
100  | 10  | 1000  | 00:00:00.1708791
100  | 50  | 1000  | 00:00:00.2817119
500  | 50  | 1000  | 00:00:00.3652651
1000 | 50  | 1000  | 00:00:00.5324507
100  | 50  | 10000 | 00:01:00.8630031
500  | 50  | 10000 | 00:00:04.0518222


# JSON协议

## 客户端到服务器

客户端到服务器的消息为一个Object，其中含有`op`、`arg1`、`arg2`三个字段。
`op`字段与操作具有以下联系：

op字段 | 描述 | arg1含义 | arg2含义
----- | --- | -------- | --------
register | 注册并登录 | 用户名 |
follow   | 开始follow另一个用户 | 用户名 |
tweet    | 发推 | 内容 | 要转推的ID（可选）
query_posts_by_at | 根据@查找推文 | @的用户名 |
query_posts_by_tag | 根据标签查找推文 | 标签名 |
query_posts_by_user | 根据用户名查找推文 | 用户名 |

## 服务端到客户端

服务端对客户端发送以下对象的数组，其中每个对象代表一个推文：

字段 | 类型 | 含义
--- | ---- | ----
post_id | number | Post ID
author  | string | 作者的用户名
at      | string array | @的用户列表
tags    | string array | 标签列表
retweet | number (nullable) | 转推的原文ID
content | string | 内容
