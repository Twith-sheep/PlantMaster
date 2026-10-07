using PlantMaster.Communication;
using PlantMaster.Models;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace PlantMaster.Services
{
    /// <summary>
    /// 网关运行管理服务。
    ///
    /// 每个GatewayRuntime拥有独立的异步锁，
    /// 同一网关的完整通信事务串行，
    /// 不同网关之间可以并行。
    /// </summary>
    public class GatewayService
    {
        private static readonly TimeSpan DefaultConnectionTimeout =
            TimeSpan.FromSeconds(10);

        private static readonly TimeSpan DefaultLifecycleTimeout =
            TimeSpan.FromSeconds(10);

        private static readonly TimeSpan DefaultTransactionTimeout =
            TimeSpan.FromSeconds(30);


        private readonly ConcurrentDictionary<string, GatewayRuntime> runtimes;

        private readonly LogService logService;

        private readonly CancellationTokenSource serviceLifetimeSource;


        /// <summary>
        /// 网关连接状态发生变化时通知上层。
        /// </summary>
        public event EventHandler<GatewayConnectionStateChangedEventArgs>?
            ConnectionStateChanged;


        public GatewayService(LogService logService)
        {
            runtimes =
                new ConcurrentDictionary<string, GatewayRuntime>();

            this.logService =
                logService;

            serviceLifetimeSource =
                new CancellationTokenSource();
        }


        /// <summary>
        /// 根据配置创建网关运行对象。
        /// </summary>
        public void AddGateway(GatewaySetting setting)
        {
            if (setting == null)
            {
                logService.Warn(
                    "创建网关运对象失败:配置为空"
                );

                return;
            }


            ICommunicationClient client =
                new TcpCommunicationClient(
                    setting.IpAddress,
                    setting.Port,
                    logService
                );

            GatewayRuntime runtime =
                new GatewayRuntime(
                    setting,
                    client
                );


            if (!runtimes.TryAdd(setting.Id, runtime))
            {
                logService.Warn(
                    $"网关已存在:{setting.Id}"
                );

                return;
            }


            logService.Info(
                $"创建网关运行对象:{setting.Id}"
            );
        }


        private GatewayRuntime? GetRuntime(string id)
        {
            runtimes.TryGetValue(
                id,
                out GatewayRuntime? runtime
            );

            return runtime;
        }


        /// <summary>
        /// 获取指定网关当前的运行状态。
        /// </summary>
        public bool IsOnline(string id)
        {
            GatewayRuntime? runtime =
                GetRuntime(id);

            return runtime != null
                && runtime.IsOnline;
        }


        private void UpdateOnlineState(
            GatewayRuntime runtime,
            bool isOnline)
        {
            if (runtime.IsOnline == isOnline)
            {
                return;
            }


            runtime.IsOnline = isOnline;


            try
            {
                ConnectionStateChanged?.Invoke(
                    this,
                    new GatewayConnectionStateChangedEventArgs(
                        runtime.Setting.Id,
                        isOnline
                    )
                );
            }
            catch (Exception ex)
            {
                logService.Error(
                    $"网关状态通知失败:{runtime.Setting.Id} {ex.Message}"
                );
            }
        }


        /// <summary>
        /// 串行执行单个网关的完整通信事务。
        ///
        /// GatewayService不知道具体协议；请求生成、发送、
        /// 接收、校验和解析均在transaction委托内完成。
        /// </summary>
        public async Task<TResult> ExecuteTransactionAsync<TResult>(
            string id,
            string operationName,
            Func<ICommunicationClient, CancellationToken, Task<TResult>> transaction,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(transaction);

            GatewayRuntime? runtime =
                GetRuntime(id);

            if (runtime == null)
            {
                string message =
                    $"不存在运行网关:{id} 操作:{operationName}";

                logService.Error(message);
                throw new InvalidOperationException(message);
            }


            return await ExecuteWithGatewayLockAsync(
                runtime,
                operationName,
                timeout ?? DefaultTransactionTimeout,
                async token =>
                {
                    if (!runtime.IsOnline)
                    {
                        throw new InvalidOperationException(
                            $"网关未连接:{id}"
                        );
                    }

                    return await transaction(
                        runtime.Client,
                        token
                    );
                },
                cancellationToken
            );
        }


        /// <summary>
        /// 连接网关。重复连接在取得锁后会再次检查状态。
        /// </summary>
        public async Task ConnectAsync(
            string id,
            CancellationToken cancellationToken = default)
        {
            GatewayRuntime? runtime =
                GetRuntime(id);

            if (runtime == null)
            {
                logService.Warn(
                    $"不存在运行网关:{id}"
                );

                return;
            }


            try
            {
                await ExecuteWithGatewayLockAsync(
                    runtime,
                    "连接",
                    DefaultConnectionTimeout,
                    async token =>
                    {
                        if (runtime.IsOnline)
                        {
                            logService.Warn(
                                $"网关已连接，忽略重复连接:{id}"
                            );

                            return true;
                        }


                        await runtime.Client
                            .ConnectAsync(token);

                        UpdateOnlineState(
                            runtime,
                            true
                        );

                        logService.Info(
                            $"网关连接成功:{id}"
                        );

                        return true;
                    },
                    cancellationToken
                );
            }
            catch
            {
                UpdateOnlineState(
                    runtime,
                    false
                );
            }
        }


        /// <summary>
        /// 断开网关。断开会等待当前通信事务完成。
        /// </summary>
        public async Task DisconnectAsync(
            string id,
            CancellationToken cancellationToken = default)
        {
            GatewayRuntime? runtime =
                GetRuntime(id);

            if (runtime == null)
            {
                return;
            }


            try
            {
                await ExecuteWithGatewayLockAsync(
                    runtime,
                    "断开",
                    DefaultLifecycleTimeout,
                    async _ =>
                    {
                        if (!runtime.IsOnline)
                        {
                            return true;
                        }


                        await runtime.Client
                            .DisconnectAsync();

                        UpdateOnlineState(
                            runtime,
                            false
                        );

                        logService.Info(
                            $"网关断开:{id}"
                        );

                        return true;
                    },
                    cancellationToken
                );
            }
            catch
            {
                // 异常已在统一执行入口记录。
            }
        }


        /// <summary>
        /// 删除网关运行对象。
        /// 先停止接受新事务，再等待当前事务完成。
        /// </summary>
        public async Task RemoveGateway(
            string id,
            CancellationToken cancellationToken = default)
        {
            GatewayRuntime? runtime =
                GetRuntime(id);

            if (runtime == null
                || !runtime.TryBeginStopping())
            {
                return;
            }


            runtimes.TryRemove(
                id,
                out _
            );


            try
            {
                await ExecuteWithGatewayLockAsync(
                    runtime,
                    "删除",
                    DefaultLifecycleTimeout,
                    async _ =>
                    {
                        await runtime.Client
                            .DisconnectAsync();

                        UpdateOnlineState(
                            runtime,
                            false
                        );

                        logService.Info(
                            $"释放网关资源:{id}"
                        );

                        return true;
                    },
                    cancellationToken,
                    allowWhenStopping: true,
                    observeServiceLifetime: false
                );
            }
            catch
            {
                // 异常已在统一执行入口记录。
            }
        }


        /// <summary>
        /// 程序关闭时并行停止所有网关。
        /// 每个网关仍使用自己的通信锁。
        /// </summary>
        public async Task ShutdownAsync(
            CancellationToken cancellationToken = default)
        {
            serviceLifetimeSource.Cancel();

            string[] gatewayIds =
                runtimes.Keys.ToArray();

            Task[] shutdownTasks =
                gatewayIds
                    .Select(id => RemoveGateway(
                        id,
                        cancellationToken
                    ))
                    .ToArray();

            await Task.WhenAll(shutdownTasks);
        }


        /// <summary>
        /// 网关级统一异步锁入口。
        /// 超时覆盖等待锁和执行事务的总时间。
        /// </summary>
        private async Task<TResult> ExecuteWithGatewayLockAsync<TResult>(
            GatewayRuntime runtime,
            string operationName,
            TimeSpan timeout,
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken,
            bool allowWhenStopping = false,
            bool observeServiceLifetime = true)
        {
            if (timeout <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timeout),
                    "通信超时时间必须大于0"
                );
            }


            using CancellationTokenSource timeoutSource =
                observeServiceLifetime
                    ? CancellationTokenSource.CreateLinkedTokenSource(
                        cancellationToken,
                        serviceLifetimeSource.Token
                    )
                    : CancellationTokenSource.CreateLinkedTokenSource(
                        cancellationToken
                    );

            timeoutSource.CancelAfter(timeout);

            bool lockTaken = false;


            try
            {
                await runtime.CommunicationSemaphore
                    .WaitAsync(timeoutSource.Token);

                lockTaken = true;


                if (runtime.IsStopping
                    && !allowWhenStopping)
                {
                    throw new InvalidOperationException(
                        $"网关正在停止:{runtime.Setting.Id}"
                    );
                }


                return await operation(
                    timeoutSource.Token
                );
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                logService.Warn(
                    $"网关通信已取消:{runtime.Setting.Id} 操作:{operationName}"
                );

                throw;
            }
            catch (OperationCanceledException)
                when (observeServiceLifetime
                    && serviceLifetimeSource.IsCancellationRequested)
            {
                logService.Warn(
                    $"网关通信因程序关闭而取消:{runtime.Setting.Id} "
                    + $"操作:{operationName}"
                );

                throw;
            }
            catch (OperationCanceledException ex)
            {
                string message =
                    $"网关通信超时:{runtime.Setting.Id} "
                    + $"操作:{operationName} 超时:{timeout.TotalSeconds:0.###}秒";

                logService.Error(message);

                throw new TimeoutException(
                    message,
                    ex
                );
            }
            catch (Exception ex)
            {
                logService.Error(
                    $"网关通信异常:{runtime.Setting.Id} "
                    + $"操作:{operationName} {ex.Message}"
                );

                throw;
            }
            finally
            {
                if (lockTaken)
                {
                    runtime.CommunicationSemaphore.Release();
                }
            }
        }
    }
}
