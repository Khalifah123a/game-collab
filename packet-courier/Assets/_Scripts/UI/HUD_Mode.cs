using UnityEngine;
using UnityEngine.UI;

public class HUD_Mode : MonoBehaviour
{
    [Header("References")]
    public Image modeIcon;
    public Text modeText;
    public Text portText;
    public Text bitsText;

    [Header("Icons")]
    public Sprite plaintextIcon;
    public Sprite encryptedIcon;

    [Header("Color")]
    public Color plaintextColor = Color.green;
    public Color encryptedColor = Color.cyan;

    [Header("Port Names")]
    public string[] portNames = { "FTP", "SSH", "HTTP", "HTTPS", "Alt HTTP" };
    public int[] portValues = { 21, 22, 80, 443, 8080 };

    private PlayerMode _mode;

    private void Start()
    {
        _mode = FindObjectOfType<PlayerMode>();
        if (_mode == null)
            enabled = false;
    }

    private void Update()
    {
        if (_mode == null) return;

        bool enc = _mode.isEncrypted;
        modeText.text = enc ? "ENCRYPTED" : "PLAINTEXT";
        modeText.color = enc ? encryptedColor : plaintextColor;

        int idx = System.Array.IndexOf(_mode.availablePorts, _mode.currentPort);
        string name = idx >= 0 && idx < portNames.Length ? portNames[idx] : "?";
        portText.text = $"PORT {_mode.currentPort} ({name})";
        portText.color = enc ? encryptedColor : plaintextColor;

        if (bitsText && GameManager.Instance)
            bitsText.text = $"BITS \u00A5 {GameManager.Instance.dataBitsCollectedThisRun}";
    }
}
