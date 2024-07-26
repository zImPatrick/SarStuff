using System.Reflection.PortableExecutable;
using System.Text;

namespace SarPacker
{
    internal class Program
    {
        struct TableEntry
        {
            public string FileName;
            public int Offset;
            public int Length;
        }

        struct SourceTargetMapEntry
        {
            public string Source;
            public string Target;
        }

        static List<SourceTargetMapEntry> GetSourceToTargetMap()
        {
            var dict = new List<SourceTargetMapEntry>();
            var reader = File.OpenText(Path.Join("toPack", "map.txt"));

            while (!reader.EndOfStream)
            {
                var str = reader.ReadLine();
                var colon = str.IndexOf(":");
                dict.Add(new SourceTargetMapEntry
                {
                    Source = str.Substring(0, colon),
                    Target = str.Substring(colon + 1)
                });
            }

            reader.Close();
            return dict;
        }

        static void Main(string[] args)
        {
            var sourceToTarget = GetSourceToTargetMap();

            BinaryWriter writer = new BinaryWriter(File.OpenWrite("D:\\Sandboxie\\drive\\C\\Program Files (x86)\\Gabelstapler Simulator 2009\\data.sar"));
            writer.Write(Encoding.ASCII.GetBytes("SARCFV"));
            writer.Write(new byte[] { 0x01, 0x01 });

            var headerTableEntriesPosition = writer.BaseStream.Position;
            // es muss in den Header noch Entries & Offset geschrieben werden

            int tableEntries;
            int tableOffset;

            writer.Seek(8, SeekOrigin.Current);

            List<TableEntry> entries = new List<TableEntry>();
            foreach (var entry in sourceToTarget)
            {
                Console.WriteLine($"Adding file {entry.Target}");
                var position = writer.BaseStream.Position;
                var stream = File.OpenRead(Path.Join("toPack", entry.Source));

                int offset = 0;
                while (true)
                {
                    byte[] buffer = new byte[8192];
                    var readBytes = stream.Read(buffer, 0, buffer.Length);
                    offset += readBytes;
                    writer.Write(buffer, 0, readBytes);
                    if (readBytes == 0) break;
                }

                var length = writer.BaseStream.Position - position;
                entries.Add(new TableEntry
                {
                    FileName = entry.Target,
                    Length = (int)length,
                    Offset = (int)position
                });
            }

            // file table
            tableEntries = entries.Count;
            tableOffset = (int)writer.BaseStream.Position;

            foreach (var entry in entries)
            {
                Console.WriteLine($"Writing file entry of " + entry.FileName);
                var fileName = Encoding.ASCII.GetBytes(entry.FileName);

                writer.Write((byte) fileName.Length);
                writer.Write(fileName);
                writer.Write((byte) 0x00);
                writer.Write((System.UInt32)entry.Offset);
                writer.Write((System.UInt32) entry.Length);
                writer.Write((System.UInt32) entry.Length); // wird iwie 2x geschrieben idk why
            }

            writer.Seek((int) headerTableEntriesPosition, SeekOrigin.Begin);
            writer.Write((Int32)tableEntries);
            writer.Write((Int32)tableOffset);

            writer.Close();
        }
    }
}
