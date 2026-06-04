using UnityEngine;
using UnityEngine.UI;

namespace YoonKeyViewer.Component
{
    public class Key : MonoBehaviour
    {
        public Image image;

        private sbyte _enable = -1;
        public sbyte enable
        {
            get => _enable;
            set
            {
                if (_enable == value) return;
                _enable = value;
                if (image != null) image.enabled = value != 0;
            }
        }
    }
}
