using System;

namespace TiffTaggReader
{
    public static class PageFlipper
    {
        //Find the location in the file for Photometric in IFD 1


        public static void FlipPageOne(string inPath, string outPath)
        {
            var bytesFile = FileHandler.ReadFile(inPath);
            var hexFile = TagReader.ByteToHexArray(bytesFile);
            var tagReader = new TagReader(hexFile);

            var header = tagReader.ReadHeader();
            var IFD = tagReader.ReadIfD(hexFile, header[2]);

            //Flip it - IntTag - IFD.Entries[4].IntIFDValueOffset
            var byteToFlip = 0;
            var bytevalue = 0;
            foreach (var entry in IFD.Entries)
            {
                if (entry.IntTag == 262)
                {
                    byteToFlip = entry.IntIFDValueOffset;
                }
            }

            if (bytesFile[byteToFlip]==0)
            {
                bytesFile[byteToFlip] = 1;
            }
                            if (bytesFile[byteToFlip]==1)
            {
                bytesFile[byteToFlip] = 0;
            }


            //ReRead it
             hexFile = TagReader.ByteToHexArray(bytesFile);
             tagReader = new TagReader(hexFile);

             header = tagReader.ReadHeader();
             tagReader.ReadIfD(hexFile, header[2]);

            FileHandler.WriteFile(outPath,bytesFile);

            Console.ReadKey();

        }

    }
}
