﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using GreenWerx.Utilites.Extensions;
using GreenWerx.Utilites.Helpers;

namespace GreenWerx.Utilites.Security
{
    /// <summary>
    /// Salted password hashing with PBKDF2-SHA1.
    /// Author: havoc AT defuse.ca
    /// www: http://crackstation.net/hashing-security.htm
    /// Compatibility: .NET 3.0 and later.
    /// </summary>
    public class PasswordHash
    {
        public const int HASH_BYTE_SIZE = 24;

        public const int ITERATION_INDEX = 0;

        public const int PBKDF2_INDEX = 2;

        public const int PBKDF2_ITERATIONS = 1000;

        // The following constants may be changed without breaking existing hashes.
        public const int SALT_BYTE_SIZE = 24;

        public const int SALT_INDEX = 1;

        public enum PasswordScore
        {
            Blank = 0,
            VeryWeak = 1,
            Weak = 2,
            Medium = 3,
            Strong = 4,
            VeryStrong = 5
        }

        public static PasswordScore CheckStrength(string password)
        {
            int score = 1;
            if (string.IsNullOrWhiteSpace(password))
                return PasswordScore.Blank;

            if (IsCommonPassword(password))
                return PasswordScore.VeryWeak;

            if (password.Length < 4)
                return PasswordScore.VeryWeak;

            if (password.Length >= 8)
                score++;
            if (password.Length >= 12)
                score++;
            if (Regex.Match(password, @"\d+", RegexOptions.ECMAScript).Success)
                score++;
            if (Regex.Match(password, @"[a-z]", RegexOptions.ECMAScript).Success &&
              Regex.Match(password, @"[A-Z]", RegexOptions.ECMAScript).Success)
                score++;
            if (Regex.Match(password, @".[!,@,#,$,%,^,&,*,?,_,~,-,£,(,)]", RegexOptions.ECMAScript).Success)
                score++;

            return (PasswordScore)score;
        }

        /// <summary>
        /// Creates a salted PBKDF2 hash of the password.
        /// </summary>
        /// <param name="password">The password to hash.</param>
        /// <returns>The hash of the password.</returns>
        public static string CreateHash(string password)
        {
            // Generate a random salt
            RNGCryptoServiceProvider csprng = new RNGCryptoServiceProvider();
            byte[] salt = new byte[SALT_BYTE_SIZE];
            csprng.GetBytes(salt);

            // Hash the password and encode the parameters
            byte[] hash = PBKDF2(password, salt, PBKDF2_ITERATIONS, HASH_BYTE_SIZE);
            return PBKDF2_ITERATIONS + ":" +
                Convert.ToBase64String(salt) + ":" +
                Convert.ToBase64String(hash);
        }

        public static string ExtractHashPassword(string hashedPassword)
        {
            if (string.IsNullOrWhiteSpace(hashedPassword))
                return hashedPassword;

            string[] tokens = hashedPassword.Split(':');

            if (tokens.Length < 3)
                return "";

            return tokens[PBKDF2_INDEX];
        }

        public static int ExtractIterations(string hashedPassword)
        {
            int iterations = -1;
            if (string.IsNullOrWhiteSpace(hashedPassword))
                return iterations;

            string[] tokens = hashedPassword.Split(':');

            if (tokens.Length < 3)
                return iterations;

            int.TryParse(tokens[ITERATION_INDEX], out iterations);

            return iterations;
        }

        /// <summary>
        /// This is only passwords hashed with this class.
        /// </summary>
        /// <param name="hashedPassword"></param>
        public static string ExtractSalt(string hashedPassword)
        {
            if (string.IsNullOrWhiteSpace(hashedPassword))
                return hashedPassword;

            string[] tokens = hashedPassword.Split(':');

            if (tokens.Length < 3)
                return "";

            return tokens[SALT_INDEX];
        }

        public static bool IsCommonPassword(string testPassword)
        {
            if (string.IsNullOrWhiteSpace(testPassword))
                return true;

            try
            {
                string pathToPasswords = Path.Combine(EnvironmentEx.AppDataFolder.Replace("\\\\", "\\"), "WordLists\\CommonPasswords.txt");

                if (!File.Exists(pathToPasswords))
                {
                    Debug.Assert(false, "PASSWORD FILE IS MISSING");
                    return true;
                }

                string[] passwords = File.ReadAllLines(pathToPasswords);

                foreach (string password in passwords)
                {
                    if ((password?.EqualsIgnoreCase(testPassword) ?? false))
                        return true;
                }
            }
            catch
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Validates a password given a hash of the correct one.
        /// </summary>
        /// <param name="password">The password to check.</param>
        /// <param name="correctHash">A hash of the correct password.</param>
        /// <returns>True if the password is correct. False otherwise.</returns>
        public static bool ValidatePassword(string password, string correctHash)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(correctHash))
                return false;

            // Extract the parameters from the hash
            char[] delimiter = { ':' };
            string[] split = correctHash.Split(delimiter);

            if (split.Length < 3)
                return false;

            int iterations;
            if (!int.TryParse(split[ITERATION_INDEX], out iterations))
                return false;

            byte[] salt = Convert.FromBase64String(split[SALT_INDEX]);
            byte[] hash = Convert.FromBase64String(split[PBKDF2_INDEX]);

            byte[] testHash = PBKDF2(password, salt, iterations, hash.Length);

            if (testHash == null)
                return false;

            return SlowEquals(hash, testHash);
        }

