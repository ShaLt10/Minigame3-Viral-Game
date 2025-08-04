using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using DG.Tweening;

namespace DigitalForensicsQuiz
{
    public class DropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Drop Zone Settings")]
        [SerializeField] private Color normalColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
        [SerializeField] private Color highlightColor = new Color(0.7f, 0.7f, 1f, 0.5f);
        [SerializeField] private Color occupiedColor = new Color(0.3f, 0.8f, 0.3f, 0.5f);
        [SerializeField] private bool allowMultipleItems = false; // Set false untuk hanya 1 item
        [SerializeField] private int maxItems = 1; // Maksimal 1 item

        private Image zoneImage;
        private RectTransform rectTransform;
        
        private string zoneId;
        private MinigameManager gameManager;
        private List<DragItem> containedItems = new List<DragItem>();
        private Color originalColor;
        private bool isHighlighted = false;

        private void Awake()
        {
            // Script ini diattach di CategoryDragDrop
            rectTransform = GetComponent<RectTransform>();
            zoneImage = GetComponent<Image>();
            
            if (zoneImage == null)
            {
                zoneImage = gameObject.AddComponent<Image>();
            }
            
            originalColor = zoneImage.color;
            zoneImage.raycastTarget = true;
        }

        public void Initialize(string id, MinigameManager manager)
        {
            zoneId = id;
            gameManager = manager;
            
            ClearAllItems();
            SetNormalAppearance();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            DragItem draggedItem = GetDraggedItem(eventData);
            if (draggedItem != null && CanAcceptItem(draggedItem))
            {
                SetHighlightAppearance();
                isHighlighted = true;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (isHighlighted)
            {
                UpdateAppearance();
                isHighlighted = false;
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            DragItem draggedItem = GetDraggedItem(eventData);
            
            if (draggedItem != null && CanAcceptItem(draggedItem))
            {
                AcceptItem(draggedItem);
            }
            
            if (isHighlighted)
            {
                UpdateAppearance();
                isHighlighted = false;
            }
        }

        private DragItem GetDraggedItem(PointerEventData eventData)
        {
            if (eventData.pointerDrag != null)
            {
                // Cari DragItem component dari object yang di-drag
                DragItem dragItem = eventData.pointerDrag.GetComponent<DragItem>();
                if (dragItem == null)
                {
                    // Cari di parent atau child
                    dragItem = eventData.pointerDrag.GetComponentInParent<DragItem>();
                    if (dragItem == null)
                    {
                        dragItem = eventData.pointerDrag.GetComponentInChildren<DragItem>();
                    }
                }
                return dragItem;
            }
            return null;
        }

        public bool CanAcceptItem(DragItem item)
        {
            if (item == null) return false;
            
            // Jika sudah ada item dan tidak allow multiple, cek apakah bisa replace
            if (!allowMultipleItems && containedItems.Count > 0)
            {
                // Allow replacement (item lama akan dipindah)
                return true;
            }
            
            if (containedItems.Count >= maxItems && !containedItems.Contains(item))
                return false;
            
            return true;
        }

        public void AcceptItem(DragItem item)
        {
            if (item == null) return;
            
            // Remove dari drop zone sebelumnya
            DropZone previousZone = item.GetCurrentDropZone();
            if (previousZone != null && previousZone != this)
            {
                previousZone.RemoveItem(item);
            }
            
            // Jika tidak allow multiple dan sudah ada item, remove item lama
            if (!allowMultipleItems && containedItems.Count > 0)
            {
                var oldItem = containedItems[0];
                if (oldItem != item) // Pastikan bukan item yang sama
                {
                    RemoveItem(oldItem);
                    // Reset item lama ke posisi original
                    oldItem.ResetToOriginalPosition();
                }
            }
            
            // Add item baru jika belum ada
            if (!containedItems.Contains(item))
            {
                containedItems.Add(item);
            }
            
            // Snap ke drop zone
            item.SnapToDropZone(this);
            
            UpdateAppearance();
        }

        public void RemoveItem(DragItem item)
        {
            if (item == null) return;
            
            if (containedItems.Remove(item))
            {
                UpdateAppearance();
            }
        }

        public void ClearAllItems()
        {
            foreach (var item in containedItems)
            {
                if (item != null)
                {
                    item.ResetToOriginalPosition();
                }
            }
            
            containedItems.Clear();
            UpdateAppearance();
        }

        public void ForceRemoveAllItems()
        {
            containedItems.Clear();
            UpdateAppearance();
        }

        private void UpdateAppearance()
        {
            if (containedItems.Count > 0)
            {
                SetOccupiedAppearance();
            }
            else
            {
                SetNormalAppearance();
            }
        }

        private void SetNormalAppearance()
        {
            if (zoneImage != null)
            {
                Color targetColor = originalColor.a > 0 ? originalColor : normalColor;
                zoneImage.DOColor(targetColor, 0.2f);
            }
        }

        private void SetHighlightAppearance()
        {
            if (zoneImage != null)
            {
                zoneImage.DOColor(highlightColor, 0.1f);
            }
        }

        private void SetOccupiedAppearance()
        {
            if (zoneImage != null)
            {
                zoneImage.DOColor(occupiedColor, 0.2f);
            }
        }

        public string GetZoneId() => zoneId;
        public bool IsEmpty => containedItems.Count == 0;
        public bool IsFull => containedItems.Count >= maxItems;
        public int ItemCount => containedItems.Count;
        public List<DragItem> GetContainedItems() => new List<DragItem>(containedItems);

        public void SetZoneColor(Color color)
        {
            originalColor = color;
            if (zoneImage != null)
            {
                zoneImage.color = color;
            }
        }

        // Get visual bounds untuk positioning calculations
        public Rect GetDropZoneBounds()
        {
            if (rectTransform != null)
            {
                return rectTransform.rect;
            }
            return new Rect();
        }

        // Get world position of drop zone center
        public Vector3 GetWorldPosition()
        {
            if (rectTransform != null)
            {
                return rectTransform.TransformPoint(rectTransform.rect.center);
            }
            return Vector3.zero;
        }
    }
}