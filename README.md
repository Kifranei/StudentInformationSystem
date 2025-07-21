# 学生教学管理一体化信息系统

这是一个为毕业设计而创建的全功能、响应式的教学管理系统。项目旨在模拟真实校园环境中的核心教务流程，为管理员、教师、学生三种不同角色提供定制化的操作界面和功能。

系统前端设计灵感来源于真实的校园教务系统，并在此基础上进行了现代化和响应式改造，实现了桌面端和移动端的完美适配。同时，项目加入了全新设计的亮色/暗色主题切换功能，提升了用户体验。

## 主要功能

### 👨‍💻 管理员 (Administrator)

- **动态仪表盘**: 实时查看学生、教师、课程、选课人次等核心数据统计，并监控服务器运行状态（IIS、.NET版本、内存占用等）。
- **学生管理**: 完整的学生信息增、删、改、查（CRUD）及按姓名/学号搜索功能。
- **教师管理**: 完整的教师信息 CRUD 及按姓名/工号搜索功能。
- **班级管理**: 完整的班级信息（含专业、学年）CRUD 和搜索功能，并可直接在班级详情页为该班级添加学生。
- **课程管理**: 完整的课程信息 CRUD 及按课程名/教师名搜索功能，支持为课程指定类别（如专业必修、公共选修等）。
- **考试管理**: 发布、修改、删除所有课程的考试安排，支持搜索考试安排。
- **选课记录查看**: 查看全校所有学生的选课记录，并支持搜索。
- **密码管理**: 可为任意学生或教师重置密码为默认值。
- **个人密码修改**: 管理员可修改自己的登录密码。

### 👩‍🏫 教师 (Teacher)

- **个性化首页**: 登录后直接看到当日授课安排和快捷功能入口。
- **我的课表**: 查看个人完整授课表，支持按周切换，并可隐藏操作按钮以获得更简洁的视图，方便截图。
- **在线调课**: 可修改自己所教课程的上课时间、地点。
- **成绩录入**: 查看所教课程的学生名单，并为学生录入/修改成绩。
- **班级名单打印**: 为所教课程生成简洁、适合打印的学生名单。
- **考试管理**: 发布和管理**自己所教课程**的考试安排。
- **个人密码修改**: 教师可修改自己的登录密码。

### 🎓 学生 (Student)

- **个性化首页**: 登录后直接看到当日课程提醒和最新公布的成绩。
- **我的课表**: 查看个人课表，支持按周切换和**导出为图片**。
- **高级在线选课**:
  - 按“专业必修”、“体育选修”等类别筛选课程。
  - 满足最低选课门数要求的规则提示。
  - 挂科后自动出现“重修”课程列表。
  - 独立的“我的已选”页面，方便查看和退选。
- **我的成绩**: 查看所有已选课程的成绩单。
- **我的考试**: 查看自己所有课程的考试安排。
- **个人密码修改**: 学生可修改自己的登录密码。

## 技术栈

- **后端**: ASP.NET MVC 5, Entity Framework 6 (Database First)
- **前端**: HTML5, CSS3, Bootstrap 3, jQuery
- **数据库**: Microsoft SQL Server
- **核心第三方库**: html2canvas.js (用于课表截图)

## 如何运行

#### 1\. 环境准备

- Visual Studio 2017 或更高版本（推荐 Visual Studio 2022 Community）
- Microsoft SQL Server 2012 或更高版本（推荐SQL Server 2022 Develop）

#### 2\. 数据库设置

1. 打开 SQL Server Management Studio (SSMS)。
2. 创建一个新的空数据库（例如 `StudentManagementDB`）或 将项目中的 SQL 脚本文件（`/db/sql.sql`）在新建的查询窗口中执行，以创建所有必需的数据库 表和样例数据。

#### 3\. 配置连接字符串

1. 打开项目根目录下的 `Web.config` 文件。

2. 找到 `<connectionStrings>` 配置节。

3. 修改 `data source` 的值为你自己的 SQL Server 实例名（如果是本地默认实例，通常是 `.` 或者 `localhost`）。确保 `initial catalog` 是你创建的数据库名。
   <connectionStrings>
  <add name="Entities" connectionString="metadata=res://*/Models.Model1.csdl|res://*/Models.Model1.ssdl|res://*/Models.Model1.msl;provider=System.Data.SqlClient;provider connection string="data source=.;initial catalog=StudentManagementDB;integrated security=True;encrypt=False;MultipleActiveResultSets=True;App=EntityFramework"" providerName="System.Data.EntityClient" />
</connectionStrings>
#### 4\. 启动项目

1. 用 Visual Studio 打开 `.sln` 项目文件。
2. 按 `F5` 或点击“启动”按钮来运行项目。

## 许可证

该项目采用 [MIT许可证](https://opensource.org/licenses/MIT)。

## 致谢

本项目的完成离不开 GitHub Copilot Pro（Gemini 2.5 Pro）和 Google Gemini 2.5 Pro 的协助。
