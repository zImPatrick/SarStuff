using System.Reflection.PortableExecutable;
using System.Text;

namespace SarPacker
{
    internal class Program
    {
        struct TableEntry
        {
            public string FileName;
            public uint Offset;
            public uint Length;
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

            BinaryWriter writer = new BinaryWriter(File.OpenWrite(args.Length > 1 ? args[1] : "data.sar"));
            writer.Write(Encoding.ASCII.GetBytes("SARCFV"));
            writer.Write(new byte[] { 0x01, 0x01 });

            // es muss in den Header noch Entries & Offset geschrieben werden
            var headerTableEntriesPosition = writer.BaseStream.Position;

            writer.Seek(8, SeekOrigin.Current);

            // Data schreiben
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
                    Length = (uint) length,
                    Offset = (uint) position
                });
            }

            // file table
            int tableEntries = entries.Count;
            int tableOffset = (int)writer.BaseStream.Position;

            foreach (var entry in entries)
            {
                Console.WriteLine($"Writing file entry of " + entry.FileName);
                var fileName = Encoding.ASCII.GetBytes(entry.FileName);

                writer.Write((byte) fileName.Length);
                writer.Write(fileName);
                writer.Write((byte) 0x00);
                writer.Write(entry.Offset);
                writer.Write(entry.Length);
                writer.Write(entry.Length); // wird iwie 2x geschrieben idk why
            }

            writer.Seek((int) headerTableEntriesPosition, SeekOrigin.Begin);
            writer.Write(tableEntries);
            writer.Write(tableOffset);

            writer.Close();
        }
    }
}
