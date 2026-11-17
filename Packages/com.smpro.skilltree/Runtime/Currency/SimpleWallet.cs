using System;
using System.Collections.Generic;
using UnityEngine;

namespace SMPro.SkillTree
{
    public class SimpleWallet : MonoBehaviour, ICurrencyWallet
    {
        [Serializable]
        public struct CurrencyAmount
        {
            public string currencyId;
            public long amount;
        }

        [SerializeField] List<CurrencyAmount> startingBalances = new() { new CurrencyAmount { currencyId = CurrencyIds.Default } };

        Dictionary<string, long> balances;

        public event Action<string> BalanceChanged;

        public long GetBalance(string currencyId) => Balances.TryGetValue(currencyId, out long amount) ? amount : 0;

        public bool CanAfford(string currencyId, long amount) => GetBalance(currencyId) >= amount;

        public bool TrySpend(string currencyId, long amount)
        {
            if (amount < 0 || !CanAfford(currencyId, amount)) return false;
            Set(currencyId, GetBalance(currencyId) - amount);
            return true;
        }

        public void Add(string currencyId, long amount) => Set(currencyId, GetBalance(currencyId) + amount);

        void Set(string currencyId, long amount)
        {
            Balances[currencyId] = amount;
            BalanceChanged?.Invoke(currencyId);
        }

        Dictionary<string, long> Balances
        {
            get
            {
                if (balances != null) return balances;
                balances = new Dictionary<string, long>();
                foreach (CurrencyAmount start in startingBalances) balances[start.currencyId] = start.amount;
                return balances;
            }
        }
    }
}
