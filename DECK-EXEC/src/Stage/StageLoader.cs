namespace Colorless.Stage
{
    using System;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using R3;
    using Sirenix.OdinInspector;
    using UnityEngine;
    using VContainer;
    using VContainer.Unity;
    using Colorless.Mission;

    /// <summary>
    /// 永続シーンの StageContainer 配下にステージプレハブを動的に差し替えるオーケストレータ。
    /// 旧来のシーン遷移（SceneManager.LoadScene）の代替で、Phase 11 でステージ＝プレハブ化したあとに利用する。
    ///
    /// 流れ：
    ///   1. SceneTransitioner.FadeOutAsync
    ///   2. 旧プレハブを Destroy（OnStageUnloading 発火）
    ///   3. 新プレハブを Instantiate（StageContainer の子として）
    ///   4. プレハブ内部の [Inject] フィールドを VContainer で解決
    ///   5. OnStageLoaded を発火（permanent シーン側の UI 等が現スタージの MissionManager を取得）
    ///   6. StageInitializer 不在時は MissionManager.OnStageReady() を直接ノック（保険）
    ///   7. SceneTransitioner.FadeInAsync
    /// </summary>
    public sealed class StageLoader : MonoBehaviour
    {
        [Title("Refs")]
        [Required, InfoBox("ロードされたステージプレハブがインスタンス化される親 Transform。永続シーンに置いた空 GameObject を割り当てる。")]
        [SerializeField] private Transform _stageContainer;

        [Title("Legacy Bootstrap")]
        [InfoBox("Phase 11 移行前のレガシーシーン（Stage1.unity 等）でこの StageLoader を使うとき、ここにシーン内の MissionManager をドラッグしておくと、Start 時に擬似的に OnStageLoaded を発火し、subscriber が初期ステージとして認識できる。新構成（StageHostScene + プレハブ）では null のままで OK。")]
        [SerializeField] private Colorless.Mission.MissionManager _initialFallbackMission;
        [SerializeField] private StageNode _initialFallbackNode;

        [Title("Runtime State")]
        [ShowInInspector, ReadOnly] private bool _isLoading;
        [ShowInInspector, ReadOnly] private string _currentStageId;

        [Inject] private IObjectResolver _container;

        private StageInstance _current;
        private readonly Subject<StageInstance> _onStageLoaded = new();
        private readonly Subject<StageInstance> _onStageUnloading = new();

        /// <summary>新ステージのロード完了時に発火（permanent シーンの UI が購読）。</summary>
        public Observable<StageInstance> OnStageLoaded => _onStageLoaded;

        /// <summary>現ステージを破棄する直前に発火（subscriber が後片付けする機会）。</summary>
        public Observable<StageInstance> OnStageUnloading => _onStageUnloading;

        public StageInstance Current => _current;
        public bool IsLoading => _isLoading;

        private void Start()
        {
            /* レガシー bootstrap：既にシーン内に MissionManager が存在する場合（Stage1.unity 等）、
               プレハブロードを経ずに「初期ステージとしてのみ存在する状態」を構築して subscriber へ通知する。
               これにより新規 UI コンポーネントが OnStageLoaded を購読しても、レガシーシーンで動作する。 */
            if (_current == null && _initialFallbackMission != null)
            {
                GameObject root = _initialFallbackMission.gameObject;
                _current = new StageInstance(_initialFallbackNode, root, _initialFallbackMission);
                _currentStageId = _initialFallbackNode != null ? _initialFallbackNode.StageId : "(legacy)";
                _onStageLoaded.OnNext(_current);
            }
        }

        private void OnDestroy()
        {
            _onStageLoaded.Dispose();
            _onStageUnloading.Dispose();
        }

        /// <summary>
        /// 指定 StageNode をロードする。フェード演出と DI 解決を全て内部で行う。
        /// </summary>
        public async UniTask LoadStageAsync(StageNode node, CancellationToken ct = default)
        {
            if (node == null)
            {
                Debug.LogWarning("[StageLoader] node が null");
                return;
            }
            if (node.StagePrefab == null)
            {
                Debug.LogWarning($"[StageLoader] {node.StageId} に StagePrefab 未設定（Phase 11-A 移行前？）");
                return;
            }
            if (_isLoading)
            {
                Debug.LogWarning("[StageLoader] すでにロード中");
                return;
            }
            if (_stageContainer == null)
            {
                Debug.LogError("[StageLoader] StageContainer 未設定");
                return;
            }

            _isLoading = true;
            try
            {
                /* 1. フェードアウト */
                SceneTransitioner transitioner = SceneTransitioner.Instance;
                if (transitioner != null) await transitioner.FadeOutAsync(ct);
                ct.ThrowIfCancellationRequested();

                /* 2. 旧ステージ Destroy */
                UnloadInternal();

                /* 3. 新プレハブ Instantiate */
                GameObject root = Instantiate(node.StagePrefab, _stageContainer);
                root.name = node.StagePrefab.name;

                /* 4. プレハブ内の [Inject] フィールドを VContainer で解決 */
                if (_container != null)
                    _container.InjectGameObject(root);

                /* 5. MissionManager を取得して StageInstance 構築 */
                MissionManager mission = root.GetComponentInChildren<MissionManager>(includeInactive: true);
                _current = new StageInstance(node, root, mission);
                _currentStageId = node.StageId;
                _onStageLoaded.OnNext(_current);

                /* 6. 保険：StageInitializer 不在のときは MissionManager.OnStageReady() を直接呼ぶ */
                if (mission != null && root.GetComponentInChildren<StageInitializer>(true) == null)
                    mission.OnStageReady();

                ct.ThrowIfCancellationRequested();

                /* 7. フェードイン */
                if (transitioner != null) await transitioner.FadeInAsync(ct);
            }
            catch (OperationCanceledException)
            {
                /* 上位でハンドリングする想定 */
            }
            finally
            {
                _isLoading = false;
            }
        }

        /// <summary>現ステージを Destroy する（フェード無し、即時）。デバッグ・テスト用。</summary>
        public void UnloadCurrent() => UnloadInternal();

        private void UnloadInternal()
        {
            if (_current == null) return;
            _onStageUnloading.OnNext(_current);
            if (_current.Root != null) Destroy(_current.Root);
            _current = null;
            _currentStageId = null;
        }
    }
}
