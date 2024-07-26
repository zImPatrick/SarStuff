

using System.Diagnostics;
using System.Text;

namespace SarExtract
{
    internal class Program
    {
        static void Main(string[] args)
        {
            BinaryReader reader = new BinaryReader(File.OpenRead("data.sar"));
            Debug.Assert(reader.ReadBytes(6) != Encoding.ASCII.GetBytes("SARCFV"));
            Debug.Assert(reader.ReadBytes(2) != new byte[] { 0x01, 0x01 });

            var tableEntries = reader.ReadInt32();
            var tableOffset = reader.ReadInt32();

            reader.BaseStream.Seek(tableOffset, SeekOrigin.Begin);

            //string map = "";

            for (int i = 0; i < tableEntries; i++) {
                var strLength = reader.ReadByte();
                var fileName = Encoding.ASCII.GetString(reader.ReadBytes(strLength + 1));
                var offset = reader.ReadUInt32();
                var length = reader.ReadUInt32();
                reader.ReadBytes(4); // kp wofür diese bytes sind

                var currentPosition = reader.BaseStream.Position;

                var fileNameNormalized = Path.GetFileName(fileName);
                //map += fileNameNormalized.Substring(0, fileNameNormalized.Length - 1) + ":" + fileName.Substring(0, fileName.Length - 1) + "\r\n";
                var fs = File.OpenWrite(fileNameNormalized.Substring(0, fileNameNormalized.Length - 1));
                reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                fs.Write(reader.ReadBytes((int) length));
                fs.Close();
                reader.BaseStream.Seek(currentPosition, SeekOrigin.Begin);
            }

            //File.WriteAllText("map.txt", map);

        }
    }
}
