using System.Security.Cryptography;
using System.Text;
using System.Linq;

namespace NisSystem.API.Helpers;

/// <summary>
/// AES 加密辅助类
/// </summary>
public static class EncryptionHelper
{
    // 使用 SHA256 从字符串生成固定 32 字节密钥（AES-256）
    private static readonly byte[] Key = SHA256.HashData(
        Encoding.UTF8.GetBytes("NisSystem2024AESEncryptionKeyForSensitiveData!@#$%^&*()")
    );
    
    // 使用 SHA256 的前 16 字节作为 IV（AES 需要 16 字节 IV）
    private static readonly byte[] IV = SHA256.HashData(
        Encoding.UTF8.GetBytes("NisSystem2024IVForAESEncryption")
    ).Take(16).ToArray();

    /// <summary>
    /// 加密敏感数据
    /// </summary>
    public static string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        using (Aes aes = Aes.Create())
        {
            aes.Key = Key;
            aes.IV = IV;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using (MemoryStream msEncrypt = new MemoryStream())
            {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(plainText);
                    }
                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }
    }

    /// <summary>
    /// 解密敏感数据
    /// </summary>
    public static string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return cipherText;

        try
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = IV;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText)))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            return srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
        }
        catch
        {
            return cipherText; // 如果解密失败，返回原文本（可能是未加密的数据）
        }
    }
}

