namespace Colorless.DI
{
    using UnityEngine;
    using VContainer;
    using VContainer.Unity;
    using Colorless.Card;
    using Colorless.Debugging;
    using Colorless.Entity;
    using Colorless.Entity.Enemy;
    using Colorless.Grid;
    using Colorless.Mission;
    using Colorless.Sequence;
    using Colorless.Stage;
    using Colorless.Turn;
    using Colorless.UI;

    public sealed class GameLifetimeScope : LifetimeScope
    {
        [Header("Assets")]
        [SerializeField] private LogColorPalette _logColorPalette;

        protected override void Configure(IContainerBuilder builder)
        {
            /* 核心マネージャー登録（シーン内から検出） */
            builder.RegisterComponentInHierarchy<GridManager>();
            builder.RegisterComponentInHierarchy<TurnManager>();
            builder.RegisterComponentInHierarchy<StageManager>();
            builder.RegisterComponentInHierarchy<StageInitializer>();
            builder.RegisterComponentInHierarchy<MissionManager>();
            builder.RegisterComponentInHierarchy<Player>();
            builder.RegisterComponentInHierarchy<CardInputController>();

            /* Phase 11 : ステージプレハブスワップ。StageLoader はまだシーンに無くても良いので、
               存在する場合のみ登録する（無いと RegisterComponentInHierarchy が Build 時に例外を投げて
               スコープ全体が壊れる）。SceneTransitioner は DontDestroyOnLoad シングルトンで Instance を直接使う。 */
            StageLoader stageLoader = FindAnyObjectByType<StageLoader>(FindObjectsInactive.Include);
            if (stageLoader != null)
                builder.RegisterInstance(stageLoader);

            /* カード・シーケンスシステム（プレーンクラス、シングルトン） */
            builder.Register<GameContext>(Lifetime.Singleton);
            builder.Register<CardHand>(Lifetime.Singleton);
            builder.Register<SequenceQueue>(Lifetime.Singleton);
            builder.Register<SequenceExecutor>(Lifetime.Singleton);
            builder.Register<EntityCollisionResolver>(Lifetime.Singleton);

            /* ログインフラ。Palette はインスペクター未割当でも GameContext 解決失敗を防ぐため、
               null の場合は ScriptableObject 既定値でフォールバックを作って登録する。
               色は後で .asset を作って差し替えるまでデフォルトのままで動作する。 */
            builder.Register<IActionLogger, ActionLogger>(Lifetime.Singleton);
            LogColorPalette palette = _logColorPalette != null
                ? _logColorPalette
                : ScriptableObject.CreateInstance<LogColorPalette>();
            builder.RegisterInstance(palette);

            /* シーン配置の MonoBehaviour へ [Inject] フィールドを注入 */
            builder.RegisterBuildCallback(container =>
            {
                /* エンティティ系（複数存在し得る） */
                foreach (Box box in FindObjectsByType<Box>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    container.Inject(box);
                }
                foreach (EnemyBase enemy in FindObjectsByType<EnemyBase>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    container.Inject(enemy);
                }

                /* UI コンポーネント（[Inject] フィールド注入のみ、サービスとしては未登録） */
                InjectAllOfType<CardHandUI>(container);
                InjectAllOfType<SequenceQueueUI>(container);
                InjectAllOfType<RunButtonUI>(container);
                InjectAllOfType<MapPreviewRenderer>(container);
                InjectAllOfType<DirectionSelectorUI>(container);
                InjectAllOfType<RestoreButtonUI>(container);
                InjectAllOfType<SequenceHistoryUI>(container);
                InjectAllOfType<ActionLogUI>(container);
                InjectAllOfType<StageStateOverlay>(container);
                InjectAllOfType<ExecutionLogAutoSwitcher>(container);

                /* デバッグ系 */
                InjectAllOfType<DebugHUD>(container);
            });
        }

        private static void InjectAllOfType<T>(IObjectResolver container) where T : MonoBehaviour
        {
            foreach (T ui in FindObjectsByType<T>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                container.Inject(ui);
            }
        }
    }
}
