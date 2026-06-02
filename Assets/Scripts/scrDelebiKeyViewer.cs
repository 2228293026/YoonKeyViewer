using UnityEngine;

namespace YoonKeyViewer.Component
{
    public class scrDelebiKeyViewer : MonoBehaviour
    {
        public RectTransform sizeTransform;
        public RectTransform locationTransform;
        public AsyncImage leftHand;
        public AsyncImage rightHand;
        public AsyncImage Delebi;
        public AsyncImage DelebiSmash;
        public AsyncImage DelebiClear;
        public Key[] keys;
        public bool isSmashing;
        public bool winkOn;
        public bool gameResult;
    }
}
