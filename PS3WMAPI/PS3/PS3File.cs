using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PS3WMAPI.PS3
{
    public class PS3File
    {
        public string SafeFileNamee { get; private set; }
        public long Length { get; private set; }
        public string FullPath { get; private set; }
        public string DirectoryName { get; private set; }

        public PS3File(string ftpurl)
        {
            var len = FTP.GetLength(ftpurl);
            if (len != 0)
            {
                var sn = ftpurl.Split('/').Last();
                SafeFileNamee = sn;
                Length = len;
                FullPath = ftpurl;
                DirectoryName = Path.GetDirectoryName(ftpurl).Replace("\\", "/");
            }
            else throw new ArgumentException("File doesnt exists.");
        }
    }
}
