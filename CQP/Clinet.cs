﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp;

namespace CQP
{
    public class Clinet
    {
        public class ApiResult
        {
            /// <summary>
            /// 调用是否失败
            /// </summary>
            public bool Fail { get; set; } = false;
            /// <summary>
            /// 调用返回的结果
            /// </summary>
            public object Data { get; set; }
            /// <summary>
            /// 错误消息
            /// </summary>
            public string Msg { get; set; } = "ok";
            /// <summary>
            /// 调用逻辑的类别
            /// </summary>
            public string Type { get; set; }
            public JObject json { get; set; }
        }
        public WebSocket ServerConnection;
        public static Clinet Instance;
        public Clinet()
        {
            Instance = this;
            Directory.CreateDirectory("logs/cqp");
            File.AppendAllLines("logs/cqp/log.txt", new string[] { $"[{DateTime.Now:G}] CQP Client Started" });
            ServerConnection = new($"ws://127.0.0.1:{ConfigHelper.GetConfig<int>("Ws_ServerPort")}/amn");
            ServerConnection.OnOpen += ServerConnection_OnOpen;
            ServerConnection.OnMessage += ServerConnection_OnMessage;
            ServerConnection.OnClose += ServerConnection_OnClose;
            ServerConnection.Connect();
        }

        private void ServerConnection_OnClose(object sender, CloseEventArgs e)
        {
            Thread.Sleep(3000);
            File.AppendAllLines("logs/cqp/log.txt", new string[] { $"[{DateTime.Now:G}] 与服务器连接断开..." });
            ServerConnection = new($"ws://localhost:{ConfigHelper.GetConfig<int>("Ws_ServerPort")}/amn");
            ServerConnection.OnOpen += ServerConnection_OnOpen;
            ServerConnection.OnMessage += ServerConnection_OnMessage;
            ServerConnection.OnClose += ServerConnection_OnClose;
            ServerConnection.Connect();
        }

        private void ServerConnection_OnMessage(object sender, MessageEventArgs e)
        {
            if (e.Data.Contains("AddLog")) return;
            if (ApiQueue.Count != 0)
            {
                ApiQueue.Peek().result = e.Data;
            }
        }

        private void ServerConnection_OnOpen(object sender, EventArgs e)
        {
            File.AppendAllLines("logs/cqp/log.txt", new string[] { $"[{DateTime.Now:G}] 连接成功" });
            Send(WsServerFunction.Info, new { role = WsClientType.CQP, key = ConfigHelper.GetConfig<string>("WsServer_Key") }, false);
        }
        Queue<QueueObject> ApiQueue = new();
        class QueueObject
        {
            public string request { get; set; }
            public string result { get; set; } = "";
        }
        public ApiResult Send(WsServerFunction type, object data, bool queue = true)
        {
            GC.Collect();
            if (ServerConnection.ReadyState == WebSocketState.Open)
            {
                try
                {
                    if (!queue)
                    {
                        ServerConnection.Send(new { type, data }.ToJson());
                        return null;
                    }
                    QueueObject queueObject = new() { request = new { type, data }.ToJson() };
                    ApiQueue.Enqueue(queueObject);
                    if (ApiQueue.Count == 1)
                        ServerConnection.Send(queueObject.request);
                    // 超时脱出
                    int timoutCountMax = ConfigHelper.GetConfig("API_Timeout", 6000);
                    int timoutCount = 0;
                    while (queueObject.result == "")
                    {
                        if (timoutCount > timoutCountMax)
                        {
                            queueObject.result = new { Fail = true }.ToJson();
                        }
                        Thread.Sleep(10);
                        if (ApiQueue.Peek() == queueObject)
                        {
                            timoutCount++; // 只有当前函数执行时才计时，防止所有排队函数均超时
                        }
                    }
                    ApiQueue.Dequeue();
                    if (ApiQueue.Count != 0)
                        ServerConnection.Send(ApiQueue.Peek().request);
                    var r = JsonConvert.DeserializeObject<ApiResult>(queueObject.result);
                    if (r.Fail is false)
                        r.json = JObject.FromObject(r.Data);
                    return r;
                }
                catch (Exception e)
                {
                    File.AppendAllLines("logs/cqp/log.txt", new string[] { $"[{DateTime.Now:G}] 异常: {e.Message}" });
                    return new ApiResult { Fail = true };
                }
            }
            return null;
        }
    }
}
