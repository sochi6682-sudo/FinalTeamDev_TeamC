using NLog;
using Equipment1.Models;
using Equipment1.Controllers;
using Equipment1.Services;

namespace Equipment1;

internal class Program
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    static async Task Main(string[] args)
    {

        DeviceController controller = new DeviceController();
        DeviceHttpListener listener = new DeviceHttpListener(controller);

        Logger.Info("========== 保管設備起動 ==========");

        //サーバーAPI受信処理
        Task listenerTask = listener.HttpListenerStartAsync();

        //イニシャル処理
        await controller.InitAsync();

        while (true)
        {
            if (controller.CurrentState.LocalAlarmStatus == LocalAlarmStatus.Alarm)
            {
                //ALARM処理
                await controller.RunIdleLoopAsync();
                await controller.RunAlarmProcessAsync();
            }
            else if (controller.CurrentState.OperatingStatus == OperatingStatus.Busy)
            {
                //BUSY処理
                await controller.RunBusyProcessAsync();
            }
            else
            {
                //IDLE処理
                await controller.RunIdleLoopAsync();
            }
        }
    }
}
