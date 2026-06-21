<!-- TODO: 「CHUNG」を本名に差し替え可 -->
# CHUNG — 開発ポートフォリオ

---

## 制作物

## 1. Reconcile with UIChan（9人チーム制作）

- **役割**：プログラマー（**対話システム** / **UI迷路 ミニゲーム** ＋ タイトル・リザルト）
- **出展**：**BitSummit 2026** 出展・受賞作<!-- TODO: 受賞名 / プレイ可能リンクを記入 -->
- **エンジン**：Unity 6 (6000.0.59f2) / URP
- **使用技術**：C# · UniTask · DOTween · Febucci Text Animator · TextMeshPro · Feel · Input System
- **設計**：C# イベント／デリゲートによる疎結合
- **規模**：担当 約7,600行 / 85ファイル
- 🔗 **コード**：[`ui-chan-to-wakai-seyo/src/`](ui-chan-to-wakai-seyo/src/)

### 担当① 対話システム（[`src/Dialogue/`](ui-chan-to-wakai-seyo/src/Dialogue/)）
CSV 駆動のシナリオエンジン。タイトル〜本編〜リザルトを貫くナラティブ基盤を実装。

- **UniTask + CancellationToken** による非同期対話ループ（再呼び出し時も安全に中断）。
- **Febucci タイプライタ**統合（完了検知 / フレーム単位スキップ）。
- **JP⇄EN リアルタイム言語切替**（表示中の行を即再レンダリング）。

```mermaid
flowchart TD
    S["StartDialogue(id)"] --> Q{現在行あり?}
    Q -- なし --> E["終了 → SectionTypeEvent.Raise"]
    Q -- あり --> T["ShowText: タイプライタ表示"]
    T --> W["入力待ち / AUTO / SKIP"]
    W --> FT["FireTrigger → 次ID = NextId"]
    FT --> Q
```

### 担当② UI迷路 ミニゲーム（[`src/UIMazeV2/`](ui-chan-to-wakai-seyo/src/UIMazeV2/)）
1つのミニゲーム枠の中に **3種のミニゲーム**（見下ろし迷路 / 横スクロール / クレジット）が入れ替わりで登場し、プレイヤー1体を引き継いで進行する。

- **ウィンドウ遷移** — クリアごとに `ShowWindow()` で対象ウィンドウのみを表示し、プレイヤー・カメラ・サウンドを次のミニゲームへ引き継ぐ。
- **SpriteMask** によるウィンドウ単位クリッピング＋プレイヤー追従フレーム。
- 軌跡記録→再生で追う**“自分の影”ゴースト**、dissolve / glitch シェーダ演出。
- 落下障害物の枠貫通衝突、着地連動のウィンドウ縮小、双方向テレポート、ローカライズドボイス。

<p align="center">
  <img src="ui-chan-to-wakai-seyo/screenshots/minigame_flow.svg" alt="3つのミニゲームのウィンドウ遷移" width="720">
</p>

<!--
## 2. （次のプロジェクト名）（ジャンル / 制作規模）
- 役割：
- 使用技術：
- コード：
-->

---

## 使用言語

- **C#** (Unityでのゲーム開発)
- **C++** (Unrealでのゲーム開発)
- **Python** (大学講義。機械学習目的のPytorchなど)
