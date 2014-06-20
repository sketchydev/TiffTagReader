using System;

namespace TiffTaggReader
{
    public class IFDFixer
    {

        public void MoveFirstIfdtoEOF(string inPath, string outPath)
        {
            var bytesFile = FileHandler.ReadFile(inPath);
            var hexFile = TagReader.ByteToHexArray(bytesFile);
            var tagReader = new TagReader(hexFile);

            var header = tagReader.ReadHeader();
            var IFD = tagReader.ReadIfD(hexFile, header[2]);
 
            //An Image File Directory (IFD) consists of a 2-byte count of the number of directory
            //entries (i.e., the number of fields), followed by a sequence of 12-byte field
            //entries, followed by a 4-byte offset of the next IFD (or 0 if none). (Do not forget to
            //write the 4 bytes of 0 after the last IFD.)
            
            var IFDSize = 2 + IFD.EntryCount*12 + 4;

            var rawIFD = new byte[IFDSize];            
            Array.Copy(bytesFile, Convert.ToInt32(header[2], 16), rawIFD, 0, IFDSize);

            var newFileBytes = new byte[bytesFile.Length + IFDSize];
            Array.Copy(bytesFile, 0, newFileBytes, 0, bytesFile.Length);
            Array.Copy(rawIFD, 0, newFileBytes, bytesFile.Length, rawIFD.Length);

            //change bytes 4-7 of newfile byte array to be the length of the original bytesfile
            var newOffset = IntToLittleEndian4(bytesFile.Length);

            Array.Copy(newOffset, 0, newFileBytes, 4, 4);

            //check the copy has worked
            hexFile = TagReader.ByteToHexArray(newFileBytes);
            tagReader = new TagReader(hexFile);
            header = tagReader.ReadHeader();
            var newIFD = tagReader.ReadIfD(hexFile, header[2]);

            //write out new file
            FileHandler.WriteFile(outPath, newFileBytes);

            Console.ReadKey();


        }

        public void ConvertToSinglePage(string inPath, string outPath)
        {
            var bytesFile = FileHandler.ReadFile(inPath);
            var hexFile = TagReader.ByteToHexArray(bytesFile);
            var tagReader = new TagReader(hexFile);

            var header = tagReader.ReadHeader();
            var IFD = tagReader.ReadIfD(hexFile, header[2]);

            var ifdSize = 2 + IFD.EntryCount * 12 + 4;
            var offsetOfNextOffset = Convert.ToInt32(header[2], 16) + ifdSize - 4;
            var zeroWord = new byte[]{0, 0, 0, 0};    
            Array.Copy(zeroWord, 0, bytesFile, offsetOfNextOffset, 4);

            //write out new file
            FileHandler.WriteFile(outPath, bytesFile);

            Console.ReadKey();


        }

        public byte[] IntToLittleEndian4(int data)
        {
            var b = new byte[4];
            b[0] = (byte)data;
            b[1] = (byte)(((uint)data >> 8) & 0xFF);
            b[2] = (byte)(((uint)data >> 16) & 0xFF);
            b[3] = (byte)(((uint)data >> 24) & 0xFF);
            return b;
        }        

        //public void TryToReadSecondIFDWithLengthOfFirst

    }
}
