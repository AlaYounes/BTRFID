using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fr.nexess.toolbox {
    public class Crypto {

        private static ushort DEFAULT_XOR_KEY = 76;

        public static string encodeDecodeXor(string str) {

            var result = new StringBuilder();

            foreach (char c in str) {
                result.Append((char)((uint)c ^ (uint)DEFAULT_XOR_KEY));
            }

            return result.ToString();
        }
    }
}
