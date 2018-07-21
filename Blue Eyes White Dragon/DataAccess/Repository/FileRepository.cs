﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Blue_Eyes_White_Dragon.DataAccess.Interface;
using Blue_Eyes_White_Dragon.Misc;
using Blue_Eyes_White_Dragon.Misc.Interface;
using Blue_Eyes_White_Dragon.UI.Models;
using Newtonsoft.Json;

namespace Blue_Eyes_White_Dragon.DataAccess.Repository
{
    public class FileRepository : IFileRepository
    {
        private readonly ILogger _logger;
        private readonly string _directoryName;

        public FileRepository(ILogger logger, string directoryName)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _directoryName = directoryName;
        }

        public FileInfo GetImageFile(string filename, DirectoryInfo imagesLocation)
        {
            var imageFile = new FileInfo(Path.Combine(imagesLocation.FullName, filename));
            if (imageFile.Exists)
            {
                _logger.LogInformation(Localization.InformationFoundPendulumImage(filename, imageFile.FullName));
                return imageFile;
            }

            _logger.LogInformation(Localization.ErrorPendulumNotFound(filename));
            return null;
        }

        public List<string> GetSupportedFileTypes()
        {
            return Enum.GetNames(typeof(Constants.SupportedImageTypes)).ToList();
        }

        public string SaveArtworkMatchToFile(IEnumerable<Artwork> artworkList)
        {
            var fileName = Constants.ArtworkMatchFileName;
            var path = Path.Combine(_directoryName, fileName);

            var jsonArtwork = JsonConvert.SerializeObject(artworkList, Formatting.Indented);
            File.WriteAllText(path, jsonArtwork);
            return path;
        }

        public IEnumerable<Artwork> LoadArtworkMatchFromFile(string path)
        {
            try
            {
                using (var reader = new StreamReader(path))
                {
                    var json = reader.ReadToEnd();
                    var files = JsonConvert.DeserializeObject<List<Artwork>>(json);
                    return files;
                }
            }
            catch (Exception e)
            {
                _logger.LogException(e);
                throw;
            }
        }

        public void CalculateHeightAndWidth(IEnumerable<Artwork> artworks)
        {
            var artworkList = artworks.ToList();

            Parallel.For(0, artworkList.Count, i =>
            {
                var artwork = artworkList[i];
                if (!artwork.IsMatched)
                {
                    _logger.LogInformation(Localization.ErrorCalculateNoMatch(artwork.GameImageMonsterName));
                    return;
                }

                var width = 0;
                var height = 0;

                var path = artwork.GameImageFilePath;
                if (!string.IsNullOrEmpty(path))
                {
                    CalculateHeightAndWidth(path, out width, out height);
                    artwork.GameImageWidth = width;
                    artwork.GameImageHeight = height;
                }

                path = artwork.ReplacementImageFilePath;
                if (!string.IsNullOrEmpty(path))
                {
                    CalculateHeightAndWidth(path, out width, out height);
                    artwork.ReplacementImageWidth = width;
                    artwork.ReplacementImageHeight = height;
                }
            });
        }

        private void CalculateHeightAndWidth(string path, out int width, out int height)
        {
            var jpg = Constants.Jpg;
            var png = Constants.Png;

            var imageFile = new FileInfo(path);
            var fileType = imageFile.Extension.ToLower().Substring(1);

            if (fileType == jpg)
            {
                GetJpegDimension(path, out width, out height);
            }
            else if (fileType == png)
            {
                GetPngDimension(path, out width, out height);
            }
            else
            {
                _logger.LogInformation(Localization.ErrorUnsupportedFileType(fileType));
                width = 0;
                height = 0;
            }



        }

        private bool GetJpegDimension(string fileName, out int width, out int height)
        {
            width = height = 0;
            bool found = false;
            bool eof = false;

            FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            BinaryReader reader = new BinaryReader(stream);

            while (!found || eof)
            {
                // read 0xFF and the type
                reader.ReadByte();
                byte type = reader.ReadByte();

                // get length
                int len = 0;
                switch (type)
                {
                    // start and end of the image
                    case 0xD8:
                    case 0xD9:
                        len = 0;
                        break;

                    // restart interval
                    case 0xDD:
                        len = 2;
                        break;

                    // the next two bytes is the length
                    default:
                        int lenHi = reader.ReadByte();
                        int lenLo = reader.ReadByte();
                        len = (lenHi << 8 | lenLo) - 2;
                        break;
                }

                // EOF?
                if (type == 0xD9)
                    eof = true;

                // process the data
                if (len > 0)
                {
                    // read the data
                    byte[] data = reader.ReadBytes(len);

                    // this is what we are looking for
                    if (type == 0xC0)
                    {
                        height = data[1] << 8 | data[2];
                        width = data[3] << 8 | data[4];
                        found = true;
                    }
                }
            }
            reader.Close();
            stream.Close();
            return found;
        }

        private bool GetPngDimension(string fileName, out int width, out int height)
        {
            var buff = new byte[32];
            using (var d = File.OpenRead(fileName))
            {
                d.Read(buff, 0, 32);
            }
            const int wOff = 16;
            const int hOff = 20;
            width = BitConverter.ToInt32(new[] { buff[wOff + 3], buff[wOff + 2], buff[wOff + 1], buff[wOff + 0], }, 0);
            height = BitConverter.ToInt32(new[] { buff[hOff + 3], buff[hOff + 2], buff[hOff + 1], buff[hOff + 0], }, 0);
            return true;
        }
    }
}
