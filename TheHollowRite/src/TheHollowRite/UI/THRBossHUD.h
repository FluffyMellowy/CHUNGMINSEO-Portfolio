/// The Hollow Rite - ボス戦HUD（ボス名・体力バーを画面上部に描画）

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/HUD.h"
#include "THRBossHUD.generated.h"

class ATHRBossCharacter;
class UTHRHealthComponent;

/**
 * ボス戦用の簡易HUD。
 * 画面上部中央にボス名と体力バーを Canvas で直接描画する。
 * M1段階のグレイボックス実装。最終的には UMG への置き換えも検討。
 */
UCLASS()
class ATHRBossHUD final : public AHUD
{
	GENERATED_BODY()

public:
	virtual void DrawHUD() override;

protected:
	/// ボスの表示名
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="THR|UI")
	FText BossName = FText::FromString(TEXT("RIKTOR"));

	/// 体力バーの幅（画面幅に対する割合）
	UPROPERTY(EditAnywhere, Category="THR|UI", meta=(ClampMin="0.1", ClampMax="1.0"))
	float BarWidthRatio = 0.5f;

	/// 体力バーの高さ（ピクセル）
	UPROPERTY(EditAnywhere, Category="THR|UI")
	float BarHeight = 18.0f;

	/// 画面上端からボス名上端までのオフセット（ピクセル）
	UPROPERTY(EditAnywhere, Category="THR|UI")
	float TopOffset = 36.0f;

	/// 体力バー満タン時の色
	UPROPERTY(EditAnywhere, Category="THR|UI")
	FLinearColor FillColor = FLinearColor(0.9f, 0.12f, 0.16f, 1.0f);

private:
	/* ボスの体力コンポーネントを取得（初回にキャッシュ） */
	UTHRHealthComponent* ResolveBossHealth();

	/* レベル内の唯一のボスをキャッシュ */
	TWeakObjectPtr<ATHRBossCharacter> CachedBoss;
};
