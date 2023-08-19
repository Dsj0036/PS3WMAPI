using BasicDataTypes;
using HtmlAgilityPack;
using PS3WMAPI.PS3;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

[DebuggerDisplay(
    "[Online: {_online}] [In Game: {_ingame}] [Initialized: {_initialized}] [UserName: {_currentUser}] [Elapsed: {_startupTime}]")]
public class PS3WMM
{
    public event EventHandler<Exception> OnFault;
    /// <summary>
    /// The IP Address of the system.
    /// </summary>
    public string IP { get => _ip; }
    /// <summary>
    /// Returns true if the system is still online.
    /// </summary>
    public bool IsOnline { get => _online & SystemStartupTime != null & this.CurrentUser != null; }
    /// <summary>
    /// Returns true if the system is currently in game.
    /// </summary>
    public bool IsInGame { get => _ingame; }
    /// <summary>
    /// The username of the initialized system user.
    /// </summary>
    public string CurrentUser { get => _currentUser; }
    /// <summary>
    /// The PSN username of the initialized system user.
    /// </summary>
    public string CurrentUserPSN { get => _currentUserPsn; }
    /// <summary>
    /// The process id if the system is currently running one if else will be 0.
    /// </summary>
    public uint ProcessId { get => IsInGame ? _processId.Value : 0; }
    /// <summary>
    /// The system's startup time.
    /// </summary>
    public Time SystemStartupTime { get => _startupTime; }
    /// <summary>
    /// The system's execution time.
    /// </summary>
    public Time SystemInGameTime { get => _playingTime; }
    /// <summary>
    /// The system's process name if is running one else will be string.Empty.
    /// </summary>
    public string ProcessName { get => _currentProcessName; }
    /// <summary>
    /// The system's process TID if is running a process.
    /// </summary>        
    public string ProcessTitleId { get => _currentProcessTitleId; }
    /// <summary>
    /// Returns the user directory path.
    /// </summary>
    public string CurrentUserDirectory { get => _currentUserDir; }
    /// <summary>
    /// Returns a string representing the system Fan Speed.
    /// </summary>
    public string FanSpeedPercentage { get => _fan; }
    /// <summary>
    /// Returns a string representing the system Memory in KBs.
    /// </summary>
    public string MemoryString { get => _mem; }
    /// <summary>
    /// Returns a string representing the Hardisk size rest.
    /// </summary>
    public string HardDiskString { get => _hrddsk; }
    /// <summary>
    /// Returns the current process execution path.
    /// </summary>
    public string ProcessPath { get => _processExLocation; }
    bool _initialized;
    const string _baseUrl = "http://{0}/cpursx.ps3";
    readonly string _url;
    readonly string _ip;
    bool _online = false;
    bool _ingame;
    string _currentUser;
    string _currentUserDir;
    string _currentUserPsn;
    uint? _processId;
    Time _startupTime;
    string _hrddsk;
    Time _playingTime;
    string _currentProcessTitleId;
    string _currentProcessName;
    string _processExLocation;
    string _fan;
    string _mem;
    readonly string _logname;
    /// <summary>
    /// Initializes a new manager instance for the provided client by address.
    /// </summary>
    /// <param name="ip">The address to connect. e.g. 192.168.0.10</param>
    public PS3WMM(string ip)
    {
        _logname = $"[{typeof(PS3WMM).Assembly.GetName().Name}|{Environment.UserName}]";
        _url = string.Format(_baseUrl, ip);
        _ip = ip;
        var flag = _initialized = Reachable(_url, out Exception error);
        if (!flag)
            if (error != null)
                HandleException(error);

    }
    /// <summary>
    /// Revalidates all information for the client and updates all his fields.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public void Retrieve(EventHandler onFinished = null, EventHandler<bool> stateChanged = null)
    {
        DoIfInitialized(() => SetInfo());
        void SetInfo()
        {
            var baseDoc = new HtmlAgilityPack.HtmlDocument();
            _online = Reachable(_url, out Exception _);
            stateChanged?.Invoke(null, _online);
            if (_online is false)
            {
                Console.WriteLine("[!] Server disconnected.");
                return;
            }
            else
            {
                var html = GetHtml(_url);
                baseDoc.LoadHtml(html);

                var doc = baseDoc.DocumentNode;
                var dec = doc.Descendants();
                var flagBase = dec.Where((s) => s.InnerText.StartsWith("webMAN")).ToArray();
                var flag = flagBase.Length > 7 & flagBase[7].InnerText.StartsWith("webMAN");

                if (flag)
                {
                    var decs = dec.ToList();
                    var nodes = decs.Where((s) => (s.InnerText.Length > 3 & s.InnerHtml.Length < 100 & s.OuterStartIndex > 800) & s.Name != "#text").ToArray();
                    if (nodes == null)
                    {
                        throw new InvalidOperationException("Request results invalid. Parsing error. This is maybe because you have enabled SMAN GUI ");
                    }
                    else
                    {
                        var mem = nodes.Where((s) => (s.InnerHtml.EndsWith("KB ") || s.InnerHtml.EndsWith("(XMB)"))).ToArray().First();
                        var hdd = nodes.Where((s) => s.InnerHtml.StartsWith("HDD")).ToArray().FirstOrDefault();
                        var fan = nodes.Where((s) => s.InnerHtml.Contains("%")).ToArray().FirstOrDefault();
                        var cpu = nodes.Where((s) => s.InnerHtml.StartsWith("CPU")).ToArray().FirstOrDefault();
                        var fw = decs.Where((s) => s.OuterHtml.StartsWith("<a class=\"s\" href=\"/setup.ps3\">")).FirstOrDefault();
                        var isonxmb = mem.InnerHtml.EndsWith("(XMB)");
                        _hrddsk = hdd.InnerText;
                        _fan = fan != null ? fan.InnerText : "";
                        _mem = mem.InnerText;
                        string game = "XMB";
                        if (nodes.Length > 4)
                        {
                            var gmregionnode = nodes.Where((s) => s.InnerText.Length == 9).FirstOrDefault();
                            string gmregion = gmregionnode != null ? gmregionnode.InnerText : "";
                            var caps = gmregion != null ? gmregion.Caps() : 0;
                            var ingame = gmregion.Length == 9 & caps > 40;
                            var startupTimeString = dec.Where((nd) => nd.InnerHtml.Contains("time.png") & nd.InnerHtml.Contains("<label title=\"Startup\"")).ToArray().LastOrDefault()?.InnerText.Trim();
                            var playElapsedTimeNode = dec.Where((nd) => nd.Name == "label" & nd.GetAttributeValue("title", "") == "Play").ToArray().FirstOrDefault();
                            string playElapsedTimeString = "";
                            if (playElapsedTimeNode != null)
                            {
                                playElapsedTimeString = playElapsedTimeNode.ParentNode.InnerText.Split(' ').Reverse().ToArray()[1];
                            }
                            var userUrlNode = dec.Where((nd) => nd.GetAttributeValue("href", "").Contains("/home/")).FirstOrDefault();
                            var userUrl = (userUrlNode != null & userUrlNode.Attributes.Count > 0 & userUrlNode.Attributes[0].Name == "href") ? userUrlNode.Attributes[0].Value : "";
                            string localusername = "";
                            string psnUserName = "";
                            string GetPSNUsername(string ip, int userid)
                            {
                                string url = $"http://{ip}//dev_hdd0//home//000000{userid}//np_cache.dat";
                                string value = new WebClient().DownloadString(url);
                                value = value.Replace("?", "");
                                value = value.Replace("b2pyps3", "[!END]");
                                value = value.Normalize();
                                value = value.Trim(new char[] { '{', '}', '?' });
                                int indkey = value.IndexOf("\0\0\0\0\0");
                                if (indkey == -1)
                                {
                                    return "unknown";
                                }

                                value = value.Substring(indkey + "\0\0\0\0\0".Length);
                                value = value.Substring(0, value.IndexOf("\0\0\0\0\0"));
                                return value;
                            }

                            if (userUrl != string.Empty)
                            {
                                var path = _url.Replace("/cpursx.ps3", userUrl);
                                var lcusnmFn = path + "/localusername";
                                try { localusername = Data.DownloadText(lcusnmFn); }
                                catch { localusername = "404"; }
                                _currentUserDir = path;
                                var iusid = int.Parse(userUrl.Split('/').Reverse().First());
                                psnUserName = GetPSNUsername(_ip, iusid);
                            }
                            else psnUserName = localusername;
                            Time playElapsedTime = Time.Zero;
                            if (playElapsedTimeString.Length > 2)
                            {
                                playElapsedTime = Time.FromString(playElapsedTimeString);
                            }
                            Time startupTime = new Time(TimeSpan.Parse(startupTimeString));
                            // var elapsedPlayingTime = new Time(TimeSpan.Parse(playElapsedTimeString));
                            Data.LogResultArray(gmregion,
                                caps, ingame, startupTimeString,
                                userUrl, localusername, psnUserName);

                            if (ingame is true)
                            {
                                string[] trash = { "KLIC", "Artemis", "Reload", "Exit","syscalls",
                                    "contenidos",
                                    "contents" };
                                HtmlNode[] already = { mem, hdd, fan, cpu };

                                var trashn = nodes.Where((s) =>
                                {
                                    foreach (string t in trash)
                                    {
                                        if (s.InnerText.Contains(t)) return true;
                                    }
                                    return false;
                                });
                                var nda = nodes.ToList();
                                foreach (var t in trashn)
                                {
                                    nda.Remove(t);
                                }
                                foreach (var t in already)
                                {
                                    nda.Remove(t);
                                }
                                var strs = nda.Where((n) => n.OuterStartIndex < 1600).ToList();

                                if (strs.Count > 0)
                                {
                                    if (strs.Count >= 4)
                                    {
                                        var pid = strs[3].InnerText.Replace("pid=", "");
                                        var pname = strs[1].InnerText;
                                        var preg = strs[0];
                                        string gamepath = string.Empty;
                                        var gamepathNode = dec.Where((s) =>
                                        {
                                            return s.GetAttributeValue("src", "null") != "null" &
                                            s.Name == "img" & s.GetAttributeValue("style", "null") == "position:relative;top:20px;";
                                        }).ToArray().FirstOrDefault();
                                        if (gamepathNode != null)
                                        {
                                            var src = gamepathNode.GetAttributeValue("src", string.Empty);
                                            var srcNoName = src.Split('/').ToList();
                                            srcNoName = srcNoName.GetRange(0, srcNoName.Count - 1);
                                            gamepath = src != string.Empty ? string.Join(
                                                "/", srcNoName.ToArray()) : "";
                                        }
                                        _ingame = true;
                                        game = pname;
                                        _processExLocation = gamepath;
                                        _currentProcessName = pname;
                                        _processId = uint.Parse(pid);
                                        _currentProcessTitleId = preg.InnerText;
                                        Data.LogResultArray(pid, pname, preg);
                                    }
                                    else
                                    {
                                        _currentProcessName = "xross";
                                        _currentProcessTitleId = "xross0000";
                                        _processId = 0000;
                                    }
                                }
                                else
                                {
                                }
                            }
                            else
                            {
                                _ingame = false;
                            }
                            _currentUserPsn = psnUserName;
                            _currentUser = localusername;
                            _playingTime = playElapsedTime;
                            _startupTime = startupTime;
                        }
                    }
                   
                }
                else
                {
                    _online = false;
                    throw new InvalidOperationException("Server is not WEBMAN.");
                }
            }
            onFinished?.Invoke(null, null);
        }
    }
    /// <summary>
    /// Returns the HTML of the specified Uniform Resource Localizator.
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    string GetHtml(string url)
    {
        try
        {
            return new WebClient().DownloadString(url);
        }
        catch (Exception error)
        {
            HandleException(error);
            return "404";
        }
    }
    /// <summary>
    /// Performs the action if initialized.
    /// </summary>
    /// <param name="action"></param>
    void DoIfInitialized(Action action)
    {
        if (_initialized) action();
        else Console.WriteLine("[!] An action cannot be perform because instance is not initialized yet.");
    }
    /// <summary>
    /// Tries to reach the provided address an returns the result in boolean.
    /// </summary>
    /// <param name="address"></param>
    /// <param name="error"></param>
    /// <returns></returns>
    bool Reachable(string address, out Exception error)
    {
        error = null;
        if (Uri.TryCreate(address, UriKind.RelativeOrAbsolute, out _) is false) return false;
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(address);
        request.Timeout = 5000;
        request.Method = "GET";
        try
        {
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                try
                {
                    return response.StatusCode == HttpStatusCode.OK;
                }
                catch (Exception er)
                {
                    error = er;
                    return false;
                }
            }
        }
        catch (Exception err)
        {
            error = err;
            return false;
        }
    }
    /// <summary>
    /// Displays an error message.
    /// </summary>
    /// <param name="error"></param>
    void HandleException(Exception error)
    {
        OnFault?.Invoke(this, error);
    }
    /// <summary>
    /// Returns the root place of the system.
    /// </summary>
    /// <returns>The path.</returns>
    public string GetRoot() => $"http://{IP}";
    /// <summary>
    /// Returns the main hard disk location of main stored content of the system.
    /// </summary>
    /// <returns>The path.</returns>
    public string GetHDD0() => GetRoot() + "/dev_hdd0";
    /// <summary>
    /// Returns the secondary hard disk location of main stored content of the system.
    /// </summary>
    /// <returns>The path.</returns>
    public string GetHDD1() => GetRoot() + "/dev_hdd1";
    /// <summary>
    /// Returns the optional USB device place of the system.
    /// </summary>
    /// <returns>The path.</returns>
    public string GetUSB0() => GetRoot() + "/dev_usb0";
    /// <summary>
    /// Returns the main packages directory of the system.
    /// </summary>
    /// <returns></returns>
    public string GetPackages() => GetHDD0() + "/packages";
    /// <summary>
    /// Returns the main game content system directory.
    /// </summary>
    /// <returns></returns>
    public string GetGame() => GetHDD0() + "/game";
    /// <summary>
    /// Returns the main ISO content directory path.
    /// </summary>
    /// <returns></returns>
    public string GetIso() => GetRoot() + "/dev_hdd0/GAMES";
    /// <summary>
    /// Returns the current user directory.
    /// </summary>
    /// <returns></returns>
    public string GetUser() => _currentUserDir;
    /// <summary>
    /// Download the specified path. Usefull for sending commands.
    /// </summary>
    /// <param name="cmd"></param>
    /// <returns></returns>
    public string SendCmd(string cmd)
    {
        try
        {
            return new WebClient().DownloadString(GetRoot() + $"/{cmd}");
        }
        catch (Exception ex)
        {
            HandleException(ex);
            return "404";

        }
    }
    /// <summary>
    /// Notifies the system the provided message string, with an icon and sound index.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="icon"></param>
    /// <param name="snd"></param>
    public void Notification(string message, int icon, int snd)
    {
        var basefo = "/popup.ps3?{0}&icon={1}&snd={2}";
        string cmd = string.Format(basefo, message.Replace(" ", "+"), icon, snd);

        Task.Run(() => Reach($"http://{IP}{cmd}", 2000));
    }
    /// <summary>
    /// Tries asyncronously to reach the provided URL and wait the specified time.
    /// </summary>
    /// <param name="url"></param>
    /// <param name="timeout"></param>
    /// <returns></returns>
    private async Task<bool> Reach(string url, int timeout = 5000)
    {
        try
        {
            using (var http = new HttpClient())
            {
                http.Timeout = new TimeSpan(0, 0, 0, 0, timeout);
                var resp = await http.GetAsync(url);
                if (resp.StatusCode == HttpStatusCode.OK) return true;
                else Console.WriteLine($"Reached url response code returned: [{resp.StatusCode}] ");
                return false;
            }

        }
        catch { return false; }
    }


}
