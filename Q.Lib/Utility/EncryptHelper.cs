using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Q.Lib
{
    public class EncryptHelper
    {
        /// <summary>
        /// 默认密钥
        /// </summary>
        public static readonly string _3DESKEY = "GgJjZhZz,YtSvJzHq.QingXu";

        #region 3DES加密
        ///  <summary > 
        /// 3DES加密 
        ///  </summary > 
        ///  <param name="Value" >待加密字符串 </param > 
        ///  <param name="sKey" >密钥 </param > 
        ///  <returns >加密后字符串 </returns > 
        public static string Encrypt3DES(string Value, string sKey)
        {
            string result = "";
            //构造对称算法 
            SymmetricAlgorithm mCSP = new TripleDESCryptoServiceProvider();

            ICryptoTransform ct;
            MemoryStream ms;
            CryptoStream cs;
            byte[] byt;
            mCSP.Key = Encoding.UTF8.GetBytes(sKey);
            //指定加密的运算模式 
            mCSP.Mode = CipherMode.ECB;
            //获取或设置加密算法的填充模式 
            mCSP.Padding = PaddingMode.PKCS7;
            ct = mCSP.CreateEncryptor(mCSP.Key, mCSP.IV);
            byt = Encoding.UTF8.GetBytes(Value);
            ms = new MemoryStream();
            cs = new CryptoStream(ms, ct, CryptoStreamMode.Write);
            cs.Write(byt, 0, byt.Length);
            cs.FlushFinalBlock();
            cs.Close();

            byte[] _result = ms.ToArray();
            for (int i = 0; i < _result.Length; i++)
            {
                result += _result[i].ToString("X2").ToUpper();
            }
            return result;
        }
        #endregion

        #region 3DES加密，采用默认密钥
        /// <summary>
        /// 3DES加密，采用默认密钥
        /// </summary>
        /// <param name="Value"></param>
        /// <returns></returns>
        public static string Encrypt3DES(string Value)
        {
            return Encrypt3DES(Value, _3DESKEY);
        }
        #endregion

        #region 3DES解密
        ///  <summary > 
        /// 3DES解密 
        ///  </summary > 
        ///  <param name="Value" >待解密字符串(16进制)</param > 
        ///  <param name="sKey" >密钥 </param > 
        ///  <returns >解密后字符串</returns > 
        public static string Decrypt3DES(string Value, string sKey)
        {
            //构造对称算法 
            SymmetricAlgorithm mCSP = new TripleDESCryptoServiceProvider();

            ICryptoTransform ct;
            MemoryStream ms;
            CryptoStream cs;
            byte[] byt;
            mCSP.Key = Encoding.UTF8.GetBytes(sKey);
            mCSP.Mode = CipherMode.ECB;
            mCSP.Padding = PaddingMode.PKCS7;
            ct = mCSP.CreateDecryptor(mCSP.Key, mCSP.IV);

            int len = Value.Length / 2;
            byt = new byte[len];
            for (int i = 0; i < len; i++)
            {
                byt[i] = Convert.ToByte(Value.Substring(i * 2, 2), 16);
            }
            ms = new MemoryStream();
            cs = new CryptoStream(ms, ct, CryptoStreamMode.Write);
            cs.Write(byt, 0, byt.Length);
            cs.FlushFinalBlock();
            cs.Close();
            return Encoding.UTF8.GetString(ms.ToArray());


        }
        #endregion

        #region 3DES解密，采用默认密钥
        /// <summary>
        /// 3DES解密，采用默认密钥
        /// </summary>
        /// <param name="Value"></param>
        /// <returns></returns>
        public static string Decrypt3DES(string Value)
        {
            if (!string.IsNullOrEmpty(Value))
            {
                return Decrypt3DES(Value, _3DESKEY);
            }
            else { return ""; }
        }
        #endregion
    }
}
