using System;
using Unity.Entities;
using Utility.InterfacesStorage;

[Serializable]
public struct DummyReserve : IComponentData, IResourceReserve {
    public int reserved;

    public int CurrentReserve() {
        return 0;
    }

    public void IncreaseReserve(int value) {
    }

    public void DecreaseReserve(int value) {
    }
}