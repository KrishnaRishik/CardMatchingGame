using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Cards : MonoBehaviour
{
    [SerializeField] private Image iconImage;

    public Sprite hiddenIconSprite;
    public Sprite iconSprite;
    public bool isSelected;
    public bool isMatched = false;

    public CardController controller;
    private bool isFlipping = false;

    public void OnCardClick()
    {
        if (!isFlipping && !isMatched)
        {
            Debug.Log("Card clicked!");
            controller.SetSelected(this);
        }
    }

    public void SetIconSprite(Sprite sp)
    {
        iconSprite = sp;
        iconImage.sprite = hiddenIconSprite;
        isSelected = false;
        isMatched = false;
    }

    public void Show()
    {
        if (isFlipping || isSelected || isMatched) return;
        StartCoroutine(FlipTo(iconSprite));
        isSelected = true;
        controller.PlayFlipSound();
    }

    public void Hide()
    {
        if (isFlipping || !isSelected || isMatched) return;
        StartCoroutine(FlipTo(hiddenIconSprite));
        isSelected = false;
    }

    public void SetMatched(bool matched)
    {
        isMatched = matched;
        isSelected = matched;

        if (matched)
        {
            iconImage.color = new Color(1f, 1f, 1f, 0.7f);
        }
    }
    public void ResetCard()
    {
        StopAllCoroutines();
        isSelected = false;
        isMatched = false;
        isFlipping = false;
        iconImage.sprite = hiddenIconSprite;
        iconImage.color = Color.white; // Reset color in case it was faded
        transform.rotation = Quaternion.identity;
    }


    private IEnumerator FlipTo(Sprite newSprite)
    {
        isFlipping = true;

        float duration = 0.4f;
        float halfDuration = duration / 2f;

        Quaternion startRotation = transform.rotation;
        Quaternion midRotation = Quaternion.Euler(0, 90, 0);
        Quaternion endRotation = Quaternion.Euler(0, 0, 0);

        float t = 0;
        while (t < halfDuration)
        {
            t += Time.deltaTime;
            float lerpT = Mathf.SmoothStep(0, 1, t / halfDuration);
            transform.rotation = Quaternion.Slerp(startRotation, midRotation, lerpT);
            yield return null;
        }

        iconImage.sprite = newSprite;

        t = 0;
        while (t < halfDuration)
        {
            t += Time.deltaTime;
            float lerpT = Mathf.SmoothStep(0, 1, t / halfDuration);
            transform.rotation = Quaternion.Slerp(midRotation, endRotation, lerpT);
            yield return null;
        }
        transform.rotation = endRotation;
        isFlipping = false;
    }
}
