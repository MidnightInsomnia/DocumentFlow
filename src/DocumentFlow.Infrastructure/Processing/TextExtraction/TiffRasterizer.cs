using System.Buffers.Binary;
using System.IO.Compression;
using BitMiracle.LibTiff.Classic;
using DocumentFlow.Application.Documents.Processing;

namespace DocumentFlow.Infrastructure.Processing.TextExtraction;

// Shared by OCR and preview. Decoding and PNG encoding stay in managed memory.
public sealed class TiffRasterizer
{
    public const int MaximumPages = 30;
    public const int MaximumDimension = 6000;
    public const long MaximumPixelsPerPage = 12_000_000;
    public const long MaximumTotalPixels = 48_000_000;
    public const long MaximumSourceBytes = 100L * 1024 * 1024;


    public TiffRasterDocument Load(Stream source, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        cancellationToken.ThrowIfCancellationRequested();
        var frames = new List<TiffRasterDocument.Frame>();
        try
        {
            if (!source.CanRead || !source.CanSeek)
                throw Invalid("Не удалось декодировать TIFF: поток не поддерживает чтение с перемоткой.");
            if (source.Length is <= 0 or > MaximumSourceBytes)
                throw Invalid("TIFF пуст или превышает лимит 100 МиБ.");

            // Check EVERY top-level IFD before opening the decoder or allocating pixels.
            // SubIFDs / Photoshop layers are not document pages. BigTIFF is rejected here.
            var dimensions = TiffHeaderLimits.Validate(source, cancellationToken);
            using var tiff = Tiff.ClientOpen("document.tiff", "r", source, new ReadStream(cancellationToken));
            if (tiff is null) throw Invalid("Не удалось открыть TIFF: файл повреждён или формат не поддерживается.");
            for (var index = 0; index < dimensions.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (index > 0 && !tiff.ReadDirectory())
                    throw Invalid("Не удалось прочитать следующую страницу TIFF.");
                var expected = dimensions[index];
                var width = tiff.GetField(TiffTag.IMAGEWIDTH)?[0].ToInt() ?? 0;
                var height = tiff.GetField(TiffTag.IMAGELENGTH)?[0].ToInt() ?? 0;
                if (width != expected.Width || height != expected.Height)
                    throw Invalid("Размеры декодированной страницы TIFF не совпадают с заголовком.");
                var orientation = tiff.GetFieldDefaulted(TiffTag.ORIENTATION)[0].ToInt();
                if (orientation is < 1 or > 8) throw Invalid("TIFF содержит неверную ориентацию страницы.");
                ValidateDecodeBuffers(tiff);
                // LibTiff handles flips but not transposes (5-8). Decode stored rows
                // top-left, then apply the original orientation exactly once in PNG.
                if (!tiff.SetField(TiffTag.ORIENTATION, Orientation.TOPLEFT))
                    throw Invalid("Не удалось нормализовать ориентацию TIFF.");
                var pixels = new int[checked(width * height)];
                if (!tiff.ReadRGBAImageOriented(width, height, pixels, Orientation.TOPLEFT, true))
                    throw Invalid("Не удалось декодировать страницу TIFF: повреждены данные или сжатие не поддерживается.");
                cancellationToken.ThrowIfCancellationRequested();
                frames.Add(new(width, height, orientation, pixels));
            }
            return new TiffRasterDocument(frames);
        }
        catch (Exception exception)
        {
            frames.Clear();
            cancellationToken.ThrowIfCancellationRequested();
            if (exception is OperationCanceledException or TextExtractionException) throw;
            throw Invalid("Не удалось декодировать TIFF: файл повреждён, имеет неверный формат или превышает ресурсные лимиты.", exception);
        }
    }

