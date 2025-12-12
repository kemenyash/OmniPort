public record ContentSnapshot(byte[] Bytes, string ContentType)
{
    public Stream OpenReadStream() => new MemoryStream(Bytes, writable: false);
    public int Length => Bytes.Length;
    public bool IsEmpty => Bytes.Length == 0;
}