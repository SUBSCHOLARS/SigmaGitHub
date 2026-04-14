using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MemoryHolePanelController : MonoBehaviour
{
    [Header("UI参照")]
    [SerializeField] private Transform targetHandArea;
    [SerializeField] private Transform playerHandArea;
    [SerializeField] private TextMeshProUGUI targetValueText;
    [SerializeField] private TextMeshProUGUI playerValueText;
    [SerializeField] private GameObject confirmButton;

    private Player targetPlayer;
    private Player humanPlayer;
    private GameObject cardPrefab;

    private CardData selectedTargetCard;
    private CardData selectedPlayerCard;
    private GameObject selectedTargetCardObj;
    private GameObject selectedPlayerCardObj;

    private Transform cachedSelectedTargetCardTransform;

    // ハイライト色
    private static readonly Color SelectedColor = new Color(1f, 0.9f, 0.3f); // 黄色
    private static readonly Color DisabledColor = new Color(0.4f, 0.4f, 0.4f); // グレー

    public void Initialize(Player target, Player human, GameObject cardPrefabRef)
    {
        targetPlayer = target;
        humanPlayer = human;
        cardPrefab = cardPrefabRef;

        selectedTargetCard = null;
        selectedPlayerCard = null;
        selectedTargetCardObj = null;
        selectedPlayerCardObj = null;
        confirmButton.SetActive(false);

        PopulateHand(targetHandArea, target.hand, isFromTarget: true, allowIdeology: true);
        PopulateHand(playerHandArea, human.hand, isFromTarget: false, allowIdeology: false);
        UpdatePreview();
    }

    private void PopulateHand(Transform container, List<CardData> hand, bool isFromTarget, bool allowIdeology)
    {
        // 既存カードを削除
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }
        int delta=0;
        foreach (CardData card in hand)
        {
            Vector3 pos=new Vector3(delta, 0, 0);
            GameObject cardObj = Instantiate(cardPrefab, container);

            // CardController でスプライトセットアップ
            CardController cc = cardObj.GetComponent<CardController>();
            if (cc != null) cc.Setup(card);

            // raycastTarget を有効化（IPointerClickHandler のために必要）
            Image img = cardObj.GetComponent<Image>();
            if (img != null) img.raycastTarget = true;

            bool isIdeology = card.ideologyType != IdeologyType.None;
            bool isSelectable = allowIdeology || !isIdeology;

            if (!isSelectable)
            {
                // イデオロギーカードはグレーアウトして非インタラクティブ
                if (img != null) img.color = DisabledColor;
            }
            else
            {
                // セレクターを追加
                MemoryHoleCardSelector selector = cardObj.AddComponent<MemoryHoleCardSelector>();
                selector.cardData = card;
                selector.isFromTarget = isFromTarget;
                selector.controller = this;
            }
            delta += 5;
        }
    }

    public void OnTargetCardClicked(CardData card, GameObject cardObj)
    {
        // 前の選択を解除
        if (selectedTargetCardObj != null)
        {
            Image prev = selectedTargetCardObj.GetComponent<Image>();
            if (prev != null) prev.color = Color.white;
        }
        selectedTargetCard = card;
        selectedTargetCardObj = cardObj;
        cachedSelectedTargetCardTransform = cardObj.transform;
        Debug.Log($"ターゲットカードの座標: {cachedSelectedTargetCardTransform.position}");

        Image img = cardObj.GetComponent<Image>();
        if (img != null) img.color = SelectedColor;

        UpdatePreview();
    }

    public void OnPlayerCardClicked(CardData card, GameObject cardObj)
    {
        // 前の選択を解除
        if (selectedPlayerCardObj != null)
        {
            Image prev = selectedPlayerCardObj.GetComponent<Image>();
            if (prev != null) prev.color = Color.white;
        }
        selectedPlayerCard = card;
        selectedPlayerCardObj = cardObj;

        Image img = cardObj.GetComponent<Image>();
        if (img != null) img.color = SelectedColor;

        UpdatePreview();
    }

    private void UpdatePreview()
    {
        int targetCurrent = SumHandValue(targetPlayer.hand);
        int playerCurrent = SumHandValue(humanPlayer.hand);

        if (selectedTargetCard != null && selectedPlayerCard != null)
        {
            int targetNew = targetCurrent - selectedTargetCard.handValue + selectedPlayerCard.handValue;
            int playerNew = playerCurrent - selectedPlayerCard.handValue;
            targetValueText.text = $"{targetPlayer.playerName}  HAND: {targetCurrent} → {targetNew}";
            playerValueText.text = $"{humanPlayer.playerName}  HAND: {playerCurrent} → {playerNew}";
            confirmButton.SetActive(true);
        }
        else if (selectedTargetCard != null)
        {
            targetValueText.text = $"{targetPlayer.playerName}  HAND: {targetCurrent} → ?";
            playerValueText.text = $"{humanPlayer.playerName}  HAND: {playerCurrent}";
            confirmButton.SetActive(false);
        }
        else if (selectedPlayerCard != null)
        {
            int playerNew = playerCurrent - selectedPlayerCard.handValue;
            targetValueText.text = $"{targetPlayer.playerName}  HAND: {targetCurrent}";
            playerValueText.text = $"{humanPlayer.playerName}  HAND: {playerCurrent} → {playerNew}";
            confirmButton.SetActive(false);
        }
        else
        {
            targetValueText.text = $"{targetPlayer.playerName}  HAND: {targetCurrent}";
            playerValueText.text = $"{humanPlayer.playerName}  HAND: {playerCurrent}";
            confirmButton.SetActive(false);
        }
    }

    private int SumHandValue(List<CardData> hand)
    {
        int total = 0;
        foreach (var card in hand) total += card.handValue;
        return total;
    }

    // CONFIRM ボタンから呼ばれる
    public void OnConfirmClicked()
    {
        if (selectedTargetCard == null || selectedPlayerCard == null) return;
        StartCoroutine(PlayConfirmAnimation());
    }

    private IEnumerator PlayConfirmAnimation()
    {
        confirmButton.SetActive(false);

        float duration = 0.5f;
        // ターゲットカードを画面外右へ
        if (selectedTargetCardObj != null)
        {
            RectTransform rt = selectedTargetCardObj.GetComponent<RectTransform>();
            if (rt != null){
            yield return rt.DOAnchorPos(rt.anchoredPosition + new Vector2(1200f, 0f), duration)
                .SetEase(Ease.InOutQuad).WaitForCompletion();
            }
        }

        // 自分のカードをターゲット手札エリアへ
        if (selectedPlayerCardObj != null)
        {
            RectTransform rt = selectedPlayerCardObj.GetComponent<RectTransform>();
            Transform destination = UIManager.Instance.GetHandContainerForPlayer(targetPlayer);

            yield return rt.DOMove(destination.position, duration).SetEase(Ease.InOutQuad).WaitForCompletion();
        }

        // 効果実行
        UIManager.Instance.HideMemoryHolePanel();
        GameManager.Instance.ExecuteMemoryHoleEffect(targetPlayer, selectedTargetCard, selectedPlayerCard);
    }
}
