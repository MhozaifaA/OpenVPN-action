using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;
using System.IO;
using System.Threading.Tasks;
using OpenVPN_action.Enums;
using System.Linq;
using System.Threading;

namespace OpenVPN_action
{
    public class OpenVPNAction : OpenVPN 
    {

        private Process process;
        private ProcessStartInfo ProcessStartInfo;
        private TcpClient tcpClient;
        private StreamWriter streamWriter;
        private StreamReader streamReader;
        private Thread threadCommandReader;

        private volatile bool LoopReader = false;

        private string Host { get; set; }
        private int Port { get; set; }
        private int ProcessId { get; set; }

        private readonly string[] ConnectedCommands;

        public int NumCycleOfReconnecting = 10;
        private int _NumCycleOfReconnecting = 10;

        private const int NumTryConnectPort = 2;
        private int _NumTryConnectPort = 2;

        public OpenVPNAction()
        {
            RemoveRefuseRun();
        }
        public OpenVPNAction(string username, string password) : this()
        {
            base.OpenVPNInfo.SetUserName(username);
            base.OpenVPNInfo.SetPassword(password);

            ConnectedCommands = new string[] { "log on all","state on","echo all on",
            "hold off", "hold release" ,
            $"username 'Auth' \"{base.OpenVPNInfo.GetUserName()}\"" , $"password 'Auth' \"{base.OpenVPNInfo.GetPassword()}\"" };
        }

        public OpenVPNAction(string openvpnPath, string ovpnPath, string username, string password) : this(username, password)
        {
            base.OpenVPNInfo.SetOpenVPNServicePath(openvpnPath);
            base.OpenVPNInfo.SetOVPNFilePath(ovpnPath);
            base.OpenVPNInfo.SetUserName(username);
            base.OpenVPNInfo.SetPassword(password);

            Configuration(base.OpenVPNInfo.GetOpenVPNServicePath(), base.OpenVPNInfo.GetOVPNFilePath());
        }


        public async Task LunchAsync(OpenVPNInfo openVPNInfo)
        {
            if (base.OpenVPNInfo.GetConnectionState() == ConnectionStates.UnConfigured)
                throw new Exception("missed call" + nameof(Configuration));

            Start();
            await Connect();
        }

       
        public void Configuration(string openvpnPath, string ovpnPath, string host = null, int port = 0)
        {
            //if (RefuseRun()) return;
            
            base.OpenVPNInfo.SetOpenVPNServicePath(openvpnPath);
            base.OpenVPNInfo.SetOVPNFilePath(ovpnPath);

            Host = host == null? System.Net.IPAddress.Loopback.ToString(): host; //"127.0.0.1"
            Port = port == 0? FreeTcpPort(port): port; //11195 tcp port

            _NumCycleOfReconnecting = NumCycleOfReconnecting;

            ProcessStartInfo = new ProcessStartInfo()
            {
                FileName = base.OpenVPNInfo.GetOpenVPNServicePath(),
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                UseShellExecute = true,
                Verb = "runas",
                Arguments =
                            " --config \"" + base.OpenVPNInfo.GetOVPNFilePath() + "\"" +
                            " --management " + Host + " " + (Port).ToString(CultureInfo.InvariantCulture) +
                            " --management-query-passwords" +
                            " --management-hold" +
                            " --management-signal" +
                            " --management-forget-disconnect" +
                            " --auth-retry interact",
            };

             
            base.OpenVPNInfo.SetConnectionState(ConnectionStates.Configured);
         
        }

