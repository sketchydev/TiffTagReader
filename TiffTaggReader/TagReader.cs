using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TiffTaggReader
{
    public class TagReader
    {
        public  bool LittleEndian;
        private  string[] _hexFile;
        private List<IFD> _ifds; 

        public static string[] ByteToHexArray(byte[] array)
        {
            string hex = BitConverter.ToString(array);

            return hex.Split(new[] { '-' }).ToArray();
        }

        public TagReader(string[] hexfile)
        {
            _hexFile = hexfile;
        }
        public TagReader(){}

        public  string[] ReadHeader()
        {
            var header = new string[3];

            var byteOrder = _hexFile[0] + _hexFile[1];
            header[0] = byteOrder;
            LittleEndian = (byteOrder == "4949");
            if (LittleEndian)
            {
                header[1] = _hexFile[3] + _hexFile[2];
                header[2] = _hexFile[7] + _hexFile[6] + _hexFile[5] + _hexFile[4];
            }
            else
            {
                header[1] = _hexFile[2] + _hexFile[3];
                header[2] = _hexFile[4] + _hexFile[5] + _hexFile[6] + _hexFile[7];
            }

            Console.WriteLine("byteOrder: " + byteOrder);
            Console.WriteLine("tiffId: " + header[1] + "(" + Convert.ToInt32(header[1], 16) + ")");
            Console.WriteLine("IFDOffset: " + header[2] + "(" + Convert.ToInt32(header[2], 16) + ")");

            return header;
        }

        public void CheckForMissingData(string filename)
        {
            ReadTags(filename);

            Console.WriteLine("File Size: {0} bytes ",_hexFile.Length);
            var ifdCount = 0;
            var ifdTotalSize = 0;
            var dataSize = 0;
            foreach (var ifd in _ifds)
            {
                Console.WriteLine("IFD {1} Base Size: {0} bytes ", (2 + 4 + (ifd.EntryCount * 12)),ifdCount);
                ifdTotalSize += (2 + 4 + (ifd.EntryCount * 12));                
               
                foreach (var ifdEntry in ifd.Entries)
                {
                    if (!(GetWordSize(ifdEntry.IntType) * ifdEntry.Count <= 4))
                    {                                                                   
                        ifdTotalSize += ifdEntry.Count*GetWordSize(ifdEntry.IntType);
                   }


                    if (ifdEntry.IntTag==279)
                    {
                        dataSize += ifdEntry.DecodedValue.Split(',').Select(int.Parse).Sum();
                    }

                }    
            
                ifdCount += 1;
            }

            Console.WriteLine("Size Of All IFDs: {0} bytes ", ifdTotalSize);
            Console.WriteLine("Size Of Known Data: {0} bytes ", dataSize);
            Console.WriteLine("Difference From File Size: {0} bytes ", (_hexFile.Length - dataSize) - ifdTotalSize);
            Console.WriteLine("Percentage Of File Accounted For= {0}%", ((decimal)(ifdTotalSize + dataSize) / _hexFile.Length)*100);
            Console.ReadKey();

        }

        public  IFD ReadIfD(string[] hexFile, string offset)
        {
            var thisIFD = new IFD();
            var intOffset = Convert.ToInt32(offset, 16);
            var ifdCount = hexFile[intOffset + 1] + hexFile[intOffset];
            var intIfdCount = Convert.ToInt32(ifdCount, 16);
            

            thisIFD.EntryCount = intIfdCount;
            thisIFD.Entries = new IFDEntry[intIfdCount];

            int intOffSetPointer = intOffset + 2;

            var ifdString = new string[intIfdCount][];

            for (var i = 0; i < intIfdCount; i++)
            {
                var ifdBytes = new string[12];
                Array.Copy(hexFile, intOffset + 2 + (i * 12), ifdBytes, 0, 12);
                ifdString[i] = ConvertIfdArrayToString(ifdBytes);

                var thisIFDEntry = new IFDEntry
                                       {
                                           RawTag = ifdString[i][0],
                                           
                                           IntTag = Convert.ToInt32(ifdString[i][0], 16)
                                       };
                foreach (var ifdByte in ifdBytes)
                {
                    thisIFDEntry.Raw += ifdByte;
                }

                //Tag
                thisIFDEntry.Tag = TagConverter(thisIFDEntry.IntTag);                

                //Type
                thisIFDEntry.Type = ifdString[i][1];
                thisIFDEntry.IntType = Convert.ToInt32(ifdString[i][1], 16);

                //Count
                thisIFDEntry.Count = Convert.ToInt32(ifdString[i][2], 16);

                //Value

                thisIFDEntry.RawValue = ifdString[i][3];
                thisIFDEntry.DecodedValue = ValueConverter(thisIFDEntry.RawTag, thisIFDEntry.RawValue,
                                                           thisIFDEntry.IntType, thisIFDEntry.Count);                                

                //Offsets
                thisIFDEntry.IFDIndex = i;
                thisIFDEntry.IntIFDOffset = intOffSetPointer +(i*12);
                thisIFDEntry.IFDOffset = thisIFDEntry.IntIFDOffset.ToString("X");
                thisIFDEntry.IntIFDValueOffset = thisIFDEntry.IntIFDOffset + 8;

                //Asign
                thisIFD.Entries[i] = thisIFDEntry;
            }            

            var nextIfdOffsetBase = intOffset + 2 + (intIfdCount * 12);

            if (LittleEndian)
            {offset = hexFile[nextIfdOffsetBase + 3] + hexFile[nextIfdOffsetBase + 2] + hexFile[nextIfdOffsetBase + 1] + hexFile[nextIfdOffsetBase];}
            else
            {offset = hexFile[nextIfdOffsetBase] + hexFile[nextIfdOffsetBase + 1] +hexFile[nextIfdOffsetBase + 2] + hexFile[nextIfdOffsetBase + 3];}
            
            thisIFD.NextOffset = offset;
            thisIFD.IntNextOffset = Convert.ToInt32(offset, 16);
            return thisIFD;
        }

        public  List<IFD> ReadAllIfDs(string[] hexFile, string ifdOffset )
        {
            var ifdList = new List<IFD>();
                       
            while (Convert.ToInt32(ifdOffset, 16) > 0)
            {
                //read IFD
                var thisIFD = ReadIfD(hexFile, ifdOffset);                

                Console.WriteLine("Next IFD at offset: " + thisIFD.IntNextOffset);
                ifdList.Add(thisIFD);
                ifdOffset = thisIFD.NextOffset;
            }
            return ifdList;
        }

        public void ReadTags(string filename)
        {
            var bytesFile = FileHandler.ReadFile(filename);            
            _hexFile = ByteToHexArray(bytesFile);

            var header = ReadHeader();
            
            _ifds = ReadAllIfDs(_hexFile, header[2]);

            Console.ReadLine();
        }

        public  string[] ConvertIfdArrayToString(string[] ifdArray)
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
            if (LittleEndian)
            {
                retval[0] = ifdArray[1] + ifdArray[0];
                retval[1] = ifdArray[3] + ifdArray[2];
                retval[2] = ifdArray[7] + ifdArray[6] + ifdArray[5] + ifdArray[4];
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
                Convert.ToInt32(retval[1], 16),
                Convert.ToInt32(retval[2], 16)));
            Console.WriteLine(sb.ToString());

            return retval;
        }

        public  string GetRational(int offset, int count)
        {
            var retval = new StringBuilder();
            for (int i = 0; i < count; i++)
            {
                var sb = new StringBuilder();
                for (int j = 0; j < 4; j++)
                {
                    sb.Append(_hexFile[offset + j + (8 * i)]);
                }
                var numerator = Convert.ToInt32(sb.ToString(), 16);
                sb.Clear();
                for (int j = 4; j < 8; j++)
                {
                    sb.Append(_hexFile[offset + j + (8 * i)]);
                }
                var denominator = Convert.ToInt32(sb.ToString(), 16);

                retval.Append(numerator / denominator);
                retval.Append(",");
            }

            return retval.ToString().TrimEnd(',');
        }

        public  string GetByType(int offset, int wordsize)
        {
            var sb = new StringBuilder();
            for (int i = wordsize - 1; i >= 0; i--)
            {
                sb.Append(_hexFile[offset + i]);
            }

            return Convert.ToInt32(sb.ToString(), 16).ToString();
        }

        public  int GetWordSize(int type)
        {
            switch (type)
            {
                case 3:
                    {
                        return 2;
                    }
                case 4:
                    {
                        return 4;
                    }
                default:
                    return 1;
            }
        }

        public  string ValueConverter(string tag, string value, int type, int count)
        {
            if (count == 1 && !(new List<int> { 5, 10, 12 }.Contains(type)))
            {
                return ValueProcessor(tag, Convert.ToInt32(value, 16).ToString(), type);
            }
            if (new List<int> { 5, 10, 12 }.Contains(type))
            {
                return GetRational(Convert.ToInt32(value, 16), count);
            }
            var sb = new StringBuilder();
            //value fits into 4 bytes so split accordingly
            if (GetWordSize(type) * count <= 4)
            {
                //split value into 1 2 or 4 bytes and order accordingly
                var byteArray = Enumerable.Range(0, value.Length / (GetWordSize(type) * 2)).Select(i => value.Substring(i * GetWordSize(type) * 2, GetWordSize(type) * 2)).ToArray();
                if (LittleEndian)//reverse
                {
                    Array.Reverse(byteArray);
                }
                foreach (var b in byteArray)
                {
                    sb.Append(Convert.ToInt32(b,16));
                    sb.Append(",");
                }
            }
            else  //value is an offset
            {
                var wordSize = GetWordSize(type);
                for (int i = 0; i < count; i++)
                {
                    sb.Append(GetByType(Convert.ToInt32(value, 16) + (wordSize * i), wordSize));
                    sb.Append(",");
                }
            }
            return ValueProcessor(tag, sb.ToString().TrimEnd(','), type);

        }

        public  string ValueProcessor(string tag, string value, int type)
        {
            var retval = value;
            if (type == 2) //ascii so convert
            {
                var chars = value.Split(',').Select(Int32.Parse);
                retval = chars.Aggregate(retval, (current, c) => current + ((char)c).ToString());
            }

            switch (Convert.ToInt32(tag, 16))
            {
                case 259: //compression
                    {
                        retval = GetCompressionValue(value);
                        break;
                    }

                case 262: //PhotometricInterpretation
                    {
                        retval = GetPhotometricInterpretation(value);
                        break;
                    }
                case 274: //GetOrientation
                    {
                        retval = GetOrientation(value);
                        break;
                    }

                case 284: //PlanarConfiguration
                    {
                        switch (value)
                        {
                            case "1":
                                {
                                    retval = "Contiguous";
                                    break;
                                }
                            case "2":
                                {
                                    retval = "Planar.";
                                    break;
                                }
                            default:
                                {
                                    retval = "Unknown";
                                    break;
                                }
                        }
                        break;
                    }
            }

            return retval;
        }

        public  string GetOrientation(string value)
        {
            switch (value)
            {
                case "1": { return "X,Y: Top Left (L>R, T>B)"; }
                case "2": { return "X,Y: Top Right (R>L, T>B)"; }
                case "3": { return "X,Y: Bottom Right (R>L, B>T)"; }
                case "4": { return "X,Y: Bottom Left (R>L, T>B)"; }
                case "5": { return "Y,X: Top Left (L>R, T>B)"; }
                case "6": { return "Y,X: Top Right (R>L, T>B)"; }
                case "7": { return "Y,X: Bottom Right (R>L, B>T)"; }
                case "8": { return "Y,X: Bottom Left (R>L, T>B)"; }
                default: { return "Unknown"; }
            }
        }

        public  string GetPhotometricInterpretation(string value)
        {
            switch (value)
            {
                case "0": { return "WhiteIsZero"; }
                case "1": { return "BlackIsZero."; }
                case "2": { return "RGB"; }
                case "3": { return "Palette Color"; }
                case "4": { return "Transparency Mask"; }
                default: { return "unknown Photometric Interpretation"; }
            }
        }

        public  string GetCompressionValue(string value)
        {
            switch (value)
            {
                case "1": { return "No Compression"; }
                case "2": { return "CCITT modified Huffman RLE"; }
                case "3": { return "CCITT Group 3 fax encoding"; }
                case "4": { return "CCITT Group 4 fax encoding"; }
                case "5": { return "LZW"; }
                case "6": { return "JPEG ('old-style' JPEG, later overriden in Technote2)"; }
                case "7": { return "JPEG ('new-style' JPEG) "; }
                case "8": { return "Deflate ('Adobe-style') "; }
                case "9": { return "Defined by TIFF-F and TIFF-FX standard (RFC 2301) as ITU-T Rec. T.82 coding, using ITU-T Rec. T.85 (which boils down to JBIG on black and white)"; }
                case "10": { return "Defined by TIFF-F and TIFF-FX standard (RFC 2301) as ITU-T Rec. T.82 coding, using ITU-T Rec. T.43 (which boils down to JBIG on color)"; }

                case "32773": { return "PACKBITS"; }
                case "32766": { return "NEXT"; }
                case "32771": { return "CCITTRLEW"; }
                case "32809": { return "THUNDERSCAN"; }
                case "32895": { return "IT8CTPAD"; }
                case "32896": { return "IT8LW"; }
                case "32897": { return "IT8MP"; }
                case "32898": { return "IT8BL"; }
                case "32908": { return "PIXARFILM"; }
                case "32909": { return "PIXARLOG"; }
                case "32946": { return "DEFLATE"; }
                case "32947": { return "DCS"; }
                case "34661": { return "JBIG"; }
                case "34676": { return "SGILOG"; }
                case "34677": { return "SGILOG24"; }
                case "34712": { return "JP2000"; }


                default:
                    return "Unknown Compression";

            }
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
                case 262: { return "PhotometricInterpretation"; } //required field for Bilevel images
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
                    return "UNKNOWN TAG (" + rawValue + ")";
            }
        }
    }
}
