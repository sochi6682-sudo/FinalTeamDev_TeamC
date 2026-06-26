using Equipment2.Controllers;
using Equipment2.Models;
using NLog;
using System.Net;
using System.Text;

namespace Equipment2.Services;

public class DeviceHttpListener
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly HttpListener _listener;
    private readonly DeviceController _controller;

    public DeviceHttpListener(DeviceController controller)
    {
        _controller = controller;
        _listener = new HttpListener();

        _listener.Prefixes.Add("http://+:8091/");
    }


    // １）受信待ち開始　
    //-------------------------------------------------------------------------------
    public async Task HttpListenerStartAsync()
    {
        try　//リスナー起動失敗対策
        {
            _listener.Start();
            Logger.Info("設備HTTPリスナー開始");
        }
        catch (Exception ex)
        {
            Logger.Fatal(ex, "設備HTTPリスナー開始失敗");
            return;
        }

        while (true)
        {
            try　//リスナー停止・エラー対策
            {
                HttpListenerContext context = await _listener.GetContextAsync();

            _ = Task.Run(() => HandleRequestAsync(context));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "HTTP受信待ちで例外発生");
            }
        }
    }


    //２）処理振り分け
    //-------------------------------------------------------------------------------
    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        try　//不正リクエスト受信・各受信処理例外対策
        {
            string path = context.Request.Url?.AbsolutePath ?? "";
            string method = context.Request.HttpMethod;

            Logger.Info($"HTTP受信 Method={method} Path={path}");

            // ３－１）EQP状態取得 GET受信
            if (method == "GET" && path == "/api/shelf-system/status")
            {
                await HandleStatusGetAsync(context);
                return;
            }

            // ３－２）払出可能 POST受信
            if (method == "POST" && path == "/api/shelf-system/unload")
            {
                await HandleUnloadPostAsync(context);
                return;
            }

            //４）405返送
            Logger.Warn($"未定義API受信 Method={method} Path={path}");
            await WriteResponseAsync(context, 405, "Method Not Allowed");

        }
        catch (Exception ex)
        {
            //４）500返送
            Logger.Error(ex, "HTTP受信処理で例外発生");

            try　//500返送例外対策
            {
                await WriteResponseAsync(context, 500, "Internal Server Error");
            }
            catch (Exception writeEx)
            {
                Logger.Error(writeEx, "500応答返送失敗");
            }
        }
    }


    // ３－１）EQP状態取得 GET受信処理
    //-------------------------------------------------------------------------------
    private async Task HandleStatusGetAsync(HttpListenerContext context)
    {
        Logger.Info("EQP状態取得GET受信");

        //現在の状態を取得してJSON形式に変換
        StateReport report = _controller.GetStateReport();
        string json = System.Text.Json.JsonSerializer.Serialize(report);

        //４）200で状態JSONを返送
        await WriteResponseAsync(context,200,json,"application/json");

    }


    // ３－２）払出完了 POST受信処理
    //-------------------------------------------------------------------------------
    private async Task HandleUnloadPostAsync(HttpListenerContext context)
    {
        Logger.Info("払出完了POST受信");

        //出庫可の状態へ移行
        _controller.SetRetrieveAvailable();

        //４）200返送
        await WriteResponseAsync(context, 200, "OK");

    }


    //４）HTTPレスポンス返送処理
    //-------------------------------------------------------------------------------
    private async Task WriteResponseAsync(
        HttpListenerContext context,
        int statusCode,
        string content,
        string contentType = "text/plain")
    {
        try　//送信途中の例外対策
        {
            byte[] buffer = Encoding.UTF8.GetBytes(content);

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = contentType;
            context.Response.ContentLength64 = buffer.Length;

            await context.Response.OutputStream.WriteAsync(buffer);

        }
        catch (Exception ex)
        {
            Logger.Error(ex, "HTTPレスポンス返却失敗");
        }
        finally
        {
            context.Response.Close();
        }
    }
}

