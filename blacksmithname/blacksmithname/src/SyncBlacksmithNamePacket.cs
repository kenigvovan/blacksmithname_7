using ProtoBuf;

namespace blacksmithname.src
{
    public class SyncBlacksmithNamePacket
    {
        [ProtoMember(1)]
        public string NameColor;
    }
}
