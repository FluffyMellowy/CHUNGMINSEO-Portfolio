using UnityEngine;

namespace UIMazeV2
{
    /// <summary>
    /// ミニゲーム1(トップビュー)用 UIちゃんグリッチ演出
    /// _gatePlayer (TopViewPlayer) を割り当てない場合は OnEnable で即発動。
    /// 割り当てた場合は そのコントローラーがenabled になった瞬間を起点に発動する
    /// （TopViewPlayerには着地概念が無いためenable立ち上がりをトリガーとする）
    /// </summary>
    public sealed class UIChanGlitchM1 : UIChanGlitchController
    {
        [Header("ミニゲーム1ゲート（任意）")]
        [Tooltip("未設定なら即発動。設定した場合はTopViewPlayerのenabledが立った瞬間に発動")]
        [SerializeField] private TopViewPlayer _gatePlayer;

        protected override bool IsGateOpen()
        {
            // ゲート未設定 = 即発動
            if (_gatePlayer == null) return true;
            return _gatePlayer.enabled;
        }
    }
}
