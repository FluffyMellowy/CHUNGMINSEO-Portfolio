/// The Hollow Rite - メインメニューHUD

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/HUD.h"
#include "THRMainMenuHUD.generated.h"

/**
 * メインメニューの簡易描画HUD。
 * タイトルと操作ガイドを Canvas で中央に描画する。
 * グレイボックス段階の実装。最終的には UMG メニューへ置き換え予定。
 */
UCLASS()
class ATHRMainMenuHUD final : public AHUD
{
	GENERATED_BODY()

public:
	virtual void DrawHUD() override;

protected:
	/// ゲームタイトル表示文字列
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="THR|UI")
	FText GameTitle = FText::FromString(TEXT("THE HOLLOW RITE"));
};
