using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DistortedGraphicRaycaster : GraphicRaycaster
{
    [Header("歪みの設定")]
    [Tooltip("シェーダーのLength補正値の計数（アスペクト比補正などがあれば）")]
    public Vector2 aspectCorrection=new Vector2(1.0f, 1.0f);
    [Tooltip("シェーダーのPowerノードの指数（通常は2）")]
    public float distortionPower=2.0f;
    [Tooltip("シェーダーのStrength（歪みの強さ）")]
    public float distortionStrength=0.25f;

    // Raycastメソッドをオーバーライドして、判定位置を書き換える
    public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
    {
        // 1. 本来のマウス位置をバックアップ
        Vector2 originalPosition=eventData.position;
        // 2. マウス位置をシェーダーと同じロジックで歪ませる
        eventData.position=GetDistortedPosition(originalPosition);
        // 3. 歪んだ位置で判定を行う（親クラスの処理を実行）
        base.Raycast(eventData, resultAppendList);
        // 4. マウス位置を元に戻す（他の処理に影響を与えないため）
        eventData.position=originalPosition;
    }
    // シェーダーグラフのロジックをC#で再現する関数
    private Vector2 GetDistortedPosition(Vector2 screenPos)
    {
        // スクリーン座標（0~width, 0~height）をUV座標（0~1）に変換
        Vector2 uv=new Vector2(screenPos.x/Screen.width, screenPos.y/Screen.height);

        // 1. 中心を(0, 0)にずらす（Add -0.5）
        Vector2 centered=uv-new Vector2(0.5f, 0.5f);
        // 2. アスペクト比補正など（もしグラフに追加していれば）
        Vector2 corrected=centered*aspectCorrection;
        // 3. 距離を計算（Length）
        float r=corrected.magnitude;
        // 4. 歪み率を計算（Power -> Multiply+1）
        // シェーダー: uv*(1+strength*r^power)
        // 補正したいのは「クリック位置」なので、シェーダーと同じ計算を適用して
        // 「見た目上のボタン位置」まで座標を飛ばす
        float factor=1.0f+distortionStrength*Mathf.Pow(r, distortionPower);
        // 5. 座標に適用
        Vector2 distorted=centered*factor;
        // 6. 座標を(0~1)に戻す（Add 0.5）
        Vector2 resultUV=distorted+new Vector2(0.5f, 0.5f);
        // スクリーン座標に戻して返す
        return new Vector2(resultUV.x*Screen.width, resultUV.y*Screen.height);
    }
}
