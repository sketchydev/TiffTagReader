using System;

namespace TiffTaggReader
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("0-Read only");
            Console.WriteLine("1-InvertFirstPage");
            Console.WriteLine("2-Move First IFD to File End");
            Console.WriteLine("3-ConvertToSinglePage");
            Console.WriteLine("4-CheckForMissingData");
            
            switch (Console.ReadKey().KeyChar)
            {
                case '0':
                    {
                        var tagReader = new TagReader();
                        tagReader.ReadTags(@"D:\Users\shanebo\Downloads\multipage_broken.tif");
                        break;
                    }
                case '1':
                    {
                        PageFlipper.FlipPageOne(@"D:\Users\shanebo\Downloads\single_page.tif", @"D:\Users\shanebo\Downloads\single_page_flipped.tif");
                        break;
                    }

                case '2':
                    {
                        var fixer = new IFDFixer();
                        fixer.MoveFirstIfdtoEOF(@"D:\Users\shanebo\Downloads\single_page.tif", @"D:\Users\shanebo\Downloads\single_page_IFD_Moved.tif");
                        break;
                    }

                case '3':
                    {
                        var fixer = new IFDFixer();
                        fixer.ConvertToSinglePage(@"D:\Users\shanebo\Downloads\multipage_broken.tif", @"D:\Users\shanebo\Downloads\multipage_broken_fixed.tif");
                        break;
                    }

                case '4':
                    {
                        var tagReader = new TagReader();
                        tagReader.CheckForMissingData(@"D:\Users\shanebo\Downloads\multipage_broken.tif");
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