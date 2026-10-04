using System;
using ShaurmaTycoon.Save;

namespace ShaurmaTycoon.Economy
{
    /// <summary>Сохраняет купюры в SaveData.Bills. Живёт в Boot — переезд в новый город их не трогает.</summary>
    public sealed class BillsSaveParticipant : ISaveParticipant
    {
        private readonly BillWallet _wallet;

        public BillsSaveParticipant(BillWallet wallet)
        {
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
        }

        public void Capture(SaveData data)
        {
            data.Bills = _wallet.Bills;
        }

        public void Apply(SaveData data)
        {
            _wallet.SetBills(data.Bills);
        }
    }
}
