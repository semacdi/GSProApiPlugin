using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GSProApiPlugin
{    
    public class GSProApi
    {
        private string _hostName = "127.0.0.1";
        private int _port = 921;

        // OnConnected
        // OnDisconnected
        // OnConnectionError

        protected Action<GSResponse> OnResponseInvoker = resp => { };
        protected Action<GSProStatus> OnConnectionStateInvoker = resp => { };


        private CancellationTokenSource _ct;

        private Socket _socket;

        /// <summary>
        /// 
        /// </summary>
        public GSProApi()
        {
            
        }

        /// <summary>
        /// 
        /// </summary>
        public event Action<GSResponse> OnResponse
        {
            add => this.OnResponseInvoker += value;
            remove => this.OnResponseInvoker -= value;
        }

        /// <summary>
        /// 
        /// </summary>
        public event Action<GSProStatus> OnConnectionState
        {
            add => this.OnConnectionStateInvoker += value;
            remove => this.OnConnectionStateInvoker -= value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="shot"></param>
        public void SendShot(GSShot shot)
        {
            var json = JsonSerializer.Serialize(shot);

            var bytes = Encoding.ASCII.GetBytes(json);

            try
            {
                var totalSent = 0;
                while (totalSent < bytes.Length)
                {
                    var bytesSent = _socket.Send(bytes, totalSent, bytes.Length - totalSent, SocketFlags.None);
                    totalSent += bytesSent;
                }
            }
            catch(Exception ex)
            {
                // Send Again?
                // Reconnect?
                // Alert UI?
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hostname"></param>
        /// <param name="port"></param>
        public GSProApi(string hostname, int port)
        {
            _hostName = hostname;
            _port = port;            
        }

        /// <summary>
        /// 
        /// </summary>
        private void ReadData()
        {
            while (_ct != null && !_ct.IsCancellationRequested)
            {
                try
                {
                    var line = "";

                    var bytes = new byte[2048];
                    var bytesRead = _socket.Receive(bytes);
                    if (bytesRead > 0)
                    {
                        line += Encoding.ASCII.GetString(bytes, 0, bytesRead);

                        var startCount = line.Count(x => x == '{');
                        var endCount = line.Count(x => x == '}');
                        if (startCount == endCount)
                        {
                            // Single Command
                            var cmd = JsonSerializer.Deserialize<GSResponse>(line);
                            var callback = OnResponseInvoker;
                            if (callback != null)
                            {
                                try
                                {
                                    callback(cmd);
                                }
                                catch(Exception ex)
                                {
                                    // Ignore client errors
                                }
                            }

                            //
                            line = "";
                        }
                        else if (startCount > endCount)
                        {
                            // Keep reading
                        }
                    }
                }
                catch(Exception ex)
                {
                    System.Threading.Thread.Sleep(5 * 1000);
                    // What happened?
                    // Try to Reconnect?
                    // Alert clients?

                    Disconnect();
                }

            }

        }

        /// <summary>
        /// 
        /// </summary>
        public bool Connect()
        {
            if (_socket != null) return true;

            _ct = new CancellationTokenSource();

            // Start Socket
            //IPHostEntry ipHost = Dns.GetHostEntry(_hostName);
            //IPAddress ipAddr = ipHost.AddressList[0];
            var ipAddr = IPAddress.Parse(_hostName);
            IPEndPoint endPoint = new IPEndPoint(ipAddr, _port);

            // 
            _socket = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                // Connect
                _socket.Connect(endPoint);

                // Start Reading
                var t = new Task(ReadData);
                t.Start();

                OnConnectionStateInvoker(new GSProStatus()
                {
                    IsConnected = true,
                    Message = "Connected to " + _hostName + ":" + _port
                });
            }
            catch (Exception ex)
            {
                _socket.Dispose();
                _socket = null;

                // TODO: Publish Error
                OnConnectionStateInvoker(new GSProStatus()
                {
                    IsConnected = false,
                    Message = "Failed to connect to " + _hostName + ":" + _port + ", Reason :" + ex.Message
                });

                return false;
            }

            return true;

        }


        /// <summary>
        /// 
        /// </summary>
        public void Disconnect()
        {
            // Kill Thread
            try
            {
                if (_socket != null)
                {
                    if (_socket.Connected)
                    {
                        _socket.Disconnect(false);
                    }

                    _socket.Dispose();
                    _socket = null;
                }

                if (_ct != null)
                {
                    _ct.Cancel();
                    _ct.Dispose();
                    _ct = null;
                }
            }
            catch (Exception ex)
            {

            }

            OnConnectionStateInvoker(new GSProStatus() { IsConnected = false, Message = "Disconnected" });
        }

    }
}
