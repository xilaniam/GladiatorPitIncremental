using System;

namespace SMPro.SkillTree
{
    public static class CurrencyIds
    {
        public const string Default = "default";
    }

    public interface ICurrencyWallet
    {
        bool CanAfford(string currencyId, long amount);
        bool TrySpend(string currencyId, long amount);
        void Add(string currencyId, long amount);
        event Action<string> BalanceChanged;
    }
}
