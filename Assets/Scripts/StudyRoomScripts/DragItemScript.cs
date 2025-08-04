using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace DigitalForensicsQuiz
{
    public class DragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Drag Settings")]
        [SerializeField] private float snapDistance = 50f;
        [SerializeField] private float returnSpeed = 0.3f;
        [SerializeField] private float dragScale = 1.1f;

        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Canvas dragCanvas;
        
        private string itemId;
        private MinigameManager gameManager;
        private Vector3 originalPosition;
        private Transform originalParent;
        private DropZone currentDropZone;
        private bool isDragging = false;
        private bool hasBeenDropped = false;
        
        private RectTransform playArea;
        
        // Reference to the root DragItem object (based on prefab structure)
        private Transform rootDragObject; // This will be "DragItem" from prefab
        private Transform contentObject;  // This will be "ScenarioDragDrop" from prefab

        private void Awake()
        {
            // Based on prefab structure: CategoryDragDrop is child of ScenarioDragDrop which is child of DragItem
            // This script should be attached to CategoryDragDrop (the actual draggable element)
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            
            // Find the root DragItem object and content object based on prefab hierarchy
            SetupHierarchyReferences();
            StoreOriginalTransform();
        }

        private void Start()
        {
            FindDragCanvas();
            FindPlayArea();
        }

        private void SetupHierarchyReferences()
        {
            // Based on prefab: CategoryDragDrop -> ScenarioDragDrop -> DragItem
            contentObject = transform.parent; // ScenarioDragDrop
            if (contentObject != null)
            {
                rootDragObject = contentObject.parent; // DragItem
            }
            
            // Fallback if hierarchy is different
            if (rootDragObject == null)
            {
                rootDragObject = transform;
                contentObject = transform;
            }
        }

        private void StoreOriginalTransform()
        {
            if (rootDragObject != null)
            {
                RectTransform rootRT = rootDragObject.GetComponent<RectTransform>();
                if (rootRT != null)
                {
                    originalPosition = rootRT.anchoredPosition;
                    originalParent = rootDragObject.parent;
                }
            }
            else
            {
                originalPosition = rectTransform.anchoredPosition;
                originalParent = transform.parent;
            }
        }

        private void FindDragCanvas()
        {
            Canvas[] canvases = FindObjectsOfType<Canvas>();
            foreach (var canvas in canvases)
            {
                if (canvas.isRootCanvas)
                {
                    dragCanvas = canvas;
                    break;
                }
            }
            
            if (dragCanvas == null && canvases.Length > 0)
            {
                dragCanvas = canvases[0];
            }
        }

        private void FindPlayArea()
        {
            if (originalParent != null)
            {
                playArea = originalParent.GetComponent<RectTransform>();
            }
        }

        public void Initialize(string id, MinigameManager manager)
        {
            itemId = id;
            gameManager = manager;
            ResetToOriginalPosition();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!CanDrag()) return;

            isDragging = true;
            hasBeenDropped = false;
            
            if (currentDropZone != null)
            {
                currentDropZone.RemoveItem(this);
                gameManager?.OnDragDropUnpair(itemId);
                currentDropZone = null;
            }

            // Move the root drag object (entire DragItem hierarchy)
            if (rootDragObject != null)
            {
                // Disable layout components temporarily
                var layoutGroup = rootDragObject.GetComponentInParent<GridLayoutGroup>();
                if (layoutGroup != null)
                {
                    layoutGroup.enabled = false;
                }
                
                var contentSizeFitter = rootDragObject.GetComponentInParent<ContentSizeFitter>();
                if (contentSizeFitter != null)
                {
                    contentSizeFitter.enabled = false;
                }
                
                rootDragObject.SetAsLastSibling();
            }

            canvasGroup.alpha = 0.8f;
            canvasGroup.blocksRaycasts = false;
            
            // Scale the content object for visual feedback
            if (contentObject != null)
            {
                contentObject.DOScale(Vector3.one * dragScale, 0.1f);
            }
            else
            {
                transform.DOScale(Vector3.one * dragScale, 0.1f);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging || !CanDrag()) return;

            // Drag the root object
            if (rootDragObject != null)
            {
                RectTransform rootRT = rootDragObject.GetComponent<RectTransform>();
                if (rootRT != null)
                {
                    Vector2 localPointerPosition;
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        rootRT.parent as RectTransform,
                        eventData.position,
                        dragCanvas.worldCamera,
                        out localPointerPosition))
                    {
                        localPointerPosition = ClampToPlayArea(localPointerPosition);
                        rootRT.anchoredPosition = localPointerPosition;
                    }
                }
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isDragging) return;

            isDragging = false;
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            
            // Reset scale
            if (contentObject != null)
            {
                contentObject.DOScale(Vector3.one, 0.1f);
            }
            else
            {
                transform.DOScale(Vector3.one, 0.1f);
            }

            // Re-enable layout components
            if (rootDragObject != null)
            {
                var layoutGroup = rootDragObject.GetComponentInParent<GridLayoutGroup>();
                if (layoutGroup != null)
                {
                    layoutGroup.enabled = true;
                }
                
                var contentSizeFitter = rootDragObject.GetComponentInParent<ContentSizeFitter>();
                if (contentSizeFitter != null)
                {
                    contentSizeFitter.enabled = true;
                }
            }

            // Delay to allow drop detection
            Invoke(nameof(CheckDropResult), 0.1f);
        }

        private void CheckDropResult()
        {
            if (!hasBeenDropped && currentDropZone == null)
            {
                ReturnToOriginalPosition();
            }
        }

        private Vector2 ClampToPlayArea(Vector2 position)
        {
            if (playArea == null) return position;

            Rect playRect = playArea.rect;
            float padding = 50f; // Increased padding for better UX
            
            position.x = Mathf.Clamp(position.x, 
                playRect.xMin + padding, 
                playRect.xMax - padding);
            position.y = Mathf.Clamp(position.y, 
                playRect.yMin + padding, 
                playRect.yMax - padding);

            return position;
        }

        public void SnapToDropZone(DropZone dropZone)
        {
            if (dropZone == null) return;

            hasBeenDropped = true;
            currentDropZone = dropZone;
            
            // Move root object to drop zone area
            if (rootDragObject != null)
            {
                Transform dropZoneParent = dropZone.transform.parent;
                if (dropZoneParent == null) dropZoneParent = dropZone.transform;
                
                // Don't change parent, just position near drop zone
                RectTransform rootRT = rootDragObject.GetComponent<RectTransform>();
                RectTransform dropZoneRT = dropZone.GetComponent<RectTransform>();
                
                if (rootRT != null && dropZoneRT != null)
                {
                    // Calculate world position of drop zone and convert to local position
                    Vector3 dropZoneWorldPos = dropZoneRT.TransformPoint(Vector3.zero);
                    Vector2 targetLocalPos;
                    
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        rootRT.parent as RectTransform,
                        RectTransformUtility.WorldToScreenPoint(dragCanvas.worldCamera, dropZoneWorldPos),
                        dragCanvas.worldCamera,
                        out targetLocalPos))
                    {
                        rootRT.DOAnchorPos(targetLocalPos, 0.2f)
                            .SetEase(Ease.OutBack)
                            .OnComplete(() => {
                                gameManager?.OnDragDropPair(itemId, dropZone.GetZoneId());
                            });
                    }
                }
            }
        }

        private void ReturnToOriginalPosition()
        {
            if (rootDragObject != null && originalParent != null)
            {
                // Ensure correct parent
                if (rootDragObject.parent != originalParent)
                {
                    rootDragObject.SetParent(originalParent, false);
                }
                
                RectTransform rootRT = rootDragObject.GetComponent<RectTransform>();
                if (rootRT != null)
                {
                    rootRT.DOAnchorPos(originalPosition, returnSpeed)
                        .SetEase(Ease.OutBack)
                        .OnComplete(() => {
                            // Re-enable layout after animation
                            var layoutGroup = originalParent.GetComponent<GridLayoutGroup>();
                            if (layoutGroup != null)
                            {
                                layoutGroup.enabled = true;
                                LayoutRebuilder.ForceRebuildLayoutImmediate(originalParent as RectTransform);
                            }
                        });
                }
            }
        }

        public void ResetToOriginalPosition()
        {
            isDragging = false;
            hasBeenDropped = false;
            currentDropZone = null;
            
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            
            // Reset scale
            if (contentObject != null)
            {
                contentObject.localScale = Vector3.one;
            }
            else
            {
                transform.localScale = Vector3.one;
            }
            
            if (rootDragObject != null && originalParent != null)
            {
                // Ensure correct parent
                if (rootDragObject.parent != originalParent)
                {
                    rootDragObject.SetParent(originalParent, false);
                }
                
                RectTransform rootRT = rootDragObject.GetComponent<RectTransform>();
                if (rootRT != null)
                {
                    rootRT.anchoredPosition = originalPosition;
                }
                
                // Re-enable layout components
                var layoutGroup = originalParent.GetComponent<GridLayoutGroup>();
                if (layoutGroup != null)
                {
                    layoutGroup.enabled = true;
                }
                
                var contentSizeFitter = originalParent.GetComponent<ContentSizeFitter>();
                if (contentSizeFitter != null)
                {
                    contentSizeFitter.enabled = true;
                }
            }
        }

        public void ForceSetOriginalPosition()
        {
            StoreOriginalTransform();
        }

        private bool CanDrag()
        {
            return gameObject.activeInHierarchy && rectTransform != null && !hasBeenDropped;
        }

        public string GetItemId() => itemId;
        public bool IsDragging => isDragging;
        public DropZone GetCurrentDropZone() => currentDropZone;
        
        public void SetupPosition(Vector3 position, Transform parent)
        {
            if (rootDragObject != null && parent != null)
            {
                if (rootDragObject.parent != parent)
                {
                    rootDragObject.SetParent(parent, false);
                }
                originalParent = parent;
                
                RectTransform rootRT = rootDragObject.GetComponent<RectTransform>();
                if (rootRT != null)
                {
                    rootRT.anchoredPosition = position;
                    originalPosition = position;
                }
            }
        }

        // Public method to get the root drag object for external reference
        public Transform GetRootDragObject()
        {
            return rootDragObject;
        }

        // Public method to get the content object for external reference
        public Transform GetContentObject()
        {
            return contentObject;
        }
    }
}