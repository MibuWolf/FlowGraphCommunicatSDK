using System;
using WebSocketSharp;
using WebSocketSharp.Server;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Text;
using System.Reflection;
using System.IO;
using Newtonsoft.Json.Linq;

/// <summary>
/// @author confiner
/// @desc   流图节点服务
/// </summary>

namespace Service
{
    public class NodesService : WebSocketBehavior
    {
        private static readonly string _SUFFIX = ".fg";
        private int _offset = 0;
        private byte[] _data = null;
        private Dictionary<string, Object> _grahps = null;

        protected override void OnMessage(MessageEventArgs e)
        {
            if (e.IsBinary)
            {
                Message msg = ProcessMessage(e.RawData);
                if (msg != null)
                {
                    if (msg.AppName == "client_call_sdk" &&
                        msg.ModuleName == "hub_call_hub_mothed" &&
                        msg.ClassName == "flowGraphData" &&
                        msg.FuctionName == "flowGraphInfo")
                    {
                        string graphJsonStr = msg.Data[0].ToString();
                        Object graph = JsonConvert.DeserializeObject(graphJsonStr);
                        JObject obj = JObject.Parse(graphJsonStr);

                        string graphName = obj["name"].ToString();
                        graphName = graphName.Substring(1, graphName.Length - 2);

                        if (_grahps.ContainsKey(graphName))
                        {
                            _grahps.Remove(graphName);
                        }

                        _grahps.Add(graphName, graphJsonStr);

                        SyncGrahps();
                        string folderFullName = @"" + FlowGraphSDK.GraphsDirectory;
                        SaveGraph(folderFullName + "\\" + graphName + _SUFFIX, graph);
                    }
                    else if (msg.AppName == "client_call_sdk" &&
                        msg.ModuleName == "hub_call_hub_mothed" &&
                        msg.ClassName == "flowGraphData" &&
                        msg.FuctionName == "flowGraphDebugInfo")
                    {
                        string debugInfoJsonStr = msg.Data[0].ToString();
                        JObject obj = JObject.Parse(debugInfoJsonStr);
                        string type = obj["debug_type"].ToString();
                        type = type.Substring(1, type.Length - 2);
                        List<string> nodeIds = null;
                        string graphName = null;
                        Dictionary<string, List<string>> breakpoints = new Dictionary<string, List<string>>();
                        if(obj["breakpoints"] != null)
                        {
                            JArray graphs = obj["breakpoints"].Value<JArray>();
                            if (graphs != null)
                            {
                                foreach (JObject graphObj in graphs)
                                {
                                    foreach (JProperty prop in graphObj.Properties())
                                    {
                                        graphName = prop.Name;
                                        nodeIds = new List<string>();
                                        foreach (string nodeId in prop.Value.Values<string>())
                                        {
                                            nodeIds.Add(nodeId);
                                        }
                                        breakpoints.Add(graphName, nodeIds);
                                    }
                                }
                            }
                        }

                        if (FlowGraphSDK.DebugCallback != null)
                        {
                            FlowGraphSDK.DebugCallback.Invoke(type, breakpoints);
                        }
                    }
                }
            }
        }

        protected override void OnClose(CloseEventArgs e)
        {
            //SaveGraphs();
            if (FlowGraphSDK.EditorExitCallback != null)
                FlowGraphSDK.EditorExitCallback.Invoke();
            base.OnClose(e);
        }

        // 建立连接
        protected override void OnOpen()
        {
            base.OnOpen();
            Send(CreateMessage("sdk_call_editor", "reg_hub_sucess"));

            //发送节点
            if (FlowGraphSDK.NodesDescriptor != null)
            {
                Send(CreateMessage("sdk_call_editor", "hub_call_editor_mothed", "sdk_call_client", "call_client_node", FlowGraphSDK.NodesDescriptor));
            }

            LoadGraphs();
            SyncGrahps();
        }

