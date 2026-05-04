using UnityEngine;

public class StartRetroWipe : MonoBehaviour
{
    [Header("シーン内のRetroWipeEffect")]
    [SerializeField] private RetroWipeEffect retroWipeEffect;
    void Start()
    {
        StartCoroutine(retroWipeEffect.AnimateWipe());
    }
}
