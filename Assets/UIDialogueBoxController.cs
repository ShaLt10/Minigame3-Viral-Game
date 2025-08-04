using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIDialogBoxController : MonoBehaviour
{
    [Header("Dialog Box Elements")]
    public Image characterPortrait;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogText;
    public Button nextButton;

    public void SetDialog(string characterName, string dialog, Sprite characterSprite)
    {
        nameText.text = characterName;
        dialogText.text = dialog;

        if (characterPortrait != null)
        {
            characterPortrait.sprite = characterSprite;
            characterPortrait.enabled = (characterSprite != null);
        }
    }

    public void SetNextButtonActive(bool active)
    {
        nextButton.gameObject.SetActive(active);
    }
}
