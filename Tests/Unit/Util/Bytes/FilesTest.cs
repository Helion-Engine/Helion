using System.IO;
using FluentAssertions;
using Helion.Tests.Helpers;
using Helion.Util.Bytes;
using Xunit;

namespace Helion.Tests.Unit.Util.Bytes
{
    public class FilesTest
    {
        [Fact(DisplayName = "Calculate MD5 of file")]
        public void TestMD5TempFile()
        {
            const string data = "hi";
            const string dataMD5 = "49f68a5c8493ec2c0bf489821c21fc3b";
            string path = FileHelper.GetUniqueTempFilePath();

            using (var handle = File.CreateText(path))
                handle.Write(data);

            Files.CalculateMD5(path).Should().Be(dataMD5);
        }
    }
}
