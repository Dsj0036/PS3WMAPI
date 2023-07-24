using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

internal class Data
{
    private static string _log;
    public static event EventHandler<string> OnWarningMessage;
    public static string GetTrace()
    {
        if (_log is null) return string.Empty;
        else return _log.Clone() as string;
    }
    internal static string DownloadText(string url) => new WebClient().DownloadString(url);
    internal static void Log(string message)
    {
        _log += message + "\n";
        OnWarningMessage?.Invoke(null, message);
        Console.WriteLine(message);
        return;
    }
    internal static byte[] Download(string url)
    {
        return new WebClient().DownloadData(url);
    }
    internal static string[] GetDirectories(string path)
    {
        try
        {
            FtpWebRequest ftpRequest = (FtpWebRequest)WebRequest.Create(path);
            ftpRequest.UseBinary = true;
            ftpRequest.Credentials = new NetworkCredential("", "") { Domain = new Uri(path).Host };
            ftpRequest.Method = WebRequestMethods.Ftp.ListDirectory;

            FtpWebResponse response = (FtpWebResponse)ftpRequest.GetResponse();
            StreamReader streamReader = new StreamReader(response.GetResponseStream());

            List<string> directories = new List<string>();

            string line = streamReader.ReadLine();
            while (!string.IsNullOrEmpty(line))
            {
                directories.Add(line);
                line = streamReader.ReadLine();
            }

            streamReader.Close();
            response.Close();


            return directories.ToArray();
        }
        catch
        {
            return new string[] { "", "" };
        }

    }
    internal static bool UrlFileExists(string url)
    {
        var request = (FtpWebRequest)WebRequest.Create
(url.Replace("http", "ftp"));
        request.Credentials = new NetworkCredential("user", "pass");
        request.Method = WebRequestMethods.Ftp.GetFileSize;

        try
        {
            FtpWebResponse response = (FtpWebResponse)request.GetResponse();
            response.Close();
            return true;
        }
        catch (WebException ex)
        {
            FtpWebResponse response = (FtpWebResponse)ex.Response;
            if (response.StatusCode ==
                FtpStatusCode.ActionNotTakenFileUnavailable)
            {
                response.Close();
                return false;
            }
        }
        return false;
    }
    internal static void LogResultArray(params object[] values)
    {
        foreach (object value in values)
        {
            if (value is null) continue;
            Console.WriteLine(value.GetType().Name + " " + value);
        }
    }
    public static string BytesToHex(byte[] bytes)
    {
        StringBuilder hexString = new StringBuilder(bytes.Length);
        for (int i = 0; i < bytes.Length; i++)
        {
            hexString.Append(bytes[i].ToString("X2"));
        }
        return hexString.ToString();
    }
    public static byte[] HexToBytes(string hex)
    {
        return Enumerable.Range(0, hex.Length).
               Where(x => 0 == x % 2).
               Select(x => Convert.ToByte(hex.Substring(x, 2), 16)).
               ToArray();
    }
}
