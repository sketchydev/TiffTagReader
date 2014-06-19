using System;

namespace TiffTaggReader
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("0-Readonly");
            Console.WriteLine("1-InvertFirstPage");
            
            switch (Console.ReadKey().KeyChar)
            {
                case '0':
                    {
                        var tagReader = new TagReader();
                        tagReader.ReadTags(@"D:\Users\shanebo\Downloads\single_page.tif");
                        break;
                    }
                case '1':
                    {
                        PageFlipper.FlipPageOne(@"D:\Users\shanebo\Downloads\single_page.tif", @"D:\Users\shanebo\Downloads\single_page_flipped.tif");
                        break;
                    }
                default:
                    {
                        break;
                    }
            }              
        }
    }
}