<!-- TODO: 「CHUNG」を本名に差し替え可 -->
# CHUNG — 開発ポートフォリオ

---

## 制作物

## 1. Reconcile with UIChan（9人チーム制作）

- **役割**：プログラマー（**対話システム** / **UI迷路 ミニゲーム** ＋ タイトル・リザルト）
- **エンジン**：Unity 6 (6000.0.59f2) / URP
- **受賞**： BitSummitGameJamにて、協賛企業賞および審査員賞をチームで受賞
- **設計**：UniTask による非同期制御 ／ イベント・デリゲート駆動の疎結合 ／ CSV データ駆動
- 🔗 **コード**：[`Reconcile-with-UIChan/src/`](Reconcile-with-UIChan/src/)
- ▶ **プレイ**：[itch.io（BitSummit Game Jam 2026）](https://bitsummit-gamejam.itch.io/uityantowakaiseyo)

### 担当① 対話システム（[`src/Dialogue/`](Reconcile-with-UIChan/src/Dialogue/)）
CSV 駆動のシナリオエンジン。タイトル〜本編〜リザルトを貫くナラティブ基盤を実装。

- **UniTask + CancellationToken** による非同期対話ループ（再呼び出し時も安全に中断）。
- **JP⇄EN マルチ言語対応

<p align="center">
  <img src="Reconcile-with-UIChan/screenshots/dialogue_flow.svg" alt="ゲーム全体のナラティブフロー" width="540">
</p>

### 担当② UI迷路 ミニゲーム（[`src/UIMazeV2/`](Reconcile-with-UIChan/src/UIMazeV2/)）
1つのミニゲーム枠の中に **3種のミニゲーム**（見下ろし迷路 / 横スクロール / クレジット）が入れ替わりで登場し、プレイヤー1体を引き継いで進行する。

- **ウィンドウ遷移** — クリアごとに `ShowWindow()` で対象ウィンドウのみを表示し、プレイヤー・カメラ・サウンドを次のミニゲームへ引き継ぐ。
- **SpriteMask** によるウィンドウ単位クリッピング＋プレイヤー追従フレーム。
- 軌跡記録→再生で追う**“自分の影”ゴースト**、dissolve / glitch シェーダ演出。
- 落下障害物の枠貫通衝突、着地連動のウィンドウ縮小、双方向テレポート、ローカライズドボイス。

<p align="center">
  <img src="Reconcile-with-UIChan/screenshots/minigame_flow.svg" alt="3つのミニゲームのウィンドウ遷移" width="720">
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
