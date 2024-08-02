

using System.Text;

namespace SarExtract
{
    internal class Program
    {
        static void Main(string[] args)
        {
            BinaryReader reader = new BinaryReader(File.OpenRead("data.sar"));
            if (Encoding.ASCII.GetString(reader.ReadBytes(6)) != "SARCFV")
            {
                throw new InvalidDataException("Data does not begin with SARCFV");
            }

            var dieseZweiBytes = reader.ReadBytes(2);
            if (dieseZweiBytes.Equals(new byte[] { 0x01, 0x01 }))
            {
                // Ich bin mir nicht sicher, ob dass wirklich eine Version Number ist, aber wer weiß?
                throw new InvalidDataException("Unsupported version?");
            }
            

            var tableEntries = reader.ReadInt32();
            var tableOffset = reader.ReadInt32();

            reader.BaseStream.Seek(tableOffset, SeekOrigin.Begin);

            string map = "";

            Console.WriteLine($"Reading {tableEntries} entries");
            for (int i = 0; i < tableEntries; i++) {
                var strLength = reader.ReadByte();
                var fileName = Encoding.ASCII.GetString(reader.ReadBytes(strLength));
                // strLength hat danach noch \00
                reader.ReadByte();
                var offset = reader.ReadUInt32();
                var length = reader.ReadUInt32();
                reader.ReadBytes(4); // kp wofür diese bytes sind

                Console.WriteLine($"Reading {fileName}");

                var currentPosition = reader.BaseStream.Position;

                var fileNameNormalized = Path.GetFileName(fileName);
                map += fileNameNormalized + ":" + fileName + "\r\n";

                var fs = File.OpenWrite(fileNameNormalized);
                reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                fs.Write(reader.ReadBytes((int) length));
                fs.Close();

                reader.BaseStream.Seek(currentPosition, SeekOrigin.Begin);
            }

            File.WriteAllText("map.txt", map);

        }
    }
}
