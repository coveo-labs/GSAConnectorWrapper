// Copyright (c) 2005-2017, Coveo Solutions Inc.

using System;
using System.IO;
using System.Text;
using Ionic.Zlib;

namespace GSAFeedPushConverter.Utilities
{
    public static class Compression
    {
        /// <summary>
        /// Convert the content of the stream in a string.
        /// </summary>
        /// <param name="p_DataStream">The stream to compress.</param>
        /// <returns>The stream in compressed string.</returns>
        public static string GetCompressedBinaryData(Stream p_DataStream)
        {
            string data;
            p_DataStream.Position = 0;
            //This might not be optimized for the memory. A cleaning would be nice. (Look to close all streams)
            using (MemoryStream compressStream = new MemoryStream()) {
                using (ZlibStream deflateStream = new ZlibStream(compressStream, CompressionMode.Compress, CompressionLevel.Level8)) {
                    p_DataStream.CopyTo(deflateStream);
                    deflateStream.Close();
                    data = Convert.ToBase64String(compressStream.ToArray());
                }
            }
            return data;
        }

        /// <summary>
        /// Get the string value of a compressed string content
        /// </summary>
        /// <param name="p_Content">The content to decompressed.</param>
        /// <returns>Decompressed content.</returns>
        public static string GetDecompressedBinaryData(string p_Content)
        {
            string decompressedData;

            using (MemoryStream dataStream = new MemoryStream(Convert.FromBase64String(p_Content))) {
                using (MemoryStream decompressStream = new MemoryStream()) {
                    using (ZlibStream deflateStream = new ZlibStream(decompressStream, CompressionMode.Decompress)) {
                        dataStream.CopyTo(deflateStream);
                        deflateStream.Close();
                        decompressedData = Encoding.UTF8.GetString(decompressStream.ToArray());
                    }
                }
            }

            return decompressedData;
        }
    }
}
