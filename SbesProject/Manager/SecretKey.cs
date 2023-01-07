using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Manager
{
    public class SecretKey
    {
		public static string GenerateKey()
		{
			SymmetricAlgorithm symmAlgorithm = TripleDESCryptoServiceProvider.Create();

			return symmAlgorithm == null ? String.Empty : ASCIIEncoding.ASCII.GetString(symmAlgorithm.Key);
		}

		public static void StoreKey(string secretKey, string outFile)
		{
			FileStream fOutput = new FileStream(outFile, FileMode.OpenOrCreate, FileAccess.Write);
			byte[] buffer = Encoding.ASCII.GetBytes(secretKey);

			try
			{
				fOutput.Write(buffer, 0, buffer.Length);
			}
			catch (Exception e)
			{
				Console.WriteLine("SecretKeys.StoreKey:: ERROR {0}", e.Message);
			}
			finally
			{
				fOutput.Close();
			}
		}

		public static string LoadKey(string inFile)
		{
			FileStream fInput = new FileStream(inFile, FileMode.Open, FileAccess.Read);
			byte[] buffer = new byte[(int)fInput.Length];

			try
			{
				fInput.Read(buffer, 0, (int)fInput.Length);
				fInput.Close();
				return ASCIIEncoding.ASCII.GetString(buffer);
			}
			catch (Exception e)
			{
				Console.WriteLine("SecretKeys.LoadKey:: ERROR {0}", e.Message);
				fInput.Close();
				return null;
			}
		}
	}
}
