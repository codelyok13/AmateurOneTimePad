using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;

namespace OneTimeLock
{
    class Program
    {
        static Random r; //Generates random values
        static byte[][] data = new byte[2][]; //stores encrypted data and key

        /*
            The largest size allowable for an array is some value below 2GBs. 
            Exact value couldn't be  found
        */  
        const long sizeToLargeForAnArray = (long)2E9; 

        static void Main(string[] args)
        {
            if(args.Length > 2)
            {
                throw new Exception("To many arguments");
            }
            foreach(string arg in args)
            {
                if(!File.Exists(arg))
                {
                    throw new Exception($"This path |{arg}| doesn't exist");
                    //Console.WriteLine("The path {0} doesn't exist", arg);
                    //return;
                }
                FileInfo fileInfo = new FileInfo(arg);
                /*
                    FileInfo returns values in bytes and an array in c# can hold about 2 Gigabytes
                    So the length of the file must be less than 2 billion bytes
                */
                long fileSize = (fileInfo.Length); 
                
                if(fileSize >= sizeToLargeForAnArray)
                {
                    throw new Exception($"This file is to large.\nCurrent Size: {fileSize}\nMaximum Size: {sizeToLargeForAnArray}\nDifference: {fileSize - sizeToLargeForAnArray}");
                }
            }

            bool encrypted = false;
            switch(args[0]){
                case string s when ((Path.GetExtension(s)).Contains("onetp") && args.Length == 2):
                    ParallelDecrypt(args[0], args[1]);
                    break;
                default:
                    ParallelEncrypt(args[0]);
                    encrypted = true;
                    break;
            }
            
            string directory = Directory.GetCurrentDirectory();
            string message = encrypted ? $"The encrypted file and key is stored in {directory}":$"The decrypted file is stored in {directory}";
            Console.WriteLine(message);
        }

        static void ParallelEncrypt(string path)
        {
            data[0] = File.ReadAllBytes(path);
            data[1] = new byte[data[0].Length];
            r = new Random();
            r.NextBytes(data[1]);

            string pathName = Path.GetFileName(path);
            
            Parallel.For(0, data[0].Length, ParallelEncryptMethod);

            File.WriteAllBytes(pathName+".onetpm",data[0]);
            File.WriteAllBytes(pathName+".keym",data[1]);
        }
        static void ParallelEncryptMethod(int i)
        {
            data[0][i] = (byte)(data[0][i]+data[1][i]);
        }
    
        static void ParallelDecrypt(string encryptedText, string key)
        {
            data[0] = File.ReadAllBytes(encryptedText);
            data[1] = File.ReadAllBytes(key);

            string nameOfFile = Path.GetFileNameWithoutExtension(key);

            Parallel.For(0, data[0].Length, ParallelDecryptMethod);

            File.Delete(encryptedText);
            File.Delete(key);
            File.WriteAllBytes(nameOfFile,data[0]);
        }

        static void ParallelDecryptMethod(int i)
        {
            data[0][i] = (byte)(data[0][i] - data[1][i]);
        }
    }
}
