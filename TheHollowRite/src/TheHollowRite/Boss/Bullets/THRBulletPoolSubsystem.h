/// The Hollow Rite - 弾幕オブジェクトプール

#pragma once

#include "CoreMinimal.h"
#include "Subsystems/WorldSubsystem.h"
#include "THRBulletTypes.h"
#include "THRBulletPoolSubsystem.generated.h"

class ATHRBullet;

/**
 * 弾幕の弾を再利用するオブジェクトプール（ワールドサブシステム）。
 * 弾は破棄せずプールへ戻すことで、数百発の弾幕でも生成/GC コストを発生させない。
 * パターン定義（FTHRBulletPatternDef）を受け取り、波（wave）に分けて発射する。
 */
UCLASS()
class UTHRBulletPoolSubsystem final : public UWorldSubsystem
{
	GENERATED_BODY()

public:
	/// パターンを発射する。Origin から、Aimed の場合は Target 方向を基準に散布。
	void FirePattern(const FTHRBulletPatternDef& Pattern, const FVector& Origin, AActor* Target);

private:
	/* 空き弾を取得（無ければ新規スポーン） */
	ATHRBullet* AcquireBullet();

	/* 1波分を発射する */
	void FireWave(FTHRBulletPatternDef Pattern, FVector Origin, TWeakObjectPtr<AActor> Target, int32 WaveIndex);

	/* プール内の全弾 */
	UPROPERTY()
	TArray<TObjectPtr<ATHRBullet>> Pool;

	/* 同時生成上限（暴走防止） */
	static constexpr int32 MaxBullets = 600;
};
