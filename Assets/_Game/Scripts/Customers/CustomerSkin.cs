using System;
using UnityEngine;

namespace ShaurmaTycoon.Customers
{
    /// <summary>
    /// Спрайты одного персонажа: стоит, идёт (кадры), радуется, взаимодействует.
    /// Без кадров ходьбы (персонажи V4 — одна поза) клиент идёт процедурно: вприпрыжку (CustomerView).
    /// </summary>
    [Serializable]
    public sealed class CustomerSkin
    {
        public Sprite Idle;
        public Sprite[] Walk = new Sprite[0];
        public Sprite Cheer;
        public Sprite Interact;
        [Tooltip("Персонаж нарисован лицом влево (V4). Нет — спрайт отражается: клиенты идут справа налево.")]
        public bool FacesLeft;
        [Tooltip("Своя реплика у окна (ключ локализации); пусто — только общие реплики.")]
        public string PhraseKey = string.Empty;
    }
}
