using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text.RegularExpressions;
using UnityEngine.InputSystem;

public class VirtualKeyboard : MonoBehaviour
{
    [Header("Display")]
    [SerializeField] private TextMeshProUGUI displayText;
    [SerializeField] private int maxCharacters = 5;

    [Header("Control Buttons")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button backspaceButton;
    [SerializeField] private Button capsLockButton;

    [Header("Key Buttons")]
    // Inspectorで「A」～「Z」のボタンを登録するためのリスト
    // 各ボタンにはテキストコンポーネントか、あるいはGameObject名から文字を判別するロジックが必要
    // ここでは簡略化のため、Buttonコンポーネントを持つ全ての子オブジェクトから取得するアプローチも可能だが、
    // 明示的に割り当てられるようにリストを用意する
    [SerializeField] private List<Button> characterButtons;

    [Header("Settings")]
    [SerializeField] private Color activeCapsColor = Color.yellow;
    [SerializeField] private Color inactiveCapsColor = Color.white;
    // キャプスロックボタンのターゲット画像（色を変えるため）
    [SerializeField] private Image capsLockImage;

    // Events
    public System.Action<string> OnConfirm;

    private string currentInput = "";
    private bool isCaps = false; // デフォルトは大文字

    private void Start()
    {
        InitializeButtons();
        UpdateDisplay();
    }

    private void OnEnable()
    {
        if (Keyboard.current != null)
        {
            Keyboard.current.onTextInput += OnTextInput;
        }
    }

    private void OnDisable()
    {
        if (Keyboard.current != null)
        {
            Keyboard.current.onTextInput -= OnTextInput;
        }
    }

    private void Update()
    {
        HandlePhysicalInput();
    }

    private void InitializeButtons()
    {
        // 文字ボタンのリスナー登録
        foreach (Button btn in characterButtons)
        {
            if (btn == null) continue;

            // ボタンのテキストを取得
            TextMeshProUGUI btnText = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                string charString = btnText.text.ToUpper(); // ラベルを文字として使う
                btn.onClick.AddListener(() => OnCharacterPress(charString));
            }
            else
            {
                // テキストがない場合、オブジェクト名から推測（例: "Key_A" -> "A"）
                string name = btn.gameObject.name;
                string charString = name.Replace("Key_", "").ToUpper();
                if(charString.Length == 1)
                {
                    btn.onClick.AddListener(() => OnCharacterPress(charString));
                }
            }
        }

        // 機能ボタン
        if (backspaceButton != null)
        {
            backspaceButton.onClick.AddListener(OnBackspace);
        }
        if (capsLockButton != null)
        {
            capsLockButton.onClick.AddListener(ToggleCaps);
        }
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmPress);
        }

        UpdateCapsVisual();
    }

    private void HandlePhysicalInput()
    {
        if (Keyboard.current == null) return;

        // バックスペース
        if (Keyboard.current.backspaceKey.wasPressedThisFrame)
        {
            OnBackspace();
        }
        // Enter
        else if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame)
        {
            OnConfirmPress();
        }
    }

    private void OnTextInput(char c)
    {
        if (char.IsLetter(c))
        {
            AddCharacter(c.ToString());
        }
    }

    public void OnCharacterPress(string character)
    {
        // 仮想キーボードからの入力はisCapsに従う
        string charToAdd = isCaps ? character.ToUpper() : character.ToLower();
        AddCharacter(charToAdd);
    }

    private void AddCharacter(string c)
    {
        if (currentInput.Length < maxCharacters)
        {
            currentInput += c;
            UpdateDisplay();
        }
    }

    public void OnBackspace()
    {
        if (currentInput.Length > 0)
        {
            currentInput = currentInput.Substring(0, currentInput.Length - 1);
            UpdateDisplay();
        }
    }

    public void ToggleCaps()
    {
        isCaps = !isCaps;
        UpdateCapsVisual();
        UpdateKeyLabels();
    }

    private void UpdateCapsVisual()
    {
        if (capsLockImage != null)
        {
            capsLockImage.color = isCaps ? activeCapsColor : inactiveCapsColor;
        }
    }

    private void UpdateKeyLabels()
    {
        foreach (Button btn in characterButtons)
        {
            if (btn == null) continue;
            TextMeshProUGUI btnText = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                string currentText = btnText.text;
                btnText.text = isCaps ? currentText.ToUpper() : currentText.ToLower();
            }
        }
    }

    public void OnConfirmPress()
    {
        // 確認
        if (currentInput.Length > 0)
        {
            OnConfirm?.Invoke(currentInput);
        }
    }

    private void UpdateDisplay()
    {
        if (displayText != null)
        {
            // カーソル点滅などを入れたい場合はここで装飾
            displayText.text = currentInput + "_"; 
        }
    }

    // 外部から初期化する場合
    public void ResetInput()
    {
        currentInput = "";
        UpdateDisplay();
    }
}
