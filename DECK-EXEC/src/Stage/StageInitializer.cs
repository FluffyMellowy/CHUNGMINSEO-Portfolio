namespace Colorless.Stage
{
    using UnityEngine;
    using VContainer;
    using Colorless.Entity;
    using Colorless.Entity.Enemy;
    using Colorless.Mission;

    /// <summary>
    /// ステージ内エンティティの初期化を担当。
    /// GridManager のグリッド生成完了後に Start で実行され、
    /// 全エンティティ初期化後に MissionManager へ通知してミッションを開始する。
    /// </summary>
    public sealed class StageInitializer : MonoBehaviour
    {
        [Inject] private MissionManager _missionManager;

        private void Start()
        {
            /* プレイヤー初期化 */
            Player[] players = FindObjectsByType<Player>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (Player player in players)
                player.Initialize();

            /* エネミー初期化 */
            EnemyBase[] enemies = FindObjectsByType<EnemyBase>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (EnemyBase enemy in enemies)
                enemy.Initialize();

            /* 箱初期化 */
            Box[] boxes = FindObjectsByType<Box>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (Box box in boxes)
                box.Initialize();

            /* 全エンティティ初期化完了 → ミッション開始 */
            _missionManager.OnStageReady();
        }
    }
}