        private byte[] CreateMessage(string appName, string moduleName, string className = null, string functionName = null, string data = null)
        {
            string temp = data;
            return new Message
            {
                AppName = appName,
                ModuleName = moduleName,
                ClassName = className,
                FuctionName = functionName,
                Data = new Object[] { data }
            }.ToBytes();
        }

        private Message ProcessMessage(byte[] data)
        {
            Message msg = null;
            byte[] bytes = new byte[_offset + data.Length];
            if (_data != null)
                _data.CopyTo(bytes, 0);

            Array.Copy(data, 0, bytes, _offset, data.Length);
            while (bytes.Length > 4)
            {
                int len = bytes[0] | bytes[1] << 8 | bytes[2] << 16 | bytes[3] << 24;
                if (len + 4 > bytes.Length)
                    break;

                Char[] chars = Encoding.UTF8.GetChars(bytes.SubArray<byte>(4, len));
                string jsonStr = new string(chars);
                msg = new Message();
                msg.Initialze(jsonStr);

                if (bytes.Length > len + 4)
                {
                    bytes = bytes.SubArray<byte>(len + 4, bytes.Length - (len + 4));
                }
                else
                {
                    bytes = null;
                    break;
                }
            }

            _data = bytes;
            _offset = bytes == null ? 0 : bytes.Length;

            return msg;
        }

        // 保存单个流图
        private void SaveGraph(string path, Object graph)
        {
            string folderFullName = @"" + FlowGraphSDK.GraphsDirectory;
            DirectoryInfo folder = new DirectoryInfo(folderFullName);
            if (folder.Exists)
            {
                if (!File.Exists(path))
                {
                    FileStream fs = File.Create(path);
                    fs.Close();
                }

                try
                {
                    using (StreamWriter sw = new StreamWriter(path))
                    {
                        try
                        {
                            JsonSerializer serializer = new JsonSerializer();
                            JsonWriter writer = new JsonTextWriter(sw);
                            serializer.Serialize(writer, graph);
                            writer.Close();
                            sw.Close();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message.ToString());
                        }
                    }
                }
                catch (System.IO.IOException e)
                {
                    Console.WriteLine(e.Message.ToString());
                }
                
            }
        }

        // 保存流图
        private void SaveGraphs()
        {
            if (_grahps != null)
            {
                string folderFullName = @"" + FlowGraphSDK.GraphsDirectory;
                foreach (KeyValuePair<string, Object> pair in _grahps)
                {
                    SaveGraph(folderFullName + "\\" + pair.Key + _SUFFIX, pair.Value);
                }
            }
        }

        // 加载流图
        private void LoadGraphs()
        {
            if (_grahps == null)
                _grahps = new Dictionary<string, Object>();

            string folderFullName = @"" + FlowGraphSDK.GraphsDirectory;
            DirectoryInfo floder = new DirectoryInfo(folderFullName);
            foreach (FileInfo nextFile in floder.GetFiles())
            {
                if (File.Exists(nextFile.FullName))
                {
                    using (StreamReader sr = new StreamReader(nextFile.FullName))
                    {
                        try
                        {
                            JsonSerializer serializer = new JsonSerializer();
                            JsonReader reader = new JsonTextReader(sr);
                            Object graph = serializer.Deserialize(reader);
                            int sufixIdx = nextFile.Name.LastIndexOf(".");
                            _grahps.Add(nextFile.Name.Remove(sufixIdx), graph.ToString());
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message.ToString());
                        }
                    }
                }
            }
        }

        // 同步流图
        private void SyncGrahps()
        {
            if (_grahps != null)
            {
                Object[] grahps = new Object[_grahps.Count];
                int i = 0;
                foreach (KeyValuePair<string, Object> pair in _grahps)
                {
                    grahps[i] = pair.Value;
                    ++i;
                }

                string grahpsDescriptor = JsonConvert.SerializeObject(grahps);
                // 发送流图
                if (grahpsDescriptor != null)
                {
                    Send(CreateMessage("sdk_call_editor", "hub_call_editor_mothed", "sdk_call_client", "call_client_graph", grahpsDescriptor));
                }
            }
        }
    }
}
