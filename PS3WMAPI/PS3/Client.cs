using PPB.classes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using webMAN_Manager;

namespace PS3WMAPI.PS3
{
    public class Client : IDisposable
    {
        TimeSpan _runtime;
        string _address;
        PS3WMM _wmserv;
        PS3MAPI _psmserv;
        PS3HDD _data;
        bool _isCon;
        string _favname;
        int _timeout;
        bool _inited;
        bool _parseData;
        int _ticks;
        System.Timers.Timer _counter;
        /// <summary>
        /// Fired when updates downloaded sucessfully.
        /// </summary>
        public event EventHandler<PS3WMM> OnUpdateReceived;
        /// <summary>
        /// Fired when client initialization sucess.
        /// </summary>
        public event EventHandler OnInitializationFinished;
        public event EventHandler<ProgressChangedEventArgs> OnProgressReport;
        /// <summary>
        /// Fired when instanced.
        /// </summary>
        public event EventHandler OnContructor;
        /// <summary>
        /// Fired when server state changed.
        /// </summary>
        public event EventHandler<bool> OnStateChanged;
        public event EventHandler<int> OnClientHeartbeat;
        public bool ParseHddData { get => _parseData; set => _parseData = value; }
        /// <summary>
        /// If the client is usable.
        /// </summary>
        public bool Ready
        {
            get => _inited && _wmserv != null && _psmserv != null && _isCon && _runtime != null;
        }
        /// <summary>
        /// Server runtime elapsed.
        /// </summary>
        public TimeSpan RunTime { get => Ready ? _runtime : TimeSpan.Zero; }
        /// <summary>
        /// Server Internet Protocol Address
        /// </summary>
        public string Address { get => _address; }
        /// <summary>
        /// Server Connected
        /// </summary>
        public bool IsConnected { get => _isCon; }
        /// <summary>
        /// Server running.
        /// </summary>
        public bool Running { get => _inited; }
        /// <summary>
        /// Server reach roundtrip time.
        /// </summary>
        public int RoundtripTime { get; private set; }
        // Servers
        /// <summary>
        /// webMAN Mod's Server Instance if initialized.
        /// </summary>
        public PS3WMM WServer { get => _wmserv; }
        /// <summary>
        /// Manager API Server Instance if initialized.
        /// </summary>
        public PS3MAPI MapiServer { get => _psmserv; }
        public PS3HDD HardDisk { get => _data; }
        /// <summary>
        /// Instances the client with the specified address, name, and timeout.
        /// </summary>
        /// <param name="address">IP Address string.</param>
        /// <param name="name">Display Name string. Def: PS3</param>
        /// <param name="timeout">Connection timeout integet. Def: 3000</param>
        public Client(string address, string name = "PS3", int timeout = 3000, bool parseDatabase = false)
        {
            _address = address;
            _favname = name;
            _timeout = timeout;
            _runtime = TimeSpan.Zero;
            Event(OnContructor);
            _parseData = parseDatabase;

        }
        /// <summary>
        /// The Initialize method checks the server's status using ServerResult. If the status is successful, it creates a new instance of PS3WMM, retrieves data, and triggers an update event. It also sends a notification with the client's name and roundtrip time. The init2 method is invoked in a separate thread. If the server status is not successful, it sets _inited to false and returns false.
        /// </summary>
        /// <returns></returns>
        public bool Initialize()
        {
            var r = ServerResult();
            if (r.Status == IPStatus.Success)
            {
                _wmserv = new PS3WMM(_address);
                _wmserv.Retrieve((S, E) =>
                {
                    Event(OnUpdateReceived, _wmserv);
                    _wmserv.Notification($"★ Client {_favname} attached ({r.RoundtripTime}ms).", WmmIcons.wifi, 0);
                });
                _counter = new System.Timers.Timer();
                Threaded(ManagerApiInitialization);
                if (_parseData)
                {
                    _wmserv.Notification($"★ Client {_favname} is retrieved your data.\nPlease wait.", WmmIcons.directory, 9);
                    Threaded(() =>
                    {
                        Debug.WriteLine("Threaded content reading started.");
                        _data = PS3HDD.Fetch(this, OnProgressReport);
                    });
                }
                return true;
            }
            else
            {
                _inited = false;
                return false;
            }
        }
        /// <summary>
        /// Performs revalidation of all information reflected on this class from external server.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public void UpdateFromServer()
        {
            void exNotInit() =>
                throw new ArgumentException("Client not initialized/running.");
            if (Ready)
            {
                if (_wmserv != null)
                {
                    var fetch = ServerResult();
                    if (fetch.Status == IPStatus.Success)
                    {
                        RoundtripTime = (int)fetch.RoundtripTime;
                    }
                    _wmserv.Retrieve((s, e) => Event(OnUpdateReceived, _wmserv), (s, e) => Event(OnStateChanged, e));
                }
                else exNotInit();
            }
            else exNotInit();

        }

        private void Event(EventHandler evnt)
        {
            evnt?.Invoke(this, null);
        }

        private void Event<T>(EventHandler<T> evnt, T t)
        {
            evnt?.Invoke(this, t);
        }

        /// <summary>
        /// phase 2
        /// </summary>
        void ManagerApiInitialization()
        {
            _counter.Interval = 1000;
            _counter.Elapsed += OnHeartbeat;
            _counter.Start();
            _psmserv = new PS3MAPI();
            _runtime = new TimeSpan();
            _inited = _psmserv.ConnectTarget(_address, 7887);
            if (_inited is true) { Event(OnInitializationFinished); }

        }

        private void OnHeartbeat(object sender, System.Timers.ElapsedEventArgs e)
        {
            _runtime.Add(new TimeSpan(0, 0, 1));
            _ticks++;
            _isCon = _psmserv.IsConnected;
            Event(OnClientHeartbeat, _ticks);
        }

        void Threaded(Action e)
        {

            new Thread(() => e()).Start();
        }
        string getname()
        {
            return typeof(Client).Name;
        }
        internal PingReply ServerResult()
        {
            var ping = new Ping();
            var result = ping.Send(_address, _timeout);
            ping.Dispose();
            return result;
        }
        public override string ToString()
        {
            return $"{_favname}_{_runtime}" + (_inited ? " OK " : "");
        }

        public void Dispose()
        {
            if (Ready)
            {
                int id = Process.GetCurrentProcess().Id;
                string add = GetLocalIPAddress();
                _psmserv.DisconnectTarget();
                _wmserv.Notification(
                    $"★ Client shutdown\n{_favname}\nPid: "
                    + id + "\nIP:" + add, WmmIcons.wifi, 0);
            }
        }
        private static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return "?";
        }
    }
}
