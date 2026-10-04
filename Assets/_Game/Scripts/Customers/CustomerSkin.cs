using System;
using UnityEngine;

namespace ShaurmaTycoon.Customers
{
    /// <summary>Спрайты одного персонажа: стоит, идёт (кадры), радуется, взаимодействует.</summary>
    [Serializable]
    public sealed class CustomerSkin
    {
        public Sprite Idle;
        public Sprite[] Walk = new Sprite[0];
        public Sprite Cheer;
        public Sprite Interact;
    }
}
