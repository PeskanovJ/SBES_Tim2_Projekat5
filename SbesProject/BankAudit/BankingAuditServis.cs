using BankContracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BankAudit
{
    public class BankingAuditServis : IBankAudit
    {
        public void AccessLog(string bank, string account, DateTime accessTime, List<string> logs)
        {
            Console.WriteLine("--------------------------------------------");
            Console.WriteLine("Access limit breached\n");
            string resultString;

            string fileInput = "Source name: " + bank + "\n";
            fileInput += "Account user: " + account + "\n";
            fileInput += "Access time: " + accessTime.ToString() + "\n";

            foreach (string s in logs)
            {
                resultString = Regex.Match(s, @"\d+").Value;

                fileInput += "Amount: " + resultString + "\n";
                fileInput += "--------------------------------------------\n";
            }

            Console.WriteLine(fileInput);

            string path = "..\\..\\bankingAudit.txt";
           
            using (StreamWriter outputFile = new StreamWriter(path, true))
            {
                outputFile.WriteLine(fileInput);
            }
        }
    }
}
