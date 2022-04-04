using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LogProbe
{
    internal class Program
    {
        static List<int> includeList;
        static List<int> excludeList;
        static IPEndPoint logServer;
        static TcpClient tcpClient = null;
        static NetworkStream stream = null;
        static byte[] buffer = null;
        static int bufferSize = 1024;
        static bool formatLogLines = true;
        static bool isShuttingDown = false;

        static void ShowUsage()
        {
            Console.Write("Usage: [IP:Port] <--noformat> <--bufsize XXXX> <--includeXXX>... <--excludeXXX>...");
        }

        static void SetStatus(string status)
        {
            Console.Title = $"LogProbe - {status}";
        }

        static void PrintStatusText(string message)
        {
            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ForegroundColor = color;
        }

        static void PrintError(string message)
        {
            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ForegroundColor = color;
        }

        static void PrintLogLine(int tag, string message)
        {
            string lineTag;
            if (formatLogLines)
            {
                switch (tag)
                {
                    case 0:
                        lineTag = "LOG";
                        break;
                    case 1:
                        lineTag = "GAMELOG";
                        break;
                    case 2:
                        lineTag = "RENLOG";
                        break;
                    case 3:
                        lineTag = "CONSOLE";
                        break;
                    default:
                        lineTag = $"CUSTOM({tag:D3})";
                        break;
                }
            }
            else
            {
                lineTag = tag.ToString("D3");
            }

            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"[{lineTag}] ");
            Console.ForegroundColor = color;
            Console.WriteLine(message.Trim('\r', '\n').Trim());
        }

        static int Main(string[] args)
        {
            Console.WriteLine("LogProbe Utility - by Unstoppable");
            SetStatus("Initializing");

            Console.CancelKeyPress += (sender, e) => 
            { 
                isShuttingDown = true;
                e.Cancel = true;
            };
            
            if (args.Length == 0)
            {
                ShowUsage();
                return 3;
            }

            var ipport = args[0].Split(':');
            if (ipport.Length != 2)
            {
                ShowUsage();
                return 4;
            }

            if (!IPAddress.TryParse(ipport[0], out IPAddress addr))
            {
                ShowUsage();
                return 5;
            }

            if (!ushort.TryParse(ipport[1], out ushort port))
            {
                ShowUsage();
                return 6;
            }

            logServer = new IPEndPoint(addr, port);

            includeList = new List<int>();
            excludeList = new List<int>();
            for (int i = 1; i < args.Length; ++i)
            {
                var arg = args[i];

                if (arg.Equals("--bufsize") && args.Length > i + 1)
                {
                    if (int.TryParse(args[++i], out int newBufSize))
                    {
                        bufferSize = newBufSize;
                    }
                    else
                    {
                        PrintError("Unable to parse value for switch --bufsize.");
                    }
                }
                else if (arg.Equals("--noformat"))
                {
                    formatLogLines = false;
                }
                else if (arg.StartsWith("--include") && int.TryParse(arg.Substring(9), out int include))
                {
                    includeList.Add(include);
                }
                else if (arg.StartsWith("--exclude") && int.TryParse(arg.Substring(9), out int exclude))
                {
                    excludeList.Add(exclude);
                }
                else
                {
                    PrintError($"Unrecognized switch: {arg}");
                }
            }

            try
            {
                PrintStatusText("Connecting to log server...");
                SetStatus("Connecting");

                tcpClient = new TcpClient
                {
                    ReceiveBufferSize = bufferSize
                };
                tcpClient.Connect(logServer);
                stream = tcpClient.GetStream();

                PrintStatusText("Connected successfully.");
                SetStatus("Connected");
            }
            catch (Exception ex)
            {
                PrintError($"Failed to create socket: {ex.Message}");
                return 7;
            }

            buffer = new byte[bufferSize];

            stream.BeginRead(buffer, 0, bufferSize, SocketCallback, null);

            while (!isShuttingDown)
            {
                Thread.Sleep(10); // To prevent high CPU usage
            }

            PrintStatusText("Shutting down...");
            SetStatus("Shutting down");
            tcpClient.Dispose();
            return 0;
        }

        static void SocketCallback(IAsyncResult result)
        {
            int read;

            try
            {
                read = stream.EndRead(result);
            }
            catch (Exception ex)
            {
                if (!(ex is ObjectDisposedException))
                {
                    PrintError($"Exception while reading: {ex.Message}");
                }
                else
                {
                    Console.WriteLine("Socket has been shut down.");
                }

                isShuttingDown = true;
                return;
            }

            var received = Encoding.Default.GetString(buffer, 0, read);

            while (tcpClient.Available > 0) // our buffer was too small to read entire message.
            {
                read = stream.Read(buffer, 0, bufferSize);
                received += Encoding.Default.GetString(buffer, 0, read);
            }

            foreach (var line in received.Split('\0'))
            {
                if (!string.IsNullOrEmpty(line) && line.Length > 3)
                {
                    int tag;

                    if (!int.TryParse(line.Remove(3), out tag))
                    {
                        PrintError($"Failed to parse tag for line \"{line}\"");
                        continue;
                    }

                    if (includeList.Count > 0)
                    {
                        if (includeList.Contains(tag))
                        {
                            PrintLogLine(tag, line.Substring(3));
                        }
                    }
                    else if (!excludeList.Contains(tag))
                    {
                        PrintLogLine(tag, line.Substring(3));
                    }
                }
            }

            stream.BeginRead(buffer, 0, bufferSize, SocketCallback, null);
        }
    }
}
