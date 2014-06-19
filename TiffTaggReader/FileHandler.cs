using System;
using System.IO;

namespace TiffTaggReader
{
    public static class FileHandler
    {
        public static byte[] ReadFile(string path)
        {
            return File.ReadAllBytes(path);            
        }

        public static void WriteFile (string path, byte[] array)
        {
            try
            {
                File.WriteAllBytes(path, array);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);                
            }
            
        }
    }
}
