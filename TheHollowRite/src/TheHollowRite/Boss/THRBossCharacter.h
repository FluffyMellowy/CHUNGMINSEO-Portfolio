/// The Hollow Rite - ボスキャラクター基底クラス

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Character.h"
#include "THRBossAttackDef.h"
#include "THRBossCharacter.generated.h"

class UTHRHealthComponent;
class UTHRMeleeHitboxComponent;
class UBehaviorTree;
class UAnimSequence;

/// ボスの戦闘フェーズ
UENUM(BlueprintType)
enum class ETHRBossPhase : uint8
{
	Phase1,
	Phase2,
	Phase3,
	Dead
};

/**
 * SFボスアクションのボス基底クラス。
 * Behavior Tree（THRBossAIController 経由）で駆動され、
 * AttackTable のパターンから距離・クールダウン・重みに基づいて攻撃を行う。
 * フェーズは体力割合（66% / 33%）で遷移する。
 */
UCLASS()
class ATHRBossCharacter final : public ACharacter
{
	GENERATED_BODY()

public:
	ATHRBossCharacter();

	/// 現在の戦闘フェーズ
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="THR|Boss")
	ETHRBossPhase CurrentPhase = ETHRBossPhase::Phase1;

	/// ボスを駆動する Behavior Tree（配置インスタンスの Details で割り当て）
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category="THR|Boss|AI")
	TObjectPtr<UBehaviorTree> BossBehaviorTree;

	/// 攻撃パターンテーブル（行の追加・数値調整はエディタで行う）
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category="THR|Boss|Attack")
	TArray<FTHRBossAttackDef> AttackTable;

	/// 指定攻撃がクールダウン中かを返す（フェーズ別クールダウン倍率を考慮）
	UFUNCTION(BlueprintPure, Category="THR|Boss|Attack")
	bool IsAttackOnCooldown(const FTHRBossAttackDef& Def) const;

	/// 攻撃モンタージュを再生し、再生時間（秒）を返す。失敗時は 0 以下。
	/// LungeImpulse が設定されていればターゲット方向へ突進する。
	float PlayAttack(const FTHRBossAttackDef& Def);

	/// 現在のフェーズを 1〜3 の整数で返す（AttackTable の MinPhase 判定用）
	UFUNCTION(BlueprintPure, Category="THR|Boss")
	int32 GetPhaseAsInt() const;

	/// デバッグ：指定フェーズの体力閾値までHPを下げ、実際のフェーズ遷移を発動する（F3/F4）
	void DebugSetPhase(int32 PhaseNum);

protected:
	virtual void BeginPlay() override;

	/// 体力コンポーネント
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="THR|Components")
	TObjectPtr<UTHRHealthComponent> HealthComponent;

	/// 近接攻撃ヒットボックス（攻撃モンタージュのアニメ通知から駆動）
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="THR|Components")
	TObjectPtr<UTHRMeleeHitboxComponent> MeleeHitbox;

	/// ボスの最大体力（3フェーズ想定の合計）
	UPROPERTY(EditAnywhere, Category="THR|Boss")
	float BossMaxHealth = 900.0f;

	/// 接近時の移動速度（cm/秒）
	UPROPERTY(EditAnywhere, Category="THR|Boss|Movement")
	float MoveSpeed = 350.0f;

	/// フェーズ2の移動速度倍率
	UPROPERTY(EditAnywhere, Category="THR|Boss|Phase", meta=(ClampMin="1.0"))
	float Phase2SpeedMult = 1.15f;

	/// フェーズ3の移動速度倍率
	UPROPERTY(EditAnywhere, Category="THR|Boss|Phase", meta=(ClampMin="1.0"))
	float Phase3SpeedMult = 1.3f;

	/// フェーズ2のクールダウン倍率（小さいほど攻撃頻度が上がる）
	UPROPERTY(EditAnywhere, Category="THR|Boss|Phase", meta=(ClampMin="0.1", ClampMax="1.0"))
	float Phase2CooldownMult = 0.85f;

	/// フェーズ3のクールダウン倍率
	UPROPERTY(EditAnywhere, Category="THR|Boss|Phase", meta=(ClampMin="0.1", ClampMax="1.0"))
	float Phase3CooldownMult = 0.7f;

	/// 死亡時に再生するアニメーション
	UPROPERTY(EditAnywhere, Category="THR|Boss")
	TObjectPtr<UAnimSequence> DeathAnim;

	/// 死亡時のコールバック
	UFUNCTION()
	void HandleDeath();

	/// 体力変化時のコールバック（フェーズ遷移を判定）
	UFUNCTION()
	void HandleHealthChanged(float NewHealth, float MaxHealth);

private:
	/* 範囲攻撃を1件スポーンする（アンカー解決を含む） */
	void SpawnAOE(const FTHRAOESpawnDef& SpawnDef);

	/* 弾幕パターンを1件発射する（プール経由） */
	void FireBulletPattern(const FTHRBulletPatternDef& Pattern);

	/* 攻撃ごとの最終使用時刻（クールダウン管理） */
	TMap<FName, float> LastAttackTimes;
};
