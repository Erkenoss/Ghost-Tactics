using System;

namespace Crimson.Map
{
    public interface IMapRewardHandler
    {
        Type RewardType { get; }
        bool Apply(MapReward reward);
    }
}