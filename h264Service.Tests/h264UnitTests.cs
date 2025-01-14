using h264.Services;
using mp4.boxes;
using methods.testing;
namespace h264Service.Tests;

public class h264UnitTests
{
    [Fact]
    public void GetFirstLevelBoxesTest()
    {
        string fileName = @"C:\H264Decoder\Data\ftype.mp4";

        TestingMethods.GetFirstLevelBoxes(fileName);

        Assert.True(true, "created successfully!");
    }
}