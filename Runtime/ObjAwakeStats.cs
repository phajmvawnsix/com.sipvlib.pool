using UnityEngine;

namespace SiPVLib.Pool
{
    public class ObjAwakeStats
    {
        // Basic
        public Vector3 localScale;
        public Quaternion localRotation;
            
        // RectTransform
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 anchoredPosition;
        public Vector2 sizeDelta;
        public Vector2 pivot;
            
        public ObjAwakeStats(GameObject obj)
        {
            var objTransform = obj.transform;
            localScale = objTransform.localScale;
            localRotation = objTransform.localRotation;

            if (objTransform is RectTransform rectTransform)
            {
                anchorMin = rectTransform.anchorMin;
                anchorMax = rectTransform.anchorMax;
                anchoredPosition = rectTransform.anchoredPosition;
                sizeDelta = rectTransform.sizeDelta;
                pivot = rectTransform.pivot;
            }
        }
        
        public ObjAwakeStats(Transform objTransform)
        {
            localScale = objTransform.localScale;
            localRotation = objTransform.localRotation;

            if (objTransform is RectTransform rectTransform)
            {
                anchorMin = rectTransform.anchorMin;
                anchorMax = rectTransform.anchorMax;
                anchoredPosition = rectTransform.anchoredPosition;
                sizeDelta = rectTransform.sizeDelta;
                pivot = rectTransform.pivot;
            }
        }
        
        public virtual void Apply(PooledObject obj)
        {
            var objTransform = obj.transform;
            objTransform.localScale = localScale;
            objTransform.localRotation = localRotation;

            if (objTransform is RectTransform rectTransform)
            {
                rectTransform.anchorMin = anchorMin;
                rectTransform.anchorMax = anchorMax;
                rectTransform.anchoredPosition = anchoredPosition;
                rectTransform.sizeDelta = sizeDelta;
                rectTransform.pivot = pivot;
            }
        }
    }
}