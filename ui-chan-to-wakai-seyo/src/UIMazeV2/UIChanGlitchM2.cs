using UnityEngine;

namespace UIMazeV2
{
    /// <summary>
    /// ミニゲーム2(横スクロール)用 UIちゃんグリッチ演出
    /// _gatePlayer (PlatformerPlayer) を割り当てない場合は OnEnable で即発動。
    /// 割り当てた場合は そのプレイヤーが地面に初着地した瞬間を起点に発動する
    /// </summary>
    public sealed class UIChanGlitchM2 : UIChanGlitchController
    {
        [Header("ミニゲーム2ゲート（着地, 任意）")]
        [Tooltip("未設定なら即発動。設定した場合はPlatformerPlayerが着地した瞬間に発動")]
        [SerializeField] private PlatformerPlayer _gatePlayer;

        protected override bool IsGateOpen()
        {
            if (_gatePlayer == null) return true;
            return _gatePlayer.IsGrounded;
        }
    }
}
