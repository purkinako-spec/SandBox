// Unity Noita-Style Pixel Physics Engine
// SandChunkCompressor.cs
using System;
using System.IO;
using PixelPhysics.Core;

namespace PixelPhysics.Networking
{
    /// <summary>
    /// RLE + Dirty Mask Delta Compressor for cellular automata pixel grids.
    /// Reduces 60KB raw frames down to ~400B-1.5KB per network tick.
    /// </summary>
    public static class SandChunkCompressor
    {
        private const int ChunkSize = 16;

        public static byte[] CompressChunks(byte[] pixels, byte[] dirtyMask, int width, int height)
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                int chunksX = (width + ChunkSize - 1) / ChunkSize;
                int chunksY = (height + ChunkSize - 1) / ChunkSize;
                int totalChunks = chunksX * chunksY;

                ushort dirtyCount = 0;
                for (int i = 0; i < totalChunks; i++)
                {
                    if (dirtyMask[i] == 1) dirtyCount++;
                }

                if (dirtyCount == 0) return null;

                writer.Write((byte)0xA5); // Magic header
                writer.Write(dirtyCount);

                for (ushort c = 0; c < totalChunks; c++)
                {
                    if (dirtyMask[c] == 0) continue;

                    writer.Write(c); // Chunk index

                    int cx = c % chunksX;
                    int cy = c / chunksX;
                    int startX = cx * ChunkSize;
                    int startY = cy * ChunkSize;

                    // Compress 16x16 pixels with RLE
                    byte lastMat = 255;
                    byte run = 0;

                    for (int dy = 0; dy < ChunkSize; dy++)
                    {
                        int py = startY + dy;
                        if (py >= height) break;
                        int row = py * width;

                        for (int dx = 0; dx < ChunkSize; dx++)
                        {
                            int px = startX + dx;
                            if (px >= width) break;

                            byte m = pixels[row + px];
                            if (m == lastMat && run < 255)
                            {
                                run++;
                            }
                            else
                            {
                                if (run > 0)
                                {
                                    writer.Write(run);
                                    writer.Write(lastMat);
                                }
                                lastMat = m;
                                run = 1;
                            }
                        }
                    }
                    if (run > 0)
                    {
                        writer.Write(run);
                        writer.Write(lastMat);
                    }
                }

                return ms.ToArray();
            }
        }

        public static bool DecompressChunks(byte[] data, IPhysicsComputeProvider provider, int width, int height)
        {
            if (data == null || data.Length < 3 || data[0] != 0xA5) return false;

            using (MemoryStream ms = new MemoryStream(data))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                reader.ReadByte(); // skip magic
                ushort dirtyCount = reader.ReadUInt16();

                int chunksX = (width + ChunkSize - 1) / ChunkSize;

                for (int d = 0; d < dirtyCount; d++)
                {
                    ushort chunkIdx = reader.ReadUInt16();
                    int cx = chunkIdx % chunksX;
                    int cy = chunkIdx / chunksX;
                    int startX = cx * ChunkSize;
                    int startY = cy * ChunkSize;

                    int px = startX;
                    int py = startY;

                    while (ms.Position < ms.Length)
                    {
                        byte run = reader.ReadByte();
                        byte mat = reader.ReadByte();

                        for (int r = 0; r < run; r++)
                        {
                            provider.SetPixel(px, py, mat);
                            px++;
                            if (px >= startX + ChunkSize || px >= width)
                            {
                                px = startX;
                                py++;
                                if (py >= startY + ChunkSize || py >= height) break;
                            }
                        }
                    }
                }
            }
            return true;
        }
    }
}
