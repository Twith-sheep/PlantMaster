using PlantMaster.Models;
using PlantMaster.Configurations;
using PlantMaster.Infrastructure.Config.Interfaces;
using PlantMaster.Services;
using System;
using System.Collections.Generic;


namespace PlantMaster.Infrastructure.Config
{

    /// <summary>
    /// 配置管理器
    ///
    /// 负责：
    ///
    /// 1.管理程序中的网关配置数据
    /// 2.提供配置增删改接口
    /// 3.调用配置读取器进行加载和保存
    /// 4.记录配置变化日志
    ///
    ///
    /// 不负责：
    ///
    /// 1.XML解析
    /// 2.JSON解析
    /// 3.文件操作
    /// 4.网关连接
    /// 5.TCP通信
    ///
    /// </summary>
    public class ConfigManager
    {


        /// <summary>
        /// 配置读取器
        ///
        /// 负责：
        /// 文件和对象之间转换
        ///
        /// 例如：
        ///
        /// Gateway.xml
        ///      ↓
        /// GatewayConfig
        ///
        /// </summary>
        private readonly IConfigReader configReader;



        /// <summary>
        /// 日志服务
        ///
        /// 用于记录：
        ///
        /// 1.配置加载
        /// 2.配置添加
        /// 3.配置删除
        /// 4.配置保存
        /// 5.配置导入
        ///
        /// </summary>
        private readonly LogService logService;



        /// <summary>
        /// 当前程序中的网关配置列表
        ///
        /// 注意：
        ///
        /// 这里保存的是：
        /// GatewaySetting
        ///
        /// 表示：
        /// 一个网关的配置信息
        ///
        /// 不包含：
        /// 1.连接状态
        /// 2.TCP对象
        /// 3.运行数据
        ///
        /// </summary>
        private readonly List<GatewaySetting> gateways;



        /// <summary>
        /// 对外提供只读网关配置列表
        ///
        /// 防止外部直接修改集合
        ///
        /// 外部可以：
        /// 查看配置
        ///
        /// 不能：
        /// Add
        /// Remove
        ///
        /// </summary>
        public IReadOnlyList<GatewaySetting> Gateways
        {
            get
            {
                return gateways;
            }
        }




        /// <summary>
        /// 创建配置管理器
        /// </summary>
        public ConfigManager(
            IConfigReader configReader,
            LogService logService)
        {

            this.configReader =
                configReader;


            this.logService =
                logService;



            gateways =
                new List<GatewaySetting>();

        }





        /// <summary>
        /// 加载网关配置
        ///
        /// 流程：
        ///
        /// Gateway.xml
        ///
        /// ↓
        ///
        /// GatewayConfig
        ///
        /// ↓
        ///
        /// List<GatewaySetting>
        ///
        /// </summary>
        public void LoadGateways()
        {

            try
            {

                GatewayConfig config =
                    configReader.Read<GatewayConfig>(
                        ConfigPath.Gateway
                    );



                gateways.Clear();



                if (config != null)
                {

                    gateways.AddRange(
                        config.Gateways
                    );


                    logService.Info(
                        $"网关配置加载完成，共{gateways.Count}个"
                    );

                }
                else
                {

                    logService.Warn(
                        "网关配置为空"
                    );

                }


            }
            catch (Exception ex)
            {

                logService.Error(
                    $"加载网关配置失败:{ex.Message}"
                );

            }

        }





        /// <summary>
        /// 添加网关配置
        ///
        /// 流程：
        ///
        /// GatewaySetting
        ///
        /// ↓
        ///
        /// 内存配置列表
        ///
        /// ↓
        ///
        /// 保存XML
        ///
        /// 注意：
        ///
        /// 这里只添加配置
        ///
        /// 不负责创建连接
        ///
        /// </summary>
        public void AddGateway(
            GatewaySetting gateway)
        {

            if (gateway == null)
            {

                logService.Warn(
                    "添加网关失败，配置为空"
                );

                return;

            }



            gateways.Add(
                gateway
            );



            SaveGateway();



            logService.Info(
                $"添加网关配置成功:{gateway.Id}"
            );

        }





        /// <summary>
        /// 根据Id删除网关配置
        ///
        /// 删除：
        ///
        /// 内存配置
        ///
        /// 并同步：
        ///
        /// XML文件
        ///
        /// 注意：
        ///
        /// 不负责断开通信
        ///
        /// 通信释放由GatewayService负责
        /// </summary>
        public void RemoveGateway(
            string id)
        {


            GatewaySetting gateway =
                gateways.Find(
                    x => x.Id == id
                );



            if (gateway != null)
            {

                gateways.Remove(
                    gateway
                );


                SaveGateway();



                logService.Info(
                    $"删除网关配置成功:{id}"
                );

            }
            else
            {

                logService.Warn(
                    $"删除网关配置失败，未找到:{id}"
                );

            }

        }





        /// <summary>
        /// 保存当前网关配置
        ///
        /// 流程：
        ///
        /// List<GatewaySetting>
        ///
        /// ↓
        ///
        /// GatewayConfig
        ///
        /// ↓
        ///
        /// Gateway.xml
        ///
        /// </summary>
        private void SaveGateway()
        {

            try
            {

                GatewayConfig config =
                    new GatewayConfig();



                config.Gateways =
                    gateways;



                configReader.Save(
                    ConfigPath.Gateway,
                    config
                );



                logService.Info(
                    "网关配置保存成功"
                );


            }
            catch (Exception ex)
            {

                logService.Error(
                    $"保存网关配置失败:{ex.Message}"
                );

            }

        }





        /// <summary>
        /// 导入外部网关配置
        ///
        /// 流程：
        ///
        /// 外部XML
        ///
        /// ↓
        ///
        /// GatewayConfig
        ///
        /// ↓
        ///
        /// GatewaySetting列表
        ///
        /// ↓
        ///
        /// 当前配置
        ///
        /// ↓
        ///
        /// 保存系统配置
        ///
        /// </summary>
        public void ImportGatewayConfig(
            string filePath)
        {

            try
            {

                GatewayConfig config =
                    configReader.Read<GatewayConfig>(
                        filePath
                    );

                //github测试

                if (config == null)
                {

                    logService.Warn(
                        "导入配置失败，文件内容为空"
                    );

                    return;

                }



                gateways.Clear();



                gateways.AddRange(
                    config.Gateways
                );



                SaveGateway();



                logService.Info(
                    $"导入网关配置成功，共{gateways.Count}个"
                );

            }
            catch (Exception ex)
            {

                logService.Error(
                    $"导入网关配置失败:{ex.Message}"
                );

            }

        }


    }

}