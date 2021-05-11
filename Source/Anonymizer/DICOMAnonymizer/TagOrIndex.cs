using System.Globalization;
using Dicom;

namespace DICOMAnonymizer
{
    public class TagOrIndex
    {
        public bool IsTag { get; }
        public DicomTag Tag { get; set; }
        public int Index { get; set; } = -1;

        public TagOrIndex(DicomTag tag)
        {
            Tag = tag;
            IsTag = true;
        }

        public TagOrIndex(int index)
        {
            Index = index;
            IsTag = false;
        }

        public override string ToString()
        {
            if (IsTag)
            {
                return Tag.ToString();
            }
            return Index.ToString(CultureInfo.InvariantCulture);
        }
    }
}
