/// The Hollow Rite - 範囲攻撃テレグラフアクター

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "THRAOETypes.h"
#include "THRAOEActor.generated.h"

class UDecalComponent;
class UMaterialInterface;
class UMaterialInstanceDynamic;

/**
 * 範囲攻撃のテレグラフ表示と発動判定を行うアクター。
 *
 * ライフサイクル：
 *   スポーン → TelegraphTime の間テレグラフが満ちる（デカール Progress 0→1）
 *   → 発動：その瞬間の対象位置で inside/outside を純粋な2D幾何で判定（スナップショット方式）
 *   → 命中対象（UTHRHealthComponent 保持ポーン）へダメージ → 短い余韻の後消滅
 *
 * テレグラフマテリアル未設定時はデバッグ描画でフォールバック表示する（グレイボックス段階）。
 * 回避の無敵フレーム中は HealthComponent 側でダメージが無効化される。
 */
UCLASS()
class ATHRAOEActor final : public AActor
{
	GENERATED_BODY()

public:
	ATHRAOEActor();

	/// スポーン直後に呼び、定義と発生源を設定する
	void InitAOE(const FTHRAOEDef& InDef, AActor* InInstigatorActor);

	virtual void Tick(float DeltaSeconds) override;

	/// 指定ワールド座標が判定範囲内かを返す（2D、アクターのローカル空間で評価）
	UFUNCTION(BlueprintPure, Category="THR|AOE")
	bool IsPointInside(const FVector& WorldPoint) const;

protected:
	virtual void BeginPlay() override;

	/// テレグラフ用デカール
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="THR|Components")
	TObjectPtr<UDecalComponent> TelegraphDecal;

	/// テレグラフマテリアル（未設定ならデバッグ描画でフォールバック）
	UPROPERTY(EditAnywhere, Category="THR|AOE")
	TObjectPtr<UMaterialInterface> TelegraphMaterial;

private:
	/* テレグラフデカールの初期化（Def 設定後に InitAOE から呼ぶ） */
	void SetupTelegraphDecal();

	/* 発動：範囲内の対象へダメージを与え、消滅を予約する */
	void Detonate();

	/* 危険度に応じたテレグラフ色 */
	FLinearColor GetSeverityColor() const;

	/* デバッグ／フォールバック用のテレグラフ形状描画 */
	void DrawTelegraphShape(float Progress, const FLinearColor& Color) const;

	/* 実行時状態 */
	FTHRAOEDef Def;
	TWeakObjectPtr<AActor> InstigatorActor;
	UPROPERTY()
	TObjectPtr<UMaterialInstanceDynamic> DecalMID;
	float Elapsed = 0.0f;
	bool bDetonated = false;
};
