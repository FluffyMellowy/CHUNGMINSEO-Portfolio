using UnityEngine;

namespace UIMazeV2
{
    /// <summary>
    /// ミニゲーム3(クレジットスクロール)用 UIちゃんグリッチ演出
    /// _gatePlayer (PlatformerPlayerB) を割り当てない場合は OnEnable で即発動。
    /// 割り当てた場合は そのプレイヤーが地面に初着地した瞬間を起点に発動する
    /// （CreditScrollerと同じ着地ゲートと同タイミングで発動可能）
    /// </summary>
    public sealed class UIChanGlitchM3 : UIChanGlitchController
    {
        [Header("ミニゲーム3ゲート（着地, 任意）")]
        [Tooltip("未設定なら即発動。設定した場合はPlatformerPlayerBが着地した瞬間に発動")]
        [SerializeField] private PlatformerPlayerB _gatePlayer;

        protected override bool IsGateOpen()
        {
            if (_gatePlayer == null) return true;
            return _gatePlayer.IsGrounded;
        }
    }
}
