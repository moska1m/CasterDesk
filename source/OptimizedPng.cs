using System.Buffers.Binary;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace CronoLive;

// Lossless PNG: omit unused channels and select a scanline predictor before DEFLATE.
// No palette quantization, resampling, or changes to transparent pixels.
internal static class OptimizedPng
{
    static readonly uint[] crcTable=Enumerable.Range(0,256).Select(i=>
    {uint c=(uint)i;for(int k=0;k<8;k++)c=(c&1)!=0?0xedb88320u^(c>>1):c>>1;return c;}).ToArray();
    internal static void Save(Image image,string path)
    {
        Bitmap? converted=null;
        var bitmap=image as Bitmap;
        if(bitmap is null||bitmap.PixelFormat!=PixelFormat.Format32bppArgb)
        {
            converted=new Bitmap(image.Width,image.Height,PixelFormat.Format32bppArgb);
            using var g=Graphics.FromImage(converted);g.CompositingMode=CompositingMode.SourceCopy;g.DrawImageUnscaled(image,0,0);bitmap=converted;
        }
        try
        {
            var data=bitmap.LockBits(new(0,0,bitmap.Width,bitmap.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
            try
            {
            byte[] pixels=new byte[checked(bitmap.Width*4)];
            bool opaque=true,gray=true;
            for(int y=0;y<bitmap.Height&&(opaque||gray);y++)
            {
                Marshal.Copy(data.Scan0+y*data.Stride,pixels,0,pixels.Length);
                for(int i=0;i<pixels.Length;i+=4){opaque&=pixels[i+3]==255;gray&=pixels[i]==pixels[i+1]&&pixels[i+1]==pixels[i+2];}
            }
            int channels=opaque?(gray?1:3):4,type=opaque?(gray?0:2):6,length=bitmap.Width*channels;
            byte[] current=new byte[length],previous=new byte[length],sub=new byte[length],up=new byte[length],paeth=new byte[length];
            using var compressed=new MemoryStream();
            using(var zlib=new ZLibStream(compressed,CompressionLevel.Optimal,true))
            {
                for(int y=0;y<bitmap.Height;y++)
                {
                    Marshal.Copy(data.Scan0+y*data.Stride,pixels,0,pixels.Length);
                    for(int x=0;x<bitmap.Width;x++)
                    {
                        int src=x*4,dst=x*channels;
                        current[dst]=pixels[src+2];
                        if(channels>1){current[dst+1]=pixels[src+1];current[dst+2]=pixels[src];}
                        if(channels==4)current[dst+3]=pixels[src+3];
                    }
                    long noneScore=0,subScore=0,upScore=0,paethScore=0;
                    for(int i=0;i<length;i++)
                    {
                        int left=i>=channels?current[i-channels]:0,above=previous[i],upperLeft=i>=channels?previous[i-channels]:0;
                        sub[i]=unchecked((byte)(current[i]-left));up[i]=unchecked((byte)(current[i]-above));
                        int p=left+above-upperLeft,pl=Math.Abs(p-left),pa=Math.Abs(p-above),pu=Math.Abs(p-upperLeft);
                        int predictor=pl<=pa&&pl<=pu?left:pa<=pu?above:upperLeft;
                        paeth[i]=unchecked((byte)(current[i]-predictor));
                        noneScore+=Score(current[i]);subScore+=Score(sub[i]);upScore+=Score(up[i]);paethScore+=Score(paeth[i]);
                    }
                    byte filter=0;byte[] row=current;long best=noneScore;
                    if(subScore<best){filter=1;row=sub;best=subScore;}
                    if(upScore<best){filter=2;row=up;best=upScore;}
                    if(paethScore<best){filter=4;row=paeth;}
                    zlib.WriteByte(filter);zlib.Write(row);(current,previous)=(previous,current);
                }
            }
            using var file=File.Create(path);file.Write(new byte[]{137,80,78,71,13,10,26,10});
            byte[] header=new byte[13];BinaryPrimitives.WriteInt32BigEndian(header,bitmap.Width);BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4),bitmap.Height);header[8]=8;header[9]=(byte)type;
            Chunk(file,"IHDR"u8,header);Chunk(file,"IDAT"u8,compressed.GetBuffer().AsSpan(0,checked((int)compressed.Length)));Chunk(file,"IEND"u8,[]);
            }
            finally{bitmap.UnlockBits(data);}
        }
        finally{converted?.Dispose();}
    }
    static int Score(byte b)=>Math.Abs((int)unchecked((sbyte)b));
    static void Chunk(Stream file,ReadOnlySpan<byte> type,ReadOnlySpan<byte> data)
    {
        Span<byte> value=stackalloc byte[4];BinaryPrimitives.WriteInt32BigEndian(value,data.Length);file.Write(value);file.Write(type);file.Write(data);
        uint crc=0xffffffff;foreach(byte b in type)crc=crcTable[(crc^b)&255]^(crc>>8);foreach(byte b in data)crc=crcTable[(crc^b)&255]^(crc>>8);
        BinaryPrimitives.WriteUInt32BigEndian(value,crc^0xffffffff);file.Write(value);
    }
}
