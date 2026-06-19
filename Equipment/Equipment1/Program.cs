using NLog;
using Equipment1.Models;
using Equipment1.Services;

namespace Equipment1;

internal class Program
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    static async Task Main(string[] args)
    {
        Console.WriteLine("保管設備１起動");
        Logger.Info("保管設備１起動");

        DeviceController controller = new DeviceController();
        DeviceHttpListener listener = new DeviceHttpListener(controller);

        //サーバーAPI受信処理
        _ = listener.HttpListenerStartAsync();

        //イニシャル処理
        await controller.InitAsync();

        while (true)
        {
            if (controller.CurrentState.LocalAlarmStatus == LocalAlarmStatus.Alarm)
            {
                //ALARM処理
                await controller.RunAlarmProcessAsync();
            }
            else if (controller.CurrentState.OperatingStatus == OperatingStatus.Busy)
            {
                //RUN処理
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
