using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Ya.Music.Downloader.Yandex
{
    enum YaType
    {
        Track,
        Album,
        Playlists,
        Users,
        Unknown
    }

    static class Tools
    {
        public static YaType ParseType(string url)
        {
            var uri = new UriBuilder(url);
            var num = uri.Uri.Segments.Length;

            switch (uri.Uri.Segments[num - 2].Replace("/", ""))
            {
                case "track":
                    return YaType.Track;
                case "album":
                    return YaType.Album;
                case "playlists":
                    return YaType.Playlists;
                default:
                    return YaType.Unknown;
            }

        }
        public static string GetMd5Hash(string input)
        {
            var md5Hash = MD5.Create();
            // Convert the input string to a byte array and compute the hash. 
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes 
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data  
            // and format each one as a hexadecimal string. 
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string. 
            return sBuilder.ToString();
        }
        public static string GetSafeFilename(string name)
        {
            return string.Join("_", name.Split(System.IO.Path.GetInvalidFileNameChars()));
        }
        
    }
}
