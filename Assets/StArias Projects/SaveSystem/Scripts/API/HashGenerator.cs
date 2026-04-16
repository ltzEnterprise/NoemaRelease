/*  
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.  
 * If a copy of the MPL was not distributed with this file, you can obtain one at  
 * https://mozilla.org/MPL/2.0/.  
 *  
 * Copyright (c) 2025 StArias - https://github.com/starias  
 */

using System.Security.Cryptography;
using System.Text;

namespace StArias.API.SaveLoadSystem
{
    public static class HashGenerator
    {
        public static string Hash(string data)
        {
            SHA256Managed mySha256 = new SHA256Managed();
            byte[] textToBytes = Encoding.UTF8.GetBytes(data);
            byte[] hashValue = mySha256.ComputeHash(textToBytes);
            return GetHexStringFromHash(hashValue);
        }

        private static string GetHexStringFromHash(byte[] hash)
        {
            string hexString = string.Empty;
            foreach (byte b in hash)
            {
                hexString += b.ToString("x2");
            }
            return hexString;
        }
    }
}