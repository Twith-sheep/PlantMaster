\# PlantMaster Development Guide





\## 项目目标



工业PLC主站软件。



技术栈：



\- C#

\- WPF

\- MVVM

\- SQLite





\## 架构规则



项目分层：



Views

&#x20;   只负责UI



ViewModels

&#x20;   管理界面状态



Services

&#x20;   业务逻辑



Communication

&#x20;   通信协议



Drivers

&#x20;   PLC驱动





禁止：



\- View直接访问PLC

\- ViewModel中new Service

\- Service引用View





\## 通信规则



支持：



\- Modbus TCP

\- TCP通信



要求：



\- async/await

\- 不阻塞UI

\- 异常必须捕获





\## 多线程规则



多网关：



允许并行





单网关：



读写必须串行





\## 日志规则



所有：



\- 连接成功

\- 连接失败

\- 断开

\- 异常

\- 数据变化



必须记录日志。





\## 修改代码要求



修改前：



说明方案





修改后：



说明：



1\. 修改文件

2\. 修改原因

3\. 是否影响架构

