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
        static byte[][] data; //stores encrypted data and key
        static byte[] randomBytes; //stores the random bytes made
        static byte[] file; //stores the file to be encrypted

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

        static void Encrypt(string path)
        {
            file = File.ReadAllBytes(path);
            r = new Random();

            string pathName = Path.GetFileName(path);

            using(FileStream fs1 = new FileStream(pathName+".onetp",FileMode.Create,FileAccess.ReadWrite))
            {
                using(FileStream fs2 = new FileStream(pathName+".key",FileMode.Create,FileAccess.ReadWrite))
                {
                    randomBytes = new byte[file.Length]; 

                    int i = 0;
                    r.NextBytes(randomBytes);
                    foreach(byte b in randomBytes)
                    {
                        fs2.WriteByte(b);
                        byte encrypt = (byte)(file[i]+b);
                        unchecked{ fs1.WriteByte(encrypt); }
                        i++;
                    }
                }
            }
        }
        
        static void Decrypt(string encryptedText, string key )
        {
            string nameOfFile = Path.GetFileNameWithoutExtension(key);
            using(FileStream fs3 = new FileStream(nameOfFile,FileMode.Create,FileAccess.ReadWrite))
            {
                byte[] byteFS1 = File.ReadAllBytes(encryptedText);
                byte[] byteFS2 = File.ReadAllBytes(key);
                Console.WriteLine(byteFS2.Length +" "+ byteFS1.Length);
                if(byteFS1.Length != byteFS2.Length){throw new Exception("The size of the key and encrypted text are diffrent.");}

                for(int i = 0; i < byteFS1.Length;i++)
                {
                    fs3.WriteByte((byte)(byteFS1[i]-byteFS2[i]));
                }
                
                File.Delete(encryptedText);
                File.Delete(key);
            }
        }

        static void ParallelEncrypt(string path)
        {
            file = File.ReadAllBytes(path);
            
            randomBytes = new byte[file.Length]; 
            r = new Random();
            r.NextBytes(randomBytes);

            string pathName = Path.GetFileName(path);
            
            
            //0 is onetp and 1 is key
            data = new byte[2][];
            for(int i = 0; i < 2; i++){data[i]=new byte[file.Length];} //Set the array to length of file
            
            Parallel.For(0, file.Length, ParallelEncryptMethod);

            File.WriteAllBytes(pathName+".onetpm",data[0]);
            File.WriteAllBytes(pathName+".keym",data[1]);
        }
        static void ParallelEncryptMethod(int i)
        {
            data[0][i] = (byte)(file[i]+randomBytes[i]);
            data[1][i] = randomBytes[i];
        }
    
        static void ParallelDecrypt(string encryptedText, string key)
        {
            data = new byte[2][];
            data[0] = File.ReadAllBytes(encryptedText);
            data[1] = File.ReadAllBytes(key);
            file = new byte[data[0].Length];
            string nameOfFile = Path.GetFileNameWithoutExtension(key);

            Parallel.For(0, file.Length, ParallelDecryptMethod);

            File.Delete(encryptedText);
            File.Delete(key);
            File.WriteAllBytes(nameOfFile,file);
        }

        static void ParallelDecryptMethod(int i)
        {
            file[i] = (byte)(data[0][i] - data[1][i]);
        }
    }
}
