using UnityEngine;
using UnityEngine.UI;

namespace YoonKeyViewer.Component
{
    public class AsyncImage : MonoBehaviour
    {
        public Image image;

        private Sprite _sprite;
        public Sprite sprite
        {
            get => _sprite;
            set
            {
                _sprite = value;
                if (image != null) image.sprite = value;
            }
        }

        private sbyte _enable = -1;
        public sbyte enable
        {
            get => _enable;
            set
            {
                _enable = value;
                if (image != null) image.enabled = value != 0;
            }
        }
    }
}
