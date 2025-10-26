# 快速开始

## 5 分钟安装指南

### 1. 下载插件

从 [Releases](https://github.com/yourusername/EmbyVersionByFolder/releases) 下载最新版本：
- `EmbyVersionByFolder.dll`
- `0Harmony.dll`

### 2. 安装到 Emby

**Linux:**
```bash
# 复制文件
cp EmbyVersionByFolder.dll /config/plugins/
cp 0Harmony.dll /config/plugins/

# 设置权限
chmod 644 /config/plugins/*.dll

# 重启 Emby
systemctl restart emby-server
```

**Windows:**
```powershell
# 复制文件到插件目录
Copy-Item EmbyVersionByFolder.dll "C:\ProgramData\Emby-Server\plugins\"
Copy-Item 0Harmony.dll "C:\ProgramData\Emby-Server\plugins\"

# 重启 Emby 服务
Restart-Service EmbyServer
```

### 3. 配置插件

1. 打开 Emby 控制台
2. 导航到 **插件** → **Version By Folder**
3. 勾选 **启用插件**
4. 点击 **保存**

### 4. 验证

打开一个有多个版本的媒体，检查版本名称是否已自动设置。

## 示例场景

### 电影多版本

```
/电影/
  ├── SGNB/
  │   └── 变形金刚 (2007) {tmdb-1858}/
  │       └── 变形金刚.iso
  └── THDBST/
      └── 变形金刚 (2007) {tmdb-1858}/
          └── 变形金刚.iso
```

**结果：** 版本名称自动设置为 `SGNB` 和 `THDBST`

### 剧集多版本

```
/剧集/
  ├── 风骚律师 (2015) {tmdb-60059} [REMUX]/
  │   └── Season 1/
  │       └── S01E01.mkv
  └── 风骚律师 (2015) {tmdb-60059} [NF]/
      └── Season 1/
          └── S01E01.mkv
```

**结果：** 版本名称自动设置为 `REMUX` 和 `NF`

## 配置选项

### 启用插件
开启或关闭插件功能。插件会自动检测所有多版本媒体（相同 TMDB ID）并设置版本名称。

