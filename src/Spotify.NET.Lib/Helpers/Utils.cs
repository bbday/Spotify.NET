﻿using System;
using System.Linq;
using Google.Protobuf;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Utilities;

namespace SpotifyNET.Helpers
{
    public static class Utils
    {

        private static readonly char[] hexArray = "0123456789ABCDEF".ToCharArray();
        private static readonly String randomString = "abcdefghijklmnopqrstuvwxyz0123456789";

        public static byte[] toByteArray(BigInteger i)
        {
            byte[] array = i.ToByteArray();
            if (array[0] == 0) array = Arrays.CopyOfRange(array, 1, array.Length);
            return array;
        }

        public static String bytesToHex(byte[] bytes, int offset, int length, bool trim, int minLength)
        {
            if (bytes == null) return "";

            int newOffset = 0;
            bool trimming = trim;
            char[] hexChars = new char[length * 2];
            for (int j = offset; j < length; j++)
            {
                int v = bytes[j] & 0xFF;
                if (trimming)
                {
                    if (v == 0)
                    {
                        newOffset = j + 1;

                        if (minLength != -1 && length - newOffset == minLength)
                            trimming = false;

                        continue;
                    }
                    else
                    {
                        trimming = false;
                    }
                }

                hexChars[j * 2] = hexArray[(uint)v >> 4];
                hexChars[j * 2 + 1] = hexArray[v & 0x0F];
            }

            return new String(hexChars, newOffset * 2, hexChars.Length - newOffset * 2);
        }

        public static byte[] toByteArray(int i)
        {
            byte[] bytes = BitConverter.GetBytes(i);
            if (BitConverter.IsLittleEndian)
            {
                return bytes.Reverse().ToArray();
            }

            return bytes;
        }
        public static String RandomHexString(int length)
        {
            byte[] bytes = new byte[length / 2];
            (new Random()).NextBytes(bytes);
            return BytesToHex(bytes, 0, bytes.Length, false, length);
        }

        public static String BytesToHex(ByteString bytes)
        {
            return BytesToHex(bytes.ToByteArray());
        }

        public static String BytesToHex(byte[] bytes)
        {
            return BytesToHex(bytes, 0, bytes.Length, false, -1);
        }

        public static String BytesToHex(byte[] bytes, int off, int len)
        {
            return BytesToHex(bytes, off, len, false, -1);
        }


        public static String BytesToHex(byte[] bytes, int offset, int length, bool trim, int minLength)
        {
            if (bytes == null) return "";

            var newOffset = 0;
            var trimming = trim;
            var hexChars = new char[length * 2];
            for (var j = offset; j < length; j++)
            {
                var v = bytes[j] & 0xFF;
                if (trimming)
                {
                    if (v == 0)
                    {
                        newOffset = j + 1;

                        if (minLength != -1 && length - newOffset == minLength)
                            trimming = false;

                        continue;
                    }
                    else
                    {
                        trimming = false;
                    }
                }

                hexChars[j * 2] = hexArray[(int)((uint)v >> 4)];
                hexChars[j * 2 + 1] = hexArray[v & 0x0F];
            }

            return new String(hexChars, newOffset * 2, hexChars.Length - newOffset * 2);
        }

        public static byte[] HexToBytes(string str)
        {
            var len = str.Length;
            var data = new byte[len / 2];
            for (var i = 0; i < len; i += 2)
                data[i / 2] = (byte)((Convert.ToInt32(str[i].ToString(),
                    16) << 4) + Convert.ToInt32(str[i + 1].ToString(), 16));
            return data;
        }
    }
}