        /// <summary>
        /// Computes the PBKDF2-SHA1 hash of a password.
        /// </summary>
        /// <param name="password">The password to hash.</param>
        /// <param name="salt">The salt.</param>
        /// <param name="iterations">The PBKDF2 iteration count.</param>
        /// <param name="outputBytes">The length of the hash to generate, in bytes.</param>
        /// <returns>A hash of the password.</returns>
        private static byte[] PBKDF2(string password, byte[] salt, int iterations, int outputBytes)
        {
            byte[] res = null;
            try
            {
                Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(password, salt);
                pbkdf2.IterationCount = iterations;
                res = pbkdf2.GetBytes(outputBytes);
            }
            catch (Exception)
            {//
            }

            return res;
        }

        /// <summary>
        /// Compares two byte arrays in length-constant time. This comparison
        /// method is used so that password hashes cannot be extracted from
        /// on-line systems using a timing attack and then attacked off-line.
        /// </summary>
        /// <param name="a">The first byte array.</param>
        /// <param name="b">The second byte array.</param>
        /// <returns>True if both byte arrays are equal. False otherwise.</returns>
        private static bool SlowEquals(byte[] a, byte[] b)
        {
            uint diff = (uint)a.Length ^ (uint)b.Length;
            for (int i = 0; i < a.Length && i < b.Length; i++)
                diff |= (uint)(a[i] ^ b[i]);
            return diff == 0;
        }

        #region Wordpress password hash

        private static string itoa64 = "./0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

        /// </summary>
        /// <param name="password"></param>
        /// <param name="hash"> the hashed password</param>
        /// <returns></returns>
        public static bool wpIsValidWordpressPassword(string password, string hash)
        {
            string output = string.Empty;
            if (hash == null)
            {
                return false;
            }

            if (hash.StartsWith(output))
                output = "";

            string id = hash.Substring(0, 3);
            // We use "$P$", phpBB3 uses "$H$" for the same thing
            if (id != "$P$" && id != "$H$")
                return false;

            // get who many times will generate the hash
            int count_log2 = itoa64.IndexOf(hash[3]);
            if (count_log2 < 7 || count_log2 > 30)
                return false;

            int count = 1 << count_log2;

            string salt = hash.Substring(4, 8);
            if (salt.Length != 8)
                return false;

            byte[] hashBytes = { };
            using (MD5 md5Hash = MD5.Create())
            {
                hashBytes = md5Hash.ComputeHash(Encoding.ASCII.GetBytes(salt + password));
                byte[] passBytes = Encoding.ASCII.GetBytes(password);
                do
                {
                    hashBytes = md5Hash.ComputeHash(hashBytes.Concat(passBytes).ToArray());
                } while (--count > 0);
            }

            output = hash.Substring(0, 12);
            string newHash = Encode64(hashBytes, 16);

            string hashedPassword = output + newHash;

            if (hashedPassword == hash)
                return true;

            return false;
        }

        public static string wpHashPasswordForWordpress(string password)  
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length > 4096)
                return string.Empty;

            //I. Gerate a salt
            #region gensalt_private
            var random = Cipher.GenerateKey();

            if (random.Length < 6)
                random = Cipher.RandomString(6);

            // int iteration_count_log2 = 8; <= this is normally a parameter
            // php if wierd. They call a function get salt to generate a salt, then name it setting
            // as a parameter to encrypt.... which then creates a salt.
            string setting = "$P$" + itoa64[Math.Min(8 + 5, 30)];
            byte[] saltBytes = Encoding.ASCII.GetBytes(random);
            setting += Encode64(saltBytes, 6);
            #endregion

            #region crypt_private
            //NOTE: setting is the salt.
            string output = "*0";

            if (setting.Substring(0, 2) == output)
                output = "*1";

            string id = setting.Substring(0, 3); // 3 may need to be changed to 5

            //# We use "$P$", phpBB3 uses "$H$" for the same thing 
            if (id != "$P$" && id != "$H$")
                return string.Empty;

            int count_log2 = itoa64.IndexOf(setting[3]);

            if (count_log2 < 7 || count_log2 > 20) {
                Debug.Assert(false, "shouldn't be here");
                return string.Empty;
            }

            int count = 1 << count_log2;

            string salt = setting.Substring(4, 8);

            if (salt.Length != 8)
                return string.Empty;

            //# We're kind of forced to use MD5 here since it's the only
            //# cryptographic primitive available in all versions of PHP
            //# currently in use.  To implement our own low-level crypto
            //# in PHP would result in much worse performance and
            //# consequently in lower iteration counts and hashes that are
            //# quicker to crack (by non-PHP code).
            byte[] hashBytes = { };
            using (MD5 md5Hash = MD5.Create())
            {
                hashBytes = md5Hash.ComputeHash(Encoding.ASCII.GetBytes(salt + password));
                byte[] passBytes = Encoding.ASCII.GetBytes(password);
                do
                {
                    hashBytes = md5Hash.ComputeHash(hashBytes.Concat(passBytes).ToArray());
                } while (--count > 0);
            }
            output = setting.Substring(0, 12);
            output += Encode64(hashBytes, 16);

            #endregion
            if(output.Length == 34)
                return output;

            return string.Empty;
        }

        static string Encode64(byte[] input, int count)
        {
            StringBuilder sb = new StringBuilder();
            int i = 0;
            do
            {
                int value = (int)input[i++];
                sb.Append(itoa64[value & 0x3f]); // to uppercase
                if (i < count)
                    value = value | ((int)input[i] << 8);
                sb.Append(itoa64[(value >> 6) & 0x3f]);
                if (i++ >= count)
                    break;
                if (i < count)
                    value = value | ((int)input[i] << 16);
                sb.Append(itoa64[(value >> 12) & 0x3f]);
                if (i++ >= count)
                    break;
                sb.Append(itoa64[(value >> 18) & 0x3f]);
            } while (i < count);

            return sb.ToString();
        }
    }
    #endregion
}
     
