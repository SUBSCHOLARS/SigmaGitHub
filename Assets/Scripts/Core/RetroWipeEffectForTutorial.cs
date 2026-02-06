using System.Collections;
using UnityEngine;

public class RetroWipeEffectForTutorial : RetroWipeEffect
{
    [SerializeField] private GameObject tutorialGameManager;
    public override IEnumerator AnimateWipe()
    {
        yield return base.AnimateWipe();
    }
    public override void Deactivate()
    {
        tutorialGameManager.SetActive(true);
        gameObject.SetActive(false);
    }
}
