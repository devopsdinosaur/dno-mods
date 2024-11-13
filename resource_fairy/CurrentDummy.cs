using System;
using Unity.Entities;
using Utility.InterfacesStorage;

[Serializable]
public struct CurrentDummy : IComponentData, IUserUIResource {
    public int CurrentAmount() {
        return 0;
    }

    public void IncreaseAmount(int amount) {
    }

    public void DecreaseAmount(int amount) {
    }

    public bool HasDailyChanges() {
        return false;
    }

    public void ClearDailyChanges() {
    }
}

