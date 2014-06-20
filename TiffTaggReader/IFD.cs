namespace TiffTaggReader
{
    public class IFD
    {
        public int EntryCount;
        public string NextOffset;
        public int IntNextOffset;

        public IFDEntry[] Entries;
        public bool isValid;

    }

    public struct IFDEntry
    {
        public string Raw;

        public string RawTag;
        public string Tag;
        public int IntTag;

        public string Type;
        public int IntType;

        public int Count;

        public string RawValue;
        public string DecodedValue;
        public int IntIFDValueOffset;

        public int IFDIndex;        
        public string IFDOffset;  //offset of IFD tag from Start of File
        public int IntIFDOffset; //offset of IFD tag from Start of File - integer format

    }
}
