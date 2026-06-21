/// The Hollow Rite - メインメニュー用プレイヤーコントローラ

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/PlayerController.h"
#include "THRMainMenuPlayerController.generated.h"

/**
 * メインメニューの入力を処理するコントローラ。
 * Enter でボスアリーナへ遷移、Q でゲーム終了。
 */
UCLASS()
class ATHRMainMenuPlayerController final : public APlayerController
{
	GENERATED_BODY()

protected:
	virtual void SetupInputComponent() override;

	/// 遷移先のボスアリーナレベル
	UPROPERTY(EditAnywhere, Category="THR|Menu")
	FName BattleLevelName = TEXT("/Game/THR/Maps/Lvl_BossArena");

private:
	void HandleStart();
	void HandleQuit();
};
