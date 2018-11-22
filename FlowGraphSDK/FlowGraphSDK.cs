using System;
using System.Net;
using WebSocketSharp;
using WebSocketSharp.Server;
using Service;
using System.Collections.Generic;

public delegate void DebugHandler(string debugOperation, Dictionary<string, List<string>> breakpoints);
public delegate void EditorExitHandler();


public class FlowGraphSDK
{
    internal static DebugHandler DebugCallback { get; private set; }
    internal static EditorExitHandler EditorExitCallback { get; private set; }

    // 节点描述
    public static string NodesDescriptor
    {
        get;
        private set;
    }

    // 流图描述
    public static string GraphsDirectory
    {
        get;
        private set;
    }
    // websocketserver
    private static WebSocketServer _SERVER = null;

    /// <summary>
    /// 注册节点描述
    /// </summary>
    /// <param name="nodesDescriptor">所有节点的描述json字符串</param>
    public static void RegisterNodes(string nodesDescriptor)
    {
        NodesDescriptor = nodesDescriptor;
    }

    /// <summary>
    /// 设置流图目录
    /// </summary>
    /// <param name="graphsDirectory">流图目录</param>
    public static void SetGraphsDirectory(string graphsDirectory)
    {
        GraphsDirectory = graphsDirectory;
    }

    // 设置调试回调
    public static void SetDebugHandler(DebugHandler handler)
    {
        DebugCallback = handler;
    }

    public static void SetEditorExitCallback(EditorExitHandler handler)
    {
        EditorExitCallback = handler;
    }

    // 设置流图调试信息json字符串
    public static void SetGraphDebugInfo(string graphDebugInfo)
    {
        WebSocketServiceHost host;
        _SERVER.WebSocketServices.TryGetServiceHost("/Nodes", out host);
        if (host != null)
        {
            Message msg = new Message
            {
                AppName = "sdk_call_editor",
                ModuleName = "hub_call_editor_mothed",
                ClassName = "sdk_call_client",
                FuctionName = "call_client_node_debug_info",
                Data = new Object[] { graphDebugInfo }
            };
            host.Sessions.Broadcast(msg.ToBytes());
        }
    }

    /// <summary>
    /// 启动sdk
    /// </summary>
    public static void Startup()
    {
        string ip = GetLocalIP();
        _SERVER = new WebSocketServer("ws://" + ip + ":1788");

        // 添加服务
        _SERVER.AddWebSocketService<NodesService>("/Nodes");

        // 开启
        _SERVER.Start();
    }

    private static string GetLocalIP()
    {
        string hostname = Dns.GetHostName();//得到本机名   
        IPHostEntry localhost = Dns.GetHostEntry(hostname);
        IPAddress localaddr = Array.Find<IPAddress>(localhost.AddressList, addr => addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
        return localaddr.ToString();
    }

    /// <summary>
    /// 关闭sdk
    /// </summary>
    public static void Shutdown()
    {
        if (_SERVER != null)
        {
            _SERVER.Stop();
            _SERVER = null;
        }
    }
}
