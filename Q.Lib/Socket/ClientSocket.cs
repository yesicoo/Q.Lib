using Q.Lib.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Q.Lib.Socket
{
    public class ClientSocket : BaseSocket, IDisposable
    {

        private bool _isDisposed;
        internal TcpClient _tcpClient;
        private Thread _thread;
        private bool _running;
        private int _receives;
        private int _errors;
        private object _errors_lock = new object();
        private object _write_lock = new object();
        private Dictionary<int, SyncReceive> _receiveHandlers = new Dictionary<int, SyncReceive>();
        private object _receiveHandlers_lock = new object();
        private DateTime _lastActive;
        private ManualResetEvent _closeWait;
        public event ClientSocketClosedEventHandler Closed;
        public event ClientSocketReceiveEventHandler Receive;
        public event ClientSocketErrorEventHandler Error;
        private Action<ClientSocket> _connectSuccess = null;
        private string _hostname;
        private int _port;
        private string _clientName;

        private WorkQueue _receiveWQ;
        private WorkQueue _receiveSyncWQ;

        /// <summary>
        /// 重连
        /// </summary>
        public void ReConnect()
        {
            ConnectAndRegist(_hostname, _port, _clientName);
        }
        public void Connect(string hostname, int port, Action<ClientSocket> connectSuccess = null)
        {
            ConnectAndRegist(hostname, port, null, connectSuccess);
        }
        public void ConnectAndRegist(string hostname, int port, string clientName, Action<ClientSocket> connectSuccess = null)
        {
            _connectSuccess = connectSuccess ?? _connectSuccess;
            _clientName = clientName;
            _hostname = hostname;
            _port = port;
            if (this._isDisposed == false && this._running == false)
            {
                this._running = true;
                try
                {
                    this._tcpClient = new TcpClient();
                    this._tcpClient.Connect(hostname, port);
                    this._receiveWQ = new WorkQueue();
                    this._receiveSyncWQ = new WorkQueue();

                }
                catch (Exception ex)
                {
                    this._running = false;
                    this.OnError(ex);
                    this.OnClosed();

                    try
                    {
                        this._tcpClient.Close();
                        QCrontab.RunWithDelay(5, () => { this.ReConnect(); }, "重连");
                    }
                    catch { }

                    return;
                }
                this._receives = 0;
                this._errors = 0;
                this._lastActive = DateTime.Now;
                ManualResetEvent waitWelcome = new ManualResetEvent(false);
                this._thread = new Thread(delegate ()
                {
                    while (this._running)
                    {
                        try
                        {
                            NetworkStream ns = this._tcpClient.GetStream();
                            ns.ReadTimeout = 1000 * 20;
                            if (ns.DataAvailable)
                            {
                                SocketMessager messager = base.Read(ns);
                                if (string.Compare(messager.Action, SocketMessager.SYS_TEST_LINK.Action) == 0)
                                {
                                }
                                else if (
                                  string.Compare(messager.Action, SocketMessager.SYS_HELLO_WELCOME.Action) == 0)
                                {
                                    this._receives++;
                                    this.Write(messager);
                                    waitWelcome.Set();
                                    if (string.IsNullOrEmpty(_clientName))
                                    {
                                        this._connectSuccess?.Invoke(this);
                                    }
                                    else
                                    {
                                        this.Write(new SocketMessager("S_Regist", new { ClientName = _clientName }), (s, e) =>
                                        {
                                            this._connectSuccess?.Invoke(this);
                                        });
                                    }

                                }
                                else if (string.Compare(messager.Action, SocketMessager.SYS_ACCESS_DENIED.Action) == 0)
                                {
                                    throw new Exception(SocketMessager.SYS_ACCESS_DENIED.Action);
                                }
                                else if (string.Compare(messager.Action, "S_Close") == 0)
                                {
                                    this._running = false;
                                    this.Error(this, new ClientSocketErrorEventArgs(new Exception(messager.Arg?.Desc), 1));
                                    this._tcpClient.Close();
                                    this._tcpClient = null;
                                }
                                else
                                {
                                    ClientSocketReceiveEventArgs e = new ClientSocketReceiveEventArgs(this._receives++, messager, this);
                                    SyncReceive receive = null;

                                    if (this._receiveHandlers.TryGetValue(messager.Id, out receive))
                                    {
                                        this._receiveSyncWQ.Enqueue(delegate ()
                                        {
                                            try
                                            {
                                                receive.ReceiveHandler(this, e);
                                            }
                                            catch (Exception ex)
                                            {
                                                this.OnError(ex);
                                            }
                                            finally
                                            {
                                                receive.Wait.Set();
                                            }
                                        });
                                    }
                                    else if (this.Receive != null)
                                    {
                                        this._receiveWQ.Enqueue(delegate ()
                                        {
                                            this.OnReceive(e);
                                        });
                                    }
                                }
                                this._lastActive = DateTime.Now;
                            }
                            else
                            {
                                TimeSpan ts = DateTime.Now - _lastActive;
                                if (ts.TotalSeconds > 3)
                                {
                                    this.Write(SocketMessager.SYS_TEST_LINK);
                                }
                            }
                            if (!ns.DataAvailable) Thread.CurrentThread.Join(1);
                        }
                        catch (Exception ex)
                        {
                            this._running = false;
                            this.OnError(ex);
                        }
                    }
                    this.Close();
                    this.OnClosed();

                    try
                    {
                        if (this._tcpClient != null)
                        {
                            this._tcpClient.Close();
                            QCrontab.RunWithDelay(5, () => { this.ReConnect(); }, "重连");
                        }
                    }
                    catch { }
                    // this._tcpClient.Dispose();
                    this._tcpClient = null;

                    int[] keys = new int[this._receiveHandlers.Count];
                    try
                    {
                        this._receiveHandlers.Keys.CopyTo(keys, 0);
                    }
                    catch
                    {
                        lock (this._receiveHandlers_lock)
                        {
                            keys = new int[this._receiveHandlers.Count];
                            this._receiveHandlers.Keys.CopyTo(keys, 0);
                        }
                    }
                    foreach (int key in keys)
                    {
                        SyncReceive receiveHandler = null;
                        if (this._receiveHandlers.TryGetValue(key, out receiveHandler))
                        {
                            receiveHandler.Wait.Set();
                        }
                    }
                    lock (this._receiveHandlers_lock)
                    {
                        this._receiveHandlers.Clear();
                    }
                    if (this._receiveWQ != null)
                    {
                        this._receiveWQ.Dispose();
                    }
                    if (this._receiveSyncWQ != null)
                    {
                        this._receiveSyncWQ.Dispose();
                    }
                    if (this._closeWait != null)
                    {
                        this._closeWait.Set();
                    }
                });
                this._thread.Start();
                waitWelcome.Reset();
                waitWelcome.WaitOne(TimeSpan.FromSeconds(5));
            }
        }

        public void Close()
        {
            if (this._running == true)
            {
                this.Write(SocketMessager.SYS_QUIT);
                this._closeWait = new ManualResetEvent(false);
                this._closeWait.Reset();
                this._running = false;
                this._closeWait.WaitOne();
            }
        }

        public void Write(SocketMessager messager)
        {
            this.Write(messager, null, TimeSpan.Zero);
        }
        public void Write(SocketMessager messager, ClientSocketReceiveEventHandler receiveHandler)
        {
            this.Write(messager, receiveHandler, TimeSpan.FromSeconds(20));
        }
        public void Write(SocketMessager messager, ClientSocketReceiveEventHandler receiveHandler, TimeSpan timeout)
        {
            SyncReceive syncReceive = null;
            try
            {
                if (receiveHandler != null)
                {
                    syncReceive = new SyncReceive(receiveHandler);
                    lock (this._receiveHandlers_lock)
                    {
                        if (!this._receiveHandlers.ContainsKey(messager.Id))
                        {
                            this._receiveHandlers.Add(messager.Id, syncReceive);
                        }
                        else
                        {
                            this._receiveHandlers[messager.Id] = syncReceive;
                        }
                    }
                }
                if (this._running)
                {
                    lock (_write_lock)
                    {
                        NetworkStream ns = this._tcpClient.GetStream();
                        base.Write(ns, messager);
                    }
                    this._lastActive = DateTime.Now;
                    if (syncReceive != null)
                    {
                        syncReceive.Wait.Reset();
                        syncReceive.Wait.WaitOne(timeout);
                        syncReceive.Wait.Set();
                        lock (this._receiveHandlers_lock)
                        {
                            this._receiveHandlers.Remove(messager.Id);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this._running = false;
                this.OnError(ex);
                if (syncReceive != null)
                {
                    syncReceive.Wait.Set();
                    lock (this._receiveHandlers_lock)
                    {
                        this._receiveHandlers.Remove(messager.Id);
                    }
                }
            }
        }

        protected virtual void OnClosed(EventArgs e)
        {
            if (this.Closed != null)
            {
                new Thread(delegate ()
                {
                    try
                    {
                        this.Closed(this, e);
                    }
                    catch (Exception ex)
                    {
                        this.OnError(ex);
                    }
                }).Start();
            }

        }
        protected void OnClosed()
        {
            this.OnClosed(new EventArgs());
        }

        protected virtual void OnReceive(ClientSocketReceiveEventArgs e)
        {
            if (this.Receive != null)
            {
                try
                {
                    this.Receive(this, e);
                }
                catch (Exception ex)
                {
                    this.OnError(ex);
                }
            }
        }

        protected virtual void OnError(ClientSocketErrorEventArgs e)
        {
            if (this.Error != null)
            {
                this.Error(this, e);
            }
        }
        protected void OnError(Exception ex)
        {
            int errors = 0;
            lock (this._errors_lock)
            {
                errors = ++this._errors;
            }
            ClientSocketErrorEventArgs e = new ClientSocketErrorEventArgs(ex, errors);
            this.OnError(e);
        }

        public bool Running
        {
            get { return this._running; }
        }

        class SyncReceive : IDisposable
        {
            private ClientSocketReceiveEventHandler _receiveHandler;
            private ManualResetEvent _wait;

            public SyncReceive(ClientSocketReceiveEventHandler receiveHandler)
            {
                this._receiveHandler = receiveHandler;
                this._wait = new ManualResetEvent(false);
            }

            public ClientSocketReceiveEventHandler ReceiveHandler
            {
                get { return _receiveHandler; }
            }
            public ManualResetEvent Wait
            {
                get { return _wait; }
            }

            #region IDisposable 成员

            public void Dispose()
            {
                this._wait.Set();
            }

            #endregion
        }

        #region IDisposable 成员

        public void Dispose()
        {
            this._isDisposed = true;
            this.Close();
        }

        #endregion
    }

    public delegate void ClientSocketClosedEventHandler(ClientSocket sender, EventArgs e);
    public delegate void ClientSocketErrorEventHandler(ClientSocket sender, ClientSocketErrorEventArgs e);
    public delegate void ClientSocketReceiveEventHandler(ClientSocket sender, ClientSocketReceiveEventArgs e);

    public class ClientSocketErrorEventArgs : EventArgs
    {

        private int _errors;
        private Exception _exception;

        public ClientSocketErrorEventArgs(Exception exception, int errors)
        {
            this._exception = exception;
            this._errors = errors;
        }

        public int Errors
        {
            get { return _errors; }
        }
        public Exception Exception
        {
            get { return _exception; }
        }
    }

    public class ClientSocketReceiveEventArgs : EventArgs
    {

        private int _receives;
        private SocketMessager _messager;
        private ClientSocket _client;

        public ClientSocketReceiveEventArgs(int receives, SocketMessager messager, ClientSocket client)
        {
            this._receives = receives;
            this._messager = messager;
            this._client = client;
        }

        public void SendMessage(SocketMessager msg)
        {
            _client.Write(msg);
        }
        public int Receives
        {
            get { return _receives; }
        }
        public SocketMessager Messager
        {
            get { return _messager; }
        }
    }
}
