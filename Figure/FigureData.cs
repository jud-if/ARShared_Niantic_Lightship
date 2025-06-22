using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.Netcode;


public struct FigureData : IEquatable<FigureData>, INetworkSerializable
{
    public ulong clientID;
    public int figureCount;

    public FigureData(ulong clientID, int figureCount)
    {
        this.clientID = clientID;
        this.figureCount = figureCount;
    }

    public bool Equals(FigureData other)
    {
        return (
            other.clientID == clientID
        );
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref clientID);
        serializer.SerializeValue(ref figureCount);
    }

}