    private static void ValidateDecodeBuffers(Tiff tiff)
    {
        // Bound decoder work buffers as well as the prevalidated output raster.
        var samples = tiff.GetFieldDefaulted(TiffTag.SAMPLESPERPIXEL)[0].ToInt();
        var bits = tiff.GetFieldDefaulted(TiffTag.BITSPERSAMPLE)[0].ToInt();
        if (samples is < 1 or > 4 || bits is not (1 or 2 or 4 or 8 or 16))
            throw Invalid("TIFF использует неподдерживаемое число каналов или глубину цвета.");
        if (tiff.IsTiled())
        {
            var tileWidth = tiff.GetField(TiffTag.TILEWIDTH)?[0].ToInt() ?? 0;
            var tileHeight = tiff.GetField(TiffTag.TILELENGTH)?[0].ToInt() ?? 0;
            if (tileWidth is < 1 or > MaximumDimension || tileHeight is < 1 or > MaximumDimension ||
                (long)tileWidth * tileHeight > MaximumPixelsPerPage ||
                tiff.GetFieldDefaulted(TiffTag.TILEDEPTH)[0].ToInt() != 1)
                throw Invalid("TIFF превышает допустимый размер блока изображения.");
        }
        var size = tiff.IsTiled() ? tiff.TileSize() : tiff.StripSize();
        var planes = tiff.GetFieldDefaulted(TiffTag.PLANARCONFIG)[0].ToInt() == (int)PlanarConfig.SEPARATE ? samples : 1;
        if (size <= 0 || (long)size * planes > 128L * 1024 * 1024)
            throw Invalid("TIFF превышает лимит памяти блока декодирования 128 МиБ.");
        var counts = tiff.GetField(tiff.IsTiled() ? TiffTag.TILEBYTECOUNTS : TiffTag.STRIPBYTECOUNTS)?[0].TolongArray();
        if (counts is null || counts.Any(count => count < 0 || count > MaximumSourceBytes))
            throw Invalid("TIFF содержит неверный или слишком большой сжатый блок.");
    }

    // Per-document callbacks; the caller retains ownership of the source stream.
    private sealed class ReadStream(CancellationToken cancellationToken) : TiffStream
    {
        public override int Read(object clientData, byte[] buffer, int offset, int count)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var stream = (Stream)clientData;
            var read = 0;
            while (read < count)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var current = stream.Read(buffer, offset + read, count - read);
                if (current == 0) break;
                read += current;
            }
            return read;
        }
        public override long Seek(object clientData, long offset, SeekOrigin origin)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return base.Seek(clientData, offset, origin);
        }
        public override void Write(object clientData, byte[] buffer, int offset, int count) =>
            throw new NotSupportedException("TIFF decoder is read-only.");
        public override void Close(object clientData) { }
    }

    private static TextExtractionException Invalid(string message, Exception? inner = null) => inner is null
        ? new(TextExtractionFailureReason.InvalidDocument, message)
        : new(TextExtractionFailureReason.InvalidDocument, message, inner);
}

public sealed class TiffRasterDocument : IDisposable
{
    internal sealed record Frame(int Width, int Height, int Orientation, int[] Pixels);
    private readonly List<Frame> _frames;
    private bool _disposed;

    internal TiffRasterDocument(List<Frame> frames) => _frames = frames;

    public int PageCount => _frames.Count;

