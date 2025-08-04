using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace DigitalForensicsQuiz
{
    public class MinigameManager : MonoBehaviour
    {
        [Header("Game Flow Panels")]
        [SerializeField] private GameObject dialogPanel;
        [SerializeField] private GameObject instructionPanel;
        [SerializeField] private GameObject minigamePanel;
        [SerializeField] private GameObject feedbackPanel;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private GameObject completionScreen;

        [Header("Dialog System")]
        [SerializeField] private TextMeshProUGUI characterNameText;
        [SerializeField] private TextMeshProUGUI dialogText;
        [SerializeField] private Button dialogNextButton;
        [SerializeField] private Image characterImage;

        [Header("Instruction Panel")]
        [SerializeField] private GameObject glitchScreen;
        [SerializeField] private Button confirmationButton;

        [Header("Minigame Panel")]
        [SerializeField] private Transform trackingBar;
        [SerializeField] private GameObject trackingCirclePrefab;
        [SerializeField] private TextMeshProUGUI questionText;
        [SerializeField] private Button submitButton;

        [Header("Multiple Choice Elements")]
        [SerializeField] private Transform multipleChoiceContainer;
        [SerializeField] private Button[] optionButtons = new Button[4];

        [Header("Drag and Drop Elements")]
        [SerializeField] private Transform dragDropContainer;
        [SerializeField] private GameObject[] dragItems = new GameObject[3];
        [SerializeField] private GameObject[] dropZones = new GameObject[3];

        [Header("Feedback Panel")]
        [SerializeField] private TextMeshProUGUI feedbackText;
        [SerializeField] private TextMeshProUGUI explanationText;
        [SerializeField] private Button nextButton;
        [SerializeField] private Image feedbackBG;

        [Header("Result Panel")]
        [SerializeField] private TextMeshProUGUI resultText;
        [SerializeField] private Button resultNextButton;
        [SerializeField] private Image resultBG;

        [Header("Completion Screen")]
        [SerializeField] private TextMeshProUGUI completionHeaderText;
        [SerializeField] private TextMeshProUGUI ipAddressText;
        [SerializeField] private TextMeshProUGUI locationText;
        [SerializeField] private Button quitButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Image completionScreenBG;

        [Header("Colors")]
        [SerializeField] private Color defaultCircleColor = Color.gray;
        [SerializeField] private Color correctCircleColor = Color.green;
        [SerializeField] private Color incorrectCircleColor = Color.red;
        [SerializeField] private Color selectedOptionColor = new Color(0.8f, 0.8f, 1f);

        private List<MinigameQuestionData> questions;
        private int currentQuestionIndex = 0;
        private int selectedAnswerIndex = -1;
        private Dictionary<string, string> dragDropAnswers = new Dictionary<string, string>();
        private List<bool> questionResults = new List<bool>();
        private List<Image> trackingCircles = new List<Image>();

        private MinigameAudioManager audioManager;
        private bool isGameInitialized = false;

        private GameObject dragLayer;
        private Canvas rootCanvas;

        // Store original parent containers for proper reset
        private Dictionary<GameObject, Transform> originalDragItemParents = new Dictionary<GameObject, Transform>();
        private Dictionary<GameObject, Vector3> originalDragItemPositions = new Dictionary<GameObject, Vector3>();

        private const string CHARACTER_NAME = "Gavi";
        private const string GAVI_DIALOG = "Baiklah tim, saatnya untuk investigasi digital tingkat lanjut! Kita akan trace bukti digital dan pahami cara kerja scam impersonation secara sistematis.";

        #region Initialization

        private void Start()
        {
            InitializeGame();
        }

        private void InitializeGame()
        {
            audioManager = MinigameAudioManager.Instance;
            questions = QuestionProvider.GetAllQuestions();
            
            if (questions == null || questions.Count == 0)
            {
                questions = CreateFallbackQuestions();
            }

            SetupUI();
            CreateTrackingBar();
            
            try 
            {
                PlatformUtils.ConfigureForPlatform();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"PlatformUtils error (non-critical): {e.Message}");
            }
            
            isGameInitialized = true;
            ShowDialogPanel();
        }

        private List<MinigameQuestionData> CreateFallbackQuestions()
        {
            var fallbackQuestions = new List<MinigameQuestionData>();
            
            var question1 = new MinigameQuestionData
            {
                id = "fallback_1",
                questionText = "Apa jenis informasi yang paling tepat menggambarkan konten video tersebut?",
                type = QuestionType.MultipleChoice,
                options = new List<string> 
                {
                    "Informasi medis yang akurat",
                    "Propaganda anti-vaksin",
                    "Edukasi kesehatan umum", 
                    "Iklan produk kesehatan"
                },
                correctAnswerIndex = 1,
                explanation = "Video tersebut merupakan propaganda anti-vaksin yang menyebarkan informasi tidak akurat tentang vaksin."
            };
            
            fallbackQuestions.Add(question1);
            return fallbackQuestions;
        }

        private void SetupUI()
        {
            SetupDragDropCanvas();
            
            SafeAddButtonListener(dialogNextButton, ShowInstructionPanel);
            SafeAddButtonListener(confirmationButton, StartMinigame);
            SafeAddButtonListener(submitButton, SubmitAnswer);
            SafeAddButtonListener(nextButton, NextQuestion);
            SafeAddButtonListener(resultNextButton, ShowCompletion);
            SafeAddButtonListener(quitButton, QuitGame);
            SafeAddButtonListener(restartButton, RestartGame);

            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (optionButtons[i] != null)
                {
                    int index = i;
                    SafeAddButtonListener(optionButtons[i], () => SelectOption(index));
                }
            }

            InitializeDragDropSystem();
            
            // Fix background images
            FixBackgroundImages();
        }

        private void FixBackgroundImages()
        {
            // Fix feedback background
            if (feedbackBG != null)
            {
                feedbackBG.type = Image.Type.Sliced;
                feedbackBG.preserveAspect = false;
                feedbackBG.fillCenter = true; // Ensure fill center is enabled
                
                // Reset color to white to show the sprite properly
                feedbackBG.color = Color.white;
            }

            // Fix result background
            if (resultBG != null)
            {
                resultBG.type = Image.Type.Sliced;
                resultBG.preserveAspect = false;
                resultBG.fillCenter = true; // Ensure fill center is enabled
                
                // Reset color to white to show the sprite properly
                resultBG.color = Color.white;
            }

            // Fix completion screen background
            if (completionScreenBG != null)
            {
                completionScreenBG.type = Image.Type.Sliced;
                completionScreenBG.preserveAspect = false;
                completionScreenBG.fillCenter = true; // Ensure fill center is enabled
                
                // Reset color to white to show the sprite properly  
                completionScreenBG.color = Color.white;
            }
        }

        private void SetupDragDropCanvas()
        {
            rootCanvas = GetComponentInParent<Canvas>();
            if (rootCanvas == null)
                rootCanvas = FindObjectOfType<Canvas>();
            
            if (rootCanvas != null)
            {
                dragLayer = rootCanvas.transform.Find("DragLayer")?.gameObject;
                if (dragLayer == null)
                {
                    dragLayer = new GameObject("DragLayer");
                    dragLayer.transform.SetParent(rootCanvas.transform);
                    dragLayer.transform.SetAsLastSibling();
                    
                    var rectTransform = dragLayer.AddComponent<RectTransform>();
                    rectTransform.anchorMin = Vector2.zero;
                    rectTransform.anchorMax = Vector2.one;
                    rectTransform.sizeDelta = Vector2.zero;
                    rectTransform.anchoredPosition = Vector2.zero;
                }
            }
        }

        private void SafeAddButtonListener(Button button, System.Action action)
        {
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => 
                {
                    audioManager?.PlayButtonClickSFX();
                    action?.Invoke();
                });
            }
        }

        #region Drag Drop Initialization

        private void InitializeDragDropSystem()
        {
            // Store original parent information for drag items
            StoreOriginalDragItemInfo();

            for (int i = 0; i < dragItems.Length; i++)
            {
                if (dragItems[i] != null)
                {
                    EnsureDragItemComponents(dragItems[i]);
                    
                    // Initialize with proper ID - will be set again in DisplayDragAndDrop
                    var dragComponent = GetDragItemComponent(dragItems[i]);
                    if (dragComponent != null)
                    {
                        dragComponent.Initialize($"drag_{i}", this);
                    }
                }
            }

            for (int i = 0; i < dropZones.Length; i++)
            {
                if (dropZones[i] != null)
                {
                    EnsureDropZoneComponents(dropZones[i]);
                    
                    var dropComponent = GetDropZoneComponent(dropZones[i]);
                    if (dropComponent != null)
                    {
                        dropComponent.Initialize($"drop_{i}", this);
                    }
                }
            }
        }

        private void StoreOriginalDragItemInfo()
        {
            originalDragItemParents.Clear();
            originalDragItemPositions.Clear();

            for (int i = 0; i < dragItems.Length; i++)
            {
                if (dragItems[i] != null)
                {
                    // Store direct object info first
                    originalDragItemParents[dragItems[i]] = dragItems[i].transform.parent;
                    
                    RectTransform itemRT = dragItems[i].GetComponent<RectTransform>();
                    if (itemRT != null)
                    {
                        originalDragItemPositions[dragItems[i]] = itemRT.anchoredPosition;
                    }
                }
            }
        }

        private DragItem GetDragItemComponent(GameObject dragItemGO)
    {
        // Karena script ada di ScenarioDragDrop, cari di child objects
        return dragItemGO.GetComponentInChildren<DragItem>();
    }

    private DropZone GetDropZoneComponent(GameObject dropZoneGO)
    {
        // Karena script ada di CategoryDragDrop, cari di child objects  
        return dropZoneGO.GetComponentInChildren<DropZone>();
    }

        private void EnsureDragItemComponents(GameObject dragItemGO)
        {
            if (dragItemGO.GetComponent<Image>() == null)
            {
                var image = dragItemGO.AddComponent<Image>();
                image.color = Color.white;
                image.raycastTarget = true;
            }
            
            if (dragItemGO.GetComponent<CanvasGroup>() == null)
            {
                dragItemGO.AddComponent<CanvasGroup>();
            }

            if (dragItemGO.GetComponent<DragItem>() == null)
            {
                dragItemGO.AddComponent<DragItem>();
            }
        }

        private void EnsureDropZoneComponents(GameObject dropZoneGO)
        {
            if (dropZoneGO.GetComponent<Image>() == null)
            {
                var image = dropZoneGO.AddComponent<Image>();
                image.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
                image.raycastTarget = true;
            }

            if (dropZoneGO.GetComponent<DropZone>() == null)
            {
                dropZoneGO.AddComponent<DropZone>();
            }
        }

        #endregion

        private void CreateTrackingBar()
        {
            if (trackingBar == null || trackingCirclePrefab == null) return;

            foreach (Transform child in trackingBar)
            {
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
            trackingCircles.Clear();

            int questionCount = questions?.Count ?? 6;
            for (int i = 0; i < questionCount; i++)
            {
                GameObject circleObj = Instantiate(trackingCirclePrefab, trackingBar);
                Image circleImage = circleObj.GetComponent<Image>();
                if (circleImage != null)
                {
                    circleImage.color = defaultCircleColor;
                    trackingCircles.Add(circleImage);
                }
            }
        }

        #endregion

        #region Game Flow

        private void ShowDialogPanel()
        {
            HideAllPanels();
            dialogPanel.SetActive(true);
            
            if (characterNameText != null)
                characterNameText.text = CHARACTER_NAME;
            
            StartCoroutine(TypewriterEffect(dialogText, GAVI_DIALOG));
        }

        private void ShowInstructionPanel()
        {
            HideAllPanels();
            instructionPanel.SetActive(true);
            StartCoroutine(PlayGlitchEffect());
        }

        private void StartMinigame()
        {
            HideAllPanels();
            minigamePanel.SetActive(true);
            audioManager?.OnGameStart();
            DisplayCurrentQuestion();
        }

        private void DisplayCurrentQuestion()
        {
            if (currentQuestionIndex >= questions.Count)
            {
                ShowResultPanel();
                return;
            }

            var currentQuestion = questions[currentQuestionIndex];
            if (questionText != null)
                questionText.text = currentQuestion.questionText;

            selectedAnswerIndex = -1;
            dragDropAnswers.Clear();
            submitButton.interactable = false;

            if (currentQuestion.type == QuestionType.MultipleChoice)
            {
                DisplayMultipleChoice(currentQuestion);
            }
            else if (currentQuestion.type == QuestionType.DragAndDrop)
            {
                DisplayDragAndDrop(currentQuestion);
            }
        }

        private void DisplayMultipleChoice(MinigameQuestionData question)
        {
            SetContainerActive(multipleChoiceContainer, true);
            SetContainerActive(dragDropContainer, false);

            int newCorrectIndex;
            List<string> shuffledOptions;
            
            try 
            {
                shuffledOptions = question.GetShuffledOptions(out newCorrectIndex);
                question.correctAnswerIndex = newCorrectIndex;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"GetShuffledOptions failed: {e.Message}. Using direct options.");
                shuffledOptions = question.options ?? new List<string>();
                newCorrectIndex = question.correctAnswerIndex;
            }

            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (optionButtons[i] != null)
                {
                    bool shouldShow = i < shuffledOptions.Count;
                    optionButtons[i].gameObject.SetActive(shouldShow);
                    
                    if (shouldShow && i < shuffledOptions.Count)
                    {
                        var buttonText = optionButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                        if (buttonText != null)
                        {
                            buttonText.text = shuffledOptions[i];
                        }
                        else
                        {
                            var textComponents = optionButtons[i].GetComponentsInChildren<TextMeshProUGUI>();
                            if (textComponents.Length > 0)
                            {
                                textComponents[0].text = shuffledOptions[i];
                            }
                            else
                            {
                                var regularText = optionButtons[i].GetComponentInChildren<Text>();
                                if (regularText != null)
                                {
                                    regularText.text = shuffledOptions[i];
                                }
                            }
                        }

                        ResetButtonColorImproved(optionButtons[i]);
                    }
                }
            }
        }

        private void DisplayDragAndDrop(MinigameQuestionData question)
        {
            SetContainerActive(multipleChoiceContainer, false);
            
            if (dragDropContainer != null)
            {
                ForceActivateHierarchy(dragDropContainer, minigamePanel?.transform);
                dragDropContainer.gameObject.SetActive(true);
                ForceLayoutRefresh(dragDropContainer);
            }

            ResetAllDragItems();

            var shuffledScenarios = question.GetShuffledScenarios();

            // Setup drag items - simplified approach
            for (int i = 0; i < dragItems.Length; i++)
            {
                if (dragItems[i] != null)
                {
                    bool shouldShow = i < shuffledScenarios.Count;
                    
                    if (shouldShow)
                    {
                        ForceActivateHierarchy(dragItems[i].transform, dragDropContainer);
                        dragItems[i].SetActive(true);

                        var scenario = shuffledScenarios[i];
                        SetupDragItemData(dragItems[i], scenario);
                    }
                    else
                    {
                        dragItems[i].SetActive(false);
                    }
                }
            }

            // Setup drop zones - simplified approach
            for (int i = 0; i < dropZones.Length; i++)
            {
                if (dropZones[i] != null)
                {
                    bool shouldShow = i < question.categories.Count;
                    
                    if (shouldShow)
                    {
                        ForceActivateHierarchy(dropZones[i].transform, dragDropContainer);
                        dropZones[i].SetActive(true);

                        var category = question.categories[i];
                        SetupDropZoneData(dropZones[i], category);
                    }
                    else
                    {
                        dropZones[i].SetActive(false);
                    }
                }
            }
            
            FixDragDropLayout(question);
            submitButton.interactable = false;
        }

        private void SetupDragItemData(GameObject dragItemGO, DragScenario scenario)
        {
            var dragText = dragItemGO.GetComponentInChildren<TextMeshProUGUI>();
            if (dragText != null) 
                dragText.text = scenario.description;

            var dragImage = dragItemGO.GetComponent<Image>();
            if (dragImage != null) 
                dragImage.color = scenario.backgroundColor;

            var dragComponent = GetDragItemComponent(dragItemGO);
            if (dragComponent != null) 
            {
                dragComponent.Initialize(scenario.id, this);
            }
        }

        private void SetupDropZoneData(GameObject dropZoneGO, DropCategory category)
        {
            var dropText = dropZoneGO.GetComponentInChildren<TextMeshProUGUI>();
            if (dropText != null) 
                dropText.text = category.categoryName;

            var dropComponent = GetDropZoneComponent(dropZoneGO);
            if (dropComponent != null) 
            {
                dropComponent.Initialize(category.id, this);
                dropComponent.SetZoneColor(category.zoneColor);
            }
        }

        private void SelectOption(int optionIndex)
        {
            selectedAnswerIndex = optionIndex;

            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (optionButtons[i] != null && optionButtons[i].gameObject.activeInHierarchy)
                {
                    if (i == optionIndex)
                    {
                        SetButtonColorImproved(optionButtons[i], selectedOptionColor);
                        
                        var buttonImage = optionButtons[i].GetComponent<Image>();
                        if (buttonImage != null)
                        {
                            buttonImage.color = selectedOptionColor;
                        }
                        
                        var outline = optionButtons[i].GetComponent<Outline>();
                        if (outline != null)
                        {
                            outline.enabled = true;
                            outline.effectColor = Color.blue;
                            outline.effectDistance = new Vector2(2, 2);
                        }
                    }
                    else
                    {
                        ResetButtonColorImproved(optionButtons[i]);
                        
                        var buttonImage = optionButtons[i].GetComponent<Image>();
                        if (buttonImage != null)
                        {
                            buttonImage.color = Color.white;
                        }
                        
                        var outline = optionButtons[i].GetComponent<Outline>();
                        if (outline != null)
                        {
                            outline.enabled = false;
                        }
                    }
                }
            }

            submitButton.interactable = true;
        }

        private void SubmitAnswer()
        {
            var currentQuestion = questions[currentQuestionIndex];
            bool isCorrect = false;

            if (currentQuestion.type == QuestionType.MultipleChoice)
            {
                isCorrect = AnswerValidator.ValidateMultipleChoice(currentQuestion, selectedAnswerIndex);
            }
            else if (currentQuestion.type == QuestionType.DragAndDrop)
            {
                isCorrect = AnswerValidator.ValidateDragAndDrop(currentQuestion, dragDropAnswers);
            }

            questionResults.Add(isCorrect);
            UpdateTrackingCircle(currentQuestionIndex, isCorrect);

            if (isCorrect)
                audioManager?.OnQuestionCorrect();
            else
                audioManager?.OnQuestionIncorrect();

            ShowFeedbackPanel(isCorrect, currentQuestion.explanation);
        }

        private void ShowFeedbackPanel(bool isCorrect, string explanation)
        {
            HideAllPanels();
            feedbackPanel.SetActive(true);

            if (feedbackText != null)
            {
                feedbackText.text = isCorrect ? "Benar!" : "Salah!";
                feedbackText.color = isCorrect ? Color.green : Color.red;
            }

            if (feedbackBG != null)
            {
                Color overlayColor = isCorrect ? 
                    new Color(0.2f, 0.8f, 0.2f, 0.3f) :  // Green overlay with transparency
                    new Color(0.8f, 0.2f, 0.2f, 0.3f);   // Red overlay with transparency
                
                // Apply color overlay while keeping the sprite visible
                feedbackBG.DOColor(overlayColor, 0.3f);
            }

            StartCoroutine(TypewriterEffect(explanationText, explanation));

            if (nextButton != null)
            {
                var buttonText = nextButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                    buttonText.text = currentQuestionIndex >= questions.Count - 1 ? "Result" : "Next";
            }
        }

        private void NextQuestion()
        {
            currentQuestionIndex++;
            
            if (currentQuestionIndex >= questions.Count)
            {
                ShowResultPanel();
            }
            else
            {
                ShowMinigamePanel();
                DisplayCurrentQuestion();
            }
        }

        private void ShowMinigamePanel()
        {
            HideAllPanels();
            minigamePanel.SetActive(true);
        }

        private void ShowResultPanel()
        {
            HideAllPanels();
            resultPanel.SetActive(true);

            bool allCorrect = questionResults.All(result => result);
            string message = allCorrect ? "SYSTEM HACKED SUCCESSFULLY! INITIALIZING ..." : "404 Not Found, Failed to Patch!";

            if (resultBG != null)
            {
                Color overlayColor = allCorrect ? 
                    new Color(0.2f, 0.8f, 0.2f, 0.3f) :  // Green overlay with transparency
                    new Color(0.8f, 0.2f, 0.2f, 0.3f);   // Red overlay with transparency
                
                // Apply color overlay while keeping the sprite visible
                resultBG.DOColor(overlayColor, 0.5f);
            }

            audioManager?.OnGameEnd(allCorrect);
            StartCoroutine(TypewriterEffect(resultText, message));
        }

        private void ShowCompletion()
        {
            HideAllPanels();
            completionScreen.SetActive(true);

            bool allCorrect = questionResults.All(result => result);
            
            if (allCorrect)
            {
                ShowSuccessCompletion();
            }
            else
            {
                ShowFailureCompletion();
            }
        }

        private void ShowSuccessCompletion()
        {
            if (completionHeaderText != null)
                completionHeaderText.text = "Target Secured!";
            
            if (ipAddressText != null)
            {
                ipAddressText.text = "IP Address: 192.168.1.100";
                ipAddressText.gameObject.SetActive(true);
            }
            
            if (locationText != null)
            {
                locationText.text = "Location: Jakarta, Indonesia";
                locationText.gameObject.SetActive(true);
            }

            if (quitButton != null) quitButton.gameObject.SetActive(true);
            if (restartButton != null) restartButton.gameObject.SetActive(false);

            if (completionScreenBG != null)
            {
                // Apply green overlay while keeping the sprite visible
                completionScreenBG.DOColor(new Color(0.0f, 0.5f, 0.0f, 0.3f), 0.7f);
            }
        }

        private void ShowFailureCompletion()
        {
            if (completionHeaderText != null)
                completionHeaderText.text = "RESTART THE PROCESS ...";
            
            if (ipAddressText != null) ipAddressText.gameObject.SetActive(false);
            if (locationText != null) locationText.gameObject.SetActive(false);

            if (quitButton != null) quitButton.gameObject.SetActive(false);
            if (restartButton != null) restartButton.gameObject.SetActive(true);

            if (completionScreenBG != null)
            {
                // Apply red overlay while keeping the sprite visible
                completionScreenBG.DOColor(new Color(0.5f, 0.0f, 0.0f, 0.3f), 0.7f);
            }
        }

        #endregion

        #region Drag Drop Integration

        public void OnDragDropPair(string scenarioId, string categoryId)
        {
            if (string.IsNullOrEmpty(scenarioId) || string.IsNullOrEmpty(categoryId))
                return;
            
            dragDropAnswers[scenarioId] = categoryId;
            audioManager?.PlayDragDropSFX();

            var currentQuestion = questions[currentQuestionIndex];
            if (currentQuestion != null)
            {
                bool allPaired = AreAllScenariosPaired(currentQuestion);
                submitButton.interactable = allPaired;
            }
        }

        public void OnDragDropUnpair(string scenarioId)
        {
            if (string.IsNullOrEmpty(scenarioId)) return;
            
            if (dragDropAnswers.ContainsKey(scenarioId))
            {
                dragDropAnswers.Remove(scenarioId);
            }
            
            var currentQuestion = questions[currentQuestionIndex];
            if (currentQuestion != null)
            {
                bool allPaired = AreAllScenariosPaired(currentQuestion);
                submitButton.interactable = allPaired;
            }
        }

        private bool AreAllScenariosPaired(MinigameQuestionData question)
        {
            if (question == null || question.type != QuestionType.DragAndDrop)
                return false;
            
            if (question.scenarios == null || question.scenarios.Count == 0)
                return false;
                
            foreach (var scenario in question.scenarios)
            {
                if (!dragDropAnswers.ContainsKey(scenario.id))
                {
                    return false;
                }
            }
            
            return true;
        }

        public void ResetAllDragItems()
        {
            dragDropAnswers.Clear();
            
            // Clear all drop zones first
            foreach (var dropZone in dropZones)
            {
                if (dropZone != null)
                {
                    var dropComponent = GetDropZoneComponent(dropZone);
                    dropComponent?.ForceRemoveAllItems();
                }
            }
            
            // Reset all drag items to their original positions - simplified
            foreach (var dragItem in dragItems)
            {
                if (dragItem != null && dragItem.activeInHierarchy)
                {
                    var dragComponent = GetDragItemComponent(dragItem);
                    if (dragComponent != null)
                    {
                        dragComponent.ResetToOriginalPosition();
                    }
                    
                    // Simple parent and position reset
                    if (originalDragItemParents.ContainsKey(dragItem))
                    {
                        Transform originalParent = originalDragItemParents[dragItem];
                        if (originalParent != null && dragItem.transform.parent != originalParent)
                        {
                            dragItem.transform.SetParent(originalParent, false);
                        }
                    }
                    
                    // Reset position
                    if (originalDragItemPositions.ContainsKey(dragItem))
                    {
                        RectTransform itemRT = dragItem.GetComponent<RectTransform>();
                        if (itemRT != null)
                        {
                            itemRT.anchoredPosition = originalDragItemPositions[dragItem];
                        }
                    }
                }
            }
            
            submitButton.interactable = false;
        }

        #endregion

        #region Layout Management

        private void FixDragDropLayout(MinigameQuestionData question)
        {
            StartCoroutine(DelayedLayoutFix(question));
        }

        private IEnumerator DelayedLayoutFix(MinigameQuestionData question)
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            
            FixDragItemsLayout(question.GetShuffledScenarios());
            FixDropZonesLayout(question.categories);
        }

        private void FixDragItemsLayout(List<DragScenario> scenarios)
        {
            if (dragDropContainer == null) return;
            
            RectTransform containerRT = dragDropContainer.GetComponent<RectTransform>();
            if (containerRT == null) return;
            
            float containerWidth = containerRT.rect.width;
            float containerHeight = containerRT.rect.height;
            
            float startX = -containerWidth * 0.35f;
            float startY = containerHeight * 0.3f;
            float itemSpacing = 80f;
            
            for (int i = 0; i < dragItems.Length && i < scenarios.Count; i++)
            {
                if (dragItems[i] != null && dragItems[i].activeInHierarchy)
                {
                    // Simplified positioning - work directly with the drag item
                    if (dragItems[i].transform.parent != dragDropContainer)
                    {
                        dragItems[i].transform.SetParent(dragDropContainer, false);
                    }
                    
                    RectTransform itemRT = dragItems[i].GetComponent<RectTransform>();
                    if (itemRT != null)
                    {
                        Vector2 targetPos = new Vector2(startX, startY - (i * itemSpacing));
                        itemRT.anchoredPosition = targetPos;
                        itemRT.localScale = Vector3.one;
                        
                        // Update original position storage
                        originalDragItemPositions[dragItems[i]] = targetPos;
                        
                        var dragComponent = GetDragItemComponent(dragItems[i]);
                        if (dragComponent != null)
                        {
                            dragComponent.SetupPosition(targetPos, dragDropContainer);
                        }
                    }
                }
            }
        }

        private void FixDropZonesLayout(List<DropCategory> categories)
        {
            if (dragDropContainer == null) return;
            
            RectTransform containerRT = dragDropContainer.GetComponent<RectTransform>();
            if (containerRT == null) return;
            
            float containerWidth = containerRT.rect.width;
            float containerHeight = containerRT.rect.height;
            
            float startX = containerWidth * 0.25f;
            float startY = containerHeight * 0.2f;
            float zoneSpacing = 100f;
            
            for (int i = 0; i < dropZones.Length && i < categories.Count; i++)
            {
                if (dropZones[i] != null && dropZones[i].activeInHierarchy)
                {
                    // Simplified positioning - work directly with the drop zone
                    if (dropZones[i].transform.parent != dragDropContainer)
                    {
                        dropZones[i].transform.SetParent(dragDropContainer, false);
                    }
                    
                    RectTransform zoneRT = dropZones[i].GetComponent<RectTransform>();
                    if (zoneRT != null)
                    {
                        Vector2 targetPos = new Vector2(startX, startY - (i * zoneSpacing));
                        zoneRT.anchoredPosition = targetPos;
                        zoneRT.localScale = Vector3.one;
                    }
                }
            }
        }

        private void ForceLayoutRefresh(Transform container)
        {
            if (container == null) return;
            
            LayoutRebuilder.ForceRebuildLayoutImmediate(container.GetComponent<RectTransform>());
            
            foreach (Transform child in container)
            {
                if (child.gameObject.activeInHierarchy)
                {
                    var childRT = child.GetComponent<RectTransform>();
                    if (childRT != null)
                    {
                        LayoutRebuilder.ForceRebuildLayoutImmediate(childRT);
                    }
                }
            }
        }

        #endregion

        #region Game Control

        private void QuitGame()
        {
            audioManager?.OnMenuReturn();
            
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        private void RestartGame()
        {
            currentQuestionIndex = 0;
            questionResults.Clear();
            selectedAnswerIndex = -1;
            dragDropAnswers.Clear();

            foreach (var circle in trackingCircles)
            {
                if (circle != null)
                    circle.color = defaultCircleColor;
            }

            // Reset background images to white
            if (feedbackBG != null)
                feedbackBG.color = Color.white;
            if (resultBG != null)
                resultBG.color = Color.white;
            if (completionScreenBG != null)
                completionScreenBG.color = Color.white;

            ResetAllDragItems();
            ShowInstructionPanel();
        }

        #endregion

        #region Utility Methods

        private void HideAllPanels()
        {
            dialogPanel?.SetActive(false);
            instructionPanel?.SetActive(false);
            minigamePanel?.SetActive(false);
            feedbackPanel?.SetActive(false);
            resultPanel?.SetActive(false);
            completionScreen?.SetActive(false);
        }

        private void SetContainerActive(Transform container, bool active)
        {
            if (container != null)
            {
                container.gameObject.SetActive(active);
            }
        }

        private void SetButtonColorImproved(Button button, Color color)
        {
            if (button != null)
            {
                var colors = button.colors;
                colors.normalColor = color;
                colors.selectedColor = color;
                colors.highlightedColor = color;
                button.colors = colors;
                
                var image = button.GetComponent<Image>();
                if (image != null)
                {
                    image.color = color;
                }
                
                var bgImage = button.transform.Find("Background")?.GetComponent<Image>();
                if (bgImage != null)
                {
                    bgImage.color = color;
                }
            }
        }

        private void ResetButtonColorImproved(Button button)
        {
            SetButtonColorImproved(button, Color.white);
        }

        private void ForceActivateHierarchy(Transform target, Transform stopAt = null)
        {
            if (target == null) return;
            
            List<Transform> hierarchy = new List<Transform>();
            Transform current = target;
            
            while (current != null && current != stopAt)
            {
                hierarchy.Add(current);
                current = current.parent;
            }
            
            for (int i = hierarchy.Count - 1; i >= 0; i--)
            {
                if (!hierarchy[i].gameObject.activeSelf)
                {
                    hierarchy[i].gameObject.SetActive(true);
                }
            }
        }

        private void UpdateTrackingCircle(int index, bool isCorrect)
        {
            if (index >= 0 && index < trackingCircles.Count)
            {
                Color targetColor = isCorrect ? correctCircleColor : incorrectCircleColor;
                trackingCircles[index].DOColor(targetColor, 0.3f);
            }
        }

        private IEnumerator PlayGlitchEffect()
        {
            if (glitchScreen != null)
            {
                glitchScreen.SetActive(true);
                audioManager?.PlayGlitchSFX();

                float duration = 2f;
                float elapsed = 0f;
                
                while (elapsed < duration)
                {
                    Vector3 offset = new Vector3(
                        Random.Range(-10f, 10f),
                        Random.Range(-10f, 10f),
                        0f
                    );
                    glitchScreen.transform.localPosition = offset;
                    
                    float scale = Random.Range(0.95f, 1.05f);
                    glitchScreen.transform.localScale = Vector3.one * scale;
                    
                    elapsed += Time.deltaTime;
                    yield return new WaitForSeconds(0.05f);
                }
                
                glitchScreen.transform.localPosition = Vector3.zero;
                glitchScreen.transform.localScale = Vector3.one;
                glitchScreen.SetActive(false);
            }
        }

        private IEnumerator TypewriterEffect(TextMeshProUGUI textComponent, string message)
        {
            if (textComponent == null || string.IsNullOrEmpty(message)) yield break;
            
            textComponent.text = "";
            audioManager?.StartTypewriterSFX(message.Length * 0.03f, 0.03f);
            
            for (int i = 0; i <= message.Length; i++)
            {
                textComponent.text = message.Substring(0, i);
                yield return new WaitForSeconds(0.03f);
            }
            
            audioManager?.StopTypewriterSFX();
        }

        #endregion

        #region Public Properties

        public bool IsInitialized => isGameInitialized;
        public int CurrentQuestionIndex => currentQuestionIndex;
        public int TotalQuestions => questions?.Count ?? 0;
        public MinigameQuestionData CurrentQuestion => 
            currentQuestionIndex < questions?.Count ? questions[currentQuestionIndex] : null;

        public GameObject DragLayer => dragLayer;
        public Canvas RootCanvas => rootCanvas;
        public Dictionary<string, string> CurrentDragDropAnswers => new Dictionary<string, string>(dragDropAnswers);

        #endregion

        #region Manual Setup Fallback

        public void SetupManualPositions()
        {
            Vector2[] dragPositions = new Vector2[]
            {
                new Vector2(-300, 150),
                new Vector2(-300, 50),
                new Vector2(-300, -50),
                new Vector2(-300, -150),
                new Vector2(-300, -250),
                new Vector2(-300, -350)
            };
            
            Vector2[] dropPositions = new Vector2[]
            {
                new Vector2(200, 100),
                new Vector2(200, 0),
                new Vector2(200, -100)
            };
            
            for (int i = 0; i < dragItems.Length && i < dragPositions.Length; i++)
            {
                if (dragItems[i] != null)
                {
                    // Simplified positioning
                    if (dragItems[i].transform.parent != dragDropContainer)
                    {
                        dragItems[i].transform.SetParent(dragDropContainer, false);
                    }
                    
                    RectTransform itemRT = dragItems[i].GetComponent<RectTransform>();
                    if (itemRT != null)
                    {
                        itemRT.anchoredPosition = dragPositions[i];
                        itemRT.localScale = Vector3.one;
                        
                        originalDragItemPositions[dragItems[i]] = dragPositions[i];
                        
                        var dragComponent = GetDragItemComponent(dragItems[i]);
                        if (dragComponent != null)
                        {
                            dragComponent.SetupPosition(dragPositions[i], dragDropContainer);
                        }
                    }
                }
            }
            
            for (int i = 0; i < dropZones.Length && i < dropPositions.Length; i++)
            {
                if (dropZones[i] != null)
                {
                    // Simplified positioning
                    if (dropZones[i].transform.parent != dragDropContainer)
                    {
                        dropZones[i].transform.SetParent(dragDropContainer, false);
                    }
                    
                    RectTransform zoneRT = dropZones[i].GetComponent<RectTransform>();
                    if (zoneRT != null)
                    {
                        zoneRT.anchoredPosition = dropPositions[i];
                        zoneRT.localScale = Vector3.one;
                    }
                }
            }
        }

        #endregion

        #region Debug and Validation

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void ValidateDragDropSetup()
        {
            Debug.Log("=== Drag Drop Setup Validation ===");
            
            for (int i = 0; i < dragItems.Length; i++)
            {
                if (dragItems[i] != null)
                {
                    var dragComponent = GetDragItemComponent(dragItems[i]);
                    Debug.Log($"DragItem[{i}]: HasComponent={dragComponent != null}, " +
                             $"Parent={dragItems[i].transform.parent?.name}");
                }
            }
            
            for (int i = 0; i < dropZones.Length; i++)
            {
                if (dropZones[i] != null)
                {
                    var dropComponent = GetDropZoneComponent(dropZones[i]);
                    Debug.Log($"DropZone[{i}]: HasComponent={dropComponent != null}, " +
                             $"Parent={dropZones[i].transform.parent?.name}");
                }
            }
        }

        #endregion
    }
}