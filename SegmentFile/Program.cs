using Microsoft.VisualBasic.FileIO;
using mp4.segmenter;


FactoryMethods factoryMethods = new FactoryMethods();

string getFilename()
{
    string filename = string.Format("ftype.mp4");
    return filename;
}

factoryMethods.GetFirstLevelBoxes(string.Format(@"C:\H264Decoder\SegmentFile\Data\{0}", getFilename()));
