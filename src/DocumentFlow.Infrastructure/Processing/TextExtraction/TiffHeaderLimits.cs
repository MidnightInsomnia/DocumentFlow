using System.Buffers.Binary;
using DocumentFlow.Application.Documents.Processing;

namespace DocumentFlow.Infrastructure.Processing.TextExtraction;

// Inspect each TIFF IFD before opening the managed decoder or allocating pixels.
// Classic TIFF (42), little/big-endian. BigTIFF (43) is explicitly unsupported.
internal static class TiffHeaderLimits
{
    public static IReadOnlyList<(uint Width, uint Height)> Validate(Stream source, CancellationToken cancellationToken)
    {
        source.Position = 0;
        Span<byte> header = stackalloc byte[8];
        Read(source, header);
        var little = header[0] == 0x49 && header[1] == 0x49;
        if (!little && !(header[0] == 0x4d && header[1] == 0x4d)) Invalid();
        var version = UInt16(header[2..], little);
        if (version == 43)
            throw new TextExtractionException(TextExtractionFailureReason.InvalidDocument,
                "BigTIFF не поддерживается. Сохраните документ в обычном TIFF (TIFF 6.0) или PDF.");
        if (version != 42) Invalid();
        var offset = UInt32(header[4..], little);
        var seen = new HashSet<ulong>();
        long totalPixels = 0;
        var pages = new List<(uint Width, uint Height)>();
        Span<byte> countBytes = stackalloc byte[2];
        Span<byte> entry = stackalloc byte[12];
        Span<byte> next = stackalloc byte[4];
        while (offset != 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (pages.Count >= TiffRasterizer.MaximumPages)
                throw new TextExtractionException(TextExtractionFailureReason.InvalidDocument,
                    "TIFF содержит более 30 страниц.");
            if (!seen.Add(offset) || offset > (ulong)source.Length - 2ul) Invalid();
            source.Position = checked((long)offset);
            Read(source, countBytes);
            var entryCount = UInt16(countBytes, little);
            if (entryCount > 4096 || (long)entryCount * 12 + 4 > source.Length - source.Position) Invalid();
            long width = 0, height = 0;
            var sawWidth = false;
            var sawHeight = false;
            for (var i = 0ul; i < entryCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Read(source, entry);
                var tag = UInt16(entry, little);
                var type = UInt16(entry[2..], little);
                var count = UInt32(entry[4..], little);
                // Bound out-of-line tag arrays before LibTiff reads/allocates them.
                // Unknown field types are left to the decoder (which ignores them).
                var elementSize = type switch { 1 or 2 or 6 or 7 => 1, 3 or 8 => 2, 4 or 9 or 11 or 13 => 4, 5 or 10 or 12 => 8, _ => 0 };
                var bytes = (ulong)count * (uint)elementSize;
                if (bytes > 4)
                {
                    var valueOffset = UInt32(entry[8..], little);
                    if (bytes > (ulong)source.Length || valueOffset > (ulong)source.Length - bytes) Invalid();
                }
                if (tag is not (256 or 257)) continue;
                if (count != 1 || type is not (3 or 4)) Invalid();
                if ((tag == 256 && sawWidth) || (tag == 257 && sawHeight)) Invalid();
                var value = entry[8..];
                var dimension = type == 3 ? UInt16(value, little) : UInt32(value, little);
                if (dimension is 0 or > TiffRasterizer.MaximumDimension)
                    throw new TextExtractionException(TextExtractionFailureReason.InvalidDocument,
                        "TIFF превышает лимит 6000 пикселей по стороне.");
                if (tag == 256) { width = checked((long)dimension); sawWidth = true; }
                else { height = checked((long)dimension); sawHeight = true; }
            }
            if (width == 0 || height == 0) Invalid();
            var pixels = width * height;
            totalPixels += pixels;
            if (pixels > TiffRasterizer.MaximumPixelsPerPage || totalPixels > TiffRasterizer.MaximumTotalPixels)
                throw new TextExtractionException(TextExtractionFailureReason.InvalidDocument,
                    "TIFF превышает лимит 12 млн пикселей на страницу или 48 млн на документ.");
            pages.Add(((uint)width, (uint)height));
            Read(source, next);
            offset = UInt32(next, little);
        }
        if (pages.Count == 0) Invalid();
        source.Position = 0;
        return pages;
    }

    private static ushort UInt16(ReadOnlySpan<byte> bytes, bool little) => little
        ? BinaryPrimitives.ReadUInt16LittleEndian(bytes)
        : BinaryPrimitives.ReadUInt16BigEndian(bytes);
    private static uint UInt32(ReadOnlySpan<byte> bytes, bool little) => little
        ? BinaryPrimitives.ReadUInt32LittleEndian(bytes)
        : BinaryPrimitives.ReadUInt32BigEndian(bytes);

    private static void Read(Stream stream, Span<byte> buffer)
    {
        if (stream.Length - stream.Position < buffer.Length) Invalid();
        stream.ReadExactly(buffer);
    }
    private static void Invalid() =>
        throw new TextExtractionException(TextExtractionFailureReason.InvalidDocument,
            "Не удалось декодировать TIFF: повреждены заголовок или сведения о страницах.");
}
