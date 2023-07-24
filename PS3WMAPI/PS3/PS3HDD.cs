
using System.Collections.ObjectModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Linq;
using System.Diagnostics;
using System.Runtime.Remoting.Channels;
using System.ComponentModel;
using System.Runtime.Remoting.Messaging;

namespace PS3WMAPI.PS3
{
    [DebuggerDisplay("{SenderClient.WServer.HardDiskString}")]
    public class PS3HDD
    {
        public Client SenderClient { get; private set; }
        public ReadOnlyCollection<PS3File> Files { get; private set; }
        /// <summary>
        /// Commonly containing .RAP license files.
        /// </summary>
        public ReadOnlyCollection<PS3File> Exdata { get; private set; }
        public ReadOnlyCollection<PS3File> Music { get; private set; }
        public ReadOnlyCollection<PS3File> Photo { get; private set; }
        public ReadOnlyCollection<PS3File> Packages { get; private set; }
        public ReadOnlyCollection<string> GameData { get; private set; }
        private static string[] fetchNames
        ={
            "exdata",
            "music",
            "photo",
            "packages",
        };
        public long GetLength()
        {
            long r = 0x000;
            ReadOnlyCollection<PS3File>[] datas =
            {
                Exdata,
                Music,
                Photo,
                Packages,
            };

            foreach (var container in datas)
            {
                foreach (PS3File file in container)
                {
                    r += file.Length;
                }
            }
            return r;
        }
        public static PS3HDD Fetch(Client client, EventHandler<ProgressChangedEventArgs> ReadingProgression = null)
        {
            var ret = new PS3HDD();
            var add = $"ftp://{client.Address}/dev_hdd0/";
            List<List<PS3File>> common = new List<List<PS3File>>();
            foreach (string dir in fetchNames)
            {

                var dest = new List<PS3File>();
                var r = FTP.GetFiles(add + dir);
                if (r != null)
                {
                    int index = 0;
                    foreach (string file in r)
                    {
                        int perc() => index * 100 / r.Length;

                        ReadingProgression?.Invoke(ret,
                             new ProgressChangedEventArgs(perc(), file));
                        var fn = add + dir + $"/{file}";
                        Console.WriteLine(fn);
                        dest.Add(new PS3File(fn));
                        index++;
                    }

                }
                common.Add(dest);
            }
            var gamesPath = add + "game";
            var games = FTP.GetDirectories(gamesPath).ToList();
            ret.Create(common[0], common[1], common[2], common[3], games);
            ret.SenderClient = client;
            ReadingProgression?.Invoke(ret,
                             new ProgressChangedEventArgs(100, true));
            return ret;
        }
        private void Create(List<PS3File> exdata, List<PS3File> music
            , List<PS3File> photo, List<PS3File> packs, List<string> gamedata)
        {
            Exdata = Col(exdata);
            Music = Col(music);
            Photo = Col(photo);
            Packages = Col(packs);
            GameData = new ReadOnlyCollection<string>(gamedata);
        }
        private ReadOnlyCollection<PS3File> Col(List<PS3File> col)
        {
            return new ReadOnlyCollection<PS3File>(col);
        }

    }
}
