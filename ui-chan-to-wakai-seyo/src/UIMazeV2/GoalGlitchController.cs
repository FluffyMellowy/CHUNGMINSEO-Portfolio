using UnityEngine;

namespace UIMazeV2
{
    /// <summary>
    /// ゴール演出用のグリッチコントローラー
    /// ミニゲーム1/2/3用のUIChanGlitchM1/M2/M3とは違いゲートを持たず、
    /// 旧UIChanGlitchControllerと同じく OnEnable で即発動する。
    /// ゴールポイントに配置されたUIちゃん/シンボルに対して、登場演出+グリッチパルスループを掛けたい時に使う。
    /// 各種パラメーター（登場・パルス・常時モード・トリガー演出）は基底クラスのインスペクター項目で設定可能。
    /// </summary>
    public sealed class GoalGlitchController : UIChanGlitchController
    {
        /// <summary>
        /// ゲート無し。OnEnable と同時に常に発動可能と判定
        /// </summary>
        protected override bool IsGateOpen() => true;
    }
}
