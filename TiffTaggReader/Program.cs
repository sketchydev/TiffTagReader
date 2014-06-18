using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TiffTaggReader
{
    class Program
    {
        static void Main(string[] args)
        {
            var bytesFile = File.ReadAllBytes(@"C:\Users\DEV\Downloads\example_small.tif");            
            
            string hex = BitConverter.ToString(bytesFile);

            string[] hexFile = hex.Split(new Char[] { '-' }).ToArray();
            string tiffId,ifdOffset;
            string byteOrder = hexFile[0] + hexFile[1];
            bool littleEndian = (byteOrder == "4949");
            if (littleEndian)
            {
                tiffId = hexFile[3] + hexFile[2];
                ifdOffset = hexFile[7] + hexFile[6] + hexFile[5] + hexFile[4];
            }
            else
            {
                tiffId = hexFile[2] + hexFile[3];
                ifdOffset = hexFile[4] + hexFile[5] + hexFile[6] + hexFile[7];
            }


            Console.WriteLine("byteOrder: " + byteOrder);
            Console.WriteLine("tiffId: " + tiffId + "(" + Convert.ToInt32(tiffId, 16) + ")");
            Console.WriteLine("IFDOffset: " + ifdOffset + "(" + Convert.ToInt32(ifdOffset,16)+")");

            while (Convert.ToInt32(ifdOffset, 16) > 0)
            {
                //read IFDs
                var intOffset = Convert.ToInt32(ifdOffset, 16);
                var ifdCount = hexFile[intOffset + 1] + hexFile[intOffset];
                var intIfdCount = Convert.ToInt32(ifdCount, 16);

                var ifdString = new string[intIfdCount][];

                for (int i = 0; i < intIfdCount; i++)
                {
                    var ifdBytes = new string[12];
                    Array.Copy(hexFile, intOffset + 2 + (i*12), ifdBytes, 0, 12);
                    ifdString[i] = ConvertIfdArrayToString(ifdBytes, littleEndian, ref hexFile);
                }

                var nextIfdOffsetBase = intOffset + 2 + (intIfdCount*12);

                if (littleEndian)
                {
                    ifdOffset = hexFile[nextIfdOffsetBase + 3] + hexFile[nextIfdOffsetBase + 2] +
                                          hexFile[nextIfdOffsetBase + 1] + hexFile[nextIfdOffsetBase];
                    
                    
                }
                else
                {
                    ifdOffset = hexFile[nextIfdOffsetBase] + hexFile[nextIfdOffsetBase + 1] +
                                        hexFile[nextIfdOffsetBase + 2] + hexFile[nextIfdOffsetBase + 3];
                }
                Console.WriteLine("Next IFD at offset: " + Convert.ToInt32(ifdOffset, 16));
            }


            Console.ReadLine();
        }

        public static string[] ConvertIfdArrayToString(string[] ifdArray, bool littleEndian, ref string[] file)
        {
            /*
             * 12 byte array:
             * Bytes 0-1: The Tag that identifies the field. NOTE: TAGS ARE ALWAYS IN NUMERIC ORDER
             * Bytes 2-3: The field Type.
                        1 = BYTE 8-bit unsigned integer.
                        2 = ASCII 8-bit byte that contains a 7-bit ASCII code; the last byte
                            must be NUL (binary zero).
                        3 = SHORT 16-bit (2-byte) unsigned integer.
                        4 = LONG 32-bit (4-byte) unsigned integer.
                        5 = RATIONAL Two LONGs: the first represents the num
                        6 = SBYTE An 8-bit signed (twos-complement) integer.
                        7 = UNDEFINED An 8-bit byte that may contain anything, depending on
                            the definition of the field.
                        8 = SSHORT A 16-bit (2-byte) signed (twos-complement) integer.
                        9 = SLONG A 32-bit (4-byte) signed (twos-complement) integer.
                        10 = SRATIONAL Two SLONG’s: the first represents the numerator of a
                            fraction, the second the denominator.
                        11 = FLOAT Single precision (4-byte) IEEE format.
                        12 = DOUBLE Double precision (8-byte) IEEE format.
             * Bytes 4-7: The number of values, Count of the indicated Type : NOTE the count is in terms of values not bytes e.g. a LONG has a value of 1 not 4
             * Bytes 8-11: The Value/Offset...if the value is a single value which fits into 4 bytes this is the value - if not then it's the offset to the real location of
             *            the values...fun eh?
             */
            var retval = new string[4];
            if (littleEndian)
            {
                retval[0] = ifdArray[1] + ifdArray[0];
                retval[1] = ifdArray[3] + ifdArray[2];
                retval[2] = ifdArray[7] + ifdArray[6] + ifdArray[5]+ifdArray[4];
                retval[3] = ifdArray[11] + ifdArray[10] + ifdArray[9] + ifdArray[8];
            }
            else
            {
                retval[0] = ifdArray[0] + ifdArray[1];
                retval[1] = ifdArray[2] + ifdArray[3];
                retval[2] = ifdArray[4] + ifdArray[5] + ifdArray[6] + ifdArray[7];
                retval[3] = ifdArray[8] + ifdArray[9] + ifdArray[10] + ifdArray[11];
            }

            var sb = new StringBuilder();

            sb.Append(TagConverter(Convert.ToInt32(retval[0], 16)));
            sb.Append(" : " + ValueConverter(
                retval[0], 
                retval[3], 
                Convert.ToInt32(retval[1], 16) , 
                Convert.ToInt32(retval[2], 16), 
                ref file ));            
            Console.WriteLine(sb.ToString());

            return retval;
        }

        public static string GetRational(int offset, int count, ref string[]file)
        {
            var retval = new StringBuilder();
            for (int i = 0; i < count; i++)
            {
                var sb = new StringBuilder();
                for (int j = 0; j <4 ; j++)
                {
                    sb.Append(file[offset + j +(8*i)]);
                }
                var numerator = Convert.ToInt32(sb.ToString(),16);
                sb.Clear();
                for (int j = 4; j < 8; j++)
                {
                    sb.Append(file[offset + j + (8 * i)]);
                }
                var denominator = Convert.ToInt32(sb.ToString(), 16);

                retval.Append(numerator/denominator);
                retval.Append(",");
            }

            return retval.ToString().TrimEnd(',');
        }


        public static string GetByType(int offset, int type, ref string[] file)
        {
            int wordsize;
            switch (type)
            {
                    case 3:
                    {
                        wordsize = 2;
                        break;
                    }
                    case 4:
                    {
                        wordsize = 4;
                        break;
                    }
                default:
                    wordsize = 1;
                    break;
            }

            var sb = new StringBuilder();
            for (int i = wordsize-1; i >= 0; i--)
            {                
                sb.Append(file[offset + i]);
            }
            return Convert.ToInt32(sb.ToString(),16).ToString();
        }


        public static string ValueConverter(string tag, string value, int type, int count, ref string[] file)
        {
            if (count == 1 && !(new List<int>{5,10,12}.Contains(type)))
            {
                return Convert.ToInt32(value, 16).ToString();
            }
            if (new List<int> { 5, 10, 12 }.Contains(type))
            {
                return GetRational(Convert.ToInt32(value, 16), count, ref file);
            }
            var sb = new StringBuilder();
            for (int i = 0; i <count ; i++)
            {
                sb.Append(GetByType(Convert.ToInt32(value, 16), type, ref file));                
                sb.Append(",");
            }            

            return sb.ToString().TrimEnd(',');
        }

        /// <summary>
        /// takes a raw value for an IFD tag and returns human readable text
        /// </summary>
        /// <param name="rawValue"></param>
        /// <returns></returns>
        public static string TagConverter(int rawValue)
        {
            switch (rawValue)
            {
                case 262: { return "PhotometicInterpretation";} //required field for Bilevel images
                case 259: { return "Compression"; } //required field for Bilevel images
                case 257: { return "ImageLength"; } //required field for Bilevel images
                case 256: { return "ImageWidth"; } //required field for Bilevel images
                case 296: { return "ResolutionUnit"; } //required field for Bilevel images
                case 282: { return "XResolution"; } //required field for Bilevel images
                case 283: { return "YResolution"; } //required field for Bilevel images
                case 278: { return "RowsPerStrip"; } //required field for Bilevel images
                case 273: { return "StripOffsets"; } //required field for Bilevel images
                case 279: { return "StripByteCounts"; } //required field for Bilevel images
                case 258: { return "BitsPerSample"; } //additional required field for GreyScale images & Palette Color images &RGB
                case 320: { return "ColorMap"; } //additional required field for Palette Color images & RGB
                case 277: { return "SamplesPerPixel"; } //additional required field for RGB Images               
                case 274: { return "Orientation"; }
                case 284: { return "PlanarConfiguration"; }
                case 297: { return "PageNumber"; }
                case 305: { return "Software"; }
                case 347: { return "JPEGTables"; }
                case 530: { return "YCbCrSubSampling"; }
                default:
                    return "UNKNOWN TAG ("+rawValue+")";
            }

            
        }

    }
}