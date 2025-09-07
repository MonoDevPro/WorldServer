using System.Numerics;
using Simulation.Application.Ports.Network.Domain.Models;

namespace Simulation.Application.Ports.Network.Outbound
{
    /// <summary>
    /// Interface para escrita de dados na rede, abstraindo a implementação específica do LiteNetLib
    /// </summary>
    public interface INetworkWriter
    {
        void WriteByte(byte value);
        void WriteSByte(sbyte value);
        void WriteBool(bool value);
        void WriteShort(short value);
        void WriteUShort(ushort value);
        void WriteInt(int value);
        void WriteUInt(uint value);
        void WriteLong(long value);
        void WriteULong(ulong value);
        void WriteFloat(float value);
        void WriteDouble(double value);
        void WriteString(string value);
        void WriteBytes(byte[] value);
        void WriteVector2(Vector2 value);
        void WriteVector3(Vector3 value);
        void WriteSerializable<T>(T value) where T : ISerializable;
        void WriteSerializable<T>(T[] values) where T : ISerializable;
        void WriteSerializable<T>(List<T> value) where T : ISerializable;

        void Recycle();
        
        void Reset();
        int Length { get; }
        byte[] Data { get; }
    }
}