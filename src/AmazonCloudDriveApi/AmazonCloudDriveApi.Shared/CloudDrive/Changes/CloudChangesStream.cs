using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace Amazon.CloudDrive
{
    public class CloudChangesStream : StreamReader
    {
        private const char UnixNewLine = '\n';

        public CloudChangesStream(Stream stream)
            : base(stream)
        { }

        public override int Read([In, Out]char[] buffer, int index, int count)
        {
            var ret = 0;

            for (var i = index; i < buffer.Length - 1; i++) {
                var c = Read();
                if (c == -1) break;
                if (c == UnixNewLine) break;

                buffer[i] = (char)c;
                ret++;
            }

            return ret;
        }

        public JsonTextReader GetJsonTextReader()
        {
            return new JsonTextReader(this);
        }
    }
}
