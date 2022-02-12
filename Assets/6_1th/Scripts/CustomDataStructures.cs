using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZeroFormatter;
namespace Generation_6_1
{
    public struct Voxel
    {
        public uint color;
        public uint xy;
        public uint z;
    };

    [System.Serializable]
    [ZeroFormattable]
    public struct DataNode
    {
        public uint this[int idx]
        {
            get
            {
                if (idx == 0) return sn0;
                else if (idx == 1) return sn1;
                else if (idx == 2) return sn2;
                else if (idx == 3) return sn3;
                else if (idx == 4) return sn4;
                else if (idx == 5) return sn5;
                else if (idx == 6) return sn6;
                else return sn7;
            }
            set
            {
                if (idx == 0) sn0 = value;
                else if (idx == 1) sn1 = value;
                else if (idx == 2) sn2 = value;
                else if (idx == 3) sn3 = value;
                else if (idx == 4) sn4 = value;
                else if (idx == 5) sn5 = value;
                else if (idx == 6) sn6 = value;
                else sn7 = value;
            }
        }

        public DataNode(uint s0, uint s1, uint s2, uint s3, uint s4, uint s5, uint s6, uint s7)
        {
            sn0 = s0;
            sn1 = s1;
            sn2 = s2;
            sn3 = s3;
            sn4 = s4;
            sn5 = s5;
            sn6 = s6;
            sn7 = s7;
        }

        [Index(0)]
        public uint sn0;
        [Index(1)]
        public uint sn1;
        [Index(2)]
        public uint sn2;
        [Index(3)]
        public uint sn3;
        [Index(4)]
        public uint sn4;
        [Index(5)]
        public uint sn5;
        [Index(6)]
        public uint sn6;
        [Index(7)]
        public uint sn7;

        public void Reset()
        {
            sn0 =
            sn1 =
            sn2 =
            sn3 =
            sn4 =
            sn5 =
            sn6 =
            sn7 = 0;
        }
    }
    [System.Serializable]
    public class PointArr
    {
        public int sizeX;
        public int sizeY;
        public int sizeZ;
        public List<PointData> data;

        public Vector3[] ConvertToVectorArray()
        {
            List<Vector3> list = new List<Vector3>();

            foreach (PointData p in data)
            {
                list.Add(new Vector3(p.r, p.g, p.b) / 256);
            }
            return list.ToArray();
        }
    }

    [System.Serializable]
    public class PointData
    {

        public int index;
        public byte r;
        public byte g;
        public byte b;

        public PointData(int idx, Color color)
        {
            index = idx;
            r = (byte)(color.r * 255);
            g = (byte)(color.g * 255);
            b = (byte)(color.b * 255);
        }
    }

    [ZeroFormattable]
    public class VoxelArray
    {
        [Index(0)]
        public virtual int scale { get; set; }
        [Index(1)]
        public virtual List<VoxelData> data { get; set; }

        public VoxelArray() { }
        public VoxelArray(int scale) { data = new List<VoxelData>(); this.scale = scale; }
        public Vector3[] ConvertToVectorArray()
        {
            List<Vector3> list = new List<Vector3>();

            foreach (VoxelData p in data)
            {
                list.Add(new Vector3(p.r, p.g, p.b) / 256);
            }
            return list.ToArray();
        }
        public void Add(Vector3 position, Vector3 color)
        {
            int idx = PosToIdx(position);
            data.Add(new VoxelData(idx, color));
        }
        public void Add(Vector3 position, Color color)
        {
            int idx = PosToIdx(position);
            data.Add(new VoxelData(idx, color));
        }
        public int PosToIdx(Vector3 p)
        {
            return (int)((int)p.z * scale * scale + (int)p.y * scale + (int)p.x);
        }
        public Vector3 IdxToPos(int idx)
        {
            Vector3 result = new Vector3();
            result.z = idx / (scale * scale);// + 0.5f;
            result.y = (idx % (scale * scale)) / scale;// + 0.5f;
            result.x = idx % scale;// + 0.5f;
            return result;
        }
    }
    [ZeroFormattable]
    public class VoxelData
    {
        [Index(0)]
        public virtual int index { get; set; }
        [Index(1)]
        public virtual byte r { get; set; }
        [Index(2)]
        public virtual byte g { get; set; }
        [Index(3)]
        public virtual byte b { get; set; }

        public VoxelData()
        {

        }
        public VoxelData(int idx, Color color)
        {
            index = idx;
            r = (byte)(color.r * 255);
            g = (byte)(color.g * 255);
            b = (byte)(color.b * 255);
        }

        public VoxelData(int idx, Vector3 color)
        {
            index = idx;
            r = (byte)(color.x * 255);
            g = (byte)(color.y * 255);
            b = (byte)(color.z * 255);
        }
    }
}