        public void Start()
        {
            if (RefuseRun()) return;

            if (base.OpenVPNInfo.GetConnectionState() == ConnectionStates.UnConfigured)
                throw new Exception("missed call" + nameof(Configuration));

            CheckCloseLastProcess();

            process = new Process()
            {
                StartInfo = ProcessStartInfo,
                EnableRaisingEvents = true,
            };
            process.Exited += (o, e) => {
                LoopReader = false;
                base.OpenVPNInfo.SetConnectionState(ConnectionStates.Disconnect);
                OnDisConnectedChanged();
            };

            if (!ConnectToPort())
                return;

            streamWriter = new StreamWriter(tcpClient.GetStream());
            streamReader = new StreamReader(tcpClient.GetStream());

            threadCommandReader = new Thread(new ThreadStart(async () => await CommandReaderLayer()));
            threadCommandReader.Name = "Thread Command Reader OPENVPN";

            base.OpenVPNInfo.SetConnectionState(ConnectionStates.Ready);
        }

        public async Task Connect()
        {
            if (RefuseRun()) return;

            if (base.OpenVPNInfo.GetConnectionState() == ConnectionStates.Configured)
                throw new Exception("missed call" + nameof(Start));

            base.OpenVPNInfo.SetConnectionState(ConnectionStates.Connecting);

            LoopReader = true;
            threadCommandReader.Start();

            OnConnectingChanged();

            foreach (var command in ConnectedCommands)
            {
                await streamWriter.WriteLineAsync(command);
                await streamWriter.FlushAsync();
                await Task.Delay(100);
            }
           
        }

        public async Task Disconnect()
        {
            if (RefuseRun()) return;

            if (streamWriter == null || streamReader == null || tcpClient == null)
            {
                try
                {
                    if (process != null)
                        if (!process.HasExited)
                            process.Kill();
                }
                catch (InvalidOperationException)
                {
                }
                return;
            }
                 //throw new Exception("missed call" + nameof(Connect));

            if (base.OpenVPNInfo.GetConnectionState() == ConnectionStates.Disconnect)
            {
                try
                {
                    if (process != null)
                        if (!process.HasExited)
                            process.Kill();
                }
                catch (InvalidOperationException)
                {
                }
                return;
            }
                 //throw new Exception("missed call" + nameof(Connect));

            await streamWriter.WriteLineAsync("signal SIGTERM");
            await streamWriter.WriteLineAsync("signal SIGTERM");
            await streamWriter.FlushAsync();
            await Task.Delay(200);

            LoopReader = false;
            if (threadCommandReader != null && !Thread.CurrentThread.Equals(threadCommandReader))
            {
                threadCommandReader.Interrupt(); //Abort Interrupt
            }

            streamReader.Close();
            streamWriter.Close();
            tcpClient.Close();

            await Task.Delay(1000);

            try
            {
                if (process != null)
                    if (!process.HasExited)
                        process.Kill();
            }
            catch (InvalidOperationException)
            {
            }

            base.OpenVPNInfo.SetConnectionState(ConnectionStates.Disconnect);
        }

        public async Task Reconnect()
        {
            if (RefuseRun()) return;

            await Disconnect();
            if(_NumCycleOfReconnecting-- != 0)
            {
                Start();
                await Connect();
            }else
                OnEndCycleChanged();
        }


      