    // pageNumber is 1-based for both the HTTP endpoint and the operator UI.
    public async Task<MemoryStream> RenderPngAsync(int pageNumber, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();
        if (pageNumber < 1 || pageNumber > PageCount)
            throw new ArgumentOutOfRangeException(nameof(pageNumber));

        var output = new MemoryStream();
        try
        {
            var frame = _frames[pageNumber - 1];
            var width = frame.Orientation >= 5 ? frame.Height : frame.Width;
            var height = frame.Orientation >= 5 ? frame.Width : frame.Height;
            output.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
            var header = new byte[13];
            BinaryPrimitives.WriteInt32BigEndian(header, width);
            BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), height);
            header[8] = 8;
            header[9] = 6; // RGBA 8-bit; source EXIF/XMP/profiles are never copied.
            WriteChunk(output, "IHDR"u8, header);
            using (var idat = new IdatStream(output, cancellationToken))
            {
                using (var zlib = new ZLibStream(idat, CompressionLevel.Fastest, leaveOpen: true))
                {
                    var row = new byte[checked(width * 4 + 1)]; // PNG filter 0.
                    for (var y = 0; y < height; y++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        for (var x = 0; x < width; x++)
                        {
                            var (sx, sy) = frame.Orientation switch
                            {
                                2 => (frame.Width - 1 - x, y),
                                3 => (frame.Width - 1 - x, frame.Height - 1 - y),
                                4 => (x, frame.Height - 1 - y),
                                5 => (y, x),
                                6 => (y, frame.Height - 1 - x),
                                7 => (frame.Width - 1 - y, frame.Height - 1 - x),
                                8 => (frame.Width - 1 - y, x),
                                _ => (x, y)
                            };
                            var pixel = frame.Pixels[sy * frame.Width + sx];
                            var alpha = Tiff.GetA(pixel);
                            var offset = 1 + x * 4;
                            // Packed 0xAABBGGRR, not BGRA bytes. LibTiff returns associated
                            // alpha; PNG requires straight/unassociated RGBA.
                            row[offset] = Unassociate(Tiff.GetR(pixel), alpha);
                            row[offset + 1] = Unassociate(Tiff.GetG(pixel), alpha);
                            row[offset + 2] = Unassociate(Tiff.GetB(pixel), alpha);
                            row[offset + 3] = (byte)alpha;
                        }
                        await zlib.WriteAsync(row, cancellationToken);
                    }
                }
                idat.Flush();
            }
            WriteChunk(output, "IEND"u8, ReadOnlySpan<byte>.Empty);
            cancellationToken.ThrowIfCancellationRequested();
            output.Position = 0;
            return output;
        }
        catch (Exception exception)
        {
            await output.DisposeAsync();
            cancellationToken.ThrowIfCancellationRequested();
            if (exception is OperationCanceledException or TextExtractionException) throw;
            throw new TextExtractionException(TextExtractionFailureReason.InvalidDocument,
                "Не удалось сформировать PNG страницы TIFF: повреждён файл или превышен ресурсный лимит.", exception);
        }
    }

    private static byte Unassociate(int channel, int alpha) => alpha == 0
        ? (byte)0 : (byte)Math.Min(255, (channel * 255 + alpha / 2) / alpha);

    private static readonly uint[] CrcTable = CreateCrcTable();
    private static uint[] CreateCrcTable()
    {
        var table = new uint[256];
        for (var i = 0; i < table.Length; i++)
        {
            var crc = (uint)i;
            for (var bit = 0; bit < 8; bit++) crc = (crc & 1) != 0 ? 0xedb88320U ^ (crc >> 1) : crc >> 1;
            table[i] = crc;
        }
        return table;
    }

    private static void WriteChunk(Stream output, ReadOnlySpan<byte> type, ReadOnlySpan<byte> data)
    {
        Span<byte> number = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(number, data.Length);
        output.Write(number);
        output.Write(type);
        output.Write(data);
        var crc = uint.MaxValue;
        foreach (var value in type) crc = CrcTable[(crc ^ value) & 255] ^ (crc >> 8);
        foreach (var value in data) crc = CrcTable[(crc ^ value) & 255] ^ (crc >> 8);
        BinaryPrimitives.WriteUInt32BigEndian(number, ~crc);
        output.Write(number);
    }

    // Stream bounded IDAT chunks rather than buffering another complete compressed page.
    private sealed class IdatStream(Stream output, CancellationToken cancellationToken) : Stream
    {
        private readonly byte[] _buffer = new byte[64 * 1024];
        private int _count;
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            while (!buffer.IsEmpty)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var count = Math.Min(buffer.Length, _buffer.Length - _count);
                buffer[..count].CopyTo(_buffer.AsSpan(_count));
                _count += count;
                buffer = buffer[count..];
                if (_count == _buffer.Length) Flush();
            }
        }
        public override void Flush()
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_count == 0) return;
            WriteChunk(output, "IDAT"u8, _buffer.AsSpan(0, _count));
            _count = 0;
        }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _frames.Clear();
    }
}