        private async Task CommandReaderLayer()
        {
            try
            {
                string line = string.Empty;
                while (LoopReader)
                {
                    if (streamReader is null)
                        break;

                    try
                    {
                        if (LoopReader && (threadCommandReader.ThreadState < System.Threading.ThreadState.AbortRequested))
                            line = streamReader.ReadLine();
                        else break;
                    }
                    catch (Exception e) when(e is IOException || e is ThreadAbortException)
                    {
                        break;
                    }


                    if (string.IsNullOrEmpty(line))
                        continue;

                    //if(line.Equals(">PASSWORD:Need 'Auth' username/password"))
                    //{
                    //    // some divece long to verify
                    //}

                    string[] STATE = line.Split(',').Select(s => s.Trim()).ToArray();
                    if (STATE.Length != 8)
                        continue;

                    if (STATE[1].Equals("CONNECTED", StringComparison.OrdinalIgnoreCase) &&
                               STATE[2].Equals("SUCCESS", StringComparison.OrdinalIgnoreCase))
                    {
                        base.OpenVPNInfo.SetConnectionState(ConnectionStates.Connected);

                        base.OpenVPNInfo.SetPublic_IP_Address(STATE[4].Trim());

                        OnConnectedChanged();

                    }
                    else
                    if (STATE[1].Equals("RECONNECTING", StringComparison.OrdinalIgnoreCase))
                    {
                        base.OpenVPNInfo.SetConnectionState(ConnectionStates.Reconnect);

                        OnReconnectedChanged();
                        await Reconnect();

                        
                        //lock(this)
                        //{
                        //    Task.Run(async ()=> {
                        //        await Disconnect();
                        //        await Connect();
                        //    });
                        //}
                    }
                    else
                    if (STATE[1].Equals("EXITING", StringComparison.OrdinalIgnoreCase))
                    {
                        base.OpenVPNInfo.SetConnectionState(ConnectionStates.Disconnect);

                        OnDisConnectedChanged();
                    }

                    //> STATE:1614891715,RECONNECTING,auth - failure,,,,,
                    // Need password(s) from management interface, waiting...
                }
            }
            catch (Exception) //ObjectDisposedException
            {
                await Disconnect();
                base.OpenVPNInfo.SetConnectionState(ConnectionStates.Corrupted);
                OnCorruptedChanged();
            }
            finally
            {
                //out
            }
        }

        private int FreeTcpPort(int firstport)
        {
            if (_NumTryConnectPort == NumTryConnectPort)  return firstport;

            TcpListener listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
            listener.Start();
            int port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private bool RefuseRun()
        {
            if(_NumTryConnectPort==0)
            _NumTryConnectPort = NumTryConnectPort;
            return base.OpenVPNInfo.GetConnectionState() == ConnectionStates.Corrupted;
        }

        private bool ConnectToPort()
        {
            if (--_NumTryConnectPort == 0) {
                base.OpenVPNInfo.SetConnectionState(ConnectionStates.Corrupted);
                OnCorruptedChanged();
                return false;
            }
            try
            {
                process.Start();
                ProcessId = process.Id;

                tcpClient = new TcpClient();
                tcpClient.Connect(Host, Port);
             
                _NumTryConnectPort = NumTryConnectPort;

                return true;
            }
            catch (Exception) //  no connection could be made because the target machine actively refused it
            {
                CheckCloseLastProcess();
                Configuration(base.OpenVPNInfo.GetOpenVPNServicePath(), base.OpenVPNInfo.GetOVPNFilePath());
                Start();
                return false;
            }
        }

        private void CheckCloseLastProcess()
        {
            if (ProcessId == 0) return;

            Process _process;
            try
            {
                _process = Process.GetProcessById(ProcessId);
            }
            catch (ArgumentException) //The process specified by the processId parameter is not running
            {
                return;
            }
            //System.InvalidOperationException: No process is associated with this object.
            try
            {
                if (_process != null && !_process.HasExited)
                    _process.Kill();
            }
            catch (InvalidOperationException)
            {

            }
        }


        public void RemoveRefuseRun()
                 => base.OpenVPNInfo.SetConnectionState(ConnectionStates.UnConfigured);


        #region -   Dispose   -

        private bool disposed = false;
        ~OpenVPNAction()
        {
            Dispose(false);
        }
        public new void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual new void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    base.Dispose();

                    
                    if (threadCommandReader !=null)
                        if (threadCommandReader.IsAlive)
                            threadCommandReader.Abort();
                    if (streamReader != null)
                        streamReader.Dispose();
                    if (streamWriter != null)
                        streamWriter.Dispose();
                    if (tcpClient != null)
                        tcpClient.Dispose();
                    if (process != null)
                    {
                        try
                        {
                            if (!process.HasExited)
                                process.Kill();
                            process.Dispose();
                        }
                        catch (InvalidOperationException)
                        {
                            process = null;
                        }
                    }

                }
                CheckCloseLastProcess();
                disposed = true;
            }
        }
        #endregion

    }
}
